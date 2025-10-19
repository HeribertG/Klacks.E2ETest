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
            var appNameInput = await Actions.FindElementByCssSelector("input[name='appName'], #appName");
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
            var appNameInput = await Actions.FindElementByCssSelector("input[name='appName'], #appName");
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

            // Act & Assert
            var logoUploadButton = await Actions.FindElementByCssSelector("input[type='file'], button:has-text('Upload Logo'), button:has-text('Logo hochladen')");
            Assert.That(logoUploadButton, Is.Not.Null, "Logo upload option should be available");

            TestContext.Out.WriteLine("Logo upload section is available");
        }

        [Test]
        public async Task Step4_VerifyIconUploadSection()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Icon Upload Section ===");

            // Act & Assert
            var iconUploadButton = await Actions.FindElementByCssSelector("input[type='file']");
            Assert.That(iconUploadButton, Is.Not.Null, "Icon upload option should be available");

            TestContext.Out.WriteLine("Icon upload section is available");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
