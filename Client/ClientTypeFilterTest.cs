using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.ClientFilterIds;
using static E2ETest.Constants.TestClientData;

namespace E2ETest.Client;

[TestFixture]
public class ClientTypeFilterTest : PlaywrightSetup
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
    public async Task Step1_FilterByClientTypeEmployee()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Filter by Client Type 'Employee' ===");

        TestContext.Out.WriteLine("Resetting to All Groups first");
        await ResetGroupFilterToAllGroups();

        await SelectGroupByPath(GroupDeutschweizMitte, GroupBE, GroupBern);
        await Actions.Wait500();

        // Act
        TestContext.Out.WriteLine("Opening client type dropdown and selecting Employee only");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait500();

        await Actions.ClickCheckBoxById(FilterTypeExternEmpId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterTypeCustomerId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseTypeDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during client type filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(3),
            $"Should find exactly 3 Employee clients (Heribert, Marie-Anne, Tommaso Gasparoli). Found: {rowCount}");

        TestContext.Out.WriteLine($"=== Employee filter test completed successfully. Found {rowCount} matching clients ===");
    }

    [Test]
    [Order(2)]
    public async Task Step2_FilterByClientTypeExternalEmployee()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Filter by Client Type 'External Employee' ===");
        TestContext.Out.WriteLine("Note: Group filter 'Bern' is already set from Step 1");

        // Act
        TestContext.Out.WriteLine("Opening client type dropdown and selecting ExternEmp only");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait500();

        await Actions.ClickCheckBoxById(FilterTypeEmployeeId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterTypeExternEmpId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseTypeDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during client type filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 External Employee client (Pierre-Alain Frey). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNamePierreAlain), $"First name should be '{FirstNamePierreAlain}'");
        Assert.That(lastName, Is.EqualTo(LastNameFrey), $"Last name should be '{LastNameFrey}'");

        TestContext.Out.WriteLine($"=== External Employee filter test completed successfully. Found {rowCount} matching client ===");
    }

    [Test]
    [Order(3)]
    public async Task Step3_FilterByClientTypeCustomer()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Filter by Client Type 'Customer' ===");
        TestContext.Out.WriteLine("Note: Group filter 'Bern' is already set from Step 1");

        // Act
        TestContext.Out.WriteLine("Opening client type dropdown and selecting Customer only");
        await Actions.ClickButtonById(DropdownTypeId);
        await Actions.Wait500();

        await Actions.ClickCheckBoxById(FilterTypeExternEmpId);
        await Actions.Wait500();
        await Actions.ClickCheckBoxById(FilterTypeCustomerId);
        await Actions.Wait500();

        TestContext.Out.WriteLine("Clicking dropdown toggle again to close and trigger filter");
        await Actions.ClickButtonById(CloseTypeDropdownId);

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rowCount = await Actions.CountElementsBySelector(ClientRowSelector);

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during client type filter. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(rowCount, Is.EqualTo(1),
            $"Should find exactly 1 Customer client (Urs Ammann). Found: {rowCount}");

        var firstName = await Actions.GetTextContentById($"{ClientFirstNamePrefix}0");
        var lastName = await Actions.GetTextContentById($"{ClientLastNamePrefix}0");
        TestContext.Out.WriteLine($"Found client: {firstName}, {lastName}");

        Assert.That(firstName, Is.EqualTo(FirstNameUrs), $"First name should be '{FirstNameUrs}'");
        Assert.That(lastName, Is.EqualTo(LastNameAmmann), $"Last name should be '{LastNameAmmann}'");

        TestContext.Out.WriteLine($"=== Customer filter test completed successfully. Found {rowCount} matching client ===");

        TestContext.Out.WriteLine("Resetting all filters using reset button");
        await ResetGroupFilterToAllGroups();
        await Actions.ClickButtonById(ResetAddressButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }
}
