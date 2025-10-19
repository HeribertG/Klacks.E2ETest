using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsGeneralTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            // Navigate to Settings
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Navigate to General Settings tab
            var generalTab = await Actions.FindElementByCssSelector("[href*='general'], button:has-text('General'), a:has-text('Allgemein')");
            if (generalTab != null)
            {
                await generalTab.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait500();
            }

            // Scroll container into viewport
            var container = await Actions.FindElementByCssSelector("form");
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
        public async Task Step1_VerifyGeneralSettingsPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify General Settings Page Loaded ===");

            // Assert
            var appNameInput = await Actions.FindElementById("setting-general-name");
            Assert.That(appNameInput, Is.Not.Null, "App name input should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("General Settings page loaded successfully");
        }

        [Test]
        public async Task Step2_ChangeAppName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Change App Name ===");
            var newAppName = $"Klacks Test {DateTime.Now.Ticks}";

            // Act
            var appNameInput = await Actions.FindElementById("setting-general-name");
            Assert.That(appNameInput, Is.Not.Null, "App name input should exist");

            var originalAppName = await appNameInput!.InputValueAsync();
            TestContext.Out.WriteLine($"Original app name: {originalAppName}");

            await appNameInput.FillAsync(newAppName);
            await Actions.Wait500();

            // Assert
            var currentValue = await appNameInput.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(newAppName), "App name should be updated");

            TestContext.Out.WriteLine($"App name changed to: {newAppName}");

            // Restore original name
            await appNameInput.FillAsync(originalAppName);
            await Actions.Wait500();
        }

        [Test]
        public async Task Step3_VerifyLogoUploadSection()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Verify Logo Upload Section ===");

            // Act & Assert - Use QuerySelector for fast lookup
            var logoUploadArea = await Page.QuerySelectorAsync("#setting-general-logo-upload-area");
            var deleteLogoButton = await Page.QuerySelectorAsync("#setting-general-delete-logo-btn");

            if (logoUploadArea != null)
            {
                TestContext.Out.WriteLine("Logo upload area is available - no logo currently uploaded");
                Assert.Pass("Logo upload area found");
            }
            else if (deleteLogoButton != null)
            {
                TestContext.Out.WriteLine("Logo already uploaded - delete button visible instead of upload area");
                Assert.Pass("Logo delete button found - logo exists");
            }
            else
            {
                Assert.Fail("Neither logo upload area nor delete button found");
            }
        }

        [Test]
        public async Task Step4_VerifyIconUploadSection()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Icon Upload Section ===");

            // Act & Assert - Use QuerySelector for fast lookup
            var iconUploadArea = await Page.QuerySelectorAsync("#setting-general-icon-upload-area");
            var deleteIconButton = await Page.QuerySelectorAsync("#setting-general-delete-icon-btn");

            if (iconUploadArea != null)
            {
                TestContext.Out.WriteLine("Icon upload area is available - no icon currently uploaded");
                Assert.Pass("Icon upload area found");
            }
            else if (deleteIconButton != null)
            {
                TestContext.Out.WriteLine("Icon already uploaded - delete button visible instead of upload area");
                Assert.Pass("Icon delete button found - icon exists");
            }
            else
            {
                Assert.Fail("Neither icon upload area nor delete button found");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
