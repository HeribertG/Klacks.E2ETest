namespace Klacks.E2ETest.Constants;

public static class GroupIds
{
    // Client Group Section (adding groups to a client)
    public const string AddGroupButton = "add-group-button";
    public const string DropdownTogglePrefix = "group-select-dropdown-toggle-";
    public const string AllGroupsPrefix = "group-select-all-groups-";

    public static string GetDropdownToggleId(int index) => $"{DropdownTogglePrefix}{index}";

    public static string GetAllGroupsId(int index) => $"{AllGroupsPrefix}{index}";

    // Navigation
    public const string AllGroupHomeContainer = "all-group-home-container";
    public const string AllGroupListContainer = "all-group-list-table-container";

    // List Actions
    public const string NewGroupButton = "all-group-list-new-button";
    public const string TreeToggleButton = "all-group-list-tree-toggle";
    public const string HeaderCheckbox = "all-group-list-header-checkbox-input";

    // Table Headers
    public const string HeaderName = "all-group-list-header-name";
    public const string HeaderDescription = "all-group-list-header-description";
    public const string HeaderFrom = "all-group-list-header-from";
    public const string HeaderUntil = "all-group-list-header-until";

    // Table Rows (use with index)
    public const string RowPrefix = "all-group-list-row-";
    public const string CellNamePrefix = "all-group-list-cell-name-";
    public const string CellDescriptionPrefix = "all-group-list-cell-description-";
    public const string CellFromPrefix = "all-group-list-cell-from-";
    public const string CellUntilPrefix = "all-group-list-cell-until-";
    public const string CheckboxPrefix = "all-group-list-checkbox-";
    public const string EditButtonPrefix = "all-group-list-edit-";
    public const string CopyButtonPrefix = "all-group-list-copy-";
    public const string DeleteButtonPrefix = "all-group-list-delete-";

    // Edit Group Form
    public const string EditGroupItemForm = "edit-group-item-form";
    public const string EditGroupItemName = "edit-group-item-name";
    public const string EditGroupItemFrom = "edit-group-item-from";
    public const string EditGroupItemUntil = "edit-group-item-until";
    public const string GroupDescriptionEditor = "group-description-editor";

    // Edit Group Parent Selection
    public const string EditGroupParentCard = "edit-group-parent-card";
    public const string EditGroupParentSelect = "edit-group-parent-select";

    // Tree View Container
    public const string TreeGroupContainer = "tree-group-container";
    public const string TreeGroupHeader = "tree-group-header";
    public const string TreeGroupTreeContainer = "tree-group-tree-container";
    public const string TreeGroupTreeRoot = "tree-group-tree-root";
    public const string TreeGroupNoTreeMessage = "tree-group-no-tree-message";

    // Tree View Actions
    public const string TreeGroupExpandButton = "tree-group-expand-button";
    public const string TreeGroupCollapseButton = "tree-group-collapse-button";
    public const string TreeGroupRefreshButton = "tree-group-refresh-button";
    public const string TreeGroupAddRootButton = "tree-group-add-root-button";
    public const string TreeGroupGridToggle = "tree-group-grid-toggle";

    // Tree Node IDs (dynamic with group id)
    public const string TreeGroupNodePrefix = "tree-group-node-";
    public const string TreeDropZonePrefix = "drop-zone-";
    public const string TreeNodeItemPrefix = "tree-node-item-";
    public const string TreeNodeTogglePrefix = "tree-node-toggle-";
    public const string TreeNodeActionsPrefix = "tree-node-actions-";
    public const string TreeNodeEditBtnPrefix = "tree-node-edit-btn-";
    public const string TreeNodeCopyBtnPrefix = "tree-node-copy-btn-";
    public const string TreeNodeAddBtnPrefix = "tree-node-add-btn-";
    public const string TreeNodeDeleteBtnPrefix = "tree-node-delete-btn-";
    public const string TreeNodeViewBtnPrefix = "tree-node-view-btn-";

    // Legacy (for backwards compatibility)
    public const string AllGroupTreeView = "all-group-tree-view";
    public const string AllGroupTree = "tree-group-tree-container";

    // Helper methods
    public static string GetRowId(int index) => $"{RowPrefix}{index}";

    public static string GetCellNameId(int index) => $"{CellNamePrefix}{index}";

    public static string GetEditButtonId(int index) => $"{EditButtonPrefix}{index}";

    public static string GetDeleteButtonId(int index) => $"{DeleteButtonPrefix}{index}";

    public static string GetCheckboxId(int index) => $"{CheckboxPrefix}{index}";

    public static string GetTreeNodeId(string groupId) => $"{TreeGroupNodePrefix}{groupId}";

    public static string GetDropZoneId(string groupId) => $"{TreeDropZonePrefix}{groupId}";

    public static string GetTreeNodeItemId(string groupId) => $"{TreeNodeItemPrefix}{groupId}";

    public static string GetTreeNodeToggleId(string groupId) => $"{TreeNodeTogglePrefix}{groupId}";

    public static string GetTreeNodeEditBtnId(string groupId) => $"{TreeNodeEditBtnPrefix}{groupId}";

    public static string GetTreeNodeCopyBtnId(string groupId) => $"{TreeNodeCopyBtnPrefix}{groupId}";

    public static string GetTreeNodeAddBtnId(string groupId) => $"{TreeNodeAddBtnPrefix}{groupId}";

    public static string GetTreeNodeDeleteBtnId(string groupId) => $"{TreeNodeDeleteBtnPrefix}{groupId}";
}
