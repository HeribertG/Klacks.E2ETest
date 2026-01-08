using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Npgsql;
using static E2ETest.Group.GroupTestData;

namespace E2ETest.Group;

[TestFixture]
[Order(17)]
public class GroupDeletionTest : PlaywrightSetup
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
            var tableRows = await Actions.GetElementsBySelector(GroupRowSelector);
            if (tableRows.Count > 0)
            {
                TestContext.Out.WriteLine($"Group table loaded after {i * delayMs}ms ({tableRows.Count} rows)");
                return;
            }

            await Task.Delay(delayMs);
        }

        TestContext.Out.WriteLine($"WARNING: Group table not loaded after {maxAttempts * delayMs}ms");
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
    public async Task Step1_DeleteTestGroupsViaTreeView()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Delete Test Groups via Tree View ===");

        // Switch to tree view for hierarchical deletion
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all to see all groups
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        var deletedCount = 0;

        // Delete in reverse order (children first, then parents)
        var groupsToDelete = Groups.Reverse().ToArray();

        foreach (var group in groupsToDelete)
        {
            TestContext.Out.WriteLine($"\n--- Attempting to delete: {group.Name} ---");

            var deleted = await TryDeleteGroupFromTree(group.Name);
            if (deleted)
            {
                deletedCount++;
                await Actions.Wait500();
            }
            else
            {
                TestContext.Out.WriteLine($"Group '{group.Name}' not found in tree");
            }
        }

        // Assert
        TestContext.Out.WriteLine($"\n=== Deleted {deletedCount} of {Groups.Length} groups via UI ===");
    }

    private async Task<bool> TryDeleteGroupFromTree(string groupName)
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

                    await Actions.ClickElementById(ModalIds.DeleteConfirm);
                    await Actions.WaitForSpinnerToDisappear();
                    await Actions.Wait1000();

                    TestContext.Out.WriteLine($"Deleted: {groupName} (ID: {deleteButtonId})");
                    return true;
                }
            }
        }

        return false;
    }

    [Test]
    [Order(2)]
    public async Task Step2_PhysicallyDeleteTestGroupsFromDatabase()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Physically Delete Test Groups from Database ===");

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

        var deletedCount = 0;

        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        TestContext.Out.WriteLine("Connected to database");

        // Delete in reverse order to handle foreign key constraints (children first)
        var groupsToDelete = Groups.Reverse().ToArray();

        foreach (var group in groupsToDelete)
        {
            TestContext.Out.WriteLine($"Physically deleting: {group.Name}");

            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM ""group""
                WHERE name = @name", connection);

            cmd.Parameters.AddWithValue("name", group.Name);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            {
                deletedCount += rowsAffected;
                TestContext.Out.WriteLine($"  -> Deleted {rowsAffected} row(s)");
            }
            else
            {
                TestContext.Out.WriteLine($"  -> No rows found");
            }
        }

        // Assert
        TestContext.Out.WriteLine($"\n=== Physically deleted {deletedCount} group(s) from database ===");
    }

    [Test]
    [Order(3)]
    public async Task Step3_VerifyAllTestGroupsDeleted()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Verify All Test Groups Deleted ===");

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var remainingGroups = 0;
        foreach (var group in Groups)
        {
            await using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM ""group"" WHERE name = @name",
                connection);
            cmd.Parameters.AddWithValue("name", group.Name);

            var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
            if (count > 0)
            {
                TestContext.Out.WriteLine($"WARNING: {group.Name} still exists in database");
                remainingGroups++;
            }
        }

        // Assert
        Assert.That(remainingGroups, Is.EqualTo(0),
            "All test groups should be deleted from database");

        TestContext.Out.WriteLine("=== All test groups successfully cleaned up ===");
    }
}
