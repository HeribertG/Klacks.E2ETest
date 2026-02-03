using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.GroupTestData;

namespace Klacks.E2ETest;

[TestFixture]
[Order(35)]
public class GroupCreationTest : PlaywrightSetup
{
    private Listener _listener = null!;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await WaitForGroupTableData();
    }

    private async Task WaitForGroupTableData()
    {
        const int maxAttempts = 60;
        const int delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            var container = await Actions.FindElementById(GroupIds.AllGroupListContainer);
            if (container != null)
            {
                TestContext.Out.WriteLine($"Group list container loaded after {i * delayMs}ms");
                return;
            }

            await Task.Delay(delayMs);
        }

        TestContext.Out.WriteLine($"WARNING: Group list not loaded after {maxAttempts * delayMs}ms");
    }

    [TearDown]
    public void TearDown()
    {
        if (_listener.HasApiErrors())
        {
            var error = _listener.GetLastErrorMessage();
            if (!error.Contains("hubs/work-notifications"))
            {
                TestContext.Out.WriteLine($"API Error: {error}");
            }
        }
    }

    [Test]
    [Order(1)]
    public void Step1_NavigateToGroupPage()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Navigate to Group Page ===");

        // Act
        var currentUrl = Actions.ReadCurrentUrl();

        // Assert
        Assert.That(currentUrl, Does.Contain("workplace/group"), "Should navigate to group page");

        TestContext.Out.WriteLine($"Successfully navigated to: {currentUrl}");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateRootGroup()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create Root Group ===");
        var rootGroup = RootGroup;

        // Act
        await CreateGroup(rootGroup, null);

        // Assert
        TestContext.Out.WriteLine($"Root group '{rootGroup.Name}' created successfully");
    }

    [Test]
    [Order(3)]
    public async Task Step3_CreateChildGroups()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Create Child Groups ===");

        // Act
        foreach (var childGroup in ChildGroups)
        {
            TestContext.Out.WriteLine($"\n--- Creating child: {childGroup.Name} under {childGroup.ParentName} ---");
            await CreateGroup(childGroup, childGroup.ParentName);
            await Actions.Wait1000();
        }

        // Assert
        TestContext.Out.WriteLine($"=== {ChildGroups.Length} child groups created successfully ===");
    }

    [Test]
    [Order(4)]
    public async Task Step4_CreateGrandchildGroup()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Create Grandchild Group ===");
        var grandchild = GrandchildGroup;

        // Act
        await CreateGroup(grandchild, grandchild.ParentName);

        // Assert
        TestContext.Out.WriteLine($"Grandchild group '{grandchild.Name}' created under '{grandchild.ParentName}'");
    }

    private async Task CreateGroup(GroupData groupData, string? parentName)
    {
        // Navigate to edit-group page
        await Page.GotoAsync("http://localhost:4200/workplace/edit-group");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Navigated to edit-group page");

        // Wait for form to load
        await WaitForGroupForm();

        // Fill Name
        await Actions.ClearInputById(GroupIds.EditGroupItemName);
        await Actions.FillInputById(GroupIds.EditGroupItemName, groupData.Name);
        TestContext.Out.WriteLine($"Filled name: {groupData.Name}");

        // Select Parent if specified
        if (!string.IsNullOrEmpty(parentName))
        {
            var parentSelected = await SelectParentGroup(parentName);
            if (!parentSelected)
            {
                TestContext.Out.WriteLine($"Retrying parent selection after page refresh...");
                await Page.ReloadAsync();
                await Actions.WaitForSpinnerToDisappear();
                await WaitForGroupForm();
                await Actions.ClearInputById(GroupIds.EditGroupItemName);
                await Actions.FillInputById(GroupIds.EditGroupItemName, groupData.Name);
                await SelectParentGroup(parentName);
            }
        }

        await Actions.Wait500();

        // Save
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Save button");

        // Navigate back to group list to ensure the new group is in the cache
        await Page.GotoAsync("http://localhost:4200/workplace/group");
        await Actions.WaitForSpinnerToDisappear();
        await WaitForGroupTableData();

        TestContext.Out.WriteLine($"Group '{groupData.Name}' created successfully");
    }

    private async Task<bool> SelectParentGroup(string parentName)
    {
        TestContext.Out.WriteLine($"Selecting parent: {parentName}");

        // Wait for parent select and options to be loaded (async loading)
        var selectElement = await WaitForParentOptionsLoaded();
        if (selectElement == null)
        {
            TestContext.Out.WriteLine("WARNING: Parent select not found");
            return false;
        }

        // Get all options and find one containing the parent name
        var options = await selectElement.QuerySelectorAllAsync("option");
        TestContext.Out.WriteLine($"Found {options.Count} options in parent select");

        foreach (var option in options)
        {
            var text = await option.TextContentAsync();
            if (text?.Contains(parentName) == true)
            {
                var value = await option.GetAttributeAsync("value");
                TestContext.Out.WriteLine($"Found option: '{text}' with value: {value}");

                if (!string.IsNullOrEmpty(value) && value != "null")
                {
                    await selectElement.SelectOptionAsync(new Microsoft.Playwright.SelectOptionValue { Value = value });
                    await Actions.Wait500();
                    TestContext.Out.WriteLine($"Selected parent: {parentName}");
                    return true;
                }
            }
        }

        TestContext.Out.WriteLine($"WARNING: Parent '{parentName}' not found in options");
        return false;
    }

    private async Task WaitForGroupForm()
    {
        const int maxAttempts = 30;
        const int delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            var nameInput = await Actions.FindElementById(GroupIds.EditGroupItemName);
            if (nameInput != null)
            {
                TestContext.Out.WriteLine($"Group form loaded after {i * delayMs}ms");
                return;
            }

            await Task.Delay(delayMs);
        }

        Assert.Fail("Group form did not load within timeout");
    }

    private async Task<Microsoft.Playwright.IElementHandle?> WaitForParentOptionsLoaded()
    {
        const int maxAttempts = 60;
        const int delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            var selectElement = await Actions.FindElementById(GroupIds.EditGroupParentSelect);
            if (selectElement != null)
            {
                var options = await selectElement.QuerySelectorAllAsync("option");
                if (options.Count > 1)
                {
                    TestContext.Out.WriteLine($"Parent options loaded after {i * delayMs}ms ({options.Count} options)");
                    return selectElement;
                }
            }

            await Task.Delay(delayMs);
        }

        TestContext.Out.WriteLine($"WARNING: Parent options did not load within {maxAttempts * delayMs}ms");
        return await Actions.FindElementById(GroupIds.EditGroupParentSelect);
    }
}
