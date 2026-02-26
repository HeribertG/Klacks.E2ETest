// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(56)]
    public class ChatbotSystemInfoPermissionsTest : ChatbotTestBase
    {
        private const string SkillGetSystemInfo = "get_system_info";
        private const string SkillGetPermissions = "get_user_permissions";
        private const string SkillGetUserContext = "get_user_context";
        private const string SkillNavigateTo = "navigate_to";

        private int _messageCountBefore;

        [Test, Order(1)]
        public async Task Step1_OpenChat()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test, Order(2)]
        public async Task Step2_GetSystemInfo()
        {
            TestContext.Out.WriteLine("=== Step 2: Get System Info ===");
            await AssertSkillEnabled(SkillGetSystemInfo);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die Systeminformationen");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return system info");
            Assert.That(
                response.Contains("Version", StringComparison.OrdinalIgnoreCase)
                || response.Contains("System", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Klacks", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain system information. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("System info retrieved successfully");
        }

        [Test, Order(3)]
        public async Task Step3_GetUserPermissions()
        {
            TestContext.Out.WriteLine("=== Step 3: Get User Permissions ===");
            await AssertSkillEnabled(SkillGetPermissions);
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Berechtigungen hat mein Benutzer?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return permissions info");
            Assert.That(
                response.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Permission", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Recht", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain permission information. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User permissions retrieved successfully");
        }

        [Test, Order(4)]
        public async Task Step4_VerifyAdminHasAllPermissions()
        {
            TestContext.Out.WriteLine("=== Step 4: Verify Admin Has All Permissions ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Bin ich ein Administrator? Darf ich Einstellungen aendern, Mitarbeiter erstellen und Filialen verwalten?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm admin status");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should confirm Admin status. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Admin permissions verified successfully");
        }

        [Test, Order(5)]
        public async Task Step5_GetCurrentUser()
        {
            TestContext.Out.WriteLine("=== Step 5: Get Current User Info ===");
            await AssertSkillEnabled(SkillGetUserContext);
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wer bin ich? Zeige mir meine Benutzerinformationen.");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return current user info");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Current user info retrieved successfully");
        }

        [Test, Order(6)]
        public async Task Step6_AskAboutSpecificPermission()
        {
            TestContext.Out.WriteLine("=== Step 6: Ask About Specific Permission ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Darf ich die KI-Einstellungen bearbeiten? Habe ich die Berechtigung 'CanEditSettings'?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond about specific permission");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Specific permission query completed");
        }

        [Test, Order(7)]
        public async Task Step7_GetSystemInfoAfterNavigation()
        {
            TestContext.Out.WriteLine("=== Step 7: Get System Info After Navigating to Settings ===");
            await AssertSkillEnabled(SkillNavigateTo);
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zu den Einstellungen und zeige mir dann die Systeminformationen");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with system info");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("System info after navigation retrieved successfully");
        }

        [Test, Order(8)]
        public async Task Step8_CombinedInfoQuery()
        {
            TestContext.Out.WriteLine("=== Step 8: Combined System and User Query ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Gib mir eine Zusammenfassung: Welches System laeuft hier, welche Version, und welche Rolle habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should provide combined system and user info");
            Assert.That(
                response.Contains("Klacks", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Version", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain system or role info. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Combined info query completed successfully");
        }
    }
}
