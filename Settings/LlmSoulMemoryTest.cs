using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(60)]
    public class LlmSoulMemoryTest : PlaywrightSetup
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
            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_SetAiSoul()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Set AI Soul ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze die KI-Persoenlichkeit auf: Du bist Klacks, ein freundlicher Planungsassistent. Du sprichst locker aber professionell.");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm soul update");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI soul set successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_GetAiSoul()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Get AI Soul ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuelle KI-Persoenlichkeit");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return the soul");
            Assert.That(response.Contains("Klacks", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should contain 'Klacks'. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI soul retrieved successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_AddMemory()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Add AI Memory ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Merke dir: Der Hauptadministrator heisst Herbert und bevorzugt die deutsche Sprache.");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm memory was added");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI memory added successfully");
        }

        [Test]
        [Order(5)]
        public async Task Step5_GetMemories()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Get AI Memories ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige alle gespeicherten Erinnerungen");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return memories");
            Assert.That(response.Contains("Herbert", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should mention 'Herbert'. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI memories retrieved successfully");
        }

        [Test]
        [Order(6)]
        public async Task Step6_AddSecondMemory()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Add Second Memory ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Merke dir auch: Das Unternehmen hat 50 Mitarbeiter und sitzt in Bern.");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm second memory was added");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Second AI memory added successfully");
        }

        [Test]
        [Order(7)]
        public async Task Step7_VerifyMultipleMemories()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Verify Multiple Memories ===");
            await EnsureChatOpen();
            await Actions.ClickButtonById(ChatClearBtn);
            await Actions.Wait1000();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie viele Erinnerungen hast du gespeichert? Liste sie auf.");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should list memories");
            Assert.That(response.Contains("Herbert", StringComparison.OrdinalIgnoreCase)
                        || response.Contains("Bern", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should mention stored facts. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Multiple memories verified successfully");
        }

        [Test]
        [Order(8)]
        public async Task Step8_ResetSoul()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Reset AI Soul ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze die KI-Persoenlichkeit auf: Du bist ein hilfreicher KI-Assistent.");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm soul reset");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI soul reset successfully");
        }

        #region Helper Methods

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

            await Actions.ClickButtonById(ChatSendBtn);
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
