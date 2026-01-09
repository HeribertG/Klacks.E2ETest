using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.SettingsUserAdministrationIds;
using static E2ETest.Constants.SettingsUserAdministrationTestData;

namespace E2ETest
{
    [TestFixture]
    [Order(22)]
    public class SettingsUserAdministrationTest : PlaywrightSetup
    {
        private Listener _listener = null!;
        private static string? _createdUserId;
        private static string? _createdUserName;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(UserAdminSection);
            await Actions.Wait500();
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
        public async Task Step1_VerifyUserAdministrationPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify User Administration Page Loaded ===");

            // Assert
            var header = await Actions.FindElementById(UserAdminHeader);
            Assert.That(header, Is.Not.Null, "User administration header should be visible");

            var addButton = await Actions.FindElementById(AddUserBtn);
            Assert.That(addButton, Is.Not.Null, "Add user button should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User Administration page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_CreateNewUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Create New User ===");
            var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
            var testFirstName = $"Test{timestamp}";
            var testLastName = $"User{timestamp}";
            _createdUserName = $"{testFirstName} {testLastName}";
            var testEmail = $"test.user.{timestamp}@test.com";
            TestContext.Out.WriteLine($"Creating user: {_createdUserName}");

            // Act
            var addButton = await Actions.FindElementById(AddUserBtn);
            Assert.That(addButton, Is.Not.Null, "Add button should exist");

            var isEnabled = await addButton!.IsEnabledAsync();
            if (!isEnabled)
            {
                TestContext.Out.WriteLine("Add user button is disabled - skipping test");
                Assert.Inconclusive("Button is disabled - user might not have permissions");
                return;
            }

            await addButton.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Clicked Add User button");

            // Fill form (Username is auto-generated and readonly)
            TestContext.Out.WriteLine($"Filling FirstName: {testFirstName}");
            await Actions.TypeIntoInputById(InputFirstName, testFirstName);

            TestContext.Out.WriteLine($"Filling LastName: {testLastName}");
            await Actions.TypeIntoInputById(InputLastName, testLastName);

            TestContext.Out.WriteLine($"Filling Email: {testEmail}");
            await Actions.TypeIntoInputById(InputEmail, testEmail);
            await Actions.Wait500();

            // Check auto-generated username
            var usernameInput = await Actions.FindElementById(InputUserName);
            if (usernameInput != null)
            {
                var autoUsername = await usernameInput.InputValueAsync();
                TestContext.Out.WriteLine($"Auto-generated username: {autoUsername}");
            }

            // Save
            var saveButton = await Actions.FindElementById(ModalSaveBtn);
            TestContext.Out.WriteLine($"Save button found: {saveButton != null}");
            Assert.That(saveButton, Is.Not.Null, "Save button should exist");

            var isButtonEnabled = await saveButton!.IsEnabledAsync();
            TestContext.Out.WriteLine($"Save button enabled: {isButtonEnabled}");
            Assert.That(isButtonEnabled, Is.True, "Save button should be enabled after filling all fields");

            await Actions.ClickElementById(ModalSaveBtn);
            TestContext.Out.WriteLine("Clicked Save button");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            // Find the created user's ID by searching for the name
            var nameInputs = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            TestContext.Out.WriteLine($"Found {nameInputs.Count} user name inputs");
            TestContext.Out.WriteLine($"Looking for: '{_createdUserName}'");

            foreach (var input in nameInputs)
            {
                var value = await input.InputValueAsync();
                var inputId = await input.GetAttributeAsync("id");
                TestContext.Out.WriteLine($"  User: '{value}' (id: {inputId})");

                if (value.StartsWith(_createdUserName))
                {
                    _createdUserId = inputId?.Replace(RowNamePrefix, "");
                    TestContext.Out.WriteLine($"  -> MATCH! Created user ID: {_createdUserId}");
                    break;
                }
            }

            // Assert
            Assert.That(_createdUserId, Is.Not.Null, "Created user should be found in the list");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"User created successfully: {_createdUserName}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ChangeNewUserRole()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Change New User Role ===");

            if (string.IsNullOrEmpty(_createdUserId))
            {
                TestContext.Out.WriteLine("No user was created in Step2 - skipping");
                Assert.Inconclusive("No user was created in previous step");
                return;
            }

            // Act - Find the admin select for the created user
            var adminSelectId = $"{RowAdminPrefix}{_createdUserId}";
            var adminSelect = await Actions.FindElementById(adminSelectId);
            Assert.That(adminSelect, Is.Not.Null, $"Admin select for user {_createdUserId} should exist");

            var currentValue = await adminSelect!.InputValueAsync();
            TestContext.Out.WriteLine($"Current admin value: {currentValue}");

            var newValue = currentValue == "true" ? "false" : "true";
            await adminSelect.SelectOptionAsync(newValue);
            await Actions.Wait500();

            // Assert
            var updatedValue = await adminSelect.InputValueAsync();
            Assert.That(updatedValue, Is.EqualTo(newValue), "Admin role should have changed");
            TestContext.Out.WriteLine($"Admin role changed from {currentValue} to {updatedValue}");

            // Restore original value
            await adminSelect.SelectOptionAsync(currentValue);
            await Actions.Wait500();

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User role changed and restored successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_VerifyUserRowsExist()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify User Rows Exist ===");

            // Act
            var userRows = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");

            // Assert
            Assert.That(userRows.Count, Is.GreaterThan(0), "At least one user should exist");
            TestContext.Out.WriteLine($"Found {userRows.Count} users in the list");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        [Order(5)]
        public async Task Step5_DeleteCreatedUser()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Delete Created User ===");

            if (string.IsNullOrEmpty(_createdUserId))
            {
                TestContext.Out.WriteLine("No user was created - skipping delete");
                Assert.Inconclusive("No user was created in previous step");
                return;
            }

            // Act - Find and click the delete button for the created user
            var deleteButtonId = $"{RowDeletePrefix}{_createdUserId}";
            var deleteButton = await Actions.FindElementById(deleteButtonId);
            Assert.That(deleteButton, Is.Not.Null, $"Delete button for user {_createdUserId} should exist");

            await deleteButton!.ClickAsync();
            await Actions.Wait500();

            // Confirm deletion in modal
            await Actions.ClickElementById(DeleteModalConfirmBtn);
            await Actions.Wait2000();

            // Assert - User should no longer exist (use QuerySelector for fast check without timeout)
            var deletedUserName = await Page.QuerySelectorAsync($"#{RowNamePrefix}{_createdUserId}");
            Assert.That(deletedUserName, Is.Null, "Deleted user should no longer exist in the list");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"User {_createdUserName} deleted successfully");
            _createdUserId = null;
            _createdUserName = null;
        }
    }
}
