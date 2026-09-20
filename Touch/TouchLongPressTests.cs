// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Klacks.E2ETest.Touch;

/// <summary>
/// Proves that a long press with one finger opens the context menu of the canvas grids, and that it
/// opens exactly once. Two engines cover the two platform families: Chromium with a touch-enabled
/// context and real CDP touch input reproduces Windows and Android, where the browser emits its own
/// contextmenu while the finger is still down; WebKit with a synthetic pointer sequence reproduces
/// iPadOS, where no native contextmenu ever arrives and LongPressContextDirective is the only opener.
/// Each test reports through the contextmenu trust probe which of the two paths actually opened the
/// menu. The base URL can be redirected with KLACKS_E2E_BASEURL to test a fixed build.
/// </summary>
[TestFixture]
[Category("TouchLongPress")]
public class TouchLongPressTests
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;
    private Wrapper _actions = null!;
    private string _baseUrl = TouchIds.DefaultBaseUrl;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private bool _headless;

    [SetUp]
    public void ReadConfiguration()
    {
        var environment = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development").Trim();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<PlaywrightSetup>(optional: true)
            .Build();

        _userName = configuration["user"] ?? throw new InvalidOperationException("The user must not be zero or empty.");
        _password = configuration["password"] ?? throw new InvalidOperationException("The password must not be zero or empty.");
        _headless = bool.Parse(configuration["PlaywrightConfig:HeadLess"] ?? "false");
        _baseUrl = (Environment.GetEnvironmentVariable(TouchIds.BaseUrlEnvVar) ?? TouchIds.DefaultBaseUrl).TrimEnd('/') + "/";
    }

    [TearDown]
    public async Task TearDownBrowserAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
            await _context.CloseAsync();
            await _browser.CloseAsync();
            _playwright.Dispose();
            _page = null!;
        }
    }

    [TestCase(TouchIds.ChromiumEngine)]
    [TestCase(TouchIds.WebkitEngine)]
    public async Task Schedule_TouchLongPress_OpensContextMenu(string engine)
    {
        await StartBrowserAsync(engine);
        await OpenAsync(TouchIds.ScheduleRoute, TouchIds.ScheduleCanvasSelector);

        await LongPressAsync(engine, TouchIds.ScheduleCanvasSelector, TouchIds.ScheduleProbeX, TouchIds.ScheduleProbeY);

        Assert.That(await MenuOpensAsync(), Is.True,
            $"A long press on a schedule cell must open the context menu ({engine}).");
        TestContext.Out.WriteLine($"CONTEXTMENU {engine}: {await _actions.ReadContextMenuTrustLog()}");
    }

    [TestCase(TouchIds.ChromiumEngine)]
    [TestCase(TouchIds.WebkitEngine)]
    public async Task Schedule_TouchLongPress_OpensTheMenuOnlyOnce(string engine)
    {
        await StartBrowserAsync(engine);
        await OpenAsync(TouchIds.ScheduleRoute, TouchIds.ScheduleCanvasSelector);

        await LongPressAsync(engine, TouchIds.ScheduleCanvasSelector, TouchIds.ScheduleProbeX, TouchIds.ScheduleProbeY);
        Assert.That(await MenuOpensAsync(), Is.True, "Precondition: the long press must open the menu.");

        var positionAfterOpen = await _actions.ReadElementPosition(TouchIds.AnyContextMenuSelector);
        await Task.Delay(TouchIds.DoubleOpenObservationMs);

        Assert.That(await _actions.CountElementsBySelector(TouchIds.AnyContextMenuSelector), Is.EqualTo(1),
            $"A long press must leave exactly one open menu ({engine}).");
        Assert.That(await _actions.ReadElementPosition(TouchIds.AnyContextMenuSelector), Is.EqualTo(positionAfterOpen),
            $"The menu must not be closed and reopened at a second position ({engine}).");
        TestContext.Out.WriteLine($"CONTEXTMENU {engine}: {await _actions.ReadContextMenuTrustLog()}");
    }

    [TestCase(TouchIds.ChromiumEngine)]
    [TestCase(TouchIds.WebkitEngine)]
    public async Task AbsenceGantt_TouchLongPress_OpensContextMenu(string engine)
    {
        await StartBrowserAsync(engine);
        await OpenAsync(TouchIds.AbsenceRoute, TouchIds.AbsenceCanvasSelector);

        await LongPressAsync(engine, TouchIds.AbsenceCanvasSelector, TouchIds.AbsenceProbeX, TouchIds.AbsenceProbeY);

        Assert.That(await MenuOpensAsync(), Is.True,
            $"A long press on the absence gantt must open the context menu ({engine}).");
        TestContext.Out.WriteLine($"CONTEXTMENU {engine}: {await _actions.ReadContextMenuTrustLog()}");
    }

    private async Task StartBrowserAsync(string engine)
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions { Headless = _headless };
        _browser = engine == TouchIds.ChromiumEngine
            ? await _playwright.Chromium.LaunchAsync(launchOptions)
            : await _playwright.Webkit.LaunchAsync(launchOptions);

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            HasTouch = true,
            IgnoreHTTPSErrors = true,
            Locale = TouchIds.LocaleCode,
            ViewportSize = new ViewportSize { Width = TouchIds.ViewportWidth, Height = TouchIds.ViewportHeight },
        });
        await _context.AddInitScriptAsync(TouchIds.ContextMenuTrustProbeScript);

        _page = await _context.NewPageAsync();
        _actions = new Wrapper(_page);

        await _actions.NavigateTo(_baseUrl + TouchIds.LoginPath);
        await _actions.Wait500();
        await _actions.FillInputById(LogInIds.InputEmailId, _userName);
        await _actions.FillInputById(LogInIds.InputPasswordId, _password);
        await _actions.ClickButtonById(LogInIds.ButtonSumitId);
        await _actions.WaitForSpinnerToDisappear();
        await _actions.WaitUntilUrlDoesNotContain(TouchIds.LoginPath, TouchIds.LoginTimeoutMs);
    }

    private async Task OpenAsync(string route, string canvasSelector)
    {
        try
        {
            await _actions.NavigateTo(_baseUrl + route);
            await _actions.WaitForSpinnerToDisappear();
            await _actions.ElementIsVisibleByCssSelector(canvasSelector);
        }
        catch (TimeoutException)
        {
            await CaptureFailureEvidenceAsync("open-" + route.Replace('/', '_'));
            throw;
        }

        await Task.Delay(TouchIds.GridLoadSettleMs);
    }

    private async Task LongPressAsync(string engine, string selector, float x, float y)
    {
        if (engine == TouchIds.ChromiumEngine)
        {
            await _actions.TouchLongPressByCssSelectorAtPosition(selector, x, y, TouchIds.LongPressHoldMs);
            return;
        }

        await _actions.DispatchPointerEventByCssSelectorAtPosition(
            selector, TouchIds.PointerDownEvent, x, y,
            TouchIds.TouchPointerType, TouchIds.TouchPointerId, TouchIds.PrimaryButton, TouchIds.NoButtonsMask);
        await Task.Delay(TouchIds.SyntheticHoldMs);
        await _actions.DispatchPointerEventByCssSelectorAtPosition(
            selector, TouchIds.PointerUpEvent, x, y,
            TouchIds.TouchPointerType, TouchIds.TouchPointerId, TouchIds.PrimaryButton, TouchIds.NoButtonsMask);
    }

    private async Task<bool> MenuOpensAsync()
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(TouchIds.ContextMenuTimeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await _actions.CountElementsBySelector(TouchIds.AnyContextMenuSelector) > 0)
            {
                return true;
            }

            await Task.Delay(TouchIds.MenuPollMs);
        }

        await CaptureFailureEvidenceAsync("menu");
        return false;
    }

    private async Task CaptureFailureEvidenceAsync(string label)
    {
        var path = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            $"{TouchIds.EvidenceFilePrefix}{label}-{DateTime.UtcNow:HHmmssfff}.png");
        await _actions.TakeScreenshotAsync(path);
        TestContext.Out.WriteLine($"EVIDENCE url={_actions.ReadCurrentUrl()}");
        TestContext.Out.WriteLine($"EVIDENCE contextmenu={await _actions.ReadContextMenuTrustLog()}");
    }
}
