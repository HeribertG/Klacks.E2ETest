using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
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
            var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
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
        public async Task Step3_NavigateToUserAdministration()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Navigate to User Administration ===");

            await CloseChatIfOpen();
            await Actions.Wait500();

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.ScrollIntoViewById(UserAdminSection);
            await Actions.Wait500();

            // Assert
            var addButton = await Actions.FindElementById(AddUserBtn);
            Assert.That(addButton, Is.Not.Null, "Add user button should be visible");

            var isEnabled = await addButton!.IsEnabledAsync();
            Assert.That(isEnabled, Is.True, "Add user button should be enabled for admin");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User Administration page loaded, Add button is enabled");
        }

        [Test]
        [Order(4)]
        public async Task Step4_CreateFirstUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Create First User via UI ===");
            var user = _testUsers[0];

            // Act
            var userId = await CreateUserViaUi(user.FirstName, user.LastName, user.Email);

            // Assert
            Assert.That(userId, Is.Not.Null, $"User {user.FirstName} {user.LastName} should be created");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"First user created: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test]
        [Order(5)]
        public async Task Step5_CreateSecondUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Create Second User via UI ===");
            var user = _testUsers[1];

            // Act
            var userId = await CreateUserViaUi(user.FirstName, user.LastName, user.Email);

            // Assert
            Assert.That(userId, Is.Not.Null, $"User {user.FirstName} {user.LastName} should be created");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Second user created: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test]
        [Order(6)]
        public async Task Step6_CreateThirdUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Create Third User via UI ===");
            var user = _testUsers[2];

            // Act
            var userId = await CreateUserViaUi(user.FirstName, user.LastName, user.Email);

            // Assert
            Assert.That(userId, Is.Not.Null, $"User {user.FirstName} {user.LastName} should be created");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Third user created: {user.FirstName} {user.LastName} (ID: {userId})");
        }

        [Test]
        [Order(7)]
        public async Task Step7_VerifyAllUsersExist()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Verify All Users Exist ===");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(UserAdminSection);
            await Actions.Wait1000();

            // Act
            var nameInputs = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            TestContext.Out.WriteLine($"Total users in list: {nameInputs.Count}");

            var foundUsers = new List<string>();
            foreach (var input in nameInputs)
            {
                var value = await input.InputValueAsync();
                TestContext.Out.WriteLine($"  User: '{value}'");

                foreach (var user in _testUsers)
                {
                    var fullName = $"{user.FirstName} {user.LastName}";
                    if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundUsers.Add(fullName);
                        TestContext.Out.WriteLine($"  -> MATCH: {fullName}");
                    }
                }
            }

            // Assert
            TestContext.Out.WriteLine($"Found {foundUsers.Count} of {_testUsers.Length} test users");
            Assert.That(foundUsers.Count, Is.EqualTo(_testUsers.Length),
                $"All {_testUsers.Length} test users should exist. Found: {string.Join(", ", foundUsers)}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("All test users verified");
        }

        [Test]
        [Order(8)]
        public async Task Step8_DeleteCreatedUsers()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Delete Created Users ===");

            if (CreatedUserIds.Count == 0)
            {
                TestContext.Out.WriteLine("No users to delete - skipping");
                Assert.Inconclusive("No users were created in previous steps");
                return;
            }

            // Act
            await CloseChatIfOpen();
            await Actions.Wait500();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(UserAdminSection);
            await Actions.Wait1000();

            var deletedCount = 0;
            foreach (var userId in CreatedUserIds.ToList())
            {
                var deleteButtonId = $"{RowDeletePrefix}{userId}";
                var deleteButton = await Actions.FindElementById(deleteButtonId);

                if (deleteButton == null)
                {
                    TestContext.Out.WriteLine($"Delete button for user {userId} not found - skipping");
                    continue;
                }

                await deleteButton.ClickAsync();
                await Actions.Wait500();

                await Actions.ClickElementById(DeleteModalConfirmBtn);
                await Actions.Wait2000();

                deletedCount++;
                TestContext.Out.WriteLine($"Deleted user {userId}");
            }

            // Assert
            foreach (var userId in CreatedUserIds)
            {
                var deletedUser = await Page.QuerySelectorAsync($"#{RowNamePrefix}{userId}");
                Assert.That(deletedUser, Is.Null, $"User {userId} should be deleted");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"All {deletedCount} test users deleted successfully");
            CreatedUserIds.Clear();
        }

        #region Helper Methods

        private async Task<string?> CreateUserViaUi(string firstName, string lastName, string email)
        {
            TestContext.Out.WriteLine($"Creating user: {firstName} {lastName} ({email})");
            var fullName = $"{firstName} {lastName}";

            await Actions.ScrollIntoViewById(AddUserBtn);
            await Actions.Wait500();

            var addButton = await Actions.FindElementById(AddUserBtn);
            Assert.That(addButton, Is.Not.Null, "Add button should exist");
            await addButton!.ClickAsync();
            await Actions.Wait1000();

            await Actions.TypeIntoInputById(InputFirstName, firstName);
            await Actions.TypeIntoInputById(InputLastName, lastName);
            await Actions.TypeIntoInputById(InputEmail, email);
            await Actions.Wait1000();

            var usernameInput = await Actions.FindElementById(InputUserName);
            if (usernameInput != null)
            {
                var autoUsername = await usernameInput.InputValueAsync();
                TestContext.Out.WriteLine($"Auto-generated username: {autoUsername}");
            }

            var saveButton = await Actions.FindElementById(ModalSaveBtn);
            Assert.That(saveButton, Is.Not.Null, "Save button should exist");

            var isEnabled = await saveButton!.IsEnabledAsync();
            TestContext.Out.WriteLine($"Save button enabled: {isEnabled}");
            Assert.That(isEnabled, Is.True, "Save button should be enabled after filling all fields");

            await Actions.ClickElementById(ModalSaveBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait3500();

            var msgOkBtn = await Page.QuerySelectorAsync($"#{PasswordResetModalOkBtn}");
            if (msgOkBtn != null)
            {
                TestContext.Out.WriteLine("Dismissing password info dialog");
                await msgOkBtn.ClickAsync();
                await Actions.Wait1000();
            }

            var nameInputs = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            TestContext.Out.WriteLine($"Found {nameInputs.Count} users in list after save");
            foreach (var input in nameInputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                {
                    var inputId = await input.GetAttributeAsync("id");
                    var userId = inputId?.Replace(RowNamePrefix, "");
                    TestContext.Out.WriteLine($"Found created user: '{value}' (ID: {userId})");
                    if (userId != null)
                    {
                        CreatedUserIds.Add(userId);
                        return userId;
                    }
                }
            }

            TestContext.Out.WriteLine($"WARNING: User '{fullName}' not found in list after creation");
            return null;
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
                TestContext.Out.WriteLine("Closing chat aside panel via JS click");
                await Page.EvaluateAsync($"() => document.getElementById('{HeaderAssistantButton}')?.click()");
                await Actions.Wait1000();

                var stillVisible = await Page.QuerySelectorAsync("app-aside.visible");
                if (stillVisible != null)
                {
                    TestContext.Out.WriteLine("Aside still visible, retrying via JS click");
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

        private async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = 60000)
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

        #endregion
    }
}
