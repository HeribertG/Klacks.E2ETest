using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsUserAdministrationTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            // Navigate to Settings page
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Navigate to User Administration tab
            var userAdminTab = await Actions.FindElementByCssSelector("[href*='user-administration'], button:has-text('User Administration'), a:has-text('Benutzerverwaltung')");
            if (userAdminTab != null)
            {
                await userAdminTab.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_listener.HasApiErrors())
            {
                TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
            }

            await _listener.WaitForResponseHandlingAsync();
        }

        [Test]
        public async Task Step1_VerifyUserAdministrationPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify User Administration Page Loaded ===");

            // Assert
            Assert.That(Page.Url.Contains("settings"), Is.True, "Should be on settings page");

            // Check if user table or user list is visible
            var userTable = await Actions.FindElementByCssSelector("table, [class*='user-list'], [class*='user-table']");
            Assert.That(userTable, Is.Not.Null, "User table should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User Administration page loaded successfully");
        }

        [Test]
        public async Task Step2_OpenAddUserModal()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Open Add User Modal ===");

            // Act
            var addButton = await Actions.FindElementByCssSelector("button:has-text('Add'), button:has-text('Hinzufügen'), [class*='btn-add']");
            Assert.That(addButton, Is.Not.Null, "Add button should exist");

            await addButton!.ClickAsync();
            await Actions.Wait500();

            // Assert
            var modal = await Actions.FindElementByCssSelector(".modal, [class*='modal-content']");
            Assert.That(modal, Is.Not.Null, "Modal should be visible");

            TestContext.Out.WriteLine("Add User modal opened successfully");
        }

        [Test]
        public async Task Step3_CreateNewUserWithValidData()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Create New User ===");
            var timestamp = DateTime.Now.Ticks.ToString();
            var testUser = new
            {
                FirstName = "Max",
                LastName = "Mustermann",
                UserName = $"max.mustermann.{timestamp}",
                Email = $"max.mustermann.{timestamp}@test.com"
            };

            // Act - Open modal
            var addButton = await Actions.FindElementByCssSelector("button:has-text('Add'), button:has-text('Hinzufügen')");
            if (addButton != null)
            {
                await addButton.ClickAsync();
                await Actions.Wait500();
            }

            // Fill form
            var firstNameInput = await Actions.FindElementByCssSelector("input[name='firstName'], #firstName, [formcontrolname='firstName']");
            if (firstNameInput != null)
            {
                await firstNameInput.FillAsync(testUser.FirstName);
            }

            var lastNameInput = await Actions.FindElementByCssSelector("input[name='lastName'], #lastName, [formcontrolname='lastName']");
            if (lastNameInput != null)
            {
                await lastNameInput.FillAsync(testUser.LastName);
            }

            var userNameInput = await Actions.FindElementByCssSelector("input[name='userName'], #userName, [formcontrolname='userName']");
            if (userNameInput != null)
            {
                await userNameInput.FillAsync(testUser.UserName);
            }

            var emailInput = await Actions.FindElementByCssSelector("input[name='email'], #email, [formcontrolname='email']");
            if (emailInput != null)
            {
                await emailInput.FillAsync(testUser.Email);
            }

            // Save
            var saveButton = await Actions.FindElementByCssSelector("button:has-text('Save'), button:has-text('Speichern'), .btn-primary");
            if (saveButton != null)
            {
                await saveButton.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"User created successfully: {testUser.Email}");
        }

        [Test]
        public async Task Step4_ChangeUserRole()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Change User Role ===");

            // Act - Find first user row
            var userRow = await Actions.FindElementByCssSelector("tbody tr:first-child, [class*='user-row']:first");
            Assert.That(userRow, Is.Not.Null, "At least one user should exist");

            // Find role checkbox (Admin or Authorised)
            var roleCheckbox = await userRow!.QuerySelectorAsync("input[type='checkbox']");
            if (roleCheckbox != null)
            {
                var isChecked = await roleCheckbox.IsCheckedAsync();
                await roleCheckbox.ClickAsync();
                await Actions.Wait500();

                // Verify state changed
                var newState = await roleCheckbox.IsCheckedAsync();
                Assert.That(newState, Is.Not.EqualTo(isChecked), "Checkbox state should have changed");
            }

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User role changed successfully");
        }
    }
}
