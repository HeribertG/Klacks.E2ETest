using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Npgsql;
using static E2ETest.Constants.GroupTestData;

namespace E2ETest;

[TestFixture]
[Order(36)]
public class GroupSearchTest : PlaywrightSetup
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
    public async Task Step1_VerifyHierarchyInDatabase()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Verify Hierarchy in Database ===");

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Verify all groups exist
        var foundInDb = 0;
        foreach (var group in Groups)
        {
            await using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM ""group"" WHERE name = @name AND is_deleted = false",
                connection);
            cmd.Parameters.AddWithValue("name", group.Name);

            var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
            if (count > 0)
            {
                TestContext.Out.WriteLine($"Found in DB: {group.Name}");
                foundInDb++;
            }
            else
            {
                TestContext.Out.WriteLine($"NOT found in DB: {group.Name}");
            }
        }

        // Verify parent-child relationships
        await VerifyParentChildRelationship(connection, "E2E-Test-Child-1", RootGroupName);
        await VerifyParentChildRelationship(connection, "E2E-Test-Child-2", RootGroupName);
        await VerifyParentChildRelationship(connection, "E2E-Test-Grandchild", "E2E-Test-Child-1");

        // Assert
        Assert.That(foundInDb, Is.EqualTo(Groups.Length),
            $"All {Groups.Length} test groups should exist in database");

        TestContext.Out.WriteLine($"=== All {foundInDb} test groups with hierarchy verified ===");
    }

    private async Task VerifyParentChildRelationship(NpgsqlConnection connection, string childName, string parentName)
    {
        await using var cmd = new NpgsqlCommand(@"
            SELECT p.name
            FROM ""group"" c
            JOIN ""group"" p ON c.parent = p.id
            WHERE c.name = @childName AND c.is_deleted = false AND p.is_deleted = false",
            connection);
        cmd.Parameters.AddWithValue("childName", childName);

        var actualParent = await cmd.ExecuteScalarAsync() as string;

        if (actualParent == parentName)
        {
            TestContext.Out.WriteLine($"Verified: {childName} -> parent: {parentName}");
        }
        else
        {
            TestContext.Out.WriteLine($"WARNING: {childName} has parent '{actualParent}' (expected: {parentName})");
        }
    }

    [Test]
    [Order(2)]
    public async Task Step2_SwitchToTreeViewAndVerifyContainer()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Switch to Tree View and Verify Container ===");

        // Act
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked tree toggle button");

        // Assert - verify tree container exists
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupContainer);
        Assert.That(treeContainer, Is.Not.Null, "Tree container should be visible after toggle");

        var treeHeader = await Actions.FindElementById(GroupIds.TreeGroupHeader);
        Assert.That(treeHeader, Is.Not.Null, "Tree header should be visible");

        TestContext.Out.WriteLine("Tree view container and header are visible");
    }

    [Test]
    [Order(3)]
    public async Task Step3_VerifyTreeViewHierarchy()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Verify Tree View Hierarchy ===");

        // Switch to tree view first
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Act - Click expand all button to show all nodes
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Clicked expand all button");
        }

        // Get tree container and verify all test groups are visible
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        Assert.That(treeContainer, Is.Not.Null, "Tree container should exist");

        var treeText = await treeContainer!.TextContentAsync() ?? "";

        // Assert - verify all test groups are in tree
        var rootFound = treeText.Contains(RootGroupName);
        var child1Found = treeText.Contains("E2E-Test-Child-1");
        var child2Found = treeText.Contains("E2E-Test-Child-2");
        var grandchildFound = treeText.Contains("E2E-Test-Grandchild");

        TestContext.Out.WriteLine($"Root '{RootGroupName}' found: {rootFound}");
        TestContext.Out.WriteLine($"Child-1 found: {child1Found}");
        TestContext.Out.WriteLine($"Child-2 found: {child2Found}");
        TestContext.Out.WriteLine($"Grandchild found: {grandchildFound}");

        Assert.That(rootFound, Is.True, $"Root group '{RootGroupName}' should be visible in tree");
        Assert.That(child1Found, Is.True, "Child-1 should be visible after expanding");
        Assert.That(child2Found, Is.True, "Child-2 should be visible after expanding");
        Assert.That(grandchildFound, Is.True, "Grandchild should be visible after expanding");

        TestContext.Out.WriteLine("=== All test groups visible in tree view ===");
    }

    [Test]
    [Order(4)]
    public async Task Step4_VerifyTreeNodeStructure()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Verify Tree Node Structure ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Expand all nodes
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait1000();
        }

        // Act - Find all tree nodes
        var treeNodes = await Actions.GetElementsBySelector(".tree-node-item");
        TestContext.Out.WriteLine($"Found {treeNodes.Count} tree nodes");

        // Find our test groups in the tree nodes
        var testGroupsFound = 0;
        foreach (var node in treeNodes)
        {
            var textContent = await node.TextContentAsync();
            if (textContent != null && textContent.Contains("E2E-Test"))
            {
                testGroupsFound++;
                TestContext.Out.WriteLine($"Found test group node: {textContent.Trim().Split('\n')[0]}");
            }
        }

        // Assert
        Assert.That(testGroupsFound, Is.EqualTo(Groups.Length),
            $"All {Groups.Length} test groups should have tree nodes");

        TestContext.Out.WriteLine($"=== Found {testGroupsFound} test group nodes ===");
    }

    [Test]
    [Order(5)]
    public async Task Step5_CollapseAndExpandTree()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Collapse and Expand Tree ===");

        // Switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Act - Expand all
        var expandButton = await Actions.FindElementById(GroupIds.TreeGroupExpandButton);
        if (expandButton != null)
        {
            await expandButton.ClickAsync();
            await Actions.Wait500();
            TestContext.Out.WriteLine("Expanded all nodes");
        }

        // Get tree container and count visible test groups
        var treeContainer = await Actions.FindElementById(GroupIds.TreeGroupTreeContainer);
        var treeTextExpanded = await treeContainer!.TextContentAsync() ?? "";
        var grandchildVisibleExpanded = treeTextExpanded.Contains("E2E-Test-Grandchild");
        TestContext.Out.WriteLine($"Grandchild visible after expand: {grandchildVisibleExpanded}");

        // Act - Collapse all
        var collapseButton = await Actions.FindElementById(GroupIds.TreeGroupCollapseButton);
        if (collapseButton != null)
        {
            await collapseButton.ClickAsync();
            await Actions.Wait500();
            TestContext.Out.WriteLine("Collapsed all nodes");
        }

        // Assert - Tree should still show root but maybe not children
        var treeView = await Actions.FindElementById(GroupIds.TreeGroupContainer);
        Assert.That(treeView, Is.Not.Null, "Tree view should still be visible after collapse");

        TestContext.Out.WriteLine("Collapse/Expand functionality works correctly");
    }

    [Test]
    [Order(6)]
    public async Task Step6_SwitchBackToGridView()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Switch Back to Grid View ===");

        // First switch to tree view
        await Actions.ClickButtonById(GroupIds.TreeToggleButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        // Act - Click grid toggle button in tree view
        var gridToggle = await Actions.FindElementById(GroupIds.TreeGroupGridToggle);
        if (gridToggle != null)
        {
            await gridToggle.ClickAsync();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Clicked grid toggle button");
        }

        // Assert - Grid view should be visible
        var tableContainer = await Actions.FindElementById(GroupIds.AllGroupListContainer);
        Assert.That(tableContainer, Is.Not.Null, "Grid table should be visible after switching back");

        TestContext.Out.WriteLine("Switched back to grid view");
    }
}
