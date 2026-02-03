using System.Text.RegularExpressions;
using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.ClientFilterIds;
using static Klacks.E2ETest.Constants.PaginationIds;
using static Klacks.E2ETest.Constants.TestClientData;

namespace Klacks.E2ETest;

[TestFixture]
[Order(11)]
public class ClientSearchTest : PlaywrightSetup
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
    }

    private async Task SelectGroupByPath(params string[] groupPath)
    {
        TestContext.Out.WriteLine($"Selecting group: {string.Join(" > ", groupPath)}");
        await Actions.ClickButtonById(GroupSelectDropdownToggleId);
        await Actions.Wait3000();

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

    [Test]
    [Order(1)]
    public async Task Step1_SearchForClients()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 1: Search for Clients with '{SearchTermGasp}' ===");
        var searchTerm = SearchTermGasp;

        // Act
        TestContext.Out.WriteLine($"Filling search input with: {searchTerm}");
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking search button");
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        TestContext.Out.WriteLine("Counting visible client rows");
        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(3),
            $"Should find exactly 3 clients with 'gasp' in their name (Heribert Gasparoli, Marie-Anne Gasparoli, Tommaso Gasparoli). Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {totalCount} matching clients ===");

        // Reset search
        TestContext.Out.WriteLine("Resetting search to initial state");
        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(2)]
    public async Task Step2_SearchForHeri()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 2: Search for '{SearchTermHeri}' ===");
        var searchTerm = SearchTermHeri;

        // Act
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(1),
            $"Should find exactly 1 client with 'heri' in their name (Heribert Gasparoli). Found: {totalCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameHeribert), $"First name should be '{FirstNameHeribert}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {totalCount} matching client ===");

        // Reset search
        TestContext.Out.WriteLine("Resetting search to initial state");
        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(3)]
    public async Task Step3_SearchForMarieAnne()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 3: Search for '{SearchTermMarieAnne}' ===");
        var searchTerm = SearchTermMarieAnne;

        // Act
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(1),
            $"Should find exactly 1 client with 'marie-anne' in their name (Marie-Anne Gasparoli). Found: {totalCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameMarieAnne), $"First name should be '{FirstNameMarieAnne}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {totalCount} matching client ===");

        // Reset search
        TestContext.Out.WriteLine("Resetting search to initial state");
        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(4)]
    public async Task Step4_SearchForTommasoGasp()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 4: Search for '{SearchTermTommasoGasp}' ===");
        var searchTerm = SearchTermTommasoGasp;

        // Act
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(1),
            $"Should find exactly 1 client with 'tommaso gasp' in their name (Tommaso Gasparoli). Found: {totalCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameTommaso), $"First name should be '{FirstNameTommaso}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {totalCount} matching client ===");

        // Reset search
        TestContext.Out.WriteLine("Resetting search to initial state");
        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(5)]
    public async Task Step5_SearchForAddressKirchstrasse52()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 5: Search for address '{SearchTermAddress}' with includeAddress ===");
        var searchTerm = SearchTermAddress;

        // Act
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Enabling includeAddress checkbox");
        await Actions.ClickCheckBoxById(SearchIncludeAddressId);
        await Actions.Wait500();

        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(2),
            $"Should find exactly 2 clients with address 'Kirchstrasse 52' (Heribert and Marie-Anne Gasparoli). Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {totalCount} matching clients ===");

        // Reset search
        TestContext.Out.WriteLine("Resetting search to initial state");
        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickCheckBoxById(SearchIncludeAddressId);
        await Actions.Wait500();
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(6)]
    public async Task Step6_FilterByGroupBern()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 6: Filter by Group '{GroupDeutschweizMitte} > {GroupBE} > {GroupBern}' ===");

        // Act
        await SelectGroupByPath(GroupDeutschweizMitte, GroupBE, GroupBern);

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during group filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(5),
            $"Should find exactly 5 clients in group 'Bern' (all created clients). Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Group filter test completed successfully. Found {totalCount} matching clients ===");

        // Reset to all groups
        await ResetGroupFilterToAllGroups();
    }

    [Test]
    [Order(7)]
    public async Task Step7_SearchGaspWithMaleFilter()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 7: Search '{SearchTermGasp}' with only Male filter active ===");
        var searchTerm = SearchTermGasp;

        // Act
        TestContext.Out.WriteLine("Setting gender filters: only Male selected (unchecking female, intersexuality, legal-entity)");
        await Actions.ClickCheckBoxById(FilterFemaleId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterIntersexualityId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterLegalEntityId);
        await Actions.Wait500();

        TestContext.Out.WriteLine($"Filling search input with: {searchTerm}");
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();

        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(2),
            $"Should find exactly 2 male clients with 'gasp' in their name (Heribert and Tommaso Gasparoli). Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Search with Male filter completed successfully. Found {totalCount} matching clients ===");

        // Reset search and filters
        TestContext.Out.WriteLine("Resetting search and filters to initial state");
        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickCheckBoxById(FilterFemaleId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterIntersexualityId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterLegalEntityId);
        await Actions.Wait500();
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(8)]
    public async Task Step8_SearchGaspWithFemaleFilter()
    {
        // Arrange
        TestContext.Out.WriteLine($"=== Step 8: Search '{SearchTermGasp}' with only Female filter active ===");
        var searchTerm = SearchTermGasp;

        // Act
        TestContext.Out.WriteLine("Setting gender filters: only Female selected (unchecking male, intersexuality, legal-entity)");
        await Actions.ClickCheckBoxById(FilterMaleId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterIntersexualityId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterLegalEntityId);
        await Actions.Wait500();

        TestContext.Out.WriteLine($"Filling search input with: {searchTerm}");
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();

        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.EqualTo(1),
            $"Should find exactly 1 female client with 'gasp' in their name (Marie-Anne Gasparoli). Found: {totalCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameMarieAnne), $"First name should be '{FirstNameMarieAnne}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search with Female filter completed successfully. Found {totalCount} matching client ===");

        // Reset search and filters
        TestContext.Out.WriteLine("Resetting search and filters to initial state");
        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickCheckBoxById(FilterMaleId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterIntersexualityId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterLegalEntityId);
        await Actions.Wait500();
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }
}
