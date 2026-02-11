using System.Diagnostics;
using System.Text.Json;
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

            var message = $"Erstelle einen neuen System-Benutzer mit Vorname '{_firstName}', Nachname '{_lastName}' und Email '{_email}'.";

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(message);
            await WaitForBotResponse(_messageCountBefore);

            // Assert - wait for user to appear in DOM (confirms function execution completed)
            var fullName = $"{_firstName} {_lastName}";
            _createdUserId = await WaitForUserInSettings(fullName);
            Assert.That(_createdUserId, Is.Not.Empty, $"User '{fullName}' should appear in DOM after creation");
            TestContext.Out.WriteLine($"User found in DOM with ID: {_createdUserId}");

            // Copy admin's password hash to new user via SQL
            await ExecuteSql(
                $"UPDATE \"AspNetUsers\" SET password_hash = (SELECT password_hash FROM \"AspNetUsers\" WHERE email = 'admin@test.com') WHERE id = '{_createdUserId}'");
            _capturedPassword = Password;
            TestContext.Out.WriteLine("Password hash copied from admin user");

            await AssignAdminRole(_createdUserId);

            TestContext.Out.WriteLine($"User created: {fullName}, will login with email: {_email}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_SetGroupScopeViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Set Group Scope via Chat ===");
            Assert.That(_createdUserId, Is.Not.Empty, "User ID should have been extracted in Step 2");

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                TestContext.Out.WriteLine($"Set group scope attempt {attempt}/3");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

                // Act
                _messageCountBefore = await GetMessageCount();
                await SendChatMessage($"Setze den Group Scope für den Benutzer mit ID '{_createdUserId}' auf '{TargetGroupName}'");
                var response = await WaitForBotResponse(_messageCountBefore);

                // Assert
                TestContext.Out.WriteLine($"Bot response: {response}");

                var hasGroupName = response.Contains(TargetGroupName, StringComparison.OrdinalIgnoreCase);
                if (hasGroupName)
                {
                    TestContext.Out.WriteLine($"Group scope set to '{TargetGroupName}' for user {_createdUserId}");

                    // Verify via API
                    var apiResult = await Page.EvaluateAsync<string>(@"async (userId) => {
                        const token = localStorage.getItem('JWT_TOKEN');
                        if (!token) return JSON.stringify({ error: 'No token' });
                        const response = await fetch('https://localhost:5001/api/backend/GroupVisibilities/GetSimpleList/' + userId, {
                            headers: { 'Authorization': 'Bearer ' + token }
                        });
                        return response.status + ': ' + (await response.text()).substring(0, 500);
                    }", _createdUserId);
                    TestContext.Out.WriteLine($"GroupVisibilities API: {apiResult}");

                    return;
                }

                TestContext.Out.WriteLine($"Response did not confirm group, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Group scope was not set after 3 attempts");
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
            TestContext.Out.WriteLine($"Using email: {_email}");

            // Act - login via UI form (password hash was copied from admin in Step 2)
            await PerformLogin(_email, _capturedPassword);

            // Assert
            var currentUrl = Actions.ReadCurrentUrl();
            Assert.That(currentUrl, Does.Not.Contain("login"), "Should not be on login page after successful login");

            TestContext.Out.WriteLine($"Logged in as {_email}");
        }

        [Test]
        [Order(6)]
        public async Task Step6_VerifyGroupVisibility()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Verify Group Visibility ===");
            TestContext.Out.WriteLine($"Logged in as user with ID: {_createdUserId}");

            // Act - get group visibilities for the created user via API
            var apiResult = await Page.EvaluateAsync<string>(@"async (userId) => {
                const token = localStorage.getItem('JWT_TOKEN');
                if (!token) return JSON.stringify({ error: 'No token' });
                const response = await fetch('https://localhost:5001/api/backend/GroupVisibilities/GetSimpleList/' + userId, {
                    headers: { 'Authorization': 'Bearer ' + token }
                });
                if (!response.ok) return JSON.stringify({ error: response.status + ' ' + response.statusText });
                const data = await response.json();
                return JSON.stringify(data);
            }", _createdUserId);

            TestContext.Out.WriteLine($"GroupVisibilities API response (truncated): {apiResult[..Math.Min(500, apiResult.Length)]}");

            using var doc = JsonDocument.Parse(apiResult);

            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                Assert.Fail($"API call failed: {errorElement.GetString()}");
                return;
            }

            // Filter entries for our created user
            var userGroupIds = new List<string>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var appUserId = item.GetProperty("appUserId").GetString() ?? "";
                    if (appUserId == _createdUserId)
                    {
                        var groupId = item.GetProperty("groupId").GetString() ?? "";
                        if (!string.IsNullOrEmpty(groupId))
                            userGroupIds.Add(groupId);
                    }
                }
            }

            TestContext.Out.WriteLine($"Groups assigned to user: {userGroupIds.Count}");

            // Look up group names via SQL
            var groupNames = new List<string>();
            if (userGroupIds.Count > 0)
            {
                var idList = string.Join("','", userGroupIds);
                var namesResult = await ExecuteSql($"SELECT name FROM \"group\" WHERE id IN ('{idList}') ORDER BY name");
                if (!string.IsNullOrEmpty(namesResult))
                {
                    groupNames = namesResult.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList();
                }
            }

            foreach (var name in groupNames)
                TestContext.Out.WriteLine($"  Assigned group: '{name}'");

            // Assert - verify the target group is among the assigned groups
            Assert.That(userGroupIds.Count, Is.GreaterThan(0), "At least one group should be assigned to the user");
            Assert.That(groupNames.Any(n => n.Contains("Mitte", StringComparison.OrdinalIgnoreCase)
                || n.Contains("Deutschweiz", StringComparison.OrdinalIgnoreCase)
                || n.Contains(TargetGroupName, StringComparison.OrdinalIgnoreCase)),
                Is.True,
                $"Target group '{TargetGroupName}' should be among assigned groups: {string.Join(", ", groupNames)}");

            TestContext.Out.WriteLine($"Verified: User has group scope including '{TargetGroupName}'");
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

            for (var attempt = 1; attempt <= 5; attempt++)
            {
                TestContext.Out.WriteLine($"Delete user attempt {attempt}/5");
                await EnsureChatOpen();

                await Actions.ClickButtonById(ChatClearBtn);
                await Actions.Wait1000();
                await WaitForChatInputEnabled();

                // Act
                _messageCountBefore = await GetMessageCount();
                await SendChatMessage($"Lösche den Systembenutzer mit Vorname '{_firstName}' und Nachname '{_lastName}'");
                var response = await WaitForBotResponse(_messageCountBefore);

                // Assert
                TestContext.Out.WriteLine($"Bot response: {response}");

                var hasConfirmation = response.Contains("gelöscht", StringComparison.OrdinalIgnoreCase)
                    || response.Contains("erfolgreich", StringComparison.OrdinalIgnoreCase)
                    || response.Contains("deleted", StringComparison.OrdinalIgnoreCase);

                if (hasConfirmation)
                {
                    TestContext.Out.WriteLine($"User {_createdUserId} deleted via chat successfully");
                    return;
                }

                TestContext.Out.WriteLine($"Response did not confirm deletion, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"User '{_firstName} {_lastName}' was not deleted after 5 attempts");
        }

        #region Helper Methods

        private static async Task<string> ExecuteSql(string sql)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"klacks_e2e_{Guid.NewGuid():N}.sql");
            await File.WriteAllTextAsync(tempFile, sql);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = @"C:\Program Files\PostgreSQL\17\bin\psql.exe",
                    Arguments = $"-h localhost -p 5434 -U postgres -d klacks -t -A -f \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.Environment["PGPASSWORD"] = "admin";

                using var process = Process.Start(psi)!;
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                return string.IsNullOrEmpty(error) ? output.Trim() : $"ERROR: {error.Trim()}";
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        private async Task AssignAdminRole(string userId)
        {
            TestContext.Out.WriteLine($"Assigning Admin role to user {userId}...");
            var result = await Page.EvaluateAsync<string>(@"async (userId) => {
                const token = localStorage.getItem('JWT_TOKEN');
                if (!token) return 'No token found';
                const response = await fetch('https://localhost:5001/api/backend/Accounts/ChangeRoleUser', {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': 'Bearer ' + token
                    },
                    body: JSON.stringify({ userId: userId, roleName: 'Admin', isSelected: true })
                });
                return response.status + ': ' + (await response.text()).substring(0, 200);
            }", userId);
            TestContext.Out.WriteLine($"Admin role assignment result: {result}");
        }

        private async Task PerformLogout()
        {
            TestContext.Out.WriteLine("Performing logout...");
            await Actions.ClickByJavaScript(LogoutButton);
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

        private async Task<string> WaitForUserInSettings(string fullName, int timeoutMs = 60000)
        {
            TestContext.Out.WriteLine($"Waiting for user '{fullName}' to appear in Settings DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                var inputs = await Page.QuerySelectorAllAsync("input[id^='user-admin-row-name-']");
                foreach (var input in inputs)
                {
                    var value = await input.InputValueAsync();
                    if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                    {
                        var id = await input.GetAttributeAsync("id");
                        return id?.Replace("user-admin-row-name-", "") ?? "";
                    }
                }
                await Actions.Wait500();
            }
            TestContext.Out.WriteLine($"User '{fullName}' NOT found in DOM after {timeoutMs / 1000}s");
            return "";
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

        #endregion
    }
}
