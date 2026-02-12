using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest;

[TestFixture]
[Order(48)]
public class LlmKimiProviderTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private int _messageCountBefore;

    private const string KimiProviderId = "kimi";
    private const string KimiProviderName = "Kimi";
    private const string KimiBaseUrl = "https://api.kimi.com/coding/v1/";
    private const string KimiApiKey = "sk-kimi-Zz6Su32IlUHrrufrsPzWSxL6uJCorj6lDDhFYTg5vZRtBE8HDLLjkmOmwVRjY99R";
    private const string KimiPriority = "90";

    private const string KimiModelId = "kimi-for-coding";
    private const string KimiModelName = "Kimi For Coding";
    private const string KimiApiModelId = "kimi-for-coding";
    private const string KimiContextWindow = "262144";
    private const string KimiMaxTokens = "4096";
    private const string KimiInputCost = "0.06";
    private const string KimiOutputCost = "0.06";

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [TearDown]
    public void TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
        }
    }

    [Test]
    [Order(1)]
    public async Task Step1_OpenSettingsAndNavigateToProviders()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Open Settings and Navigate to LLM Providers ===");

        // Act
        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(SettingsLlmProvidersIds.Section);
        await Actions.Wait500();

        // Assert
        var section = await Actions.FindElementById(SettingsLlmProvidersIds.Section);
        Assert.That(section, Is.Not.Null, "LLM Providers section should be visible");

        TestContext.Out.WriteLine("LLM Providers section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateKimiProvider()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create Kimi Provider ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();
        await Actions.ScrollIntoViewById(SettingsLlmProvidersIds.Section);
        await Actions.Wait500();

        var rowCountBefore = await GetProviderRowCount();
        TestContext.Out.WriteLine($"Providers before: {rowCountBefore}");

        // Act
        var addButton = await FindAddButtonInSection(SettingsLlmProvidersIds.Section);
        Assert.That(addButton, Is.Not.Null, "Add button should exist");

        await addButton!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Add Provider button");

        var modalHeader = await Actions.FindElementById(SettingsLlmProvidersIds.ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");

        await Actions.ClearInputById(SettingsLlmProvidersIds.ModalInputProviderId);
        await Actions.TypeIntoInputById(SettingsLlmProvidersIds.ModalInputProviderId, KimiProviderId);

        await Actions.ClearInputById(SettingsLlmProvidersIds.ModalInputProviderName);
        await Actions.TypeIntoInputById(SettingsLlmProvidersIds.ModalInputProviderName, KimiProviderName);

        await Actions.ClearInputById(SettingsLlmProvidersIds.ModalInputBaseUrl);
        await Actions.TypeIntoInputById(SettingsLlmProvidersIds.ModalInputBaseUrl, KimiBaseUrl);

        await Actions.ClearInputById(SettingsLlmProvidersIds.ModalInputPriority);
        await Actions.TypeIntoInputById(SettingsLlmProvidersIds.ModalInputPriority, KimiPriority);

        await Actions.ClearInputById(SettingsLlmProvidersIds.ModalInputApiKey);
        await Actions.TypeIntoInputById(SettingsLlmProvidersIds.ModalInputApiKey, KimiApiKey);

        TestContext.Out.WriteLine($"Filled provider form: {KimiProviderName} ({KimiProviderId})");

        var saveBtn = await Actions.FindElementById(SettingsLlmProvidersIds.ModalSaveBtn);
        Assert.That(saveBtn, Is.Not.Null, "Save button should exist");

        await saveBtn!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var rowCountAfter = await GetProviderRowCount();
        TestContext.Out.WriteLine($"Providers after: {rowCountAfter}");

        // Assert
        Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore + 1), "Provider count should increase by 1");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Kimi provider created successfully");
    }

    [Test]
    [Order(3)]
    public async Task Step3_VerifyKimiProviderExists()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Verify Kimi Provider Exists ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();
        await Actions.ScrollIntoViewById(SettingsLlmProvidersIds.Section);
        await Actions.Wait500();

        // Act
        var foundProviderId = await FindProviderRowByName(KimiProviderName);

        // Assert
        Assert.That(foundProviderId, Is.Not.Null, $"Provider '{KimiProviderName}' should exist in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Provider '{KimiProviderName}' found successfully");
    }

    [Test]
    [Order(4)]
    public async Task Step4_NavigateToModelsSection()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Navigate to LLM Models Section ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        // Act
        await Actions.ScrollIntoViewById(SettingsLlmModelsIds.Section);
        await Actions.Wait500();

        // Assert
        var modelsSection = await Actions.FindElementById(SettingsLlmModelsIds.Section);
        Assert.That(modelsSection, Is.Not.Null, "LLM Models section should be visible");

        TestContext.Out.WriteLine("LLM Models section loaded successfully");
    }

    [Test]
    [Order(5)]
    public async Task Step5_CreateKimiModel()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Create Kimi Model ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();
        await Actions.ScrollIntoViewById(SettingsLlmModelsIds.Section);
        await Actions.Wait500();

        var rowCountBefore = await GetModelRowCount();
        TestContext.Out.WriteLine($"Models before: {rowCountBefore}");

        // Act
        var addButton = await FindAddButtonInSection(SettingsLlmModelsIds.Section);
        Assert.That(addButton, Is.Not.Null, "Add button should exist");

        await addButton!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Add Model button");

        var modalHeader = await Actions.FindElementById(SettingsLlmModelsIds.ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");

        await Actions.ClearInputById(SettingsLlmModelsIds.ModalInputModelId);
        await Actions.TypeIntoInputById(SettingsLlmModelsIds.ModalInputModelId, KimiModelId);

        await Actions.ClearInputById(SettingsLlmModelsIds.ModalInputModelName);
        await Actions.TypeIntoInputById(SettingsLlmModelsIds.ModalInputModelName, KimiModelName);

        var providerSelect = await Actions.FindElementById(SettingsLlmModelsIds.ModalSelectProvider);
        if (providerSelect != null)
        {
            await providerSelect.SelectOptionAsync(new Microsoft.Playwright.SelectOptionValue { Label = KimiProviderName });
            TestContext.Out.WriteLine($"Selected provider: {KimiProviderName}");
        }

        await Actions.ClearInputById(SettingsLlmModelsIds.ModalInputApiModelId);
        await Actions.TypeIntoInputById(SettingsLlmModelsIds.ModalInputApiModelId, KimiApiModelId);

        await Actions.ClearInputById(SettingsLlmModelsIds.ModalInputContextWindow);
        await Actions.TypeIntoInputById(SettingsLlmModelsIds.ModalInputContextWindow, KimiContextWindow);

        await Actions.ClearInputById(SettingsLlmModelsIds.ModalInputMaxTokens);
        await Actions.TypeIntoInputById(SettingsLlmModelsIds.ModalInputMaxTokens, KimiMaxTokens);

        await Actions.ClearInputById(SettingsLlmModelsIds.ModalInputInputCost);
        await Actions.TypeIntoInputById(SettingsLlmModelsIds.ModalInputInputCost, KimiInputCost);

        await Actions.ClearInputById(SettingsLlmModelsIds.ModalInputOutputCost);
        await Actions.TypeIntoInputById(SettingsLlmModelsIds.ModalInputOutputCost, KimiOutputCost);

        var isDefaultCheckbox = await Actions.FindElementById(SettingsLlmModelsIds.ModalCheckboxIsDefault);
        if (isDefaultCheckbox != null)
        {
            var isChecked = await isDefaultCheckbox.IsCheckedAsync();
            if (!isChecked)
            {
                await isDefaultCheckbox.ClickAsync();
                TestContext.Out.WriteLine("Set as default model");
            }
        }

        TestContext.Out.WriteLine($"Filled model form: {KimiModelName} ({KimiModelId})");

        var saveBtn = await Actions.FindElementById(SettingsLlmModelsIds.ModalSaveBtn);
        Assert.That(saveBtn, Is.Not.Null, "Save button should exist");

        await saveBtn!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var rowCountAfter = await GetModelRowCount();
        TestContext.Out.WriteLine($"Models after: {rowCountAfter}");

        // Assert
        Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore + 1), "Model count should increase by 1");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Kimi model created successfully");
    }

    [Test]
    [Order(6)]
    public async Task Step6_VerifyKimiModelExists()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Verify Kimi Model Exists ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();
        await Actions.ScrollIntoViewById(SettingsLlmModelsIds.Section);
        await Actions.Wait500();

        // Act
        var foundModelId = await FindModelRowByName(KimiModelName);

        // Assert
        Assert.That(foundModelId, Is.Not.Null, $"Model '{KimiModelName}' should exist in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Model '{KimiModelName}' found successfully");
    }

    [Test]
    [Order(7)]
    public async Task Step7_ChatSmokeTestWithKimi()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 7: Chat Smoke Test with Kimi Model ===");

        await CloseChatIfOpen();
        await Actions.ClickButtonById(HeaderAssistantButton);
        await Actions.Wait1000();

        // Act
        await SelectKimiModel();

        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Sag einfach Hallo");
        var response = await WaitForBotResponse(_messageCountBefore);

        // Assert
        TestContext.Out.WriteLine($"Bot response: {response}");
        Assert.That(response, Is.Not.Empty, "Bot should respond to the message");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Chat smoke test completed successfully");
    }

    #region Helper Methods

    private async Task<Microsoft.Playwright.IElementHandle?> FindAddButtonInSection(string sectionId)
    {
        var section = await Page.QuerySelectorAsync($"#{sectionId}");
        if (section == null)
        {
            return null;
        }

        return await section.QuerySelectorAsync(".add-button");
    }

    private async Task<int> GetProviderRowCount()
    {
        var rows = await Page.QuerySelectorAllAsync(SettingsLlmProvidersIds.RowSelector);
        return rows.Count;
    }

    private async Task<int> GetModelRowCount()
    {
        var rows = await Page.QuerySelectorAllAsync(SettingsLlmModelsIds.RowSelector);
        return rows.Count;
    }

    private async Task<string?> FindProviderRowByName(string providerName)
    {
        var rows = await Page.QuerySelectorAllAsync(SettingsLlmProvidersIds.RowSelector);

        foreach (var row in rows)
        {
            var displayInput = await row.QuerySelectorAsync("input[readonly]");
            if (displayInput == null) continue;

            var displayValue = await displayInput.InputValueAsync();
            var displayId = await displayInput.GetAttributeAsync("id");

            if (!string.IsNullOrEmpty(displayValue) && displayValue.Contains(providerName))
            {
                return displayId?.Replace("llm-providers-row-display-", "") ?? string.Empty;
            }
        }

        return null;
    }

    private async Task<string?> FindModelRowByName(string modelName)
    {
        var rows = await Page.QuerySelectorAllAsync(SettingsLlmModelsIds.RowSelector);

        foreach (var row in rows)
        {
            var displayInput = await row.QuerySelectorAsync("input[readonly]");
            if (displayInput == null) continue;

            var displayValue = await displayInput.InputValueAsync();
            var displayId = await displayInput.GetAttributeAsync("id");

            if (!string.IsNullOrEmpty(displayValue) && displayValue.Contains(modelName))
            {
                return displayId?.Replace("llm-models-row-display-", "") ?? string.Empty;
            }
        }

        return null;
    }

    private async Task SelectKimiModel()
    {
        TestContext.Out.WriteLine("Selecting Kimi model in chat dropdown...");

        var modelDropdown = await Page.QuerySelectorAsync(".model-dropdown");
        Assert.That(modelDropdown, Is.Not.Null, "Model dropdown button should exist");

        await modelDropdown!.ClickAsync();
        await Actions.Wait500();

        var kimiOption = await Page.QuerySelectorAsync($".dropdown-item[aria-label='{KimiModelName}']");
        if (kimiOption == null)
        {
            kimiOption = await Page.QuerySelectorAsync($".dropdown-item[aria-label*='Moonshot']");
        }

        Assert.That(kimiOption, Is.Not.Null, $"Kimi model '{KimiModelName}' should appear in dropdown");

        await kimiOption!.ClickAsync();
        await Actions.Wait500();

        TestContext.Out.WriteLine("Kimi model selected");
    }

    private async Task EnsureChatOpen()
    {
        var chatInput = await Actions.FindElementById(ChatInput);
        if (chatInput == null)
        {
            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();
        }

        await WaitForChatInputEnabled();
    }

    private async Task CloseChatIfOpen()
    {
        var chatInput = await Actions.FindElementById(ChatInput);
        if (chatInput != null)
        {
            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait500();
        }
    }

    private async Task WaitForChatInputEnabled()
    {
        var maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var isEnabled = await WaitForInputEnabled(10000);
            if (isEnabled) return;

            TestContext.Out.WriteLine($"Chat input disabled (attempt {attempt + 1}/{maxRetries}), refreshing page...");
            await Actions.Reload();
            await Actions.Wait2000();

            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();
        }

        Assert.Fail("Chat input remained disabled after multiple refresh attempts");
    }

    private async Task<bool> WaitForInputEnabled(int timeoutMs)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            var chatInput = await Actions.FindElementById(ChatInput);
            if (chatInput != null)
            {
                var isDisabled = await chatInput.IsDisabledAsync();
                if (!isDisabled) return true;
            }

            await Actions.Wait500();
        }

        return false;
    }

    private async Task SendChatMessage(string message)
    {
        TestContext.Out.WriteLine($"Sending message: {message}");
        await Actions.FillInputWithDispatch(ChatInput, message);
        await Actions.ClickButtonById(ChatSendBtn);
    }

    private async Task<int> GetMessageCount()
    {
        var messages = await Actions.QuerySelectorAll($"#{ChatMessages} .message-wrapper.assistant");
        return messages.Count;
    }

    private async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = 60000)
    {
        TestContext.Out.WriteLine("Waiting for bot response...");

        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);

        while (DateTime.UtcNow - startTime < timeout)
        {
            var typingIndicator = await Actions.QuerySelector($"#{ChatMessages} .typing-indicator");
            var currentMessages = await Actions.QuerySelectorAll($"#{ChatMessages} .message-wrapper.assistant");

            if (typingIndicator == null && currentMessages.Count > previousMessageCount)
            {
                var lastMessage = currentMessages[currentMessages.Count - 1];
                var messageText = await Actions.QueryChildSelector(lastMessage, ".message-text");
                if (messageText != null)
                {
                    var text = await Actions.GetElementText(messageText);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        TestContext.Out.WriteLine($"Bot responded after {(DateTime.UtcNow - startTime).TotalSeconds:F1}s");
                        return text.Trim();
                    }
                }
            }

            await Actions.Wait500();
        }

        Assert.Fail($"Bot did not respond within {timeoutMs / 1000}s");
        return string.Empty;
    }

    #endregion
}
