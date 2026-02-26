// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(49)]
    public class ChatbotDiagnosticTest : ChatbotTestBase
    {
        private const string CssWarningIcon = ".no-api-key-warning .warning-icon";
        private const string CssLoadingSpinner = ".no-api-key-warning .loading-spinner";

        private readonly List<string> _consoleLogs = new();
        private readonly List<string> _apiResponses = new();

        [SetUp]
        public async Task DiagnosticSetUp()
        {
            _consoleLogs.Clear();
            _apiResponses.Clear();

            Page.Console += (_, msg) =>
            {
                _consoleLogs.Add($"[{msg.Type}] {msg.Text}");
            };

            await Page.RouteAsync("**/api/backend/assistant/providers", async route =>
            {
                var response = await route.FetchAsync();
                var body = await response.TextAsync();
                _apiResponses.Add(body);
                TestContext.Out.WriteLine($"=== PROVIDERS API RESPONSE (status {response.Status}): ===");
                TestContext.Out.WriteLine(body.Length > 2000 ? body[..2000] : body);
                await route.FulfillAsync(new Microsoft.Playwright.RouteFulfillOptions
                {
                    Response = response,
                });
            });
        }

        [TearDown]
        public void DiagnosticTearDown()
        {
            TestContext.Out.WriteLine("=== CONSOLE LOGS ===");
            foreach (var log in _consoleLogs.Where(l => l.Contains("LLM") || l.Contains("error") || l.Contains("Error")))
            {
                TestContext.Out.WriteLine(log);
            }
        }

        [Test, Order(1)]
        public async Task Diagnose_ChatInitialization()
        {
            TestContext.Out.WriteLine("=== Diagnose: Chat Initialization ===");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait2000();

            var warningElement = await Actions.QuerySelector(CssWarningIcon);
            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            var isDisabled = chatInput != null && await chatInput.IsDisabledAsync();
            var spinnerVisible = await Actions.QuerySelector(CssLoadingSpinner);

            TestContext.Out.WriteLine($"Warning element: {(warningElement != null ? "FOUND" : "NOT FOUND")}");
            TestContext.Out.WriteLine($"Spinner visible: {(spinnerVisible != null ? "YES" : "NO")}");
            TestContext.Out.WriteLine($"Chat input disabled: {isDisabled}");
            TestContext.Out.WriteLine($"API responses received: {_apiResponses.Count}");

            await Actions.Wait3500();

            var warningAfterWait = await Actions.QuerySelector(CssWarningIcon);
            var isDisabledAfter = chatInput != null && await chatInput.IsDisabledAsync();

            TestContext.Out.WriteLine("--- After 3.5s wait ---");
            TestContext.Out.WriteLine($"Warning element: {(warningAfterWait != null ? "FOUND" : "NOT FOUND")}");
            TestContext.Out.WriteLine($"Chat input disabled: {isDisabledAfter}");
            TestContext.Out.WriteLine($"Total API responses: {_apiResponses.Count}");

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors. Error: {TestListener.GetLastErrorMessage()}");
        }
    }
}
