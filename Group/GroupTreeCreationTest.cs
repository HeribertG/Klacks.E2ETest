using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Npgsql;

namespace E2ETest.Group;

[TestFixture]
[Order(18)]
public class GroupTreeCreationTest : PlaywrightSetup
{
    private Listener _listener = null!;

    private const string TreeRootName = "E2E-Tree-Root";
    private const string TreeChild1Name = "E2E-Tree-Child-1";
    private const string TreeChild2Name = "E2E-Tree-Child-2";
    private const string TreeGrandchildName = "E2E-Tree-Grandchild";
    private const string TreeChild1CopyName = "E2E-Tree-Child-1-copy";

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_listener.HasApiErrors())
        {
            var error = _listener.GetLastErrorMessage();
            if (!error.Contains("hubs/work-notifications"))
            {
                TestContext.Out.WriteLine($"API Error: {error}");
            }
        }

        await CleanupTestGroups();
    }

    private async Task CleanupTestGroups()
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var groupNames = new[] { TreeGrandchildName, TreeChild1CopyName, TreeChild1Name, TreeChild2Name, TreeRootName };

        foreach (var name in groupNames)
        {
            await using var cmd = new NpgsqlCommand(
                @"DELETE FROM ""group"" WHERE name = @name", connection);
            cmd.Parameters.AddWithValue("name", name);
            var deleted = await cmd.ExecuteNonQueryAsync();
            if (deleted > 0)
            {
                TestContext.Out.WriteLine($"Cleanup: Deleted '{name}'");
            }
        }
    }

    [Test]
    [Order(1)]
    public async Task Step1_SwitchToTreeView()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Switch to Tree View ===");

        // Act
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Assert
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupContainer);
        Assert.That(treeContainer, Is.Not.Null, "Tree view should be visible");

        TestContext.Out.WriteLine("Switched to Tree View");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateRootGroupViaTreeView()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create Root Group via Tree View ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Act - Click "Neue Stammgruppe" button
        var addRootButton = await Actions.FindElementById(GroupIds.TreeGroupAddRootButton);
        Assert.That(addRootButton, Is.Not.Null, "Add root button should exist");

        await addRootButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked 'Neue Stammgruppe' button");

        // Fill the form
        await WaitForGroupForm();
        await Actions.ClearInputById(GroupIds.EditGroupItemName);
        await Actions.FillInputById(GroupIds.EditGroupItemName, TreeRootName);
        TestContext.Out.WriteLine($"Filled name: {TreeRootName}");

        // Save
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Assert - Navigate back to tree and verify
        await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        var treeText = await treeContainer!.TextContentAsync() ?? "";

        Assert.That(treeText.Contains(TreeRootName), Is.True,
            $"Root group '{TreeRootName}' should be visible in tree");

        TestContext.Out.WriteLine($"Root group '{TreeRootName}' created successfully via Tree View");
    }

    [Test]
    [Order(3)]
    public async Task Step3_CreateChildGroupViaAddButton()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Create Child Group via Add Button in Tree Node ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Find the root group node and click Add button
        var addButtonClicked = await ClickAddButtonForGroup(TreeRootName);
        Assert.That(addButtonClicked, Is.True, $"Should find and click Add button for '{TreeRootName}'");

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Fill the child group form
        await WaitForGroupForm();
        await Actions.ClearInputById(GroupIds.EditGroupItemName);
        await Actions.FillInputById(GroupIds.EditGroupItemName, TreeChild1Name);
        TestContext.Out.WriteLine($"Filled child name: {TreeChild1Name}");

        // Verify parent is pre-selected
        var parentSelect = await Actions.FindElementById(GroupIds.EditGroupParentSelect);
        if (parentSelect != null)
        {
            var selectedText = await parentSelect.EvaluateAsync<string>("el => el.options[el.selectedIndex]?.text || ''");
            TestContext.Out.WriteLine($"Pre-selected parent: {selectedText}");
        }

        // Save
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Assert - Navigate back and verify hierarchy
        await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        var treeText = await treeContainer!.TextContentAsync() ?? "";

        Assert.That(treeText.Contains(TreeChild1Name), Is.True,
            $"Child group '{TreeChild1Name}' should be visible in tree");

        TestContext.Out.WriteLine($"Child group '{TreeChild1Name}' created via Add button");
    }

    [Test]
    [Order(4)]
    public async Task Step4_CreateSecondChildAndGrandchild()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Create Second Child and Grandchild ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        // Create second child under root
        await ClickAddButtonForGroup(TreeRootName);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await WaitForGroupForm();
        await Actions.ClearInputById(GroupIds.EditGroupItemName);
        await Actions.FillInputById(GroupIds.EditGroupItemName, TreeChild2Name);
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine($"Created second child: {TreeChild2Name}");

        // Navigate back and create grandchild
        await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all
        expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        // Create grandchild under Child-1
        await ClickAddButtonForGroup(TreeChild1Name);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await WaitForGroupForm();
        await Actions.ClearInputById(GroupIds.EditGroupItemName);
        await Actions.FillInputById(GroupIds.EditGroupItemName, TreeGrandchildName);
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine($"Created grandchild: {TreeGrandchildName}");

        // Assert - Verify complete hierarchy
        await VerifyHierarchyInDatabase();
    }

    [Test]
    [Order(5)]
    public async Task Step5_CopyGroupViaCopyButton()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Copy Group via Copy Button ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        // Act - Copy Child-1
        var copyButtonClicked = await ClickCopyButtonForGroup(TreeChild1Name);
        Assert.That(copyButtonClicked, Is.True, $"Should find and click Copy button for '{TreeChild1Name}'");

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Verify the copy form is loaded with "-copy" suffix
        await WaitForGroupForm();
        var nameInput = await Actions.FindElementById(GroupIds.EditGroupItemName);
        Assert.That(nameInput, Is.Not.Null, "Name input should exist");

        var nameValue = await nameInput!.InputValueAsync();
        TestContext.Out.WriteLine($"Copy form name: {nameValue}");
        Assert.That(nameValue, Does.Contain("-copy"), "Copy should have '-copy' suffix");

        // Save the copy
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Assert - Navigate back and verify copy exists
        await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all
        expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        var treeText = await treeContainer!.TextContentAsync() ?? "";

        Assert.That(treeText.Contains(TreeChild1CopyName), Is.True,
            $"Copied group '{TreeChild1CopyName}' should be visible in tree");

        TestContext.Out.WriteLine($"Group '{TreeChild1Name}' copied successfully as '{TreeChild1CopyName}'");
    }

    [Test]
    [Order(6)]
    public async Task Step6_VerifyHierarchyInTreeView()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Verify Hierarchy in Tree View ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        // Act
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        var treeText = await treeContainer!.TextContentAsync() ?? "";

        // Assert
        Assert.That(treeText.Contains(TreeRootName), Is.True, "Root should be visible");
        Assert.That(treeText.Contains(TreeChild1Name), Is.True, "Child-1 should be visible");
        Assert.That(treeText.Contains(TreeChild2Name), Is.True, "Child-2 should be visible");
        Assert.That(treeText.Contains(TreeGrandchildName), Is.True, "Grandchild should be visible");
        Assert.That(treeText.Contains(TreeChild1CopyName), Is.True, "Child-1-copy should be visible");

        TestContext.Out.WriteLine("All groups visible in Tree View:");
        TestContext.Out.WriteLine($"  - {TreeRootName}");
        TestContext.Out.WriteLine($"    - {TreeChild1Name}");
        TestContext.Out.WriteLine($"      - {TreeGrandchildName}");
        TestContext.Out.WriteLine($"    - {TreeChild2Name}");
        TestContext.Out.WriteLine($"    - {TreeChild1CopyName}");
    }

    [Test]
    [Order(7)]
    public async Task Step7_DeleteGroupsViaTreeView()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 7: Delete Groups via Tree View (children first) ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        // Act - Delete in reverse order (children first to avoid FK constraints)
        // 1. Delete Grandchild
        var deleted = await ClickDeleteButtonForGroup(TreeGrandchildName);
        Assert.That(deleted, Is.True, $"Should delete '{TreeGrandchildName}'");
        await Actions.Wait1000();

        // Refresh and expand
        await RefreshTreeView();

        // 2. Delete Child-1 Copy
        deleted = await ClickDeleteButtonForGroup(TreeChild1CopyName);
        Assert.That(deleted, Is.True, $"Should delete '{TreeChild1CopyName}'");
        await Actions.Wait1000();

        await RefreshTreeView();

        // 3. Delete Child-1
        deleted = await ClickDeleteButtonForGroup(TreeChild1Name);
        Assert.That(deleted, Is.True, $"Should delete '{TreeChild1Name}'");
        await Actions.Wait1000();

        await RefreshTreeView();

        // 4. Delete Child-2
        deleted = await ClickDeleteButtonForGroup(TreeChild2Name);
        Assert.That(deleted, Is.True, $"Should delete '{TreeChild2Name}'");
        await Actions.Wait1000();

        await RefreshTreeView();

        // 5. Delete Root
        deleted = await ClickDeleteButtonForGroup(TreeRootName);
        Assert.That(deleted, Is.True, $"Should delete '{TreeRootName}'");
        await Actions.Wait1000();

        TestContext.Out.WriteLine("All test groups deleted via Tree View");
    }

    [Test]
    [Order(8)]
    public async Task Step8_VerifyAllGroupsDeletedFromDatabase()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 8: Verify All Groups Deleted from Database ===");

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var groupNames = new[] { TreeRootName, TreeChild1Name, TreeChild2Name, TreeGrandchildName, TreeChild1CopyName };
        var remainingGroups = new List<string>();

        foreach (var name in groupNames)
        {
            await using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM ""group"" WHERE name = @name AND is_deleted = false",
                connection);
            cmd.Parameters.AddWithValue("name", name);

            var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
            if (count > 0)
            {
                remainingGroups.Add(name);
                TestContext.Out.WriteLine($"WARNING: '{name}' still exists in database");
            }
            else
            {
                TestContext.Out.WriteLine($"Confirmed deleted: '{name}'");
            }
        }

        // Assert
        Assert.That(remainingGroups, Is.Empty,
            $"All test groups should be deleted. Remaining: {string.Join(", ", remainingGroups)}");

        TestContext.Out.WriteLine("=== All test groups successfully deleted ===");
    }

    private async Task<bool> ClickDeleteButtonForGroup(string groupName)
    {
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        if (treeContainer == null)
        {
            return false;
        }

        var treeNodes = await treeContainer.QuerySelectorAllAsync("[id^='tree-node-item-']");

        foreach (var node in treeNodes)
        {
            var textContent = await node.TextContentAsync();
            if (textContent?.Contains(groupName) == true)
            {
                var nodeId = await node.GetAttributeAsync("id");
                if (nodeId == null)
                {
                    continue;
                }

                var groupId = nodeId.Replace("tree-node-item-", "");
                var deleteButtonId = GroupIds.GetTreeNodeDeleteBtnId(groupId);
                var deleteBtn = await Actions.FindElementById(deleteButtonId);

                if (deleteBtn != null)
                {
                    await deleteBtn.ClickAsync();
                    await Actions.Wait500();
                    TestContext.Out.WriteLine($"Clicked Delete button for '{groupName}' (ID: {deleteButtonId})");

                    await Actions.ClickElementById(ModalIds.DeleteConfirm);
                    await Actions.WaitForSpinnerToDisappear();
                    await Actions.Wait500();

                    TestContext.Out.WriteLine($"Confirmed deletion of '{groupName}'");
                    return true;
                }
            }
        }

        TestContext.Out.WriteLine($"WARNING: Could not find Delete button for '{groupName}'");
        return false;
    }

    private async Task RefreshTreeView()
    {
        var refreshButton = await Actions.FindElementById(GroupIds.TreeGroupRefreshButton);
        if (refreshButton != null)
        {
            await refreshButton.ClickAsync();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
        }

        // Expand all after refresh
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait500();
        }
    }

    private async Task<bool> ClickAddButtonForGroup(string groupName)
    {
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        if (treeContainer == null)
        {
            return false;
        }

        var treeNodes = await treeContainer.QuerySelectorAllAsync("[id^='tree-node-item-']");

        foreach (var node in treeNodes)
        {
            var textContent = await node.TextContentAsync();
            if (textContent?.Contains(groupName) == true)
            {
                var nodeId = await node.GetAttributeAsync("id");
                if (nodeId == null)
                {
                    continue;
                }

                var groupId = nodeId.Replace("tree-node-item-", "");
                var addButtonId = GroupIds.GetTreeNodeAddBtnId(groupId);
                var addBtn = await Actions.FindElementById(addButtonId);

                if (addBtn != null)
                {
                    await addBtn.ClickAsync();
                    TestContext.Out.WriteLine($"Clicked Add button for '{groupName}' (ID: {addButtonId})");
                    return true;
                }
            }
        }

        TestContext.Out.WriteLine($"WARNING: Could not find Add button for '{groupName}'");
        return false;
    }

    private async Task<bool> ClickCopyButtonForGroup(string groupName)
    {
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        if (treeContainer == null)
        {
            return false;
        }

        var treeNodes = await treeContainer.QuerySelectorAllAsync("[id^='tree-node-item-']");

        foreach (var node in treeNodes)
        {
            var textContent = await node.TextContentAsync();
            if (textContent?.Contains(groupName) == true)
            {
                var nodeId = await node.GetAttributeAsync("id");
                if (nodeId == null)
                {
                    continue;
                }

                var groupId = nodeId.Replace("tree-node-item-", "");
                var copyButtonId = GroupIds.GetTreeNodeCopyBtnId(groupId);
                var copyBtn = await Actions.FindElementById(copyButtonId);

                if (copyBtn != null)
                {
                    await copyBtn.ClickAsync();
                    TestContext.Out.WriteLine($"Clicked Copy button for '{groupName}' (ID: {copyButtonId})");
                    return true;
                }
            }
        }

        TestContext.Out.WriteLine($"WARNING: Could not find Copy button for '{groupName}'");
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

    private async Task VerifyHierarchyInDatabase()
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Verify Child-1 has Root as parent
        await VerifyParentChild(connection, TreeChild1Name, TreeRootName);

        // Verify Child-2 has Root as parent
        await VerifyParentChild(connection, TreeChild2Name, TreeRootName);

        // Verify Grandchild has Child-1 as parent
        await VerifyParentChild(connection, TreeGrandchildName, TreeChild1Name);

        TestContext.Out.WriteLine("Database hierarchy verified successfully");
    }

    private async Task VerifyParentChild(NpgsqlConnection connection, string childName, string expectedParent)
    {
        await using var cmd = new NpgsqlCommand(@"
            SELECT p.name
            FROM ""group"" c
            JOIN ""group"" p ON c.parent = p.id
            WHERE c.name = @childName AND c.is_deleted = false", connection);
        cmd.Parameters.AddWithValue("childName", childName);

        var actualParent = await cmd.ExecuteScalarAsync() as string;

        Assert.That(actualParent, Is.EqualTo(expectedParent),
            $"'{childName}' should have parent '{expectedParent}' but has '{actualParent}'");

        TestContext.Out.WriteLine($"Verified: {childName} -> {actualParent}");
    }
}
