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
            TestContext.Out.WriteLine("=== Step 3: Create First User via LLM Chat ===");
            await EnsureChatOpen();
            var user = _testUsers[0];

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Erstelle einen neuen System-Benutzer: Vorname '{user.FirstName}', Nachname '{user.LastName}', Email '{user.Email}'");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            var hasUsername = response.Contains("username", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Benutzername", StringComparison.OrdinalIgnoreCase);
            Assert.That(hasUsername, Is.True, $"Response should contain username. Got: {response}");

            var userId = ExtractUserIdFromResponse(response, user.FirstName, user.LastName);
            if (!string.IsNullOrEmpty(userId))
                CreatedUserIds.Add(userId);

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"First user created via chat: {user.FirstName} {user.LastName}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_CreateSecondUserViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Create Second User via LLM Chat ===");
            await EnsureChatOpen();
            var user = _testUsers[1];

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Erstelle einen neuen System-Benutzer: Vorname '{user.FirstName}', Nachname '{user.LastName}', Email '{user.Email}'");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var userId = ExtractUserIdFromResponse(response, user.FirstName, user.LastName);
            if (!string.IsNullOrEmpty(userId))
                CreatedUserIds.Add(userId);

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Second user created via chat: {user.FirstName} {user.LastName}");
        }

        [Test]
        [Order(5)]
        public async Task Step5_CreateThirdUserViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Create Third User via LLM Chat ===");
            await EnsureChatOpen();
            var user = _testUsers[2];

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Erstelle einen neuen System-Benutzer: Vorname '{user.FirstName}', Nachname '{user.LastName}', Email '{user.Email}'");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var userId = ExtractUserIdFromResponse(response, user.FirstName, user.LastName);
            if (!string.IsNullOrEmpty(userId))
                CreatedUserIds.Add(userId);

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Third user created via chat: {user.FirstName} {user.LastName}");
        }

        [Test]
        [Order(6)]
        public async Task Step6_VerifyAllUsersViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Verify All Users via LLM Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle System-Benutzer auf");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with user list");

            var allFound = true;
            foreach (var user in _testUsers)
            {
                var fullName = $"{user.FirstName} {user.LastName}";
                var found = response.Contains(user.FirstName, StringComparison.OrdinalIgnoreCase)
                    || response.Contains(fullName, StringComparison.OrdinalIgnoreCase);
                TestContext.Out.WriteLine($"  User '{fullName}': {(found ? "FOUND" : "NOT FOUND")}");
                if (!found)
                    allFound = false;
            }

            Assert.That(allFound, Is.True, $"All {_testUsers.Length} test users should be in the response. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("All test users verified via chat");
        }

        [Test]
        [Order(7)]
        public async Task Step7_DeleteCreatedUsersViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Delete Created Users via LLM Chat ===");

            if (CreatedUserIds.Count == 0)
            {
                TestContext.Out.WriteLine("No users to delete - skipping");
                Assert.Inconclusive("No users were created in previous steps");
                return;
            }

            await EnsureChatOpen();

            // Act
            foreach (var userId in CreatedUserIds.ToList())
            {
                _messageCountBefore = await GetMessageCount();
                await SendChatMessage($"Lösche den Benutzer mit ID {userId}");
                var response = await WaitForBotResponse(_messageCountBefore);

                TestContext.Out.WriteLine($"Delete response for {userId}: {response}");
                Assert.That(response, Is.Not.Empty, $"Bot should respond for user {userId}");

                var hasConfirmation = response.Contains("gelöscht", StringComparison.OrdinalIgnoreCase)
                    || response.Contains("erfolgreich", StringComparison.OrdinalIgnoreCase)
                    || response.Contains("deleted", StringComparison.OrdinalIgnoreCase);
                Assert.That(hasConfirmation, Is.True,
                    $"Response should confirm deletion of user {userId}. Got: {response}");
            }

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"All {CreatedUserIds.Count} test users deleted via chat");
            CreatedUserIds.Clear();
        }

        [Test]
        [Order(8)]
        public async Task Step8_VerifyUsersDeletedViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Verify Users Deleted via LLM Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle System-Benutzer auf");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with user list");

            foreach (var user in _testUsers)
            {
                var fullName = $"{user.FirstName} {user.LastName}";
                var stillExists = response.Contains(fullName, StringComparison.OrdinalIgnoreCase);
                TestContext.Out.WriteLine($"  User '{fullName}': {(stillExists ? "STILL EXISTS" : "DELETED")}");
                Assert.That(stillExists, Is.False, $"User '{fullName}' should no longer exist");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("All test users confirmed deleted");
        }

        #region Helper Methods

        private static string? ExtractUserIdFromResponse(string response, string firstName, string lastName)
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim().TrimStart('-', '*', ' ');

                if (trimmed.Contains("userId", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("ID:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var id = parts[1].Trim().Trim('`', '\'', '"', '*', ' ');
                        if (!string.IsNullOrEmpty(id) && id.Contains('-'))
                            return id;
                    }
                }
            }

            return null;
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

        private async Task CloseChatIfOpen()
        {
            var aside = await Actions.QuerySelector("app-aside.visible");
            if (aside != null)
            {
                TestContext.Out.WriteLine("Closing chat aside panel");
                await Actions.ClickButtonById(HeaderAssistantButton);
                await Actions.Wait1000();

                var stillVisible = await Actions.QuerySelector("app-aside.visible");
                if (stillVisible != null)
                {
                    TestContext.Out.WriteLine("Aside still visible, retrying");
                    await Actions.ClickButtonById(HeaderAssistantButton);
                    await Actions.Wait1000();
                }
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
