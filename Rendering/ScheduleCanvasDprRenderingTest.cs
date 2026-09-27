using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.PageObjects;
using Klacks.E2ETest.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Klacks.E2ETest.Rendering;

/// <summary>
/// Diagnostic fixture reproducing the reported Safari/macOS canvas-grid rendering
/// drift (cell text clipped to roughly two thirds of the row height, growing
/// misalignment while scrolling) by running the identical Schedule-grid flow
/// under the real WebKit engine at several deviceScaleFactor values, alongside
/// Chromium baselines at matching values for comparison. Screenshots are the
/// verdict here, not assertions - this fixture answers "does the drift reproduce
/// outside Safari itself", not "is the fix correct".
/// </summary>
[TestFixture]
[Category("RenderingDiagnostic")]
public class ScheduleCanvasDprRenderingTest
{
    private static readonly string ScreenshotFolder = BuildScreenshotFolder();

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;
    private Wrapper _actions = null!;
    private SchedulePage _schedule = null!;

    private static string BuildScreenshotFolder()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\.."));
        var folder = Path.Combine(projectRoot, "Screenshots", "DprRendering");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private const string DiagnosticClientId = "1ba0cc28-8e95-4901-a0cc-445c4cf1e581";
    private const string DiagnosticWorkDate = "2026-09-01";

    [Test]
    public async Task Debug_ListFirstVisibleRows()
    {
        await SetUpBrowserAsync("chromium", 1f);
        try
        {
            await _schedule.NavigateToScheduleAsync();
            await _schedule.WaitForGridLoadAsync();

            await _actions.SearchById("search", "Sarah Haase");
            await Task.Delay(1000);

            await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(ScreenshotFolder, "debug-02-after-search.png"),
            });

            var cell = await _schedule.Grid.FindCellByClientAndDateViaApiAsync(DiagnosticClientId, DiagnosticWorkDate);
            TestContext.Out.WriteLine($"found cell via API: {(cell != null ? $"row={cell.Row} col={cell.Column}" : "null")}");

            var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
            foreach (var c in cells.Where(c => c.ClientId != null).Take(20))
            {
                TestContext.Out.WriteLine($"row={c.Row} col={c.Column} clientId={c.ClientId} clientName={c.ClientName} date={c.Date}");
            }

            TestContext.Out.WriteLine($"total cells: {cells.Count}, with clientId: {cells.Count(c => c.ClientId != null)}");
        }
        finally
        {
            await TearDownBrowserAsync();
        }
    }

    [TestCase("chromium", 1f)]
    [TestCase("chromium", 2f)]
    [TestCase("webkit", 2f)]
    [TestCase("webkit", 3f)]
    public async Task GridRendersConsistently(string engine, float deviceScaleFactor)
    {
        await SetUpBrowserAsync(engine, deviceScaleFactor);
        try
        {
            await _schedule.NavigateToScheduleAsync();
            await _schedule.WaitForGridLoadAsync();

            var prefix = $"{engine}-dpr{deviceScaleFactor:0.#}";

            var cell = await _schedule.Grid.FindCellByClientAndDateViaApiAsync(DiagnosticClientId, DiagnosticWorkDate);
            if (cell != null)
            {
                await _schedule.Grid.ScrollToRowAsync(Math.Max(0, cell.Row - 2));
                await Task.Delay(300);
            }
            else
            {
                TestContext.Out.WriteLine($"{prefix}: diagnostic work cell not found via window API - screenshot will show whatever is currently visible");
            }

            await _schedule.Grid.TakeGridScreenshotAsync(
                Path.Combine(ScreenshotFolder, $"{prefix}-01-with-shift-text.png"));

            await _actions.ScrollPageDown(pixels: 600);
            await Task.Delay(300);

            await _schedule.Grid.TakeGridScreenshotAsync(
                Path.Combine(ScreenshotFolder, $"{prefix}-02-after-scroll.png"));

            var reportedDpr = await _page.EvaluateAsync<double>("() => window.devicePixelRatio");
            TestContext.Out.WriteLine($"{prefix}: window.devicePixelRatio reported by the page = {reportedDpr}");
        }
        finally
        {
            await TearDownBrowserAsync();
        }
    }

    private async Task SetUpBrowserAsync(string engine, float deviceScaleFactor)
    {
        var environment = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development").Trim();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<PlaywrightSetup>(optional: true)
            .Build();

        var userName = configuration["user"] ?? throw new InvalidOperationException("The user must not be zero or empty.");
        var password = configuration["password"] ?? throw new InvalidOperationException("The password must not be zero or empty.");
        var headless = bool.Parse(configuration["PlaywrightConfig:HeadLess"] ?? "false");

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        IBrowserType browserType = engine switch
        {
            "webkit" => _playwright.Webkit,
            "chromium" => _playwright.Chromium,
            _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, "Unsupported engine"),
        };

        _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            Locale = "de-CH",
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            DeviceScaleFactor = deviceScaleFactor,
        });

        _page = await _context.NewPageAsync();
        _actions = new Wrapper(_page);
        _schedule = new SchedulePage(_page, _actions, "http://localhost:4200/");

        await _page.GotoAsync("http://localhost:4200/login");
        await LoginWithAsync(userName, password);
        await _page.WaitForURLAsync(url => !url.Contains("login"), new() { Timeout = 10000 });
    }

    private async Task LoginWithAsync(string userName, string password)
    {
        await _actions.Wait500();
        await _actions.FillInputById(LogInIds.InputEmailId, userName);
        await _actions.WaitForSpinnerToDisappear();
        await _actions.FillInputById(LogInIds.InputPasswordId, password);
        await _actions.WaitForSpinnerToDisappear();
        await _actions.ClickButtonById(LogInIds.ButtonSumitId);
        await _actions.WaitForSpinnerToDisappear();
    }

    private async Task TearDownBrowserAsync()
    {
        await _page.CloseAsync();
        await _context.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}
