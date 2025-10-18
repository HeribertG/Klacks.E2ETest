using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Navigation
{
    [TestFixture]
    public class NavigationTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public void Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();
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
        public async Task Step1_VerifyLoginSuccessful()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Login ===");
            TestContext.Out.WriteLine($"Current URL: {Page.Url}");
            TestContext.Out.WriteLine($"Logged in as: {UserName}");

            // Assert
            Assert.That(Page.Url, Does.Not.Contain("login"),
                "Login should be successful - URL should not contain 'login'");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur during login. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Login verification completed successfully");

            await Actions.Wait1000();
        }

        [Test]
        public async Task Step2_NavigateToAbsencePage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Navigate to Absence Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("absence"), Is.True, "Should be on absence page");
            TestContext.Out.WriteLine($"Successfully navigated to absence page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to absence page completed successfully");
        }

        [Test]
        public async Task Step3_NavigateToGroupsPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Navigate to Groups Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("group"), Is.True, "Should be on groups page");
            TestContext.Out.WriteLine($"Successfully navigated to groups page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to groups page completed successfully");
        }

        [Test]
        public async Task Step4_NavigateToShiftsPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Navigate to Shifts Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenShiftsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("shift"), Is.True, "Should be on shifts page");
            TestContext.Out.WriteLine($"Successfully navigated to shifts page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to shifts page completed successfully");
        }

        [Test]
        public async Task Step5_NavigateToSchedulesPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Navigate to Schedules Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("schedule"), Is.True, "Should be on schedules page");
            TestContext.Out.WriteLine($"Successfully navigated to schedules page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to schedules page completed successfully");
        }

        [Test]
        public async Task Step6_NavigateToEmployeesPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Navigate to Employees Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("client"), Is.True, "Should be on employees/clients page");
            TestContext.Out.WriteLine($"Successfully navigated to employees page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to employees page completed successfully");
        }

        [Test]
        public async Task Step7_NavigateToProfilePage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Navigate to Profile Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenProfileId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("profile"), Is.True, "Should be on profile page");
            TestContext.Out.WriteLine($"Successfully navigated to profile page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to profile page completed successfully");
        }

        [Test]
        public async Task Step8_NavigateToSettingsPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Navigate to Settings Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("settings"), Is.True, "Should be on settings page");
            TestContext.Out.WriteLine($"Successfully navigated to settings page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to settings page completed successfully");
        }
    }
}
