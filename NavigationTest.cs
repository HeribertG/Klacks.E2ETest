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
            await Page.GotoAsync(BaseUrl);
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
            TestContext.Out.WriteLine("Starting navigation through all menu items test");

            // Navigate to Absence
            await NavigateAndVerify(MainNavIds.OpenAbsenceId, "absence", "Absence page");

            // Navigate to Groups (only visible for admin users)
            var groupsButton = await Actions.FindElementById(MainNavIds.OpenGroupsId);
            if (groupsButton != null)
            {
                await NavigateAndVerify(MainNavIds.OpenGroupsId, "group", "Groups page");
            }
            else
            {
                TestContext.Out.WriteLine("Groups menu item not visible - user might not be admin");
            }

            // Navigate to Shifts
            await NavigateAndVerify(MainNavIds.OpenShiftsId, "shift", "Shifts page");

            // Navigate to Schedules
            await NavigateAndVerify(MainNavIds.OpenSchedulesId, "schedule", "Schedules page");

            // Navigate to Employees
            await NavigateAndVerify(MainNavIds.OpenEmployeesId, "client", "Employees page");

            // Navigate to Profile
            await NavigateToProfile();

            // Navigate to Settings (only visible for admin users)
            var settingsButton = await Actions.FindElementById(MainNavIds.OpenSettingsId);
            if (settingsButton != null)
            {
                await NavigateAndVerify(MainNavIds.OpenSettingsId, "settings", "Settings page");
            }
            else
            {
                TestContext.Out.WriteLine("Settings menu item not visible - user might not be admin");
            }

            TestContext.Out.WriteLine("Navigation test completed successfully");
        }

        [Test]
        [Order(2)]
        public async Task VerifyNavigationTooltips()
        {
            TestContext.Out.WriteLine("Verifying navigation tooltips");

            // Check tooltips for each navigation item
            await VerifyTooltip(MainNavIds.OpenAbsenceId, "Alt+1");
            
            var groupsButton = await Actions.FindElementById(MainNavIds.OpenGroupsId);
            if (groupsButton != null)
            {
                await VerifyTooltip(MainNavIds.OpenGroupsId, "Alt+2");
            }

            await VerifyTooltip(MainNavIds.OpenShiftsId, "Alt+3");
            await VerifyTooltip(MainNavIds.OpenSchedulesId, "Alt+4");
            await VerifyTooltip(MainNavIds.OpenEmployeesId, "Alt+5");

            TestContext.Out.WriteLine("Tooltip verification completed");
        }

        [Test]
        [Order(3)]
        public async Task VerifyKeyboardShortcuts()
        {
            TestContext.Out.WriteLine("Testing keyboard shortcuts for navigation");

            // Test Alt+1 for Absence
            await Page.Keyboard.PressAsync("Alt+1");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            Assert.That(Page.Url.Contains("absence"), Is.True, "Alt+1 should navigate to Absence page");

            // Test Alt+3 for Shifts
            await Page.Keyboard.PressAsync("Alt+3");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            Assert.That(Page.Url.Contains("shift"), Is.True, "Alt+3 should navigate to Shifts page");

            // Test Alt+4 for Schedules
            await Page.Keyboard.PressAsync("Alt+4");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            Assert.That(Page.Url.Contains("schedule"), Is.True, "Alt+4 should navigate to Schedules page");

            // Test Alt+5 for Employees
            await Page.Keyboard.PressAsync("Alt+5");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            Assert.That(Page.Url.Contains("client"), Is.True, "Alt+5 should navigate to Employees page");

            TestContext.Out.WriteLine("Keyboard shortcut test completed");
        }

        private async Task NavigateAndVerify(string elementId, string urlContains, string pageName)
        {
            TestContext.Out.WriteLine($"Navigating to {pageName}");
            TestContext.Out.WriteLine($"Current URL before navigation: {Page.Url}");
            
            // Check if element exists and is visible
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
            
            // Try clicking with retry
            await Actions.ClickButtonById(elementId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
            
            // If URL hasn't changed, try clicking again
            if (!_pageTracker.HasChanged(Page))
            {
                TestContext.Out.WriteLine($"First click didn't navigate, retrying...");
                await Actions.ClickButtonById(elementId);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            TestContext.Out.WriteLine($"Current URL after navigation: {Page.Url}");
            TestContext.Out.WriteLine($"Expected URL to contain: {urlContains}");
            TestContext.Out.WriteLine($"URL has changed: {_pageTracker.HasChanged(Page)}");
            
            // Verify navigation occurred
            Assert.That(_pageTracker.HasChanged(Page), Is.True, $"URL should change when navigating to {pageName}. Before: {_pageTracker.InitialUrl}, After: {Page.Url}");
            Assert.That(Page.Url.Contains(urlContains), Is.True, $"URL should contain '{urlContains}' for {pageName}. Actual URL: {Page.Url}");

            // Wait for page content to load
            await Actions.WaitForElementToBeStable(elementId);
            
            // Check for API errors
            Assert.That(_listener.HasApiErrors(), Is.False, $"No API errors should occur when loading {pageName}");
            
            TestContext.Out.WriteLine($"Successfully navigated to {pageName}");
        }

        private async Task NavigateToProfile()
        {
            TestContext.Out.WriteLine("Navigating to Profile");
            
            _pageTracker = new PageUrlTracker(Page);

            // Profile navigation might be different (icon or image)
            var profileButton = await Actions.FindElementById(MainNavIds.OpenProfileId);
            if (profileButton != null)
            {
                await Actions.ClickButtonById(MainNavIds.OpenProfileId);
            }
            else
            {
                // Try clicking on the image container if user has profile image
                var imageContainer = await Page.QuerySelectorAsync(".imgIconContainer");
                if (imageContainer != null)
                {
                    await imageContainer.ClickAsync();
                }
                else
                {
                    // Click on user icon
                    await Page.ClickAsync(".icon_user");
                }
            }

            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            Assert.That(_pageTracker.HasChanged(Page), Is.True, "URL should change when navigating to Profile");
            Assert.That(Page.Url.Contains("profile") || Page.Url.Contains("user"), Is.True, "URL should contain 'profile' or 'user'");
            
            TestContext.Out.WriteLine("Successfully navigated to Profile");
        }

        private async Task VerifyTooltip(string elementId, string expectedTooltipContent)
        {
            var element = await Actions.FindElementById(elementId);
            if (element != null)
            {
                await element.HoverAsync();
                await Actions.Wait500();
                
                // Check if tooltip appears
                var tooltip = await Page.QuerySelectorAsync("[role='tooltip']");
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