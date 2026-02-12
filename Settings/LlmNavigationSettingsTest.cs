using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(66)]
    public class LlmNavigationSettingsTest : PlaywrightSetup
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
        public async Task Step2_NavigateToSettings()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Navigate to Settings via Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zu den Einstellungen");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");
            Assert.That(currentUrl, Does.Contain("settings"),
                $"URL should contain 'settings'. Got: {currentUrl}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to settings successful");
        }

        [Test]
        [Order(3)]
        public async Task Step3_NavigateToEmployees()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Navigate to Employees via Chat ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zur Mitarbeiterliste");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");
            Assert.That(
                currentUrl.Contains("employee", StringComparison.OrdinalIgnoreCase)
                || currentUrl.Contains("client", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"URL should contain 'employee' or 'client'. Got: {currentUrl}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to employees successful");
        }

        [Test]
        [Order(4)]
        public async Task Step4_NavigateToSchedule()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Navigate to Schedule via Chat ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zum Einsatzplan");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");
            Assert.That(
                currentUrl.Contains("schedule", StringComparison.OrdinalIgnoreCase)
                || currentUrl.Contains("work", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"URL should contain 'schedule' or 'work'. Got: {currentUrl}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to schedule successful");
        }

        [Test]
        [Order(5)]
        public async Task Step5_NavigateToShifts()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Navigate to Shifts via Chat ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zur Schichtplanung");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");
            Assert.That(
                currentUrl.Contains("shift", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"URL should contain 'shift'. Got: {currentUrl}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to shifts successful");
        }

        [Test]
        [Order(6)]
        public async Task Step6_NavigateToAbsences()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Navigate to Absences via Chat ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zu den Abwesenheiten");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");
            Assert.That(
                currentUrl.Contains("absence", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"URL should contain 'absence'. Got: {currentUrl}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to absences successful");
        }

        [Test]
        [Order(7)]
        public async Task Step7_NavigateBackToSettings()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Navigate Back to Settings via Chat ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Gehe zurueck zu den Einstellungen");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");
            Assert.That(currentUrl, Does.Contain("settings"),
                $"URL should contain 'settings'. Got: {currentUrl}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation back to settings successful");
        }

        [Test]
        [Order(8)]
        public async Task Step8_NavigateToDashboard()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Navigate to Dashboard via Chat ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zum Dashboard");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");
            Assert.That(
                currentUrl.Contains("dashboard", StringComparison.OrdinalIgnoreCase)
                || currentUrl.EndsWith("/"),
                Is.True,
                $"URL should contain 'dashboard' or be root. Got: {currentUrl}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to dashboard successful");
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
