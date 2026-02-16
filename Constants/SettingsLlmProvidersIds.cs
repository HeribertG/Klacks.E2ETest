namespace Klacks.E2ETest.Constants;

public static class SettingsLlmProvidersIds
{
    public const string Section = "settings-llm-providers";
    public const string Card = "llm-providers-card";
    public const string Header = "llm-providers-header";
    public const string RowsContainer = "llm-providers-rows";
    public const string AddBtn = "llm-providers-add-btn";

    public const string TableHeader = "llm-providers-table-header";
    public const string RowSelector = "[id^='llm-providers-row-']:not([id*='display']):not([id*='delete']):not([id*='form']):not([id*='container'])";

    public const string ModalHeader = "llm-providers-modal-header";
    public const string ModalBody = "llm-providers-modal-body";
    public const string ModalForm = "llm-providers-modal-form";
    public const string ModalFooter = "llm-providers-modal-footer";

    public const string ModalInputProviderId = "llm-providers-modal-provider-id";
    public const string ModalInputProviderName = "llm-providers-modal-provider-name";
    public const string ModalInputBaseUrl = "llm-providers-modal-base-url";
    public const string ModalInputApiVersion = "llm-providers-modal-api-version";
    public const string ModalInputPriority = "llm-providers-modal-priority";
    public const string ModalInputApiKey = "llm-providers-modal-api-key";
    public const string ModalCheckboxIsEnabled = "llm-providers-modal-is-enabled";

    public const string ModalCancelBtn = "llm-providers-modal-cancel-btn";
    public const string ModalSaveBtn = "llm-providers-modal-save-btn";
    public const string ModalCloseBtn = "llm-providers-modal-close-btn";

    public static string GetRowId(int index) => $"llm-providers-row-{index}";

    public static string GetRowDisplayId(string providerId) => $"llm-providers-row-display-{providerId}";

    public static string GetRowDeleteId(string providerId) => $"llm-providers-row-delete-{providerId}";
}

public static class SettingsLlmProvidersTestData
{
    public const string TestProviderId = "e2e-test-provider";
    public const string TestProviderName = "E2E Test Provider";
    public const string TestBaseUrl = "https://api.e2e-test.com/v1/";
    public const string TestApiVersion = "v1";
    public const string TestPriority = "50";
    public const string TestApiKey = "sk-e2e-test-key-12345";

    public const string UpdatedProviderName = "E2E Updated Provider";
    public const string UpdatedBaseUrl = "https://api.e2e-updated.com/v2/";
    public const string UpdatedApiVersion = "v2";
    public const string UpdatedPriority = "75";
}
