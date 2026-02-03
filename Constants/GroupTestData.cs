namespace Klacks.E2ETest.Constants;

public static class GroupTestData
{
    public record GroupData(string Name, string Description, string? ParentName = null);

    public static readonly string RootGroupName = "E2E-Test-Root";

    public static readonly GroupData[] Groups =
    [
        new(RootGroupName, "Root-Testgruppe für E2E Tests"),
        new("E2E-Test-Child-1", "Erste Untergruppe", RootGroupName),
        new("E2E-Test-Child-2", "Zweite Untergruppe", RootGroupName),
        new("E2E-Test-Grandchild", "Enkel-Gruppe unter Child-1", "E2E-Test-Child-1")
    ];

    public static readonly GroupData RootGroup = Groups[0];
    public static readonly GroupData[] ChildGroups = [Groups[1], Groups[2]];
    public static readonly GroupData GrandchildGroup = Groups[3];

    public const string GroupRowSelector = "[id^='all-group-list-row-']";
    public const string TreeNodeSelector = "[id^='tree-group-node-']";
}
