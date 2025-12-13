namespace E2ETest.Constants;

public static class GroupIds
{
    public static readonly string AddGroupButton = "add-group-button";
    public static readonly string DropdownToggle = "group-select-dropdown-toggle";

    public static string GetGroupToggleId(string groupId) => $"group-toggle-{groupId}";

    public static string GetGroupOptionId(string groupId) => $"group-option-{groupId}";

    public static string GetGroupNodeId(string groupId) => $"group-node-{groupId}";
}
