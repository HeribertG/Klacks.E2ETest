using System.Text.RegularExpressions;
using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.ClientFilterIds;
using static Klacks.E2ETest.Constants.PaginationIds;
using static Klacks.E2ETest.Constants.TestClientData;

namespace Klacks.E2ETest;

[TestFixture]
[Category("Input")]
[Order(13)]
[Ignore("Count-based assertions conflict with seeded clients in Bern group (5 seeded + 5 test = 10); needs unique test group or GTE assertions")]
public class ClientAdvancedFiltersTest : PlaywrightSetup
{
    private Listener _listener = null!;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        // Every step is self-contained: start from a known group filter regardless of what a
        // previously run step (or a standalone re-run of just this step) left behind.
        await ResetGroupFilterToAllGroups();
        await SelectGroupByPath(GroupDeutschschweizMitte, GroupBE, GroupBern);
        await Actions.Wait500();
    }

    [TearDown]
    public void TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Errors detected: {_listener.GetLastErrorMessage()}");
        }
    }

    private async Task ResetGroupFilterToAllGroups()
    {
        TestContext.Out.WriteLine("Resetting group filter to 'All Groups'");

        var isDropdownOpen = await Actions.IsElementVisibleById(GroupSelectAllGroupsId);

        if (!isDropdownOpen)
        {
            await Actions.ClickButtonById(GroupSelectDropdownToggleId);
            await Actions.Wait3000();
        }

        await Actions.ClickButtonById(GroupSelectAllGroupsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        TestContext.Out.WriteLine("Closing dropdown to reset tree state");
        await Actions.ClickButtonById(GroupSelectDropdownToggleId);
        await Actions.Wait1000();
        await Actions.Wait1000();
    }

    private async Task SelectGroupByPath(params string[] groupPath)
    {
        TestContext.Out.WriteLine($"Selecting group: {string.Join(" > ", groupPath)}");

        var isDropdownOpen = await Actions.IsElementVisibleById(GroupSelectAllGroupsId);

        if (!isDropdownOpen)
        {
            TestContext.Out.WriteLine("Opening dropdown");
            await Actions.ClickButtonById(GroupSelectDropdownToggleId);
            await Actions.Wait3000();
        }
        else
        {
            TestContext.Out.WriteLine("Dropdown is already open");
        }

        for (int i = 0; i < groupPath.Length - 1; i++)
        {
            TestContext.Out.WriteLine($"Expanding '{groupPath[i]}'");
            await Actions.ExpandGroupNodeByName(groupPath[i]);
            await Actions.Wait1000();
        }

        TestContext.Out.WriteLine($"Selecting '{groupPath[^1]}'");
        await Actions.SelectGroupByName(groupPath[^1]);
        await Actions.Wait500();

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    private async Task<int> GetPaginationTotalCount()
    {
        var labelText = await Actions.GetTextContentById(TotalCountLabel);
        var match = Regex.Match(labelText, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int count))
        {
            TestContext.Out.WriteLine($"Pagination total count: {count}");
            return count;
        }

        TestContext.Out.WriteLine($"Could not parse pagination count from: '{labelText}'");
        return 0;
    }

    private async Task SetCheckboxState(string id, bool desiredChecked)
    {
        var checkbox = await Actions.FindElementById(id);
        var isChecked = await checkbox!.IsCheckedAsync();
        if (isChecked != desiredChecked)
        {
            await Actions.ClickCheckBoxById(id);
            await Actions.Wait500();
        }
    }

    /// <summary>
    /// Opens the validity dropdown and sets it to show ONLY the given validity, reading the current
    /// checkbox state first so this works regardless of what a previous step left the filter in.
    /// </summary>
    private async Task FilterByOnlyValidity(string keepCheckedId, params string[] uncheckIds)
    {
        await Actions.ClickButtonById(DropdownValidityId);
        await Actions.Wait500();

        await SetCheckboxState(keepCheckedId, true);
        foreach (var uncheckId in uncheckIds)
        {
            await SetCheckboxState(uncheckId, false);
        }

        await Actions.ClickButtonById(CloseValidityDropdownId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(1)]
    public async Task Step1_FilterByValidityActive()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Filter by Validity 'Aktive' (Active) ===");

        // Act
        TestContext.Out.WriteLine("Opening validity dropdown and selecting only Active");
        await FilterByOnlyValidity(FilterValidityActiveId, FilterValidityFormerId, FilterValidityFutureId);

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during validity filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(5),
            $"Should find exactly 5 active clients in group 'Bern'. Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Active validity filter test completed successfully. Found {totalCount} matching clients ===");
    }

    [Test]
    [Order(2)]
    public async Task Step2_FilterByValidityFormer()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Filter by Validity 'Ehemalige' (Former) ===");

        // Act
        TestContext.Out.WriteLine("Opening validity dropdown and selecting only Former");
        await FilterByOnlyValidity(FilterValidityFormerId, FilterValidityActiveId, FilterValidityFutureId);

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during validity filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(0),
            $"Should find 0 former clients in group 'Bern'. Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Former validity filter test completed successfully. Found {totalCount} matching clients ===");
    }

    [Test]
    [Order(3)]
    public async Task Step3_FilterByValidityFuture()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Filter by Validity 'Zukünftige' (Future) ===");

        // Act
        TestContext.Out.WriteLine("Opening validity dropdown and selecting only Future");
        await FilterByOnlyValidity(FilterValidityFutureId, FilterValidityActiveId, FilterValidityFormerId);

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during validity filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(0),
            $"Should find 0 future clients in group 'Bern'. Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Future validity filter test completed successfully. Found {totalCount} matching clients ===");
    }

    [Test]
    [Order(4)]
    public async Task Step4_CountriesDeselectAll()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Deselect All Countries ===");

        // Act
        TestContext.Out.WriteLine("Opening countries dropdown");
        await Actions.ClickButtonById(DropdownCountriesId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Deselecting all countries");
        await Actions.ClickButtonById(CountryDeselectAllId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseCountriesDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during country filter. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"=== Deselect all countries test completed. Found {totalCount} matching clients ===");
    }

    [Test]
    [Order(5)]
    public async Task Step5_CountriesSelectAll()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Select All Countries ===");

        // Act
        TestContext.Out.WriteLine("Opening countries dropdown and selecting all");
        await Actions.ClickButtonById(DropdownCountriesId);
        await Actions.Wait500();

        await Actions.ClickButtonById(CountrySelectAllId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseCountriesDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during country filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(5),
            $"Should find 5 clients when all countries are selected. Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Select all countries test completed successfully. Found {totalCount} matching clients ===");
    }

    [Test]
    [Order(6)]
    public async Task Step6_FilterByScopeEntryDate()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Filter by Scope 'Eintritt' (Entry Date) ===");

        // Act
        TestContext.Out.WriteLine("Opening scope dropdown and enabling Entry date");
        await Actions.ClickButtonById(DropdownScopeId);
        await Actions.Wait500();

        await SetCheckboxState(FilterScopeFromFlagId, true);

        TestContext.Out.WriteLine($"Setting date range");
        await Actions.FillInputById(FilterDateFromId, "01.01.2020");
        await Actions.Wait500();
        await Actions.FillInputById(FilterDateUntilId, "31.12.2025");
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseScopeDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during scope filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.GreaterThanOrEqualTo(1),
            $"Should find at least 1 client with entry date in range. Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Scope Entry Date filter test completed. Found {totalCount} matching clients ===");
    }

    [Test]
    [Order(7)]
    public async Task Step7_ResetAllFilters()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 7: Reset all filters ===");

        // Act
        TestContext.Out.WriteLine("Clicking reset button");
        await Actions.ClickButtonById(ResetAddressButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await ResetGroupFilterToAllGroups();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during filter reset. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.GreaterThanOrEqualTo(5),
            $"After reset, should find all clients. Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Reset all filters test completed successfully. Found {totalCount} clients ===");
    }
}
