using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(50)]
    public class LlmSettingsGeneralTest : PlaywrightSetup
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
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();

            var chatMessages = await Page.QuerySelectorAsync($"#{ChatMessages}");
            Assert.That(chatMessages, Is.Not.Null, "Chat messages container should be visible");

            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_AskAppName()
        {
            TestContext.Out.WriteLine("=== Step 2: Ask App Name ===");

            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie heisst die App?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");

            Assert.That(response, Is.Not.Empty, "Bot should respond with a message");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name query completed successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ChangeAppName()
        {
            TestContext.Out.WriteLine("=== Step 3: Change App Name ===");

            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze den App-Namen auf KlacksTestLLM");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");

            Assert.That(response, Is.Not.Empty, "Bot should respond with a confirmation");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name change request completed");
        }

        [Test]
        [Order(4)]
        public async Task Step4_VerifyChangedName()
        {
            TestContext.Out.WriteLine("=== Step 4: Verify Changed App Name ===");

            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie heisst die App jetzt?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");

            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var containsNewName = response.Contains("KlacksTestLLM", StringComparison.OrdinalIgnoreCase);
            Assert.That(containsNewName, Is.True,
                $"Response should contain 'KlacksTestLLM'. Got: {response}");

            TestContext.Out.WriteLine("App name verification successful");
        }

        [Test]
        [Order(5)]
        public async Task Step5_ResetAppName()
        {
            TestContext.Out.WriteLine("=== Step 5: Reset App Name ===");

            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze den App-Namen auf Klacks");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");

            Assert.That(response, Is.Not.Empty, "Bot should respond with a confirmation");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name reset completed");
        }

        [Test]
        [Order(6)]
        public async Task Step6_AskPermissions()
        {
            TestContext.Out.WriteLine("=== Step 6: Ask Permissions ===");

            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");

            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Permissions query completed");
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
