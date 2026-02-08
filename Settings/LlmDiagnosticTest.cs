using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;

namespace Klacks.E2ETest;

[TestFixture]
[Order(49)]
public class LlmDiagnosticTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private readonly List<string> _consoleLogs = new();
    private readonly List<string> _apiResponses = new();

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
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
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Response = response,
            });
        });
    }

    [TearDown]
    public void TearDown()
    {
        TestContext.Out.WriteLine("=== CONSOLE LOGS ===");
        foreach (var log in _consoleLogs.Where(l => l.Contains("LLM") || l.Contains("error") || l.Contains("Error")))
        {
            TestContext.Out.WriteLine(log);
        }

        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
        }
    }

    [Test]
    [Order(1)]
    public async Task Diagnose_ChatInitialization()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Diagnose: Chat Initialization ===");

        // Act
        await Actions.ClickButtonById(HeaderAssistantButton);
        await Actions.Wait2000();

        // Assert
        var warningElement = await Page.QuerySelectorAsync(".no-api-key-warning .warning-icon");
        var warningText = warningElement != null ? await warningElement.TextContentAsync() : null;

        var chatInput = await Page.QuerySelectorAsync($"#{ChatInput}");
        var isDisabled = chatInput != null ? await chatInput.IsDisabledAsync() : true;

        var spinnerVisible = await Page.QuerySelectorAsync(".no-api-key-warning .loading-spinner");

        TestContext.Out.WriteLine($"Warning element: {(warningElement != null ? "FOUND" : "NOT FOUND")}");
        TestContext.Out.WriteLine($"Warning text: '{warningText?.Trim()}'");
        TestContext.Out.WriteLine($"Spinner visible: {(spinnerVisible != null ? "YES" : "NO")}");
        TestContext.Out.WriteLine($"Chat input disabled: {isDisabled}");
        TestContext.Out.WriteLine($"API responses received: {_apiResponses.Count}");

        // Warte noch und prüfe erneut
        await Actions.Wait3500();

        var warningAfterWait = await Page.QuerySelectorAsync(".no-api-key-warning .warning-icon");
        var warningTextAfter = warningAfterWait != null ? await warningAfterWait.TextContentAsync() : null;
        var isDisabledAfter = chatInput != null ? await chatInput.IsDisabledAsync() : true;

        TestContext.Out.WriteLine($"--- After 3.5s wait ---");
        TestContext.Out.WriteLine($"Warning text: '{warningTextAfter?.Trim()}'");
        TestContext.Out.WriteLine($"Chat input disabled: {isDisabledAfter}");
        TestContext.Out.WriteLine($"Total API responses: {_apiResponses.Count}");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors. Error: {_listener.GetLastErrorMessage()}");
    }
}
