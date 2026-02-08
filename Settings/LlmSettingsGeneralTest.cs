using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;
using static Klacks.E2ETest.Constants.SettingsGeneralIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(50)]
    public class LlmSettingsGeneralTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;
        private const string IconFilePath = @"C:\Users\hgasp\OneDrive\Bilder\Baustelle-mittel.ico";
        private const string LogoFilePath = @"C:\Users\hgasp\OneDrive\Bilder\Baustelle-mittel.png";

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
            var chatMessages = await Page.QuerySelectorAsync($"#{ChatMessages}");
            Assert.That(chatMessages, Is.Not.Null, "Chat messages container should be visible");

            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_VerifyAdminRights()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Verify Admin Rights ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"User must have Admin rights for settings tests. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Admin rights verified successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_AskAppName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Ask App Name ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie heisst die App?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with a message");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name query completed successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_ChangeAppName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Change App Name ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze den App-Namen auf KlacksTestLLM");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with a confirmation");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name change request completed");
        }

        [Test]
        [Order(5)]
        public async Task Step5_VerifyChangedName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Verify Changed App Name ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie heisst die App jetzt?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(response.Contains("KlacksTestLLM", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should contain 'KlacksTestLLM'. Got: {response}");

            TestContext.Out.WriteLine("App name verification successful");
        }

        [Test]
        [Order(6)]
        public async Task Step6_ResetAppName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Reset App Name ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze den App-Namen auf Klacks");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with a confirmation");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name reset completed");
        }

        [Test]
        [Order(7)]
        public async Task Step7_UploadIcon()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Upload Icon ===");

            await CloseChatIfOpen();
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Act
            var deleteIconButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteIconBtn}");
            if (deleteIconButton != null)
            {
                TestContext.Out.WriteLine("Icon exists - deleting it first");
                await deleteIconButton.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            TestContext.Out.WriteLine($"Uploading icon from: {IconFilePath}");
            var iconFileInput = await Page.QuerySelectorAsync($"#{SettingGeneralIconFileInput}");
            Assert.That(iconFileInput, Is.Not.Null, "Icon file input should be available");

            await iconFileInput!.SetInputFilesAsync(IconFilePath);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            // Assert
            deleteIconButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteIconBtn}");
            Assert.That(deleteIconButton, Is.Not.Null, "Delete icon button should be visible after upload");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur during icon upload. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Icon uploaded successfully");
        }

        [Test]
        [Order(8)]
        public async Task Step8_UploadLogo()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Upload Logo ===");

            var settingsForm = await Page.QuerySelectorAsync("#settings-general-form");
            if (settingsForm == null)
            {
                await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            // Act
            var deleteLogoButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteLogoBtn}");
            if (deleteLogoButton != null)
            {
                TestContext.Out.WriteLine("Logo exists - deleting it first");
                await deleteLogoButton.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            TestContext.Out.WriteLine($"Uploading logo from: {LogoFilePath}");
            var logoFileInput = await Page.QuerySelectorAsync($"#{SettingGeneralLogoFileInput}");
            Assert.That(logoFileInput, Is.Not.Null, "Logo file input should be available");

            await logoFileInput!.SetInputFilesAsync(LogoFilePath);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            // Assert
            deleteLogoButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteLogoBtn}");
            Assert.That(deleteLogoButton, Is.Not.Null, "Delete logo button should be visible after upload");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur during logo upload. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Logo uploaded successfully");
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

        private async Task CloseChatIfOpen()
        {
            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
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
