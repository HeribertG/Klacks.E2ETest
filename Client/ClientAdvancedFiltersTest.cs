using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.ClientFilterIds;
using static E2ETest.Constants.TestClientData;

namespace E2ETest.Client;

[TestFixture]
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

    [Test]
    [Order(1)]
    public async Task Step1_FilterByValidityActive()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Filter by Validity 'Aktive' (Active) ===");

        TestContext.Out.WriteLine("Resetting to All Groups first");
        await ResetGroupFilterToAllGroups();

        await SelectGroupByPath(GroupDeutschweizMitte, GroupBE, GroupBern);
        await Actions.Wait500();

        // Act
        TestContext.Out.WriteLine("Opening validity dropdown and selecting only Active");
        await Actions.ClickButtonById(DropdownValidityId);
        await Actions.Wait500();

        await Actions.ClickCheckBoxById(FilterValidityFormerId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterValidityFutureId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseValidityDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during validity filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(5),
            $"Should find exactly 5 active clients in group 'Bern'. Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Active validity filter test completed successfully. Found {rowCount} matching clients ===");
    }

    [Test]
    [Order(2)]
    public async Task Step2_FilterByValidityFormer()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Filter by Validity 'Ehemalige' (Former) ===");
        TestContext.Out.WriteLine("Note: Group filter 'Bern' is already set from Step 1");

        // Act
        TestContext.Out.WriteLine("Opening validity dropdown and selecting only Former");
        await Actions.ClickButtonById(DropdownValidityId);
        await Actions.Wait500();

        await Actions.ClickCheckBoxById(FilterValidityActiveId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterValidityFormerId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseValidityDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during validity filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(0),
            $"Should find 0 former clients in group 'Bern'. Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Former validity filter test completed successfully. Found {rowCount} matching clients ===");
    }

    [Test]
    [Order(3)]
    public async Task Step3_FilterByValidityFuture()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Filter by Validity 'Zukünftige' (Future) ===");
        TestContext.Out.WriteLine("Note: Group filter 'Bern' is already set from Step 1");

        // Act
        TestContext.Out.WriteLine("Opening validity dropdown and selecting only Future");
        await Actions.ClickButtonById(DropdownValidityId);
        await Actions.Wait500();

        await Actions.ClickCheckBoxById(FilterValidityFormerId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterValidityFutureId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseValidityDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during validity filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(0),
            $"Should find 0 future clients in group 'Bern'. Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Future validity filter test completed successfully. Found {rowCount} matching clients ===");

        TestContext.Out.WriteLine("Resetting validity to Active for next tests");
        await Actions.ClickButtonById(DropdownValidityId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterValidityActiveId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterValidityFutureId);
        await Actions.Wait500();
        await Actions.ClickButtonById(CloseValidityDropdownId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(4)]
    public async Task Step4_CountriesDeselectAll()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Deselect All Countries ===");
        TestContext.Out.WriteLine("Note: Group filter 'Bern' is already set from Step 1");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during country filter. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"=== Deselect all countries test completed. Found {rowCount} matching clients ===");
    }

    [Test]
    [Order(5)]
    public async Task Step5_CountriesSelectAll()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Select All Countries ===");
        TestContext.Out.WriteLine("Note: Group filter 'Bern' is already set from Step 1");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during country filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(5),
            $"Should find 5 clients when all countries are selected. Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Select all countries test completed successfully. Found {rowCount} matching clients ===");
    }

    [Test]
    [Order(6)]
    public async Task Step6_FilterByScopeEntryDate()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Filter by Scope 'Eintritt' (Entry Date) ===");
        TestContext.Out.WriteLine("Note: Group filter 'Bern' is already set from Step 1");

        // Act
        TestContext.Out.WriteLine("Opening scope dropdown and enabling Entry date");
        await Actions.ClickButtonById(DropdownScopeId);
        await Actions.Wait500();

        await Actions.ClickCheckBoxById(FilterScopeFromFlagId);
        await Actions.Wait500();

        TestContext.Out.WriteLine($"Setting date range");
        await Actions.FillInputById(FilterDateFromId, "01.01.2020");
        await Actions.Wait500();
        await Actions.FillInputById(FilterDateUntilId, "31.12.2025");
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseScopeDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during scope filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.GreaterThanOrEqualTo(1),
            $"Should find at least 1 client with entry date in range. Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Scope Entry Date filter test completed. Found {rowCount} matching clients ===");
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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during filter reset. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.GreaterThanOrEqualTo(5),
            $"After reset, should find all clients. Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Reset all filters test completed successfully. Found {rowCount} clients ===");
    }
}
