using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using System.Text.Json;

namespace E2ETest.WorkSchedule;

[TestFixture]
[Order(100)]
public class WorkScheduleBulkOperationsTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private List<string> _consoleLogs = new();
    private Dictionary<string, string> _apiResponses = new();

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
        _consoleLogs.Clear();
        _apiResponses.Clear();

        Page.Console += (_, msg) =>
        {
            var logMessage = msg.Text;
            _consoleLogs.Add(logMessage);
            TestContext.Out.WriteLine($"[CONSOLE] {logMessage}");
        };

        await Page.RouteAsync("**/api/work/**", async route =>
        {
            var response = await route.FetchAsync();
            var url = route.Request.Url;
            var method = route.Request.Method;

            if (method == "POST" || method == "DELETE")
            {
                var responseBody = await response.TextAsync();
                TestContext.Out.WriteLine($"\n[API {method}] {url}");
                TestContext.Out.WriteLine($"[RESPONSE] {responseBody}");

                _apiResponses[$"{method}_{DateTime.Now.Ticks}"] = responseBody;
            }

            await route.ContinueAsync();
        });

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        TestContext.Out.WriteLine("=== WorkSchedule E2E Test Setup Complete ===");
    }

    [TearDown]
    public void TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Errors detected: {_listener.GetLastErrorMessage()}");
        }

        TestContext.Out.WriteLine($"\n=== Console Logs Summary ({_consoleLogs.Count} total) ===");
        var periodHoursLogs = _consoleLogs.Where(l => l.Contains("periodHours") || l.Contains("PeriodHours")).ToList();
        TestContext.Out.WriteLine($"Found {periodHoursLogs.Count} logs containing 'periodHours'");

        TestContext.Out.WriteLine($"\n=== API Responses Summary ({_apiResponses.Count} total) ===");
        foreach (var kvp in _apiResponses)
        {
            TestContext.Out.WriteLine($"Response: {kvp.Key.Substring(0, Math.Min(50, kvp.Key.Length))}...");
        }
    }

    [Test]
    [Order(1)]
    public async Task InterceptWorkScheduleOperations()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Intercept WorkSchedule BulkOperations ===");
        TestContext.Out.WriteLine("This test monitors Console logs and API responses");
        TestContext.Out.WriteLine("to verify periodHours calculation in real usage");

        // Act
        TestContext.Out.WriteLine("\n--- Test Instructions ---");
        TestContext.Out.WriteLine("This test will wait 60 seconds for you to:");
        TestContext.Out.WriteLine("  1. Find a client in the schedule");
        TestContext.Out.WriteLine("  2. Create works for Saturday (18.01.2025), Sunday (19.01.2025), Monday (20.01.2025)");
        TestContext.Out.WriteLine("  3. Observe the periodHours display");
        TestContext.Out.WriteLine("  4. Delete the works using bulk-delete");
        TestContext.Out.WriteLine("  5. Observe the periodHours update");
        TestContext.Out.WriteLine("\nStarting 60 second wait...\n");

        for (int i = 1; i <= 12; i++)
        {
            await Actions.Wait1000();
            await Actions.Wait1000();
            await Actions.Wait1000();
            await Actions.Wait1000();
            await Actions.Wait1000();
            TestContext.Out.WriteLine($"... {i * 5} seconds elapsed ...");

            var recentLogs = _consoleLogs.Skip(Math.Max(0, _consoleLogs.Count - 5)).ToList();
            if (recentLogs.Any(l => l.Contains("periodHours")))
            {
                TestContext.Out.WriteLine("  >> Found periodHours in recent logs!");
            }

            if (_apiResponses.Count > 0)
            {
                TestContext.Out.WriteLine($"  >> Captured {_apiResponses.Count} API responses so far");
            }
        }

        // Assert
        TestContext.Out.WriteLine("\n=== Analyzing Captured Data ===");

        TestContext.Out.WriteLine("\n--- Console Logs with 'periodHours' ---");
        var periodHoursLogs = _consoleLogs.Where(l => l.Contains("periodHours") || l.Contains("PeriodHours")).ToList();
        foreach (var log in periodHoursLogs)
        {
            TestContext.Out.WriteLine($"  {log}");
        }

        TestContext.Out.WriteLine($"\n--- API Responses ({_apiResponses.Count} total) ---");
        foreach (var kvp in _apiResponses)
        {
            TestContext.Out.WriteLine($"\n{kvp.Key}:");

            try
            {
                var json = JsonDocument.Parse(kvp.Value);
                if (json.RootElement.TryGetProperty("periodHours", out var periodHours))
                {
                    TestContext.Out.WriteLine("  periodHours found:");
                    TestContext.Out.WriteLine($"  {periodHours.GetRawText()}");
                }
                else
                {
                    TestContext.Out.WriteLine("  No periodHours in response");
                }
            }
            catch (JsonException ex)
            {
                TestContext.Out.WriteLine($"  Failed to parse JSON: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine("\n=== Test Complete ===");
        TestContext.Out.WriteLine("Review the logs above to identify any discrepancies");
        TestContext.Out.WriteLine("between expected and actual periodHours values");

        Assert.Pass("Manual verification test completed. Review logs for issues.");
    }
}
