using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(65)]
    public class LlmAiGuidelinesTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;

        private const string TestGuideline = "Antworte immer hoeflich und verwende die Anrede 'Sie'. Gib bei Fehlern immer eine Loesung vor.";
        private const string ResetGuideline = "Sei ein hilfreicher Assistent.";

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
        public async Task Step2_GetCurrentGuidelines()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Get Current AI Guidelines ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen KI-Richtlinien");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return current guidelines");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Current guidelines retrieved successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_UpdateGuidelines()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Update AI Guidelines ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Aktualisiere die KI-Richtlinien auf: {TestGuideline}");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm guidelines update");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI guidelines updated successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_VerifyUpdatedGuidelines()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Updated AI Guidelines ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen KI-Richtlinien");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return updated guidelines");
            Assert.That(
                response.Contains("hoeflich", StringComparison.OrdinalIgnoreCase)
                || response.Contains("höflich", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Sie", StringComparison.Ordinal),
                Is.True,
                $"Response should contain updated guideline text. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Updated guidelines verified successfully");
        }

        [Test]
        [Order(5)]
        public async Task Step5_UpdateGuidelinesAgain()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Update Guidelines with Different Text ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze die KI-Richtlinien auf: Verwende kurze Saetze. Keine Emojis. Antworte auf Deutsch.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm second guidelines update");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Guidelines updated again successfully");
        }

        [Test]
        [Order(6)]
        public async Task Step6_VerifySecondUpdate()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Verify Second Guidelines Update ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Was sind die aktuellen KI-Richtlinien?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return current guidelines");
            Assert.That(
                response.Contains("kurze", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Deutsch", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Emojis", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain second update text. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Second guidelines update verified");
        }

        [Test]
        [Order(7)]
        public async Task Step7_ResetGuidelines()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Reset AI Guidelines ===");
            await EnsureChatOpen();

            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();
            await WaitForChatInputEnabled();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze die KI-Richtlinien zurueck auf: {ResetGuideline}");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm guidelines reset");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI guidelines reset successfully");
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
