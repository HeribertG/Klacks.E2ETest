using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Microsoft.Playwright;
using static E2ETest.Constants.ClientFilterIds;
using static E2ETest.Constants.TestClientData;

namespace E2ETest.Client;

[TestFixture]
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
        await Actions.Wait1000();
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
        await Actions.ClickButtonById(GroupSelectDropdownToggleId);
        await Actions.Wait1000();
        await Actions.ClickButtonById(GroupSelectAllGroupsId);
        await Actions.Wait500();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    private async Task SelectGroupByPath(params string[] groupPath)
    {
        TestContext.Out.WriteLine($"Selecting group: {string.Join(" > ", groupPath)}");
        await Actions.ClickButtonById(GroupSelectDropdownToggleId);
        await Actions.Wait1000();

        for (int i = 0; i < groupPath.Length - 1; i++)
        {
            TestContext.Out.WriteLine($"Expanding '{groupPath[i]}'");
            await Actions.ExpandGroupNodeByName(groupPath[i]);
            await Actions.Wait500();
        }

        TestContext.Out.WriteLine($"Selecting '{groupPath[^1]}'");
        await Actions.SelectGroupByName(groupPath[^1]);
        await Actions.Wait500();

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
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
        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(3),
            $"Should find exactly 3 clients with 'gasp' in their name (Heribert Gasparoli, Marie-Anne Gasparoli, Tommaso Gasparoli). Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {rowCount} matching clients ===");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 client with 'heri' in their name (Heribert Gasparoli). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameHeribert), $"First name should be '{FirstNameHeribert}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {rowCount} matching client ===");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 client with 'marie-anne' in their name (Marie-Anne Gasparoli). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameMarieAnne), $"First name should be '{FirstNameMarieAnne}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {rowCount} matching client ===");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 client with 'tommaso gasp' in their name (Tommaso Gasparoli). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameTommaso), $"First name should be '{FirstNameTommaso}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {rowCount} matching client ===");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(2),
            $"Should find exactly 2 clients with address 'Kirchstrasse 52' (Heribert and Marie-Anne Gasparoli). Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Search test completed successfully. Found {rowCount} matching clients ===");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during group filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(5),
            $"Should find exactly 5 clients in group 'Bern' (all created clients). Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Group filter test completed successfully. Found {rowCount} matching clients ===");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(2),
            $"Should find exactly 2 male clients with 'gasp' in their name (Heribert and Tommaso Gasparoli). Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Search with Male filter completed successfully. Found {rowCount} matching clients ===");

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

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during search. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 female client with 'gasp' in their name (Marie-Anne Gasparoli). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameMarieAnne), $"First name should be '{FirstNameMarieAnne}'");
        Assert.That(lastName, Is.EqualTo(LastNameGasparoli), $"Last name should be '{LastNameGasparoli}'");

        TestContext.Out.WriteLine($"=== Search with Female filter completed successfully. Found {rowCount} matching client ===");

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

    [Test]
    [Order(9)]
    public async Task Step9_FilterByClientTypeEmployee()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 9: Filter by Client Type 'Employee' ===");

        await SelectGroupByPath(GroupDeutschweizMitte, GroupBE, GroupBern);

        // Act
        TestContext.Out.WriteLine("Opening client type dropdown and selecting Employee only");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait1000();

        // Uncheck ExternEmp and Customer, keep Employee checked
        await Actions.ClickCheckBoxById(FilterTypeExternEmpId);
        await Actions.Wait1000();
        await Actions.ClickCheckBoxById(FilterTypeCustomerId);
        await Actions.Wait1000();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait1000();
        await Actions.Wait1000();

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during client type filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(3),
            $"Should find exactly 3 Employee clients (Heribert, Marie-Anne, Tommaso Gasparoli). Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Employee filter test completed successfully. Found {rowCount} matching clients ===");

        // Reset all filters using reset button
        TestContext.Out.WriteLine("Resetting all filters using reset button");
        await ResetGroupFilterToAllGroups();
        await Actions.ClickButtonById(ResetAddressButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(10)]
    public async Task Step10_FilterByClientTypeExternalEmployee()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 10: Filter by Client Type 'External Employee' ===");

        await SelectGroupByPath(GroupDeutschweizMitte, GroupBE, GroupBern);
       
        // Act
        TestContext.Out.WriteLine("Opening client type dropdown and selecting ExternEmp only");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait1000();

        // Uncheck Employee and Customer, keep ExternEmp checked
        await Actions.ClickCheckBoxById(FilterTypeEmployeeId);
        await Actions.Wait1000();
        await Actions.ClickCheckBoxById(FilterTypeCustomerId);
        await Actions.Wait1000();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait1000();
        await Actions.Wait1000();

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during client type filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 External Employee client (Urs Ammann). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameUrs), $"First name should be '{FirstNameUrs}'");
        Assert.That(lastName, Is.EqualTo(LastNameAmmann), $"Last name should be '{LastNameAmmann}'");

        TestContext.Out.WriteLine($"=== External Employee filter test completed successfully. Found {rowCount} matching client ===");

        // Reset all filters using reset button
        TestContext.Out.WriteLine("Resetting all filters using reset button");
        await ResetGroupFilterToAllGroups();
        await Actions.ClickButtonById(ResetAddressButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(11)]
    public async Task Step11_FilterByClientTypeCustomer()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 11: Filter by Client Type 'Customer' ===");

        await SelectGroupByPath(GroupDeutschweizMitte, GroupBE, GroupBern);
       
        // Act
        TestContext.Out.WriteLine("Opening client type dropdown and selecting Customer only");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait1000();

        // Uncheck Employee and ExternEmp, keep Customer checked
        await Actions.ClickCheckBoxById(FilterTypeEmployeeId);
        await Actions.Wait1000();
        await Actions.ClickCheckBoxById(FilterTypeExternEmpId);
        await Actions.Wait1000();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait1000();
        await Actions.Wait1000();

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during client type filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 Customer client (Pierre-Alain Frey). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNamePierreAlain), $"First name should be '{FirstNamePierreAlain}'");
        Assert.That(lastName, Is.EqualTo(LastNameFrey), $"Last name should be '{LastNameFrey}'");

        TestContext.Out.WriteLine($"=== Customer filter test completed successfully. Found {rowCount} matching client ===");

        // Reset all filters using reset button
        TestContext.Out.WriteLine("Resetting all filters using reset button");
        await ResetGroupFilterToAllGroups();
        await Actions.ClickButtonById(ResetAddressButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }
}
