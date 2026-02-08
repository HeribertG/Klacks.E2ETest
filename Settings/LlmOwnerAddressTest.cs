using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(51)]
    public class LlmOwnerAddressTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;
        private string _lastBotResponse = "";

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

            TestContext.Out.WriteLine("Admin rights verified successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ReadCurrentAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Read Current Address ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie lautet die aktuelle Firmenadresse?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address info");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Current address read successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_SetInvalidAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Set Invalid Address - LLM should detect and reject ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze die Firmenadresse auf Bahnhofstrasse 10, 3011 Bern, Schweiz. Der Firmenname ist Klacks AG.");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var mentionsValidation = response.Contains("nicht", StringComparison.OrdinalIgnoreCase)
                || response.Contains("ungültig", StringComparison.OrdinalIgnoreCase)
                || response.Contains("gefunden", StringComparison.OrdinalIgnoreCase)
                || response.Contains("prüf", StringComparison.OrdinalIgnoreCase)
                || response.Contains("validier", StringComparison.OrdinalIgnoreCase)
                || response.Contains("existiert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("verifiz", StringComparison.OrdinalIgnoreCase)
                || response.Contains("fehlt", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Response mentions validation issue: {mentionsValidation}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Invalid address test completed - LLM should have validated via internet");
        }

        [Test]
        [Order(5)]
        public async Task Step5_SetValidAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Set Valid Address - validate and save ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Verwende stattdessen: Klacks AG, Bundesplatz 3, 3005 Bern, Schweiz, " +
                "E-Mail info@klacks.ch, Telefon 031 123 45 67. " +
                "Bitte validiere die Adresse über das Internet und speichere sie sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            _lastBotResponse = response;
            TestContext.Out.WriteLine("Valid address provided and save requested");
        }

        [Test]
        [Order(6)]
        public async Task Step6_ConfirmSaveIfNeeded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Confirm save if LLM asked for confirmation ===");
            await EnsureChatOpen();

            var alreadySaved = _lastBotResponse.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)
                || _lastBotResponse.Contains("aktualisiert", StringComparison.OrdinalIgnoreCase)
                || _lastBotResponse.Contains("erfolgreich", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Already saved in Step5: {alreadySaved}");

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                alreadySaved
                    ? "Zeige mir nochmal die gespeicherte Firmenadresse mit Kanton und Land."
                    : "Ja, bitte speichere die Adresse jetzt mit allen Angaben inklusive Kanton und Land.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            var mentionsSaved = response.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("aktualisiert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Kanton", StringComparison.OrdinalIgnoreCase)
                || response.Contains("BE", StringComparison.Ordinal)
                || response.Contains("Bern", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Response confirms save/address: {mentionsSaved}");
            TestContext.Out.WriteLine("Save confirmation step completed");
        }

        [Test]
        [Order(7)]
        public async Task Step7_VerifyAddressOnSettingsPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Verify Address on Settings Page ===");

            await CloseChatIfOpen();
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            // Assert
            var nameInput = await Page.QuerySelectorAsync("#setting-owner-address-name") as IElementHandle;
            Assert.That(nameInput, Is.Not.Null, "Address name input should exist");

            var streetInput = await Page.QuerySelectorAsync("#setting-owner-address-street") as IElementHandle;
            var zipInput = await Page.QuerySelectorAsync("#setting-owner-address-zip") as IElementHandle;
            var cityInput = await Page.QuerySelectorAsync("#setting-owner-address-city") as IElementHandle;
            var countrySelect = await Page.QuerySelectorAsync("#setting-owner-address-country") as IElementHandle;
            var stateSelect = await Page.QuerySelectorAsync("#setting-owner-address-state") as IElementHandle;

            var nameValue = nameInput != null ? await nameInput.InputValueAsync() : "";
            var streetValue = streetInput != null ? await streetInput.InputValueAsync() : "";
            var zipValue = zipInput != null ? await zipInput.InputValueAsync() : "";
            var cityValue = cityInput != null ? await cityInput.InputValueAsync() : "";
            var countryValue = countrySelect != null ? await countrySelect.InputValueAsync() : "";
            var stateValue = stateSelect != null ? await stateSelect.InputValueAsync() : "";

            TestContext.Out.WriteLine($"Name: {nameValue}");
            TestContext.Out.WriteLine($"Street: {streetValue}");
            TestContext.Out.WriteLine($"ZIP: {zipValue}");
            TestContext.Out.WriteLine($"City: {cityValue}");
            TestContext.Out.WriteLine($"Country: {countryValue}");
            TestContext.Out.WriteLine($"State: {stateValue}");

            Assert.That(countryValue, Is.Not.Empty, "Country MUST be set");
            Assert.That(stateValue, Is.Not.Empty, "State/Canton MUST be set");

            TestContext.Out.WriteLine("Address verified - State and Country are set");
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
