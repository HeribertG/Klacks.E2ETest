using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
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

        private static readonly List<string> CreatedBranchNames = new();

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
            var chatInput = await Actions.FindElementById(ChatInput);
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
            await SendChatMessage("Bin ich ein Administrator? Welche Berechtigungen habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(
                response.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Recht", StringComparison.OrdinalIgnoreCase),
                Is.True,
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
        public async Task Step4_CreateZurichBranchViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Create Zürich Branch via LLM Chat (UI) ===");
            var branch = BranchZurich;

            // Act & Assert
            await CreateBranchWithRetry(branch.Name, branch.Address, branch.Phone, branch.Email);
            CreatedBranchNames.Add(branch.Name);

            TestContext.Out.WriteLine($"Zürich branch created via UI: {branch.Name}");
        }

        [Test]
        [Order(5)]
        public async Task Step5_ValidateLausanneAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Validate Lausanne Address via Chat ===");
            await EnsureChatOpen();
            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();

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
        public async Task Step6_CreateLausanneBranchViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Create Lausanne Branch via LLM Chat (UI) ===");
            var branch = BranchLausanne;

            // Act & Assert
            await CreateBranchWithRetry(branch.Name, branch.Address, branch.Phone, branch.Email);
            CreatedBranchNames.Add(branch.Name);

            TestContext.Out.WriteLine($"Lausanne branch created via UI: {branch.Name}");
        }

        [Test]
        [Order(7)]
        public async Task Step7_VerifyBothBranchesViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Verify Both Branches in DOM ===");

            // Assert
            var zurichExists = await BranchExistsInDom(BranchZurich.Name);
            var lausanneExists = await BranchExistsInDom(BranchLausanne.Name);

            TestContext.Out.WriteLine($"  Zürich: {(zurichExists ? "FOUND in DOM" : "NOT FOUND in DOM")}");
            TestContext.Out.WriteLine($"  Lausanne: {(lausanneExists ? "FOUND in DOM" : "NOT FOUND in DOM")}");

            Assert.That(zurichExists && lausanneExists, Is.True, "Both test branches should be visible in Settings DOM");

            TestContext.Out.WriteLine("Both branches verified in DOM");
        }

        [Test]
        [Order(8)]
        public async Task Step8_DeleteBothBranchesViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Delete Both Branches via LLM Chat (UI) ===");

            if (CreatedBranchNames.Count == 0)
            {
                TestContext.Out.WriteLine("No branches to delete - skipping");
                Assert.Inconclusive("No branches were created in previous steps");
                return;
            }

            // Act
            foreach (var branchName in CreatedBranchNames.ToList())
            {
                await DeleteBranchWithRetry(branchName);
                await Actions.Wait2000();
            }

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"All {CreatedBranchNames.Count} test branches deleted via UI");
            CreatedBranchNames.Clear();
        }

        [Test]
        [Order(9)]
        public async Task Step9_VerifyBranchesDeletedViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 9: Verify Branches Deleted ===");
            await Actions.Wait2000();

            // Assert
            var zurichExists = await BranchExistsInDom(BranchZurich.Name);
            var lausanneExists = await BranchExistsInDom(BranchLausanne.Name);

            TestContext.Out.WriteLine($"  Zürich: {(zurichExists ? "STILL EXISTS" : "DELETED")}");
            TestContext.Out.WriteLine($"  Lausanne: {(lausanneExists ? "STILL EXISTS" : "DELETED")}");

            Assert.That(zurichExists, Is.False, $"Branch '{BranchZurich.Name}' should no longer exist in DOM");
            Assert.That(lausanneExists, Is.False, $"Branch '{BranchLausanne.Name}' should no longer exist in DOM");

            TestContext.Out.WriteLine("All test branches confirmed deleted");
        }

        #region Helper Methods

        private async Task CreateBranchWithRetry(string name, string address, string phone, string email, int maxAttempts = 3)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                TestContext.Out.WriteLine($"Create branch attempt {attempt}/{maxAttempts}: {name}");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Erstelle eine neue Filiale mit dem Namen '{name}', " +
                    $"Adresse '{address}', Telefon '{phone}', Email '{email}'");
                var response = await WaitForBotResponse(_messageCountBefore, 120000);
                TestContext.Out.WriteLine($"Bot response ({response.Length} chars): {response[..Math.Min(200, response.Length)]}");

                var found = await WaitForBranchInDom(name);
                if (found)
                {
                    if (_listener.HasApiErrors() && _listener.GetLastErrorMessage().Contains("already exists"))
                    {
                        TestContext.Out.WriteLine("Ignoring 'already exists' error since branch was created successfully");
                        _listener.ResetErrors();
                    }
                    Assert.That(_listener.HasApiErrors(), Is.False,
                        $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
                    return;
                }

                TestContext.Out.WriteLine($"Branch not found in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Branch '{name}' was not created after {maxAttempts} attempts");
        }

        private async Task DeleteBranchWithRetry(string branchName, int maxAttempts = 3)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                TestContext.Out.WriteLine($"Delete branch attempt {attempt}/{maxAttempts}: {branchName}");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Lösche die Filiale '{branchName}'");
                var response = await WaitForBotResponse(_messageCountBefore, 90000);
                TestContext.Out.WriteLine($"Delete response: {response[..Math.Min(200, response.Length)]}");

                var removed = await WaitForBranchRemovedFromDom(branchName);
                if (removed)
                {
                    TestContext.Out.WriteLine($"Branch '{branchName}' confirmed removed from DOM");
                    return;
                }

                TestContext.Out.WriteLine($"Branch '{branchName}' still in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Branch '{branchName}' was not deleted after {maxAttempts} attempts");
        }

        private async Task<bool> WaitForBranchInDom(string branchName, int timeoutMs = 30000)
        {
            TestContext.Out.WriteLine($"Waiting for branch '{branchName}' to appear in DOM...");

            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                if (await BranchExistsInDom(branchName))
                {
                    TestContext.Out.WriteLine($"Branch '{branchName}' found in DOM");
                    return true;
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"Branch '{branchName}' NOT found in DOM after {timeoutMs / 1000}s");
            return false;
        }

        private async Task<bool> BranchExistsInDom(string branchName)
        {
            var inputs = await Page.QuerySelectorAllAsync($"input[id^=\"{RowNamePrefix}\"]");
            foreach (var input in inputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(branchName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private async Task<bool> WaitForBranchRemovedFromDom(string branchName, int timeoutMs = 20000)
        {
            TestContext.Out.WriteLine($"Waiting for branch '{branchName}' to be removed from DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                if (!await BranchExistsInDom(branchName))
                {
                    TestContext.Out.WriteLine($"Branch '{branchName}' removed from DOM");
                    return true;
                }
                await Actions.Wait500();
            }
            TestContext.Out.WriteLine($"Branch '{branchName}' still in DOM after {timeoutMs / 1000}s");
            return false;
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

        private async Task WaitForChatInputEnabled()
        {
            var maxRetries = 3;

            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                var isEnabled = await WaitForInputEnabled(15000);
                if (isEnabled)
                    return;

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
}
