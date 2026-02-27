// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(51)]
    public class ChatbotNavigationTest : ChatbotTestBase
    {
        private const string SkillNavigateTo = "navigate_to";
        private const string PageKeyMainNav = "main-nav";

        private const string NavKeySettings = "settings";
        private const string NavKeyEmployees = "employees";
        private const string NavKeySchedules = "schedules";
        private const string NavKeyShifts = "shifts";
        private const string NavKeyAbsences = "absences";
        private const string NavKeyDashboard = "dashboard";

        private int _messageCountBefore;
        private Dictionary<string, string> _navRoutes = new();

        [OneTimeSetUp]
        public async Task LoadNavRoutes()
        {
            _navRoutes = await DbHelper.GetUiControlRoutesAsync(PageKeyMainNav);
            Assert.That(_navRoutes, Is.Not.Empty, "Navigation routes must be loaded from ui_controls");
        }

        [Test, Order(1)]
        public async Task Step1_OpenChat()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test, Order(2)]
        public async Task Step2_NavigateToSettings()
        {
            TestContext.Out.WriteLine("=== Step 2: Navigate to Settings via Chat ===");
            await AssertSkillEnabled(SkillNavigateTo);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zu den Einstellungen");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            TestContext.Out.WriteLine($"Bot response: {response}");
            AssertUrlContainsRoute(NavKeySettings);
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to settings successful");
        }

        [Test, Order(3)]
        public async Task Step3_NavigateToEmployees()
        {
            TestContext.Out.WriteLine("=== Step 3: Navigate to Employees via Chat ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zur Mitarbeiterliste");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            TestContext.Out.WriteLine($"Bot response: {response}");
            AssertUrlContainsRoute(NavKeyEmployees);
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to employees successful");
        }

        [Test, Order(4)]
        public async Task Step4_NavigateToSchedule()
        {
            TestContext.Out.WriteLine("=== Step 4: Navigate to Schedule via Chat ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zum Einsatzplan");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            TestContext.Out.WriteLine($"Bot response: {response}");
            AssertUrlContainsRoute(NavKeySchedules);
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to schedule successful");
        }

        [Test, Order(5)]
        public async Task Step5_NavigateToShifts()
        {
            TestContext.Out.WriteLine("=== Step 5: Navigate to Shifts via Chat ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Öffne Shifts");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            TestContext.Out.WriteLine($"Bot response: {response}");
            AssertUrlContainsRoute(NavKeyShifts);
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to shifts successful");
        }

        [Test, Order(6)]
        public async Task Step6_NavigateToAbsences()
        {
            TestContext.Out.WriteLine("=== Step 6: Navigate to Absences via Chat ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zu den Abwesenheiten");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            TestContext.Out.WriteLine($"Bot response: {response}");
            AssertUrlContainsRoute(NavKeyAbsences);
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to absences successful");
        }

        [Test, Order(7)]
        public async Task Step7_NavigateBackToSettings()
        {
            TestContext.Out.WriteLine("=== Step 7: Navigate Back to Settings via Chat ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Gehe zurueck zu den Einstellungen");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            TestContext.Out.WriteLine($"Bot response: {response}");
            AssertUrlContainsRoute(NavKeySettings);
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation back to settings successful");
        }

        [Test, Order(8)]
        public async Task Step8_NavigateToDashboard()
        {
            TestContext.Out.WriteLine("=== Step 8: Navigate to Dashboard via Chat ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Navigiere zum Dashboard");
            var response = await WaitForBotResponse(_messageCountBefore);
            await Actions.Wait2000();

            TestContext.Out.WriteLine($"Bot response: {response}");
            AssertUrlContainsRoute(NavKeyDashboard);
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to dashboard successful");
        }

        private void AssertUrlContainsRoute(string navKey)
        {
            var currentUrl = Actions.ReadCurrentUrl();
            TestContext.Out.WriteLine($"Current URL: {currentUrl}");

            Assert.That(_navRoutes.ContainsKey(navKey), Is.True,
                $"Route for '{navKey}' not found in ui_controls");

            var expectedRoute = _navRoutes[navKey];
            Assert.That(currentUrl, Does.Contain(expectedRoute),
                $"URL should contain '{expectedRoute}'. Got: {currentUrl}");
        }
    }
}
