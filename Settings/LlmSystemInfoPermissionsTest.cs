using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(67)]
    public class LlmSystemInfoPermissionsTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;

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
        public async Task Step2_GetSystemInfo()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Get System Info ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die Systeminformationen");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return system info");
            Assert.That(
                response.Contains("Version", StringComparison.OrdinalIgnoreCase)
                || response.Contains("System", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Klacks", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain system information. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("System info retrieved successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_GetUserPermissions()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Get User Permissions ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Berechtigungen hat mein Benutzer?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return permissions info");
            Assert.That(
                response.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Permission", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Recht", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain permission information. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User permissions retrieved successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_VerifyAdminHasAllPermissions()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Admin Has All Permissions ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Bin ich ein Administrator? Darf ich Einstellungen aendern, Mitarbeiter erstellen und Filialen verwalten?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm admin status");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should confirm Admin status. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Admin permissions verified successfully");
        }

        [Test]
        [Order(5)]
        public async Task Step5_GetCurrentUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Get Current User Info ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wer bin ich? Zeige mir meine Benutzerinformationen.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return current user info");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Current user info retrieved successfully");
        }

        [Test]
        [Order(6)]
        public async Task Step6_AskAboutSpecificPermission()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Ask About Specific Permission ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Darf ich die KI-Einstellungen bearbeiten? Habe ich die Berechtigung 'CanEditSettings'?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond about specific permission");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Specific permission query completed");
        }

        [Test]
        [Order(7)]
        public async Task Step7_GetSystemInfoAfterNavigation()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Get System Info After Navigating to Settings ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zu den Einstellungen und zeige mir dann die Systeminformationen");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with system info");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("System info after navigation retrieved successfully");
        }

        [Test]
        [Order(8)]
        public async Task Step8_CombinedInfoQuery()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Combined System and User Query ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Gib mir eine Zusammenfassung: Welches System laeuft hier, welche Version, und welche Rolle habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should provide combined system and user info");
            Assert.That(
                response.Contains("Klacks", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Version", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain system or role info. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Combined info query completed successfully");
        }

        #region Helper Methods

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
