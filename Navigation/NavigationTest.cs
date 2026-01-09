using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest
{
    [TestFixture]
    [Order(3)]
    [NonParallelizable]
    public class NavigationTest : PlaywrightSetup
    {
        private PageUrlTracker _pageTracker;
        private Listener _listener;

        [SetUp]
        public async Task Setup()
        {
            _pageTracker = new PageUrlTracker(Page);
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();
            await Actions.NavigateTo(BaseUrl);
            await Actions.WaitForSpinnerToDisappear();
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
        public async Task NavigateThroughAllMenuItems()
        {
            // Arrange
            TestContext.Out.WriteLine("Starting navigation through all menu items test");

            // Act
            await NavigateAndVerify(MainNavIds.OpenAbsenceId, "absence", "Absence page");

            var groupsButton = await Actions.FindElementById(MainNavIds.OpenGroupsId);
            if (groupsButton != null)
            {
                await NavigateAndVerify(MainNavIds.OpenGroupsId, "group", "Groups page");
            }
            else
            {
                TestContext.Out.WriteLine("Groups menu item not visible - user might not be admin");
            }

            await NavigateAndVerify(MainNavIds.OpenShiftsId, "shift", "Shifts page");
            await NavigateAndVerify(MainNavIds.OpenSchedulesId, "schedule", "Schedules page");
            await NavigateAndVerify(MainNavIds.OpenEmployeesId, "client", "Employees page");
            await NavigateToProfile();

            var settingsButton = await Actions.FindElementById(MainNavIds.OpenSettingsId);
            if (settingsButton != null)
            {
                await NavigateAndVerify(MainNavIds.OpenSettingsId, "settings", "Settings page");
            }
            else
            {
                TestContext.Out.WriteLine("Settings menu item not visible - user might not be admin");
            }

            // Assert
            TestContext.Out.WriteLine("Navigation test completed successfully");
        }

        [Test]
        [Order(2)]
        public async Task VerifyNavigationTooltips()
        {
            // Arrange
            TestContext.Out.WriteLine("Verifying navigation tooltips");

            // Act
            await VerifyTooltip(MainNavIds.OpenAbsenceId, "Alt+1");

            var groupsButton = await Actions.FindElementById(MainNavIds.OpenGroupsId);
            if (groupsButton != null)
            {
                await VerifyTooltip(MainNavIds.OpenGroupsId, "Alt+2");
            }

            await VerifyTooltip(MainNavIds.OpenShiftsId, "Alt+3");
            await VerifyTooltip(MainNavIds.OpenSchedulesId, "Alt+4");
            await VerifyTooltip(MainNavIds.OpenEmployeesId, "Alt+5");

            // Assert
            TestContext.Out.WriteLine("Tooltip verification completed");
        }

        [Test]
        [Order(3)]
        public async Task VerifyKeyboardShortcuts()
        {
            // Arrange
            TestContext.Out.WriteLine("Testing keyboard shortcuts for navigation");

            // Act
            await Actions.PressKey("Alt+1");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("absence"), Is.True, "Alt+1 should navigate to Absence page");

            // Act
            await Actions.PressKey("Alt+3");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("shift"), Is.True, "Alt+3 should navigate to Shifts page");

            // Act
            await Actions.PressKey("Alt+4");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("schedule"), Is.True, "Alt+4 should navigate to Schedules page");

            // Act
            await Actions.PressKey("Alt+5");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            // Assert
            Assert.That(Actions.ReadCurrentUrl().Contains("client"), Is.True, "Alt+5 should navigate to Employees page");

            TestContext.Out.WriteLine("Keyboard shortcut test completed");
        }

        private async Task NavigateAndVerify(string elementId, string urlContains, string pageName)
        {
            TestContext.Out.WriteLine($"Navigating to {pageName}");
            TestContext.Out.WriteLine($"Current URL before navigation: {Actions.ReadCurrentUrl()}");

            var element = await Actions.FindElementById(elementId);
            if (element == null)
            {
                Assert.Fail($"Element with ID '{elementId}' not found for {pageName}");
            }

            var isVisible = await element.IsVisibleAsync();
            if (!isVisible)
            {
                Assert.Fail($"Element with ID '{elementId}' is not visible for {pageName}");
            }

            _pageTracker = new PageUrlTracker(Page);

            await Actions.ClickButtonById(elementId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            if (!_pageTracker.HasChanged(Page))
            {
                TestContext.Out.WriteLine($"First click didn't navigate, retrying...");
                await Actions.ClickButtonById(elementId);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            TestContext.Out.WriteLine($"Current URL after navigation: {Actions.ReadCurrentUrl()}");
            TestContext.Out.WriteLine($"Expected URL to contain: {urlContains}");
            TestContext.Out.WriteLine($"URL has changed: {_pageTracker.HasChanged(Page)}");

            Assert.That(_pageTracker.HasChanged(Page), Is.True, $"URL should change when navigating to {pageName}. Before: {_pageTracker.InitialUrl}, After: {Actions.ReadCurrentUrl()}");
            Assert.That(Actions.ReadCurrentUrl().Contains(urlContains), Is.True, $"URL should contain '{urlContains}' for {pageName}. Actual URL: {Actions.ReadCurrentUrl()}");

            await Actions.WaitForElementToBeStable(elementId);

            Assert.That(_listener.HasApiErrors(), Is.False, $"No API errors should occur when loading {pageName}");

            TestContext.Out.WriteLine($"Successfully navigated to {pageName}");
        }

        private async Task NavigateToProfile()
        {
            TestContext.Out.WriteLine("Navigating to Profile");

            _pageTracker = new PageUrlTracker(Page);

            var profileButton = await Actions.FindElementById(MainNavIds.OpenProfileId);
            if (profileButton != null)
            {
                await Actions.ClickButtonById(MainNavIds.OpenProfileId);
            }
            else
            {
                var imageContainer = await Actions.FindElementByCssSelector(".imgIconContainer");
                if (imageContainer != null)
                {
                    await Actions.ClickElement(imageContainer);
                }
                else
                {
                    var iconUser = await Actions.FindElementByCssSelector(".icon_user");
                    if (iconUser != null)
                    {
                        await Actions.ClickElement(iconUser);
                    }
                }
            }

            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            Assert.That(_pageTracker.HasChanged(Page), Is.True, "URL should change when navigating to Profile");
            Assert.That(Actions.ReadCurrentUrl().Contains("profile") || Actions.ReadCurrentUrl().Contains("user"), Is.True, "URL should contain 'profile' or 'user'");

            TestContext.Out.WriteLine("Successfully navigated to Profile");
        }

        private async Task VerifyTooltip(string elementId, string expectedTooltipContent)
        {
            var element = await Actions.FindElementById(elementId);
            if (element != null)
            {
                await element.HoverAsync();
                await Actions.Wait500();

                var tooltip = await Actions.FindElementByCssSelector("[role='tooltip']");
                if (tooltip != null)
                {
                    var tooltipText = await tooltip.TextContentAsync();
                    Assert.That(tooltipText.Contains(expectedTooltipContent), Is.True,
                        $"Tooltip for {elementId} should contain '{expectedTooltipContent}'");
                }
            }
        }
    }
}
