namespace E2ETest.Constants;

public static class SettingsLlmModelsIds
{
    public const string Section = "settings-llm-models";
    public const string Card = "settings-list-card";
    public const string Header = "settings-list-header";
    public const string RowsContainer = "settings-list-rows";
    public const string AddBtn = "settings-list-add-btn";

    public const string TableHeader = "llm-models-table-header";
    public const string RowSelector = "[id^='llm-models-row-']:not([id*='display']):not([id*='delete']):not([id*='form']):not([id*='container'])";

    public const string ModalHeader = "llm-models-modal-header";
    public const string ModalBody = "llm-models-modal-body";
    public const string ModalForm = "llm-models-modal-form";
    public const string ModalFooter = "llm-models-modal-footer";

    public const string ModalInputModelId = "llm-models-modal-model-id";
    public const string ModalInputModelName = "llm-models-modal-model-name";
    public const string ModalSelectProvider = "llm-models-modal-provider";
    public const string ModalInputDescription = "llm-models-modal-description";
    public const string ModalInputApiModelId = "llm-models-modal-api-model-id";
    public const string ModalInputContextWindow = "llm-models-modal-context-window";
    public const string ModalInputMaxTokens = "llm-models-modal-max-tokens";
    public const string ModalInputInputCost = "llm-models-modal-input-cost";
    public const string ModalInputOutputCost = "llm-models-modal-output-cost";
    public const string ModalInputApiKey = "llm-models-modal-api-key";
    public const string ModalCheckboxIsEnabled = "llm-models-modal-is-enabled";
    public const string ModalCheckboxIsDefault = "llm-models-modal-is-default";

    public const string ModalCancelBtn = "llm-models-modal-cancel-btn";
    public const string ModalSaveBtn = "llm-models-modal-save-btn";
    public const string ModalCloseBtn = "llm-models-modal-close-btn";

    public static string GetRowId(int index) => $"llm-models-row-{index}";

    public static string GetRowDisplayId(string modelId) => $"llm-models-row-display-{modelId}";

    public static string GetRowDeleteId(string modelId) => $"llm-models-row-delete-{modelId}";
}

public static class SettingsLlmModelsTestData
{
    public const string TestModelId = "e2e-test-model";
    public const string TestModelName = "E2E Test Model";
    public const string TestDescription = "Test model for E2E testing";
    public const string TestApiModelId = "gpt-4-test";
    public const string TestContextWindow = "128000";
    public const string TestMaxTokens = "4096";
    public const string TestInputCost = "0.01";
    public const string TestOutputCost = "0.03";
    public const string TestApiKey = "sk-e2e-test-key-12345";

    public const string UpdatedModelName = "E2E Updated Model";
    public const string UpdatedDescription = "Updated test model";
    public const string UpdatedContextWindow = "256000";
    public const string UpdatedMaxTokens = "8192";
}
