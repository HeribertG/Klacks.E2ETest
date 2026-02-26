// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;
using Klacks.E2ETest.Constants;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(58)]
    public class ChatbotUserAdministrationTest : ChatbotTestBase
    {
        private const string SkillGetPermissions = "get_user_permissions";
        private const string SkillCreateEmployee = "create_employee";
        private const string SkillSearchEmployees = "search_employees";

        private const string CssUserRowName = "input[id^='user-admin-row-name-']";
        private const string UserRowNamePrefix = "user-admin-row-name-";

        private const int CreateTimeoutMs = 120000;
        private const int WaitDomTimeoutMs = 30000;
        private const int MaxRetries = 3;
        private const int MaxDeleteRetries = 5;

        private static string _timestamp = "";
        private static (string FirstName, string LastName, string Email)[] _testUsers = Array.Empty<(string, string, string)>();
        private static readonly List<string> CreatedUserIds = new();

        private int _messageCountBefore;

        [Test, Order(1)]
        public async Task Step1_OpenChat()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");
            _timestamp = DateTime.Now.Ticks.ToString()[10..16];
            _testUsers =
            [
                ($"Anna{_timestamp}", "Testerin", $"anna.{_timestamp}@klacks-test.ch"),
                ($"Marco{_timestamp}", "Beispiel", $"marco.{_timestamp}@klacks-test.ch"),
                ($"Lisa{_timestamp}", "Muster", $"lisa.{_timestamp}@klacks-test.ch")
            ];
            TestContext.Out.WriteLine($"Timestamp: {_timestamp}");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test, Order(2)]
        public async Task Step2_VerifyPermissions()
        {
            TestContext.Out.WriteLine("=== Step 2: Verify User Management Permissions ===");
            await AssertSkillEnabled(SkillGetPermissions);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich? Darf ich Benutzer verwalten?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"User must have Admin rights. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Permissions verified successfully");
        }

        [Test, Order(3)]
        public async Task Step3_CreateFirstUserViaChat()
        {
            TestContext.Out.WriteLine("=== Step 3: Create First User via LLM Chat (UI) ===");
            await AssertSkillEnabled(SkillCreateEmployee);
            var user = _testUsers[0];

            var userId = await CreateUserWithRetry(user.FirstName, user.LastName, user.Email);
            CreatedUserIds.Add(userId);

            TestContext.Out.WriteLine($"First user created via UI: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test, Order(4)]
        public async Task Step4_CreateSecondUserViaChat()
        {
            TestContext.Out.WriteLine("=== Step 4: Create Second User via LLM Chat (UI) ===");
            var user = _testUsers[1];

            var userId = await CreateUserWithRetry(user.FirstName, user.LastName, user.Email);
            CreatedUserIds.Add(userId);

            TestContext.Out.WriteLine($"Second user created via UI: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test, Order(5)]
        public async Task Step5_CreateThirdUserViaChat()
        {
            TestContext.Out.WriteLine("=== Step 5: Create Third User via LLM Chat (UI) ===");
            var user = _testUsers[2];

            var userId = await CreateUserWithRetry(user.FirstName, user.LastName, user.Email);
            CreatedUserIds.Add(userId);

            TestContext.Out.WriteLine($"Third user created via UI: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test, Order(6)]
        public async Task Step6_VerifyAllUsersViaChat()
        {
            TestContext.Out.WriteLine("=== Step 6: Verify All Users via LLM Chat (UI) ===");
            await AssertSkillEnabled(SkillSearchEmployees);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle System-Benutzer auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            var allFound = true;
            foreach (var user in _testUsers)
            {
                var fullName = $"{user.FirstName} {user.LastName}";
                var existsInDom = await UserExistsInDom(user.FirstName, user.LastName);
                TestContext.Out.WriteLine($"  User '{fullName}': {(existsInDom ? "FOUND in DOM" : "NOT FOUND in DOM")}");
                if (!existsInDom)
                    allFound = false;
            }

            Assert.That(allFound, Is.True, $"All {_testUsers.Length} test users should be visible in Settings DOM");

            TestContext.Out.WriteLine("All test users verified in DOM");
        }

        [Test, Order(7)]
        public async Task Step7_DeleteCreatedUsersViaChat()
        {
            TestContext.Out.WriteLine("=== Step 7: Delete Created Users via LLM Chat (UI) ===");

            if (_testUsers.Length == 0)
            {
                TestContext.Out.WriteLine("No users to delete - skipping");
                Assert.Inconclusive("No users were created in previous steps");
                return;
            }

            foreach (var user in _testUsers)
            {
                await DeleteUserWithRetry(user.FirstName, user.LastName);
                await Actions.Wait2000();
            }

            TestContext.Out.WriteLine($"All {_testUsers.Length} test users deleted via UI");
            CreatedUserIds.Clear();
        }

        [Test, Order(8)]
        public async Task Step8_VerifyUsersDeletedViaChat()
        {
            TestContext.Out.WriteLine("=== Step 8: Verify Users Deleted ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle System-Benutzer auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            foreach (var user in _testUsers)
            {
                var fullName = $"{user.FirstName} {user.LastName}";
                var stillExists = await UserExistsInDom(user.FirstName, user.LastName);
                TestContext.Out.WriteLine($"  User '{fullName}': {(stillExists ? "STILL EXISTS" : "DELETED")}");
                Assert.That(stillExists, Is.False, $"User '{fullName}' should no longer exist in DOM");
            }

            TestContext.Out.WriteLine("All test users confirmed deleted");
        }

        private async Task<string> CreateUserWithRetry(string firstName, string lastName, string email)
        {
            var fullName = $"{firstName} {lastName}";

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                TestContext.Out.WriteLine($"Create user attempt {attempt}/{MaxRetries}: {fullName}");
                await EnsureChatOpen();
                await ClearChatAndWait();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Erstelle einen neuen Systembenutzer: Vorname '{firstName}', Nachname '{lastName}', Email '{email}'");
                var response = await WaitForBotResponse(_messageCountBefore, CreateTimeoutMs);
                TestContext.Out.WriteLine($"Bot response ({response.Length} chars): {response[..Math.Min(200, response.Length)]}");

                await Task.Delay(5000);

                var userId = await WaitForUserInDom(firstName, lastName);
                if (!string.IsNullOrEmpty(userId))
                {
                    if (TestListener.HasApiErrors())
                        TestContext.Out.WriteLine($"Warning: API error during creation (user still created): {TestListener.GetLastErrorMessage()}");
                    return userId;
                }

                TestContext.Out.WriteLine($"User not found in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"User '{fullName}' was not created after {MaxRetries} attempts");
            return string.Empty;
        }

        private async Task DeleteUserWithRetry(string firstName, string lastName)
        {
            var fullName = $"{firstName} {lastName}";

            for (var attempt = 1; attempt <= MaxDeleteRetries; attempt++)
            {
                TestContext.Out.WriteLine($"Delete user attempt {attempt}/{MaxDeleteRetries}: {fullName}");
                await EnsureChatOpen();
                await ClearChatAndWait();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Lösche den Systembenutzer mit Vorname '{firstName}' und Nachname '{lastName}'");
                var response = await WaitForBotResponse(_messageCountBefore, 90000);
                TestContext.Out.WriteLine($"Delete response: {response[..Math.Min(200, response.Length)]}");

                var removed = await WaitForUserRemovedFromDom(firstName, lastName);
                if (removed)
                {
                    TestContext.Out.WriteLine($"User '{fullName}' confirmed removed from DOM");
                    return;
                }

                TestContext.Out.WriteLine($"User '{fullName}' still in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"User '{fullName}' was not deleted after {MaxDeleteRetries} attempts. LLM may have chosen list_system_users instead of delete_system_user.");
        }

        private async Task<string> WaitForUserInDom(string firstName, string lastName)
        {
            var fullName = $"{firstName} {lastName}";
            TestContext.Out.WriteLine($"Waiting for user '{fullName}' to appear in DOM...");

            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < WaitDomTimeoutMs)
            {
                var inputs = await Actions.QuerySelectorAll(CssUserRowName);
                foreach (var input in inputs)
                {
                    var value = await input.InputValueAsync();
                    if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                    {
                        var id = await input.GetAttributeAsync("id");
                        var userId = id?.Replace(UserRowNamePrefix, "") ?? "";
                        TestContext.Out.WriteLine($"User '{fullName}' found in DOM with ID: {userId}");
                        return userId;
                    }
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"User '{fullName}' NOT found in DOM after {WaitDomTimeoutMs / 1000}s");
            return "";
        }

        private async Task<bool> UserExistsInDom(string firstName, string lastName)
        {
            var fullName = $"{firstName} {lastName}";
            var inputs = await Actions.QuerySelectorAll(CssUserRowName);
            foreach (var input in inputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private async Task<bool> WaitForUserRemovedFromDom(string firstName, string lastName)
        {
            var fullName = $"{firstName} {lastName}";
            TestContext.Out.WriteLine($"Waiting for user '{fullName}' to be removed from DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < WaitDomTimeoutMs)
            {
                var userAdminSection = await Actions.FindElementById(SettingsUserAdministrationIds.UserAdminSection);
                if (userAdminSection == null)
                {
                    TestContext.Out.WriteLine("Settings closed during delete wait, reopening...");
                    await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
                    await Actions.Wait2000();
                    continue;
                }

                if (!await UserExistsInDom(firstName, lastName))
                {
                    TestContext.Out.WriteLine($"User '{fullName}' removed from DOM");
                    return true;
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"User '{fullName}' still in DOM after {WaitDomTimeoutMs / 1000}s");
            return false;
        }
    }
}
