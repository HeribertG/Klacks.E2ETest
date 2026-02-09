using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;
using static Klacks.E2ETest.Constants.SettingsMacroIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(64)]
    public class LlmMacrosTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;

        private static readonly List<string> CreatedMacroIds = new();
        private static readonly string MacroName = $"TestMacro_{DateTime.UtcNow:yyyyMMddHHmmss}";

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
        public async Task Step2_CreateMacroWithScriptViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine($"=== Step 2: Create Macro '{MacroName}' with Script via LLM Chat (UI) ===");

            // Act
            var (macroId, response) = await CreateMacroWithRetry(MacroName);
            CreatedMacroIds.Add(macroId);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response.Contains("Syntax OK", StringComparison.OrdinalIgnoreCase)
                     || response.Contains("Script", StringComparison.OrdinalIgnoreCase)
                     || response.Contains("erstellt", StringComparison.OrdinalIgnoreCase),
                Is.True, $"Response should confirm script was created. Got: {response}");

            TestContext.Out.WriteLine($"Macro created with script via UI: {MacroName} (ID: {macroId})");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ListMacrosViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: List Macros via LLM Chat (UI) ===");
            await EnsureChatOpen();

            // Act
            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle Macros auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            // Assert
            var macroExists = await MacroExistsInDom(MacroName);
            TestContext.Out.WriteLine($"  {MacroName}: {(macroExists ? "FOUND in DOM" : "NOT FOUND in DOM")}");

            Assert.That(macroExists, Is.True, $"Macro '{MacroName}' should be visible in Settings DOM");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Macro verified in DOM");
        }

        [Test]
        [Order(4)]
        public async Task Step4_DeleteMacroViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Delete Macro via LLM Chat (UI) ===");

            if (CreatedMacroIds.Count == 0)
            {
                TestContext.Out.WriteLine("No macros to delete - skipping");
                Assert.Inconclusive("No macros were created in previous steps");
                return;
            }

            // Act
            foreach (var macroId in CreatedMacroIds.ToList())
            {
                await DeleteMacroWithRetry(macroId);
                await Actions.Wait2000();
            }

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"All {CreatedMacroIds.Count} test macros deleted via UI");
            CreatedMacroIds.Clear();
        }

        [Test]
        [Order(5)]
        public async Task Step5_VerifyMacroDeletedViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Verify Macro Deleted ===");
            await EnsureChatOpen();

            // Act
            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle Macros auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            // Assert
            var macroExists = await MacroExistsInDom(MacroName);
            TestContext.Out.WriteLine($"  {MacroName}: {(macroExists ? "STILL EXISTS" : "DELETED")}");

            Assert.That(macroExists, Is.False, $"Macro '{MacroName}' should no longer exist in DOM");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Test macro confirmed deleted");
        }

        #region Helper Methods

        private async Task<(string MacroId, string Response)> CreateMacroWithRetry(string name, int maxAttempts = 3)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                TestContext.Out.WriteLine($"Create macro attempt {attempt}/{maxAttempts}: {name}");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Erstelle ein neues Macro mit dem Namen '{name}'. " +
                    "Das Macro soll einen Wochenendzuschlag berechnen: " +
                    "Wenn der Wochentag >= 6 ist (Samstag oder Sonntag), soll der sorate-Zuschlag ausgegeben werden, sonst 0.");
                var response = await WaitForBotResponse(_messageCountBefore, 120000);
                TestContext.Out.WriteLine($"Bot response ({response.Length} chars): {response[..Math.Min(300, response.Length)]}");

                var macroId = await WaitForMacroInDom(name);
                if (!string.IsNullOrEmpty(macroId))
                {
                    Assert.That(_listener.HasApiErrors(), Is.False,
                        $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
                    return (macroId, response);
                }

                TestContext.Out.WriteLine($"Macro not found in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Macro '{name}' was not created after {maxAttempts} attempts");
            return (string.Empty, string.Empty);
        }

        private async Task DeleteMacroWithRetry(string macroId, int maxAttempts = 3)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                TestContext.Out.WriteLine($"Delete macro attempt {attempt}/{maxAttempts}: {macroId}");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Lösche das Macro mit der ID '{macroId}'");
                var response = await WaitForBotResponse(_messageCountBefore, 90000);
                TestContext.Out.WriteLine($"Delete response: {response[..Math.Min(200, response.Length)]}");

                var removed = await WaitForMacroRemovedFromDom(macroId);
                if (removed)
                {
                    TestContext.Out.WriteLine($"Macro {macroId} confirmed removed from DOM");
                    return;
                }

                TestContext.Out.WriteLine($"Macro {macroId} still in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Macro '{macroId}' was not deleted after {maxAttempts} attempts");
        }

        private async Task<string> WaitForMacroInDom(string macroName, int timeoutMs = 30000)
        {
            TestContext.Out.WriteLine($"Waiting for macro '{macroName}' to appear in DOM...");

            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                var inputs = await Page.QuerySelectorAllAsync($"input[id^=\"{RowNamePrefix}\"]");
                foreach (var input in inputs)
                {
                    var value = await input.InputValueAsync();
                    if (value.Contains(macroName, StringComparison.OrdinalIgnoreCase))
                    {
                        var id = await input.GetAttributeAsync("id");
                        var macroId = id?.Replace(RowNamePrefix, "") ?? "";
                        TestContext.Out.WriteLine($"Macro '{macroName}' found in DOM with ID: {macroId}");
                        return macroId;
                    }
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"Macro '{macroName}' NOT found in DOM after {timeoutMs / 1000}s");
            return "";
        }

        private async Task<bool> MacroExistsInDom(string macroName)
        {
            var inputs = await Page.QuerySelectorAllAsync($"input[id^=\"{RowNamePrefix}\"]");
            foreach (var input in inputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(macroName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private async Task<bool> WaitForMacroRemovedFromDom(string macroId, int timeoutMs = 20000)
        {
            TestContext.Out.WriteLine($"Waiting for macro '{macroId}' to be removed from DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                var element = await Page.QuerySelectorAsync($"#{RowNamePrefix}{macroId}");
                if (element == null)
                {
                    TestContext.Out.WriteLine($"Macro '{macroId}' removed from DOM");
                    return true;
                }
                await Actions.Wait500();
            }
            TestContext.Out.WriteLine($"Macro '{macroId}' still in DOM after {timeoutMs / 1000}s");
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
