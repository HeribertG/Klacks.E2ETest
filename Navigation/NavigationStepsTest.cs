using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest
{
    [TestFixture]
    [Order(2)]
    public class NavigationStepsTest : PlaywrightSetup
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
        [Order(1)]
        public async Task Step1_VerifyLoginSuccessful()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Login ===");
            TestContext.Out.WriteLine($"Current URL: {Actions.ReadCurrentUrl()}");
            TestContext.Out.WriteLine($"Logged in as: {UserName}");

            // Assert
            Assert.That(Actions.ReadCurrentUrl(), Does.Not.Contain("login"),
                "Login should be successful - URL should not contain 'login'");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur during login. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Login verification completed successfully");
            await Actions.Wait1000();
        }

        [Test]
        [Order(2)]
        public async Task Step2_NavigateToAbsencePage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Navigate to Absence Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("absence"), Is.True, "Should be on absence page");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Successfully navigated to absence page: {Actions.ReadCurrentUrl()}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_NavigateToGroupsPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Navigate to Groups Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenGroupsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("group"), Is.True, "Should be on groups page");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Successfully navigated to groups page: {Actions.ReadCurrentUrl()}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_NavigateToShiftsPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Navigate to Shifts Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenShiftsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("shift"), Is.True, "Should be on shifts page");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Successfully navigated to shifts page: {Actions.ReadCurrentUrl()}");
        }

        [Test]
        [Order(5)]
        public async Task Step5_NavigateToSchedulesPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Navigate to Schedules Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("schedule"), Is.True, "Should be on schedules page");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Successfully navigated to schedules page: {Actions.ReadCurrentUrl()}");
        }

        [Test]
        [Order(6)]
        public async Task Step6_NavigateToEmployeesPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Navigate to Employees Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("client"), Is.True, "Should be on employees/clients page");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Successfully navigated to employees page: {Actions.ReadCurrentUrl()}");
        }

        [Test]
        [Order(7)]
        public async Task Step7_NavigateToProfilePage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Navigate to Profile Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenProfileId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("profile"), Is.True, "Should be on profile page");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Successfully navigated to profile page: {Actions.ReadCurrentUrl()}");
        }

        [Test]
        [Order(8)]
        public async Task Step8_NavigateToSettingsPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Navigate to Settings Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("settings"), Is.True, "Should be on settings page");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Successfully navigated to settings page: {Actions.ReadCurrentUrl()}");
        }
    }
}
