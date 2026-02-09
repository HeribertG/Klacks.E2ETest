using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;
using static Klacks.E2ETest.Constants.SettingsBranchIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(63)]
    public class LlmBranchesTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;

        private static readonly List<string> CreatedBranchIds = new();

        private static readonly (string Name, string Address, string Phone, string Email) BranchZurich =
            ("Filiale Zürich", "Bahnhofstrasse 1, 8001 Zürich", "044 123 45 67", "zuerich@klacks-test.ch");

        private static readonly (string Name, string Address, string Phone, string Email) BranchLausanne =
            ("Filiale Lausanne", "Place de la Gare 1, 1003 Lausanne", "021 123 45 67", "lausanne@klacks-test.ch");

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
        public async Task Step1_OpenChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            // Act
            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();

            // Assert
            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_VerifyPermissions()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Verify Branch Management Permissions ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich? Darf ich Filialen verwalten?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"User must have Admin rights. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Permissions verified successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ValidateZurichAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Validate Zürich Address via Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Validiere die Adresse: Bahnhofstrasse 1, 8001 Zürich, Schweiz");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address validation result");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Zürich address validated successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_CreateZurichBranch()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Create Zürich Branch via UI ===");

            await CloseChatIfOpen();
            await Actions.Wait500();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.ScrollIntoViewById(AddBranchBtn);
            await Actions.Wait500();

            // Act
            var branchId = await CreateBranchViaUi(BranchZurich.Name, BranchZurich.Address, BranchZurich.Phone, BranchZurich.Email);

            // Assert
            Assert.That(branchId, Is.Not.Null, $"Branch {BranchZurich.Name} should be created");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Zürich branch created (ID: {branchId})");
        }

        [Test]
        [Order(5)]
        public async Task Step5_ValidateLausanneAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Validate Lausanne Address via Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Validiere die Adresse: Place de la Gare 1, 1003 Lausanne, Schweiz");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address validation result");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Lausanne address validated successfully");
        }

        [Test]
        [Order(6)]
        public async Task Step6_CreateLausanneBranch()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Create Lausanne Branch via UI ===");

            await CloseChatIfOpen();
            await Actions.Wait500();

            // Act
            await Actions.ScrollIntoViewById(AddBranchBtn);
            await Actions.Wait500();

            var branchId = await CreateBranchViaUi(BranchLausanne.Name, BranchLausanne.Address, BranchLausanne.Phone, BranchLausanne.Email);

            // Assert
            Assert.That(branchId, Is.Not.Null, $"Branch {BranchLausanne.Name} should be created");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Lausanne branch created (ID: {branchId})");
        }

        [Test]
        [Order(7)]
        public async Task Step7_VerifyBothBranches()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Verify Both Branches Exist ===");

            await CloseChatIfOpen();
            await Actions.Wait500();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(AddBranchBtn);
            await Actions.Wait1000();

            // Act
            var nameInputs = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            TestContext.Out.WriteLine($"Total branches in list: {nameInputs.Count}");

            var foundBranches = new List<string>();
            foreach (var input in nameInputs)
            {
                var value = await input.InputValueAsync();
                TestContext.Out.WriteLine($"  Branch: '{value}'");

                if (value.Contains(BranchZurich.Name, StringComparison.OrdinalIgnoreCase))
                    foundBranches.Add(BranchZurich.Name);
                if (value.Contains(BranchLausanne.Name, StringComparison.OrdinalIgnoreCase))
                    foundBranches.Add(BranchLausanne.Name);
            }

            // Assert
            TestContext.Out.WriteLine($"Found {foundBranches.Count} of 2 test branches");
            Assert.That(foundBranches.Count, Is.EqualTo(2),
                $"Both test branches should exist. Found: {string.Join(", ", foundBranches)}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Both branches verified successfully");
        }

        [Test]
        [Order(8)]
        public async Task Step8_DeleteBothBranches()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Delete Both Branches ===");

            if (CreatedBranchIds.Count == 0)
            {
                TestContext.Out.WriteLine("No branches to delete - skipping");
                Assert.Inconclusive("No branches were created in previous steps");
                return;
            }

            await CloseChatIfOpen();
            await Actions.Wait500();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(AddBranchBtn);
            await Actions.Wait1000();

            // Act
            var deletedCount = 0;
            foreach (var branchId in CreatedBranchIds.ToList())
            {
                var deleteButtonId = $"{RowDeletePrefix}{branchId}";
                var deleteButton = await Actions.FindElementById(deleteButtonId);

                if (deleteButton == null)
                {
                    TestContext.Out.WriteLine($"Delete button for branch {branchId} not found - skipping");
                    continue;
                }

                await deleteButton.ClickAsync();
                await Actions.Wait500();

                await Actions.ClickElementById(DeleteModalConfirmBtn);
                await Actions.Wait2000();

                deletedCount++;
                TestContext.Out.WriteLine($"Deleted branch {branchId}");
            }

            // Assert
            foreach (var branchId in CreatedBranchIds)
            {
                var deletedBranch = await Page.QuerySelectorAsync($"#{RowNamePrefix}{branchId}");
                Assert.That(deletedBranch, Is.Null, $"Branch {branchId} should be deleted");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"All {deletedCount} test branches deleted successfully");
            CreatedBranchIds.Clear();
        }

        #region Helper Methods

        private async Task<string?> CreateBranchViaUi(string name, string address, string phone, string email)
        {
            TestContext.Out.WriteLine($"Creating branch: {name} ({address})");

            await Actions.ScrollIntoViewById(AddBranchBtn);
            await Actions.Wait500();

            var addButton = await Actions.FindElementById(AddBranchBtn);
            Assert.That(addButton, Is.Not.Null, "Add branch button should exist");
            await addButton!.ClickAsync();
            await Actions.Wait1000();

            await Actions.TypeIntoInputById(ModalInputName, name);
            await Actions.TypeIntoInputById(ModalInputAddress, address);
            await Actions.TypeIntoInputById(ModalInputPhone, phone);
            await Actions.TypeIntoInputById(ModalInputEmail, email);
            await Actions.Wait1000();

            var saveButton = await Actions.FindElementById(ModalSaveBtn);
            Assert.That(saveButton, Is.Not.Null, "Save button should exist");

            var isEnabled = await saveButton!.IsEnabledAsync();
            TestContext.Out.WriteLine($"Save button enabled: {isEnabled}");
            Assert.That(isEnabled, Is.True, "Save button should be enabled after filling required fields");

            await Actions.ClickElementById(ModalSaveBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait3500();

            var nameInputs = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            TestContext.Out.WriteLine($"Found {nameInputs.Count} branches in list after save");
            foreach (var input in nameInputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    var inputId = await input.GetAttributeAsync("id");
                    var branchId = inputId?.Replace(RowNamePrefix, "");
                    TestContext.Out.WriteLine($"Found created branch: '{value}' (ID: {branchId})");
                    if (branchId != null)
                    {
                        CreatedBranchIds.Add(branchId);
                        return branchId;
                    }
                }
            }

            TestContext.Out.WriteLine($"WARNING: Branch '{name}' not found in list after creation");
            return null;
        }

        private async Task EnsureChatOpen()
        {
            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
            if (chatInput == null)
            {
                await Actions.ClickButtonById(HeaderAssistantButton);
                await Actions.Wait1000();
            }

            await WaitForChatInputEnabled();
        }

        private async Task CloseChatIfOpen()
        {
            var aside = await Page.QuerySelectorAsync("app-aside.visible");
            if (aside != null)
            {
                TestContext.Out.WriteLine("Closing chat aside panel via JS click");
                await Page.EvaluateAsync($"() => document.getElementById('{HeaderAssistantButton}')?.click()");
                await Actions.Wait1000();

                var stillVisible = await Page.QuerySelectorAsync("app-aside.visible");
                if (stillVisible != null)
                {
                    TestContext.Out.WriteLine("Aside still visible, retrying via JS click");
                    await Page.EvaluateAsync($"() => document.getElementById('{HeaderAssistantButton}')?.click()");
                    await Actions.Wait1000();
                }
            }
        }

        private async Task WaitForChatInputEnabled()
        {
            var maxRetries = 3;

            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                var isEnabled = await WaitForInputEnabled(10000);
                if (isEnabled)
                    return;

                TestContext.Out.WriteLine($"Chat input disabled (attempt {attempt + 1}/{maxRetries}), refreshing page...");
                await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
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
                var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
                if (chatInput != null)
                {
                    var isDisabled = await chatInput.IsDisabledAsync();
                    if (!isDisabled)
                        return true;
                }

                await Actions.Wait500();
            }

            return false;
        }

        private async Task SendChatMessage(string message)
        {
            TestContext.Out.WriteLine($"Sending message: {message}");

            var inputLocator = Page.Locator($"#{ChatInput}");
            await inputLocator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            });

            await inputLocator.FillAsync(message);
            await Actions.Wait200();

            await Page.EvaluateAsync($@"() => {{
                const textarea = document.getElementById('{ChatInput}');
                if (textarea) {{
                    textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
                }}
            }}");
            await Actions.Wait200();

            await Page.EvaluateAsync($"() => document.getElementById('{ChatSendBtn}')?.click()");
        }

        private async Task<int> GetMessageCount()
        {
            var messages = await Page.QuerySelectorAllAsync($"#{ChatMessages} .message-wrapper.assistant");
            return messages.Count;
        }

        private async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = 60000)
        {
            TestContext.Out.WriteLine("Waiting for bot response...");

            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);

            while (DateTime.UtcNow - startTime < timeout)
            {
                var typingIndicator = await Page.QuerySelectorAsync($"#{ChatMessages} .typing-indicator");
                var currentMessages = await Page.QuerySelectorAllAsync($"#{ChatMessages} .message-wrapper.assistant");

                if (typingIndicator == null && currentMessages.Count > previousMessageCount)
                {
                    var lastMessage = currentMessages[currentMessages.Count - 1];
                    var messageText = await lastMessage.QuerySelectorAsync(".message-text");
                    if (messageText != null)
                    {
                        var text = await messageText.TextContentAsync();
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
}
