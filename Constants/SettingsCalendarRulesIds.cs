namespace Klacks.E2ETest.Constants;

public static class SettingsCalendarRulesIds
{
    public const string Section = "settings-calendar-rules";
    public const string Container = "calendar-rules-container";
    public const string Header = "calendar-rules-header";
    public const string Table = "calendar-rules-table";
    public const string TableBody = "calendar-rules-table-body";
    public const string AddBtn = "calendar-rules-add-btn";
    public const string Pagination = "calendar-rules-pagination";

    public const string ModalHeader = "calendar-rules-modal-header";
    public const string ModalCloseBtn = "calendar-rules-modal-close-btn";
    public const string ModalBody = "calendar-rules-modal-body";

    public const string ModalTabFormLink = "calendar-rules-modal-tab-form-link";
    public const string ModalTabHelpLink = "calendar-rules-modal-tab-help-link";

    public const string ModalInputName = "calendar-rules-modal-name";
    public const string ModalInputRule = "calendar-rules-modal-rule";
    public const string ModalInputSubRule = "calendar-rules-modal-subrule";
    public const string ModalInputCountry = "calendar-rules-modal-country";
    public const string ModalInputState = "calendar-rules-modal-state";
    public const string ModalInputIsMandatory = "calendar-rules-modal-is-mandatory";
    public const string ModalInputIsPaid = "calendar-rules-modal-is-paid";
    public const string ModalInputDescription = "calendar-rules-modal-description";
    public const string ModalResult = "calendar-rules-modal-result";

    public const string ModalCancelBtn = "calendar-rules-modal-cancel-btn";
    public const string ModalAddBtn = "calendar-rules-modal-add-btn";

    public const string RowSelector = "tr[id^='calendar-rules-row-']";

    public static string GetRowId(int index) => $"calendar-rules-row-{index}";
    public static string GetCellNameId(int index) => $"calendar-rules-cell-name-{index}";
    public static string GetCellCountryId(int index) => $"calendar-rules-cell-country-{index}";
    public static string GetCellStateId(int index) => $"calendar-rules-cell-state-{index}";
    public static string GetCellDescriptionId(int index) => $"calendar-rules-cell-description-{index}";
    public static string GetEditBtnId(int index) => $"calendar-rules-edit-btn-{index}";
    public static string GetCopyBtnId(int index) => $"calendar-rules-copy-btn-{index}";
    public static string GetDeleteBtnId(int index) => $"calendar-rules-delete-btn-{index}";

    public const string CalendarDropdown = "calendar-rules-calendar-dropdown";
    public const string DropdownFormButton = "dropdownForm";
    public const string DropdownMenu = "calendar-dropdown-menu";
    public const string DropdownCountrySelect = "calendar-dropdown-country-select";
    public const string DropdownSelectAllBtn = "calendar-dropdown-select-all-btn";
    public const string DropdownDeselectAllBtn = "calendar-dropdown-deselect-all-btn";
    public const string DropdownStatesContainer = "calendar-dropdown-states-container";
    public const string DropdownCloseBtn = "calendar-dropdown-close-btn";

    public static string GetDropdownStateCheckboxId(string country, string state) =>
        $"calendar-dropdown-checkbox-{country}-{state}";
}

public static class SettingsCalendarRulesTestData
{
    public const string TestRuleName = "E2E Test Neujahr";
    public const string TestRule = "01/01";
    public const string TestSubRule = "";
    public const string TestCountry = "CH";
    public const string TestState = "BE";
    public const string TestDescription = "E2E Test Rule";

    public const string UpdatedRuleName = "E2E Test Karfreitag";
    public const string UpdatedRule = "EASTER-02";
    public const string UpdatedSubRule = "SA+2;SU+1";

    public const string EasterRule = "EASTER+00";
    public const string AscensionRule = "EASTER+39";
    public const string ThanksgivingRule = "11/22+000+TH";
}
