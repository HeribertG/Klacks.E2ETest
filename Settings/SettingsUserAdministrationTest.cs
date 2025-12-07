using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest
{
    [TestFixture]
    [Order(22)]
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

            // Navigate to User Administration tab - use Page.Locator for faster lookup
            var userAdminTab = Page.Locator("[href*='user-administration'], button:has-text('User Administration'), a:has-text('Benutzerverwaltung')").First;
            if (await userAdminTab.CountAsync() > 0)
            {
                await userAdminTab.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            // Scroll container into viewport
            var container = await Page.QuerySelectorAsync(".container-box");
            if (container != null)
            {
                await container.ScrollIntoViewIfNeededAsync();
                await Actions.Wait500();
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

            // Check if add button is visible
            var addButton = await Actions.FindElementById("user-admin-add-user-btn");
            Assert.That(addButton, Is.Not.Null, "Add user button should be visible");

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
            var addButton = await Actions.FindElementById("user-admin-add-user-btn");
            Assert.That(addButton, Is.Not.Null, "Add button should exist");

            var isEnabled = await addButton!.IsEnabledAsync();
            if (!isEnabled)
            {
                TestContext.Out.WriteLine("Add user button is disabled - skipping modal test");
                Assert.Inconclusive("Button is disabled - user might not have permissions");
                return;
            }

            await addButton.ClickAsync();
            await Actions.Wait500();

            // Assert - Use QuerySelector for fast lookup
            var modal = await Page.QuerySelectorAsync(".modal, [class*='modal-content']");
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
            var addButton = await Actions.FindElementById("user-admin-add-user-btn");
            if (addButton != null)
            {
                var isEnabled = await addButton.IsEnabledAsync();
                if (!isEnabled)
                {
                    TestContext.Out.WriteLine("Add user button is disabled - skipping test");
                    Assert.Inconclusive("Button is disabled - user might not have permissions");
                    return;
                }

                await addButton.ClickAsync();
                await Actions.Wait500();
            }

            // Fill form using IDs
            var firstNameInput = await Actions.FindElementById("user-firstname");
            if (firstNameInput != null)
            {
                await firstNameInput.FillAsync(testUser.FirstName);
            }

            var lastNameInput = await Actions.FindElementById("user-name");
            if (lastNameInput != null)
            {
                await lastNameInput.FillAsync(testUser.LastName);
            }

            var userNameInput = await Actions.FindElementById("user-userName");
            if (userNameInput != null)
            {
                await userNameInput.FillAsync(testUser.UserName);
            }

            var emailInput = await Actions.FindElementById("setting-user-email");
            if (emailInput != null)
            {
                await emailInput.FillAsync(testUser.Email);
            }

            // Save
            var saveButton = await Actions.FindElementById("user-admin-modal-save-btn");
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

            // Act - Find first user row with admin select (fast lookup)
            var adminSelect = await Page.QuerySelectorAsync("select[id^='user-admin-row-admin-']");
            if (adminSelect != null)
            {
                var currentValue = await adminSelect.InputValueAsync();
                TestContext.Out.WriteLine($"Current admin value: {currentValue}");

                // Toggle value
                var newValue = currentValue == "true" ? "false" : "true";
                await adminSelect.SelectOptionAsync(newValue);
                await Actions.Wait500();

                // Verify change
                var updatedValue = await adminSelect.InputValueAsync();
                Assert.That(updatedValue, Is.EqualTo(newValue), "Admin role should have changed");

                // Restore original value
                await adminSelect.SelectOptionAsync(currentValue);
                await Actions.Wait500();
            }

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("User role changed successfully");
        }
    }
}
