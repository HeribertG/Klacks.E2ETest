namespace E2ETest.Constants;

public static class SettingsAbsenceIds
{
    public const string Section = "absence-container";
    public const string Header = "absence-header";
    public const string Table = "absence-table";
    public const string TableBody = "absence-table-body";
    public const string AddBtn = "absence-add-btn";
    public const string Pagination = "absence-pagination";
    public const string ExportExcelBtn = "absence-export-excel-btn";

    public const string ModalHeader = "absence-modal-header";
    public const string ModalCloseBtn = "absence-modal-close-btn";
    public const string ModalBody = "absence-modal-body";
    public const string ModalForm = "absence-modal-form";
    public const string ModalFooter = "absence-modal-footer";

    public const string ModalInputName = "absence-modal-name";
    public const string ModalInputDescription = "absence-modal-description";
    public const string ModalInputDefaultLength = "absence-modal-default-length";
    public const string ModalInputDefaultValue = "absence-modal-default-value";
    public const string ModalCheckboxSaturday = "absence-modal-with-saturday";
    public const string ModalCheckboxSunday = "absence-modal-with-sunday";
    public const string ModalCheckboxHoliday = "absence-modal-with-holiday";
    public const string ModalInputColorPicker = "absence-modal-color-picker";
    public const string ModalInputColorText = "absence-modal-color-text";
    public const string ModalCheckboxHideInGantt = "absence-modal-hide-in-gantt";

    public const string ModalCancelBtn = "absence-modal-cancel-btn";
    public const string ModalSaveBtn = "absence-modal-save-btn";

    public const string RowSelector = "tr[id^='absence-row-']";

    public static string GetRowId(int index) => $"absence-row-{index}";

    public static string GetCellColorId(int index) => $"absence-cell-color-{index}";

    public static string GetCellNameId(int index) => $"absence-cell-name-{index}";

    public static string GetCellDescriptionId(int index) => $"absence-cell-description-{index}";

    public static string GetCellActionsId(int index) => $"absence-cell-actions-{index}";

    public static string GetEditBtnId(int index) => $"absence-edit-btn-{index}";

    public static string GetCopyBtnId(int index) => $"absence-copy-btn-{index}";

    public static string GetDeleteBtnId(int index) => $"absence-delete-btn-{index}";
}

public static class SettingsAbsenceTestData
{
    public const string TestAbsenceName = "E2E Test Urlaub";
    public const string TestDescription = "E2E Test Abwesenheit";
    public const int TestDefaultLength = 20;
    public const int TestDefaultValue = 100;
    public const string TestColor = "#3498db";

    public const string UpdatedAbsenceName = "E2E Test Ferien";
    public const string UpdatedDescription = "E2E Test Ferien Beschreibung";
    public const int UpdatedDefaultLength = 25;
    public const int UpdatedDefaultValue = 80;
    public const string UpdatedColor = "#e74c3c";
}
