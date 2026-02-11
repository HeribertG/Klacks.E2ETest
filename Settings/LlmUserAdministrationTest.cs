using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;
using static Klacks.E2ETest.Constants.SettingsUserAdministrationIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(62)]
    public class LlmUserAdministrationTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;

        private static string _timestamp = "";
        private static (string FirstName, string LastName, string Email)[] _testUsers = Array.Empty<(string, string, string)>();
        private static readonly List<string> CreatedUserIds = new();
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
            _timestamp = DateTime.Now.Ticks.ToString()[10..16];
            _testUsers = new[]
            {
                ($"Anna{_timestamp}", "Testerin", $"anna.{_timestamp}@klacks-test.ch"),
                ($"Marco{_timestamp}", "Beispiel", $"marco.{_timestamp}@klacks-test.ch"),
                ($"Lisa{_timestamp}", "Muster", $"lisa.{_timestamp}@klacks-test.ch")
            };
            TestContext.Out.WriteLine($"Timestamp: {_timestamp}");

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
        public async Task Step2_VerifyPermissions()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Verify User Management Permissions ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich? Darf ich Benutzer verwalten?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"User must have Admin rights. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Permissions verified successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_CreateFirstUserViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Create First User via LLM Chat (UI) ===");
            var user = _testUsers[0];

            // Act & Assert
            var userId = await CreateUserWithRetry(user.FirstName, user.LastName, user.Email);
            CreatedUserIds.Add(userId);

            TestContext.Out.WriteLine($"First user created via UI: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test]
        [Order(4)]
        public async Task Step4_CreateSecondUserViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Create Second User via LLM Chat (UI) ===");
            var user = _testUsers[1];

            // Act & Assert
            var userId = await CreateUserWithRetry(user.FirstName, user.LastName, user.Email);
            CreatedUserIds.Add(userId);

            TestContext.Out.WriteLine($"Second user created via UI: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test]
        [Order(5)]
        public async Task Step5_CreateThirdUserViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Create Third User via LLM Chat (UI) ===");
            var user = _testUsers[2];

            // Act & Assert
            var userId = await CreateUserWithRetry(user.FirstName, user.LastName, user.Email);
            CreatedUserIds.Add(userId);

            TestContext.Out.WriteLine($"Third user created via UI: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test]
        [Order(6)]
        public async Task Step6_VerifyAllUsersViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Verify All Users via LLM Chat (UI) ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle System-Benutzer auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            // Assert
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

        [Test]
        [Order(7)]
        public async Task Step7_DeleteCreatedUsersViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Delete Created Users via LLM Chat (UI) ===");

            if (_testUsers.Length == 0)
            {
                TestContext.Out.WriteLine("No users to delete - skipping");
                Assert.Inconclusive("No users were created in previous steps");
                return;
            }

            // Act
            foreach (var user in _testUsers)
            {
                await DeleteUserWithRetry(user.FirstName, user.LastName);
                await Actions.Wait2000();
            }

            // Assert
            TestContext.Out.WriteLine($"All {_testUsers.Length} test users deleted via UI");
            CreatedUserIds.Clear();
        }

        [Test]
        [Order(8)]
        public async Task Step8_VerifyUsersDeletedViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Verify Users Deleted ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle System-Benutzer auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            // Assert
            foreach (var user in _testUsers)
            {
                var fullName = $"{user.FirstName} {user.LastName}";
                var stillExists = await UserExistsInDom(user.FirstName, user.LastName);
                TestContext.Out.WriteLine($"  User '{fullName}': {(stillExists ? "STILL EXISTS" : "DELETED")}");
                Assert.That(stillExists, Is.False, $"User '{fullName}' should no longer exist in DOM");
            }

            TestContext.Out.WriteLine("All test users confirmed deleted");
        }

        #region Helper Methods

        private async Task<string> CreateUserWithRetry(string firstName, string lastName, string email, int maxAttempts = 3)
        {
            var fullName = $"{firstName} {lastName}";

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                TestContext.Out.WriteLine($"Create user attempt {attempt}/{maxAttempts}: {fullName}");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

                _messageCountBefore = await GetMessageCount();

                await SendChatMessage(
                    $"Erstelle einen neuen Systembenutzer: Vorname '{firstName}', Nachname '{lastName}', Email '{email}'");

                var response = await WaitForBotResponse(_messageCountBefore, 120000);
                TestContext.Out.WriteLine($"Bot response ({response.Length} chars): {response[..Math.Min(200, response.Length)]}");

                await Task.Delay(5000);

                var userId = await WaitForUserInDom(firstName, lastName);
                if (!string.IsNullOrEmpty(userId))
                {
                    if (_listener.HasApiErrors())
                        TestContext.Out.WriteLine($"Warning: API error during creation (user still created): {_listener.GetLastErrorMessage()}");
                    return userId;
                }

                TestContext.Out.WriteLine($"User not found in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"User '{fullName}' was not created after {maxAttempts} attempts");
            return string.Empty;
        }

        private async Task DeleteUserWithRetry(string firstName, string lastName, int maxAttempts = 5)
        {
            var fullName = $"{firstName} {lastName}";

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                TestContext.Out.WriteLine($"Delete user attempt {attempt}/{maxAttempts}: {fullName}");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

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

            Assert.Fail($"User '{fullName}' was not deleted after {maxAttempts} attempts. LLM may have chosen list_system_users instead of delete_system_user.");
        }

        private async Task<string> WaitForUserInDom(string firstName, string lastName, int timeoutMs = 30000)
        {
            var fullName = $"{firstName} {lastName}";
            TestContext.Out.WriteLine($"Waiting for user '{fullName}' to appear in DOM...");

            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                var inputs = await Page.QuerySelectorAllAsync($"input[id^=\"{RowNamePrefix}\"]");
                foreach (var input in inputs)
                {
                    var value = await input.InputValueAsync();
                    if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                    {
                        var id = await input.GetAttributeAsync("id");
                        var userId = id?.Replace(RowNamePrefix, "") ?? "";
                        TestContext.Out.WriteLine($"User '{fullName}' found in DOM with ID: {userId}");
                        return userId;
                    }
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"User '{fullName}' NOT found in DOM after {timeoutMs / 1000}s");
            return "";
        }

        private async Task<bool> UserExistsInDom(string firstName, string lastName)
        {
            var fullName = $"{firstName} {lastName}";
            var inputs = await Page.QuerySelectorAllAsync($"input[id^=\"{RowNamePrefix}\"]");
            foreach (var input in inputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private async Task<bool> WaitForUserRemovedFromDom(string firstName, string lastName, int timeoutMs = 30000)
        {
            var fullName = $"{firstName} {lastName}";
            TestContext.Out.WriteLine($"Waiting for user '{fullName}' to be removed from DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                var userAdminSection = await Page.QuerySelectorAsync($"#{UserAdminSection}");
                if (userAdminSection == null)
                {
                    TestContext.Out.WriteLine("Settings closed during delete wait, reopening...");
                    await Actions.ClickButtonById("open-settings");
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
            TestContext.Out.WriteLine($"User '{fullName}' still in DOM after {timeoutMs / 1000}s");
            return false;
        }

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
