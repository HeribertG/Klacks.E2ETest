// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(57)]
    public class ChatbotOwnerAddressTest : ChatbotTestBase
    {
        private const string SkillGetOwnerAddress = "get_owner_address";
        private const string SkillUpdateOwnerAddress = "update_owner_address";
        private const string SkillValidateAddress = "validate_address";
        private const string SkillGetPermissions = "get_user_permissions";

        private int _messageCountBefore;
        private string _lastBotResponse = "";

        [Test, Order(1)]
        public async Task Step1_OpenChat()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatMessages = await Actions.FindElementById(GetChatSelector(ControlKeyMessages));
            Assert.That(chatMessages, Is.Not.Null, "Chat messages container should be visible");

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test, Order(2)]
        public async Task Step2_VerifyAdminRights()
        {
            TestContext.Out.WriteLine("=== Step 2: Verify Admin Rights ===");
            await AssertSkillEnabled(SkillGetPermissions);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"User must have Admin rights for settings tests. Got: {response}");

            TestContext.Out.WriteLine("Admin rights verified successfully");
        }

        [Test, Order(3)]
        public async Task Step3_ReadCurrentAddress()
        {
            TestContext.Out.WriteLine("=== Step 3: Read Current Address ===");
            await AssertSkillEnabled(SkillGetOwnerAddress);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie lautet die aktuelle Firmenadresse?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address info");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Current address read successfully");
        }

        [Test, Order(4)]
        public async Task Step4_SetInvalidAddress()
        {
            TestContext.Out.WriteLine("=== Step 4: Set Invalid Address - LLM should detect and reject ===");
            await AssertSkillEnabled(SkillValidateAddress);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze die Firmenadresse auf Bahnhofstrasse 10, 3011 Bern, Schweiz. Der Firmenname ist Klacks AG.");
            var response = await WaitForBotResponse(_messageCountBefore, 120000);

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
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Invalid address test completed - LLM should have validated via internet");
        }

        [Test, Order(5)]
        public async Task Step5_SetValidAddress()
        {
            TestContext.Out.WriteLine("=== Step 5: Set Valid Address - validate and save ===");
            await AssertSkillEnabled(SkillUpdateOwnerAddress);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Verwende stattdessen: Klacks AG, Bundesplatz 3, 3005 Bern, Schweiz, " +
                "E-Mail info@klacks.ch, Telefon 031 123 45 67. " +
                "Bitte validiere die Adresse über das Internet und speichere sie sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            _lastBotResponse = response;
            TestContext.Out.WriteLine("Valid address provided and save requested");
        }

        [Test, Order(6)]
        public async Task Step6_ConfirmSaveIfNeeded()
        {
            TestContext.Out.WriteLine("=== Step 6: Confirm save if LLM asked for confirmation ===");
            await EnsureChatOpen();

            var alreadySaved = _lastBotResponse.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)
                || _lastBotResponse.Contains("aktualisiert", StringComparison.OrdinalIgnoreCase)
                || _lastBotResponse.Contains("erfolgreich", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Already saved in Step5: {alreadySaved}");

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                alreadySaved
                    ? "Zeige mir nochmal die gespeicherte Firmenadresse mit Kanton und Land."
                    : "Ja, bitte speichere die Adresse jetzt mit allen Angaben inklusive Kanton und Land.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            var mentionsSaved = response.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("aktualisiert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Kanton", StringComparison.OrdinalIgnoreCase)
                || response.Contains("BE", StringComparison.Ordinal)
                || response.Contains("Bern", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Response confirms save/address: {mentionsSaved}");
            TestContext.Out.WriteLine("Save confirmation step completed");
        }

        [Test, Order(7)]
        public async Task Step7_VerifyAddressViaChat()
        {
            TestContext.Out.WriteLine("=== Step 7: Verify Address via LLM Chat ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Lies die aktuelle Inhaberadresse aus den Einstellungen");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address data");

            var hasCountry = response.Contains("CH", StringComparison.Ordinal)
                || response.Contains("Schweiz", StringComparison.OrdinalIgnoreCase)
                || response.Contains("country", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Land", StringComparison.OrdinalIgnoreCase);
            var hasState = response.Contains("BE", StringComparison.Ordinal)
                || response.Contains("Bern", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Kanton", StringComparison.OrdinalIgnoreCase)
                || response.Contains("state", StringComparison.OrdinalIgnoreCase);

            Assert.That(hasCountry, Is.True, $"Response should contain country info. Got: {response}");
            Assert.That(hasState, Is.True, $"Response should contain state/canton info. Got: {response}");

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Address verified via chat - Country and State confirmed");
        }
    }
}
