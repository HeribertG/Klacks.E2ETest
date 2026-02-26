// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;
using Klacks.E2ETest.Constants;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(48)]
    public class ChatbotKimiProviderTest : ChatbotTestBase
    {
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

        private const string CssModelDropdown = ".model-dropdown";
        private const string CssAddButton = ".add-button";
        private const string CssReadonlyInput = "input[readonly]";

        private int _messageCountBefore;

        [OneTimeSetUp]
        public async Task CleanupKimiFromDb()
        {
            TestContext.Out.WriteLine("=== Cleanup: Deleting existing Kimi model and provider from DB ===");

            var deleteModel = await DbHelper.ExecuteSqlAsync(
                $"DELETE FROM llm_models WHERE model_id = '{KimiModelId}'");
            TestContext.Out.WriteLine($"Delete Kimi model result: {deleteModel}");

            var deleteProvider = await DbHelper.ExecuteSqlAsync(
                $"DELETE FROM llm_providers WHERE provider_id = '{KimiProviderId}'");
            TestContext.Out.WriteLine($"Delete Kimi provider result: {deleteProvider}");

            TestContext.Out.WriteLine("Kimi cleanup completed");
        }

        [Test, Order(1)]
        public async Task Step1_OpenSettingsAndNavigateToProviders()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Settings and Navigate to LLM Providers ===");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(SettingsLlmProvidersIds.Section);
            await Actions.Wait500();

            var section = await Actions.FindElementById(SettingsLlmProvidersIds.Section);
            Assert.That(section, Is.Not.Null, "LLM Providers section should be visible");

            TestContext.Out.WriteLine("LLM Providers section loaded successfully");
        }

        [Test, Order(2)]
        public async Task Step2_CreateKimiProvider()
        {
            TestContext.Out.WriteLine("=== Step 2: Create Kimi Provider ===");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            await Actions.ScrollIntoViewById(SettingsLlmProvidersIds.Section);
            await Actions.Wait500();

            var rowCountBefore = await GetRowCount(SettingsLlmProvidersIds.RowSelector);
            TestContext.Out.WriteLine($"Providers before: {rowCountBefore}");

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

            var rowCountAfter = await GetRowCount(SettingsLlmProvidersIds.RowSelector);
            TestContext.Out.WriteLine($"Providers after: {rowCountAfter}");

            Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore + 1), "Provider count should increase by 1");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Kimi provider created successfully");
        }

        [Test, Order(3)]
        public async Task Step3_VerifyKimiProviderExists()
        {
            TestContext.Out.WriteLine("=== Step 3: Verify Kimi Provider Exists ===");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            await Actions.ScrollIntoViewById(SettingsLlmProvidersIds.Section);
            await Actions.Wait500();

            var foundProviderId = await FindRowByName(SettingsLlmProvidersIds.RowSelector, KimiProviderName);

            Assert.That(foundProviderId, Is.Not.Null, $"Provider '{KimiProviderName}' should exist in the list");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Provider '{KimiProviderName}' found successfully");
        }

        [Test, Order(4)]
        public async Task Step4_NavigateToModelsSection()
        {
            TestContext.Out.WriteLine("=== Step 4: Navigate to LLM Models Section ===");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(SettingsLlmModelsIds.Section);
            await Actions.Wait500();

            var modelsSection = await Actions.FindElementById(SettingsLlmModelsIds.Section);
            Assert.That(modelsSection, Is.Not.Null, "LLM Models section should be visible");

            TestContext.Out.WriteLine("LLM Models section loaded successfully");
        }

        [Test, Order(5)]
        public async Task Step5_CreateKimiModel()
        {
            TestContext.Out.WriteLine("=== Step 5: Create Kimi Model ===");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            await Actions.ScrollIntoViewById(SettingsLlmModelsIds.Section);
            await Actions.Wait500();

            var rowCountBefore = await GetRowCount(SettingsLlmModelsIds.RowSelector);
            TestContext.Out.WriteLine($"Models before: {rowCountBefore}");

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

            var rowCountAfter = await GetRowCount(SettingsLlmModelsIds.RowSelector);
            TestContext.Out.WriteLine($"Models after: {rowCountAfter}");

            Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore + 1), "Model count should increase by 1");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Kimi model created successfully");
        }

        [Test, Order(6)]
        public async Task Step6_VerifyKimiModelExists()
        {
            TestContext.Out.WriteLine("=== Step 6: Verify Kimi Model Exists ===");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            await Actions.ScrollIntoViewById(SettingsLlmModelsIds.Section);
            await Actions.Wait500();

            var foundModelId = await FindRowByName(SettingsLlmModelsIds.RowSelector, KimiModelName);

            Assert.That(foundModelId, Is.Not.Null, $"Model '{KimiModelName}' should exist in the list");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Model '{KimiModelName}' found successfully");
        }

        [Test, Order(7)]
        public async Task Step7_ChatSmokeTestWithKimi()
        {
            TestContext.Out.WriteLine("=== Step 7: Chat Smoke Test with Kimi Model ===");

            await CloseChatIfOpen();
            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            await SelectKimiModel();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Sag einfach Hallo");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond to the message");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Chat smoke test completed successfully");
        }

        private async Task<Microsoft.Playwright.IElementHandle?> FindAddButtonInSection(string sectionId)
        {
            var section = await Actions.FindElementById(sectionId);
            if (section == null)
                return null;

            return await Actions.QueryChildSelector(section, CssAddButton);
        }

        private async Task<int> GetRowCount(string rowSelector)
        {
            var rows = await Actions.QuerySelectorAll(rowSelector);
            return rows.Count;
        }

        private async Task<string?> FindRowByName(string rowSelector, string name)
        {
            var rows = await Actions.QuerySelectorAll(rowSelector);

            foreach (var row in rows)
            {
                var displayInput = await Actions.QueryChildSelector(row, CssReadonlyInput);
                if (displayInput == null) continue;

                var displayValue = await displayInput.InputValueAsync();
                if (!string.IsNullOrEmpty(displayValue) && displayValue.Contains(name))
                {
                    var displayId = await displayInput.GetAttributeAsync("id");
                    return displayId ?? string.Empty;
                }
            }

            return null;
        }

        private async Task SelectKimiModel()
        {
            TestContext.Out.WriteLine("Selecting Kimi model in chat dropdown...");

            var modelDropdown = await Actions.QuerySelector(CssModelDropdown);
            Assert.That(modelDropdown, Is.Not.Null, "Model dropdown button should exist");

            await modelDropdown!.ClickAsync();
            await Actions.Wait500();

            var kimiOption = await Actions.QuerySelector($".dropdown-item[aria-label='{KimiModelName}']");
            if (kimiOption == null)
            {
                kimiOption = await Actions.QuerySelector(".dropdown-item[aria-label*='Moonshot']");
            }

            Assert.That(kimiOption, Is.Not.Null, $"Kimi model '{KimiModelName}' should appear in dropdown");

            await kimiOption!.ClickAsync();
            await Actions.Wait500();

            TestContext.Out.WriteLine("Kimi model selected");
        }
    }
}
