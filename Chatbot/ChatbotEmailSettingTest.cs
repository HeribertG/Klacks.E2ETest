// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(60)]
    public class ChatbotEmailSettingTest : ChatbotTestBase
    {
        private const string SkillGetEmailSettings = "get_email_settings";
        private const string SkillUpdateEmailSettings = "update_email_settings";
        private const string SkillGetImapSettings = "get_imap_settings";
        private const string SkillUpdateImapSettings = "update_imap_settings";
        private const string SkillGetPermissions = "get_user_permissions";

        private int _messageCountBefore;

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
        public async Task Step3_ReadCurrentEmailSettings()
        {
            TestContext.Out.WriteLine("=== Step 3: Read Current Email Settings ===");
            await AssertSkillEnabled(SkillGetEmailSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen Email-Einstellungen für den Postausgang.");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with email settings");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Email settings read successfully");
        }

        [Test, Order(4)]
        public async Task Step4_UpdateSmtpSettings()
        {
            TestContext.Out.WriteLine("=== Step 4: Update SMTP Settings ===");
            await AssertSkillEnabled(SkillUpdateEmailSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Setze die SMTP-Einstellungen: Server mail.test.ch, Port 587, SSL aktiviert, " +
                "Authentifizierung LOGIN, Benutzername testuser@test.ch. Speichere sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            var mentionsUpdate = response.Contains("aktualisiert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("updated", StringComparison.OrdinalIgnoreCase)
                || response.Contains("gesetzt", StringComparison.OrdinalIgnoreCase)
                || response.Contains("geändert", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Response confirms update: {mentionsUpdate}");
            TestContext.Out.WriteLine("SMTP settings update completed");
        }

        [Test, Order(5)]
        public async Task Step5_VerifySmtpUpdate()
        {
            TestContext.Out.WriteLine("=== Step 5: Verify SMTP Update ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuelle SMTP-Konfiguration.");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with SMTP config");

            var hasServer = response.Contains("mail.test.ch", StringComparison.OrdinalIgnoreCase);
            var hasPort = response.Contains("587", StringComparison.Ordinal);

            Assert.That(hasServer, Is.True,
                $"Response should contain server 'mail.test.ch'. Got: {response}");
            Assert.That(hasPort, Is.True,
                $"Response should contain port '587'. Got: {response}");

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("SMTP update verified successfully");
        }

        [Test, Order(6)]
        public async Task Step6_ReadCurrentImapSettings()
        {
            TestContext.Out.WriteLine("=== Step 6: Read Current IMAP Settings ===");
            await AssertSkillEnabled(SkillGetImapSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen IMAP-Einstellungen für den Posteingang.");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with IMAP settings");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("IMAP settings read successfully");
        }

        [Test, Order(7)]
        public async Task Step7_UpdateImapSettings()
        {
            TestContext.Out.WriteLine("=== Step 7: Update IMAP Settings ===");
            await AssertSkillEnabled(SkillUpdateImapSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Setze die IMAP-Einstellungen: Server imap.test.ch, Port 993, SSL aktiviert, " +
                "Ordner INBOX, Poll-Intervall 60, Benutzername imapuser@test.ch. Speichere sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            var mentionsUpdate = response.Contains("aktualisiert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("updated", StringComparison.OrdinalIgnoreCase)
                || response.Contains("gesetzt", StringComparison.OrdinalIgnoreCase)
                || response.Contains("geändert", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Response confirms update: {mentionsUpdate}");
            TestContext.Out.WriteLine("IMAP settings update completed");
        }

        [Test, Order(8)]
        public async Task Step8_VerifyImapUpdate()
        {
            TestContext.Out.WriteLine("=== Step 8: Verify IMAP Update ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuelle IMAP-Konfiguration.");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with IMAP config");

            var hasServer = response.Contains("imap.test.ch", StringComparison.OrdinalIgnoreCase);
            var hasPort = response.Contains("993", StringComparison.Ordinal);
            var hasFolder = response.Contains("INBOX", StringComparison.OrdinalIgnoreCase);

            Assert.That(hasServer, Is.True,
                $"Response should contain server 'imap.test.ch'. Got: {response}");
            Assert.That(hasPort, Is.True,
                $"Response should contain port '993'. Got: {response}");
            Assert.That(hasFolder, Is.True,
                $"Response should contain folder 'INBOX'. Got: {response}");

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("IMAP update verified successfully");
        }

        [Test, Order(9)]
        public async Task Step9_ResetSmtpToDefaults()
        {
            TestContext.Out.WriteLine("=== Step 9: Reset SMTP to Defaults ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Setze die SMTP-Einstellungen zurück: Server mail.gmx.net, Port 587, " +
                "SSL aktiviert, Authentifizierung LOGIN. Speichere sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("SMTP reset to defaults completed");
        }

        [Test, Order(10)]
        public async Task Step10_ResetImapToDefaults()
        {
            TestContext.Out.WriteLine("=== Step 10: Reset IMAP to Defaults ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Setze die IMAP-Einstellungen zurück: Lösche alle Werte (Server, Port, Ordner, " +
                "Benutzername auf leer setzen, SSL deaktiviert, Poll-Intervall leer). Speichere sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("IMAP reset to defaults completed");
        }
    }
}
