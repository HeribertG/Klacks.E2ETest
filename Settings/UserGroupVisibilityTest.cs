using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(64)]
    public class UserGroupVisibilityTest : PlaywrightSetup
    {
        private Listener _listener = null!;
        private int _messageCountBefore;

        private static string _timestamp = "";
        private static string _firstName = "";
        private static string _lastName = "GroupScope";
        private static string _email = "";
        private static string _createdUserId = "";
        private static string _capturedUsername = "";
        private static string _capturedPassword = "";

        private const string LogoutButton = "header-logout-button";
        private const string TargetGroupName = "Deutschweiz Mitte";

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
        public async Task Step1_OpenChatAndInitTestData()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Open Chat + Init Test Data ===");
            _timestamp = DateTime.Now.Ticks.ToString()[10..16];
            _firstName = $"Test{_timestamp}";
            _email = $"testgs.{_timestamp}@klacks-test.ch";
            TestContext.Out.WriteLine($"Timestamp: {_timestamp}, Name: {_firstName} {_lastName}, Email: {_email}");

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
        public async Task Step2_CreateUserViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Create User via LLM Chat ===");
            await EnsureChatOpen();

            var message = $"Erstelle einen neuen System-Benutzer über die UI (create_system_user) mit Vorname '{_firstName}', Nachname '{_lastName}' und Email '{_email}'. Gib mir Username und Password zurück.";

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(message);
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var hasUsername = response.Contains("username", StringComparison.OrdinalIgnoreCase) ||
                             response.Contains("Benutzername", StringComparison.OrdinalIgnoreCase);
            Assert.That(hasUsername, Is.True, $"Response should contain username. Got: {response}");

            (_capturedUsername, _capturedPassword) = ParseCredentialsFromResponse(response);
            _createdUserId = ExtractUserIdFromResponse(response) ?? "";

            TestContext.Out.WriteLine($"Parsed Username: {_capturedUsername}");
            TestContext.Out.WriteLine($"Parsed Password length: {_capturedPassword.Length}");
            TestContext.Out.WriteLine($"Parsed UserId: {_createdUserId}");

            Assert.That(_capturedUsername, Is.Not.Empty, "Username should be extracted from bot response");
            Assert.That(_capturedPassword, Is.Not.Empty, "Password should be extracted from bot response");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"User created via chat: {_capturedUsername}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_SetGroupScopeViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Set Group Scope via Chat ===");
            Assert.That(_createdUserId, Is.Not.Empty, "User ID should have been extracted in Step 2");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze den Group Scope für den Benutzer mit ID '{_createdUserId}' auf '{TargetGroupName}'");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var hasGroupName = response.Contains(TargetGroupName, StringComparison.OrdinalIgnoreCase);
            Assert.That(hasGroupName, Is.True, $"Response should confirm group '{TargetGroupName}'. Got: {response}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Group scope set to '{TargetGroupName}' for user {_createdUserId}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_LogoutAsAdmin()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Logout as Admin ===");

            // Act
            await CloseChatIfOpen();
            await Actions.Wait500();
            await PerformLogout();

            // Assert
            var currentUrl = Actions.ReadCurrentUrl();
            Assert.That(currentUrl, Does.Contain("login"), "Should be on login page after logout");

            var loginInput = await Actions.FindElementById(LogInIds.InputEmailId);
            Assert.That(loginInput, Is.Not.Null, "Login form should be visible");

            TestContext.Out.WriteLine("Logged out successfully, login page visible");
        }

        [Test]
        [Order(5)]
        public async Task Step5_LoginAsNewUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Login as New User ===");
            TestContext.Out.WriteLine($"Using username: {_capturedUsername}");

            // Act
            await PerformLogin(_capturedUsername, _capturedPassword);

            // Assert
            var currentUrl = Actions.ReadCurrentUrl();
            Assert.That(currentUrl, Does.Not.Contain("login"), "Should not be on login page after successful login");

            TestContext.Out.WriteLine($"Logged in as {_capturedUsername}");
        }

        [Test]
        [Order(6)]
        public async Task Step6_VerifyGroupVisibility()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Verify Group Visibility ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            var groupNames = new List<string>();
            for (var i = 0; i < 100; i++)
            {
                var cellId = $"{GroupIds.CellNamePrefix}{i}";
                var cell = await Actions.FindElementById(cellId);
                if (cell == null)
                    break;

                var text = await Actions.GetTextContentById(cellId);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    groupNames.Add(text.Trim());
                    TestContext.Out.WriteLine($"  Group {i}: '{text.Trim()}'");
                }
            }

            // Assert
            TestContext.Out.WriteLine($"Total visible groups: {groupNames.Count}");
            Assert.That(groupNames.Count, Is.EqualTo(1), $"Only one group should be visible, but found: {string.Join(", ", groupNames)}");
            Assert.That(groupNames[0], Does.Contain(TargetGroupName), $"Visible group should be '{TargetGroupName}', but was '{groupNames[0]}'");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Verified: Only '{TargetGroupName}' is visible");
        }

        [Test]
        [Order(7)]
        public async Task Step7_LogoutAndLoginAsAdmin()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Logout + Login as Admin ===");

            // Act
            await PerformLogout();
            await PerformLogin(UserName, Password);

            // Assert
            var currentUrl = Actions.ReadCurrentUrl();
            Assert.That(currentUrl, Does.Not.Contain("login"), "Should be logged in as admin");

            TestContext.Out.WriteLine("Logged back in as admin");
        }

        [Test]
        [Order(8)]
        public async Task Step8_CleanupDeleteCreatedUserViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Cleanup - Delete Created User via Chat ===");

            if (string.IsNullOrEmpty(_createdUserId))
            {
                TestContext.Out.WriteLine("No user to delete - skipping");
                Assert.Inconclusive("No user was created in previous steps");
                return;
            }

            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Lösche den Benutzer mit ID {_createdUserId}");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, $"Bot should respond for user {_createdUserId}");

            var hasConfirmation = response.Contains("gelöscht", StringComparison.OrdinalIgnoreCase)
                || response.Contains("erfolgreich", StringComparison.OrdinalIgnoreCase)
                || response.Contains("deleted", StringComparison.OrdinalIgnoreCase);
            Assert.That(hasConfirmation, Is.True,
                $"Response should confirm deletion of user {_createdUserId}. Got: {response}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"User {_createdUserId} deleted via chat successfully");
        }

        #region Helper Methods

        private async Task PerformLogout()
        {
            TestContext.Out.WriteLine("Performing logout...");
            await Actions.ClickButtonById(LogoutButton);
            await Actions.Wait2000();

            await Actions.WaitUntilUrlContains("login");
            TestContext.Out.WriteLine("Logout complete, on login page");
        }

        private async Task PerformLogin(string username, string password)
        {
            TestContext.Out.WriteLine($"Performing login as {username}...");

            await Actions.Wait500();
            await Actions.FillInputById(LogInIds.InputEmailId, username);
            await Actions.WaitForSpinnerToDisappear();

            await Actions.FillInputById(LogInIds.InputPasswordId, password);
            await Actions.WaitForSpinnerToDisappear();

            await Actions.ClickButtonById(LogInIds.ButtonSumitId);
            await Actions.WaitForSpinnerToDisappear();

            await Actions.WaitUntilUrlNotContaining("login");
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Login complete");
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

        private async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = 90000)
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

        private static string? ExtractUserIdFromResponse(string response)
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

        private static (string Username, string Password) ParseCredentialsFromResponse(string response)
        {
            var username = "";
            var password = "";

            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim().TrimStart('-', '*', ' ');

                if (TryExtractValue(trimmed, "Username:", out var u) ||
                    TryExtractValue(trimmed, "Benutzername:", out u) ||
                    TryExtractValue(trimmed, "username:", out u))
                {
                    username = CleanExtractedValue(u);
                }
                else if (TryExtractValue(trimmed, "Password:", out var p) ||
                         TryExtractValue(trimmed, "Passwort:", out p) ||
                         TryExtractValue(trimmed, "password:", out p))
                {
                    password = CleanExtractedValue(p);
                }
            }

            return (username, password);
        }

        private static bool TryExtractValue(string line, string prefix, out string value)
        {
            value = "";
            if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            value = line[prefix.Length..].Trim();
            return true;
        }

        private static string CleanExtractedValue(string value)
        {
            return value.Trim('`', '\'', '"', '*', ' ');
        }

        #endregion
    }
}
