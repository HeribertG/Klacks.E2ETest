// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(50)]
    public class ChatbotSettingsGeneralTest : ChatbotTestBase
    {
        private const string SkillGetPermissions = "get_user_permissions";
        private const string SkillGetGeneralSettings = "get_general_settings";
        private const string SkillUpdateGeneralSettings = "update_general_settings";

        private const string PageKeySettingsGeneral = "settings-general";
        private const string PageKeyMainNav = "main-nav";

        private const string TestAppName = "KlacksTestLLM";
        private const string OriginalAppName = "Klacks";

        private const string IconFilePath = @"C:\Users\hgasp\OneDrive\Bilder\Baustelle-mittel.ico";
        private const string LogoFilePath = @"C:\Users\hgasp\OneDrive\Bilder\Baustelle-mittel.png";

        private const string ControlKeySettings = "settings";
        private const string ControlKeyDeleteIconBtn = "delete-icon-btn";
        private const string ControlKeyIconFileInput = "icon-file-input";
        private const string ControlKeyDeleteLogoBtn = "delete-logo-btn";
        private const string ControlKeyLogoFileInput = "logo-file-input";
        private const string SettingsFormId = "settings-general-form";

        private int _messageCountBefore;
        private Dictionary<string, string> _settingsGeneralSelectors = new();
        private Dictionary<string, string> _mainNavSelectors = new();

        [OneTimeSetUp]
        public async Task LoadSelectors()
        {
            _settingsGeneralSelectors = await DbHelper.GetUiControlSelectorsAsync(PageKeySettingsGeneral);
            Assert.That(_settingsGeneralSelectors, Is.Not.Empty, "Settings-general selectors must be loaded from ui_controls");

            _mainNavSelectors = await DbHelper.GetUiControlSelectorsAsync(PageKeyMainNav);
            Assert.That(_mainNavSelectors, Is.Not.Empty, "Main-nav selectors must be loaded from ui_controls");
        }

        [Test, Order(1)]
        public async Task Step1_OpenChat()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatMessages = await Actions.FindElementById(GetChatSelector(ControlKeyMessages));
            Assert.That(chatMessages, Is.Not.Null, "Chat messages container should be visible");

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test, Order(2)]
        public async Task Step2_VerifyAdminRights()
        {
            TestContext.Out.WriteLine("=== Step 2: Verify Admin Rights ===");
            await AssertSkillEnabled(SkillGetPermissions);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"User must have Admin rights for settings tests. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Admin rights verified successfully");
        }

        [Test, Order(3)]
        public async Task Step3_AskAppName()
        {
            TestContext.Out.WriteLine("=== Step 3: Ask App Name ===");
            await AssertSkillEnabled(SkillGetGeneralSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie heisst die App?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with a message");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name query completed successfully");
        }

        [Test, Order(4)]
        public async Task Step4_ChangeAppName()
        {
            TestContext.Out.WriteLine("=== Step 4: Change App Name ===");
            await AssertSkillEnabled(SkillUpdateGeneralSettings);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze den App-Namen auf {TestAppName}");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with a confirmation");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name change request completed");
        }

        [Test, Order(5)]
        public async Task Step5_VerifyChangedName()
        {
            TestContext.Out.WriteLine("=== Step 5: Verify Changed App Name ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie heisst die App jetzt?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");
            Assert.That(response.Contains(TestAppName, StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should contain '{TestAppName}'. Got: {response}");

            TestContext.Out.WriteLine("App name verification successful");
        }

        [Test, Order(6)]
        public async Task Step6_ResetAppName()
        {
            TestContext.Out.WriteLine("=== Step 6: Reset App Name ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze den App-Namen auf {OriginalAppName}");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with a confirmation");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("App name reset completed");
        }

        [Test, Order(7)]
        public async Task Step7_UploadIcon()
        {
            TestContext.Out.WriteLine("=== Step 7: Upload Icon ===");

            await CloseChatIfOpen();
            await Actions.ClickButtonById(_mainNavSelectors[ControlKeySettings]);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            var deleteIconSelector = _settingsGeneralSelectors[ControlKeyDeleteIconBtn];
            var deleteIconButton = await Actions.FindElementById(deleteIconSelector);
            if (deleteIconButton != null)
            {
                TestContext.Out.WriteLine("Icon exists - deleting it first");
                await Actions.ClickElementById(deleteIconSelector);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            var iconFileInputSelector = _settingsGeneralSelectors[ControlKeyIconFileInput];
            TestContext.Out.WriteLine($"Uploading icon from: {IconFilePath}");
            var iconFileInput = await Actions.FindElementById(iconFileInputSelector);
            Assert.That(iconFileInput, Is.Not.Null, "Icon file input should be available");

            await iconFileInput!.SetInputFilesAsync(IconFilePath);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            var deleteIconBtnAfter = await Actions.FindElementById(deleteIconSelector);
            Assert.That(deleteIconBtnAfter, Is.Not.Null, "Delete icon button should be visible after upload");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur during icon upload. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Icon uploaded successfully");
        }

        [Test, Order(8)]
        public async Task Step8_UploadLogo()
        {
            TestContext.Out.WriteLine("=== Step 8: Upload Logo ===");

            var settingsForm = await Actions.FindElementById(SettingsFormId);
            if (settingsForm == null)
            {
                await Actions.ClickButtonById(_mainNavSelectors[ControlKeySettings]);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            var deleteLogoSelector = _settingsGeneralSelectors[ControlKeyDeleteLogoBtn];
            var deleteLogoButton = await Actions.FindElementById(deleteLogoSelector);
            if (deleteLogoButton != null)
            {
                TestContext.Out.WriteLine("Logo exists - deleting it first");
                await Actions.ClickElementById(deleteLogoSelector);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }

            var logoFileInputSelector = _settingsGeneralSelectors[ControlKeyLogoFileInput];
            TestContext.Out.WriteLine($"Uploading logo from: {LogoFilePath}");
            var logoFileInput = await Actions.FindElementById(logoFileInputSelector);
            Assert.That(logoFileInput, Is.Not.Null, "Logo file input should be available");

            await logoFileInput!.SetInputFilesAsync(LogoFilePath);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            var deleteLogoBtnAfter = await Actions.FindElementById(deleteLogoSelector);
            Assert.That(deleteLogoBtnAfter, Is.Not.Null, "Delete logo button should be visible after upload");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur during logo upload. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Logo uploaded successfully");
        }
    }
}
