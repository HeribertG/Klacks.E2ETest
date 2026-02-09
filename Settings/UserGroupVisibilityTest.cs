using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;
using static Klacks.E2ETest.Constants.SettingsUserAdministrationIds;

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
            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
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
            TestContext.Out.WriteLine($"Parsed Username: {_capturedUsername}");
            TestContext.Out.WriteLine($"Parsed Password length: {_capturedPassword.Length}");

            Assert.That(_capturedUsername, Is.Not.Empty, "Username should be extracted from bot response");
            Assert.That(_capturedPassword, Is.Not.Empty, "Password should be extracted from bot response");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"User created via chat: {_capturedUsername}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_FindUserIdAndSetGroupScope()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Find User ID + Set Group Scope via Chat ===");
            await EnsureChatOpen();

            await CloseChatIfOpen();
            await Actions.Wait500();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.ScrollIntoViewById(UserAdminSection);
            await Actions.Wait1000();

            var fullName = $"{_firstName} {_lastName}";
            var nameInputs = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            foreach (var input in nameInputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                {
                    var inputId = await input.GetAttributeAsync("id");
                    _createdUserId = inputId?.Replace(RowNamePrefix, "") ?? "";
                    TestContext.Out.WriteLine($"Found created user in list: '{value}' (ID: {_createdUserId})");
                    break;
                }
            }

            Assert.That(_createdUserId, Is.Not.Empty, $"User '{fullName}' should exist in user list");

            // Act
            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();
            await EnsureChatOpen();

            var message = $"Setze den Group Scope für den Benutzer mit ID '{_createdUserId}' auf '{TargetGroupName}'";

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage(message);
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
                var cell = await Page.QuerySelectorAsync($"#{cellId}");
                if (cell == null)
                    break;

                var text = await cell.TextContentAsync();
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
        public async Task Step8_CleanupDeleteCreatedUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Cleanup - Delete Created User ===");

            if (string.IsNullOrEmpty(_createdUserId))
            {
                TestContext.Out.WriteLine("No user to delete - skipping");
                Assert.Inconclusive("No user was created in previous steps");
                return;
            }

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(UserAdminSection);
            await Actions.Wait1000();

            var deleteButtonId = $"{RowDeletePrefix}{_createdUserId}";
            var deleteButton = await Actions.FindElementById(deleteButtonId);
            Assert.That(deleteButton, Is.Not.Null, $"Delete button for user {_createdUserId} should exist");

            await deleteButton!.ClickAsync();
            await Actions.Wait500();

            await Actions.ClickElementById(DeleteModalConfirmBtn);
            await Actions.Wait2000();

            // Assert
            var deletedUser = await Page.QuerySelectorAsync($"#{RowNamePrefix}{_createdUserId}");
            Assert.That(deletedUser, Is.Null, $"User {_createdUserId} should be deleted");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"User {_createdUserId} deleted successfully");
        }

        #region Helper Methods

        private async Task PerformLogout()
        {
            TestContext.Out.WriteLine("Performing logout...");
            await Page.EvaluateAsync($"() => document.getElementById('{LogoutButton}')?.click()");
            await Actions.Wait2000();

            await Page.WaitForURLAsync(url => url.Contains("login"), new() { Timeout = 10000 });
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

            await Page.WaitForURLAsync(url => !url.Contains("login"), new() { Timeout = 10000 });
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Login complete");
        }

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
            var aside = await Page.QuerySelectorAsync("app-aside.visible");
            if (aside != null)
            {
                TestContext.Out.WriteLine("Closing chat aside panel");
                await Page.EvaluateAsync($"() => document.getElementById('{HeaderAssistantButton}')?.click()");
                await Actions.Wait1000();

                var stillVisible = await Page.QuerySelectorAsync("app-aside.visible");
                if (stillVisible != null)
                {
                    await Page.EvaluateAsync($"() => document.getElementById('{HeaderAssistantButton}')?.click()");
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

        private async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = 90000)
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
