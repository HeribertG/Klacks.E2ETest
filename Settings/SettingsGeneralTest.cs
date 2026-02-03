using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsGeneralIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(20)]
    public class SettingsGeneralTest : PlaywrightSetup
    {
        private Listener _listener;
        private const string IconFilePath = "C:\\SourceCode\\Klacks.Ui\\src\\assets\\icon\\Baustelle-mittel.ico";
        private const string LogoFilePath = "C:\\SourceCode\\Klacks.Ui\\src\\assets\\png\\Baustelle-mittel.png";

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            var generalForm = await Page.QuerySelectorAsync("#settings-general-form");
            if (generalForm != null)
            {
                await generalForm.ScrollIntoViewIfNeededAsync();
                await Actions.Wait500();
            }
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
        public async Task Step1_VerifyGeneralSettingsPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify General Settings Page Loaded ===");

            // Assert
            var appNameInput = await Actions.FindElementById(SettingGeneralName);
            Assert.That(appNameInput, Is.Not.Null, "App name input should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("General Settings page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_ChangeAppName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Change App Name ===");
            var newAppName = $"Klacks Test {DateTime.Now.Ticks}";

            // Act
            var appNameInput = await Actions.FindElementById(SettingGeneralName);
            Assert.That(appNameInput, Is.Not.Null, "App name input should exist");

            var originalAppName = await appNameInput!.InputValueAsync();
            TestContext.Out.WriteLine($"Original app name: {originalAppName}");

            await Actions.FillInputAndEnterById(SettingGeneralName, newAppName);
            await Actions.Wait500();

            // Assert
            var currentValue = await appNameInput.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(newAppName), "App name should be updated");

            TestContext.Out.WriteLine($"App name changed to: {newAppName}");

            // Restore original name
            await Actions.FillInputAndEnterById(SettingGeneralName, originalAppName);
            await Actions.Wait500();
        }

        [Test]
        [Order(3)]
        public async Task Step3_VerifyLogoUploadSection()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Verify Logo Upload Section ===");

            // Act & Assert - Use QuerySelector for fast lookup
            var logoUploadArea = await Page.QuerySelectorAsync($"#{SettingGeneralLogoUploadArea}");
            var deleteLogoButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteLogoBtn}");

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
        [Order(4)]
        public async Task Step4_VerifyIconUploadSection()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Icon Upload Section ===");

            // Act & Assert - Use QuerySelector for fast lookup
            var iconUploadArea = await Page.QuerySelectorAsync($"#{SettingGeneralIconUploadArea}");
            var deleteIconButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteIconBtn}");

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

        [Test]
        [Order(5)]
        public async Task Step5_DeleteAndUploadIcon()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Delete and Upload Icon ===");

            // Act
            var deleteIconButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteIconBtn}");
            if (deleteIconButton != null)
            {
                TestContext.Out.WriteLine("Icon exists - deleting it first");
                await deleteIconButton.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
                TestContext.Out.WriteLine("Icon deleted successfully");
            }
            else
            {
                TestContext.Out.WriteLine("No icon to delete - proceeding with upload");
            }

            TestContext.Out.WriteLine($"Uploading icon from: {IconFilePath}");
            var iconFileInput = await Page.QuerySelectorAsync($"#{SettingGeneralIconFileInput}");
            Assert.That(iconFileInput, Is.Not.Null, "Icon file input should be available");

            await iconFileInput!.SetInputFilesAsync(IconFilePath);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            // Assert
            deleteIconButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteIconBtn}");
            Assert.That(deleteIconButton, Is.Not.Null, "Delete icon button should be visible after upload");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur during icon upload. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Icon uploaded successfully");
        }

        [Test]
        [Order(6)]
        public async Task Step6_DeleteAndUploadLogo()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Delete and Upload Logo ===");

            // Act
            var deleteLogoButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteLogoBtn}");
            if (deleteLogoButton != null)
            {
                TestContext.Out.WriteLine("Logo exists - deleting it first");
                await deleteLogoButton.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
                TestContext.Out.WriteLine("Logo deleted successfully");
            }
            else
            {
                TestContext.Out.WriteLine("No logo to delete - proceeding with upload");
            }

            TestContext.Out.WriteLine($"Uploading logo from: {LogoFilePath}");
            var logoFileInput = await Page.QuerySelectorAsync($"#{SettingGeneralLogoFileInput}");
            Assert.That(logoFileInput, Is.Not.Null, "Logo file input should be available");

            await logoFileInput!.SetInputFilesAsync(LogoFilePath);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            // Assert
            deleteLogoButton = await Page.QuerySelectorAsync($"#{SettingGeneralDeleteLogoBtn}");
            Assert.That(deleteLogoButton, Is.Not.Null, "Delete logo button should be visible after upload");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur during logo upload. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Logo uploaded successfully");
        }

        [Test]
        [Order(7)]
        public async Task Step7_ChangeAppNameToUnderConstruction()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Change App Name to 'under Construction' ===");
            const string newAppName = "under Construction";

            // Act
            var originalAppName = await Actions.ReadInput(SettingGeneralName);
            TestContext.Out.WriteLine($"Original app name: {originalAppName}");

            await Actions.FillInputById(SettingGeneralName, newAppName);
            await Actions.Wait500();

            // Remove focus from input (blur)
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait1000();

            // Assert
            var currentValue = await Actions.ReadInput(SettingGeneralName);
            TestContext.Out.WriteLine($"Value after blur: {currentValue}");
            Assert.That(currentValue, Is.EqualTo(newAppName), "App name should be updated to 'under Construction'");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"App name changed successfully to: {newAppName}");
        }
    }
}
