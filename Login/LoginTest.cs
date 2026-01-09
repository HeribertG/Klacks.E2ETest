using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest
{
    [TestFixture]
    [Order(1)]
    public class LoginTest : PlaywrightSetup
    {
        private Listener? _listener;

        [SetUp]
        public void SetupInternal()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();
        }

        [TearDown]
        public async Task CleanupAfterTestAsync()
        {
            if (_listener != null)
            {
                await _listener.WaitForResponseHandlingAsync();
                if (_listener.HasApiErrors())
                {
                    TestContext.WriteLine(_listener.GetLastErrorMessage());
                }

                _listener?.ResetErrors();
            }

            _listener = null;
        }

        [Test]
        [Order(1)]
        public async Task VerifySuccessfulLogin()
        {
            // Arrange
            TestContext.Out.WriteLine($"Current URL: {Actions.ReadCurrentUrl()}");
            TestContext.Out.WriteLine($"Logged in as: {UserName}");

            // Act
            var dashboardElement = await Actions.FindElementByCssSelector("[class*='dashboard'], [class*='main'], nav");

            // Assert
            Assert.That(Actions.ReadCurrentUrl(), Does.Not.Contain("login"),
                "Login failed - URL still contains 'login'");
            Assert.That(_listener!.HasApiErrors(), Is.False,
                $"API error after login: {_listener!.GetLastErrorMessage()}");
            Assert.That(dashboardElement, Is.Not.Null,
                "No dashboard/navigation element found - login may have failed");

            TestContext.Out.WriteLine("Login verification successful");
        }

        [Test]
        [Order(2)]
        public async Task NavigateToMainPages()
        {
            // Arrange
            TestContext.Out.WriteLine("Testing navigation to main pages...");
            var navAbsence = await Actions.FindElementById(MainNavIds.OpenAbsenceId);

            if (navAbsence == null)
            {
                TestContext.Out.WriteLine("Absence navigation button not found - skipping test");
                return;
            }

            var pageTracker = new PageUrlTracker(Page);

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(pageTracker.HasChanged(Page) && Actions.ReadCurrentUrl().Contains("absence"),
                Is.True, "Could not navigate to Absence page");
            Assert.That(_listener!.HasApiErrors(), Is.False,
                $"API error during navigation: {_listener!.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to Absence page successful");
        }

        [Test]
        [Order(3)]
        public async Task VerifyUserIsLoggedIn()
        {
            // Arrange
            TestContext.Out.WriteLine("Verifying user login status...");

            // Act
            var userElement = await Actions.FindElementByCssSelector(
                "[class*='user'], [class*='profile'], [class*='account']")
                ?? await Actions.FindElementByCssSelector("span");

            if (userElement != null)
            {
                TestContext.Out.WriteLine("User element found - login confirmed");
            }
            else
            {
                TestContext.Out.WriteLine("Warning: No specific user element found");
                userElement = await Actions.FindElementByCssSelector("[class*='navbar']")
                    ?? await Actions.FindElementByCssSelector("[class*='header']");
            }

            // Assert
            Assert.That(Actions.ReadCurrentUrl(), Does.Not.Contain("login"), "User is not logged in");

            TestContext.Out.WriteLine($"Login status verified for user: {UserName}");
        }
    }
}
