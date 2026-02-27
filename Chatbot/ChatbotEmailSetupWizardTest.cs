// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(61)]
    public class ChatbotEmailSetupWizardTest : ChatbotTestBase
    {
        private const string SkillGetPermissions = "get_user_permissions";
        private const string SkillWebSearch = "web_search";
        private const string SkillUpdateEmailSettings = "update_email_settings";
        private const string SkillUpdateImapSettings = "update_imap_settings";
        private const string SkillGetEmailSettings = "get_email_settings";
        private const string SkillGetImapSettings = "get_imap_settings";
        private const string SkillTestSmtp = "test_smtp_connection";
        private const string SkillTestImap = "test_imap_connection";

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
                $"User must have Admin rights for email setup tests. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Admin rights verified successfully");
        }

        [Test, Order(3)]
        public async Task Step3_RequestEmailSetup()
        {
            TestContext.Out.WriteLine("=== Step 3: Request Email Setup for GMX ===");
            await AssertSkillEnabled(SkillUpdateEmailSettings);
            await AssertSkillEnabled(SkillUpdateImapSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Richte bitte die Email-Einstellungen ein fuer test@gmx.ch. " +
                "Konfiguriere SMTP und IMAP. Speichere sofort, frage nicht nach Bestätigung.");
            var response = await WaitForBotResponse(_messageCountBefore, 120000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with setup progress");

            var mentionsGmx = response.Contains("gmx", StringComparison.OrdinalIgnoreCase);
            var mentionsConfig = response.Contains("konfigur", StringComparison.OrdinalIgnoreCase)
                || response.Contains("eingest", StringComparison.OrdinalIgnoreCase)
                || response.Contains("gesetzt", StringComparison.OrdinalIgnoreCase)
                || response.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("smtp", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Mentions GMX: {mentionsGmx}, Mentions config: {mentionsConfig}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Email setup request completed");
        }

        [Test, Order(4)]
        public async Task Step4_VerifySmtpConfigured()
        {
            TestContext.Out.WriteLine("=== Step 4: Verify SMTP Settings Configured ===");
            await AssertSkillEnabled(SkillGetEmailSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen SMTP-Einstellungen.");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with SMTP settings");

            var hasGmxServer = response.Contains("gmx", StringComparison.OrdinalIgnoreCase);
            var hasSmtpPort = response.Contains("587", StringComparison.Ordinal)
                || response.Contains("465", StringComparison.Ordinal);

            Assert.That(hasGmxServer, Is.True,
                $"SMTP server should contain 'gmx'. Got: {response}");
            Assert.That(hasSmtpPort, Is.True,
                $"SMTP port should be 587 or 465. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("SMTP settings verified successfully");
        }

        [Test, Order(5)]
        public async Task Step5_VerifyImapConfigured()
        {
            TestContext.Out.WriteLine("=== Step 5: Verify IMAP Settings Configured ===");
            await AssertSkillEnabled(SkillGetImapSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen IMAP-Einstellungen.");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with IMAP settings");

            var hasGmxServer = response.Contains("gmx", StringComparison.OrdinalIgnoreCase);
            var hasImapPort = response.Contains("993", StringComparison.Ordinal);

            Assert.That(hasGmxServer, Is.True,
                $"IMAP server should contain 'gmx'. Got: {response}");
            Assert.That(hasImapPort, Is.True,
                $"IMAP port should be 993. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("IMAP settings verified successfully");
        }

        [Test, Order(6)]
        public async Task Step6_TestSmtpConnectionExpectFailure()
        {
            TestContext.Out.WriteLine("=== Step 6: Test SMTP Connection (expect auth failure) ===");
            await AssertSkillEnabled(SkillTestSmtp);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Ich habe das Passwort eingegeben. Bitte teste jetzt die SMTP-Verbindung.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with test result");

            var mentionsError = response.Contains("fehl", StringComparison.OrdinalIgnoreCase)
                || response.Contains("error", StringComparison.OrdinalIgnoreCase)
                || response.Contains("fail", StringComparison.OrdinalIgnoreCase)
                || response.Contains("nicht", StringComparison.OrdinalIgnoreCase)
                || response.Contains("passwort", StringComparison.OrdinalIgnoreCase)
                || response.Contains("password", StringComparison.OrdinalIgnoreCase)
                || response.Contains("authentif", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Response mentions error/failure: {mentionsError}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("SMTP test completed - error expected and handled");
        }

        [Test, Order(7)]
        public async Task Step7_VerifySmtpTestErrorAnalysis()
        {
            TestContext.Out.WriteLine("=== Step 7: Verify Bot Analyzes SMTP Error ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Was genau war der Fehler beim SMTP-Test? Analysiere die Fehlermeldung und schlage eine Lösung vor.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should analyze the error");

            var providesAnalysis = response.Contains("passwort", StringComparison.OrdinalIgnoreCase)
                || response.Contains("password", StringComparison.OrdinalIgnoreCase)
                || response.Contains("authentif", StringComparison.OrdinalIgnoreCase)
                || response.Contains("SSL", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Verbindung", StringComparison.OrdinalIgnoreCase)
                || response.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || response.Contains("server", StringComparison.OrdinalIgnoreCase)
                || response.Contains("port", StringComparison.OrdinalIgnoreCase);

            Assert.That(providesAnalysis, Is.True,
                $"Bot should provide error analysis with technical details. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("SMTP error analysis completed");
        }

        [Test, Order(8)]
        public async Task Step8_TestImapConnectionExpectFailure()
        {
            TestContext.Out.WriteLine("=== Step 8: Test IMAP Connection (expect auth failure) ===");
            await AssertSkillEnabled(SkillTestImap);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Teste jetzt bitte die IMAP-Verbindung.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with IMAP test result");

            var mentionsResult = response.Contains("fehl", StringComparison.OrdinalIgnoreCase)
                || response.Contains("error", StringComparison.OrdinalIgnoreCase)
                || response.Contains("fail", StringComparison.OrdinalIgnoreCase)
                || response.Contains("nicht", StringComparison.OrdinalIgnoreCase)
                || response.Contains("passwort", StringComparison.OrdinalIgnoreCase)
                || response.Contains("password", StringComparison.OrdinalIgnoreCase)
                || response.Contains("authentif", StringComparison.OrdinalIgnoreCase)
                || response.Contains("test", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"Response mentions test result: {mentionsResult}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("IMAP test completed - error expected and handled");
        }

        [Test, Order(9)]
        public async Task Step9_ResetSmtpSettings()
        {
            TestContext.Out.WriteLine("=== Step 9: Reset SMTP Settings ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Setze die SMTP-Einstellungen zurueck: Server mail.gmx.net, Port 587, " +
                "SSL aktiviert, Authentifizierung LOGIN. Speichere sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm SMTP reset");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("SMTP settings reset completed");
        }

        [Test, Order(10)]
        public async Task Step10_ResetImapSettings()
        {
            TestContext.Out.WriteLine("=== Step 10: Reset IMAP Settings ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(
                "Setze die IMAP-Einstellungen zurueck: Loesche alle Werte (Server, Port, Ordner, " +
                "Benutzername auf leer setzen, SSL deaktiviert, Poll-Intervall leer). Speichere sofort.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm IMAP reset");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("IMAP settings reset completed");
        }
    }
}
