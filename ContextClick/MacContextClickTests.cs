// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Klacks.E2ETest.ContextClick;

/// <summary>
/// Replays the macOS Safari Ctrl+Click sequence (primary-button mousedown with ctrlKey, followed by a
/// contextmenu event, with or without a mouseup) on the canvas based controls under the real WebKit
/// engine. Playwright WebKit on Windows neither turns Ctrl+Click into a contextmenu nor reports a Mac
/// platform, so the platform is overridden through an init script and the event sequence is dispatched
/// synthetically. The base URL can be redirected with KLACKS_E2E_BASEURL to test a fixed build.
/// </summary>
[TestFixture]
[Category("MacContextClick")]
public class MacContextClickTests
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;
    private Wrapper _actions = null!;
    private string _baseUrl = MacContextClickIds.DefaultBaseUrl;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private readonly List<string> _pageEvents = new();

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

    [Test]
    public async Task Schedule_RealRightClick_OpensContextMenu()
    {
        await SetUpBrowserAsync(emulateMac: false);
        await OpenAsync(MacContextClickIds.ScheduleRoute, MacContextClickIds.ScheduleCanvasSelector);

        await _actions.RightClickByCssSelectorAtPosition(
            MacContextClickIds.ScheduleCanvasSelector, MacContextClickIds.ScheduleProbeX, MacContextClickIds.ScheduleProbeY);

        Assert.That(await MenuOpensAsync(MacContextClickIds.AnyContextMenuSelector), Is.True,
            "A real right-click on a schedule cell must open the context menu.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Schedule_MacCtrlClick_OpensContextMenu(bool withMouseUp)
    {
        await SetUpBrowserAsync(emulateMac: true);
        await OpenAsync(MacContextClickIds.ScheduleRoute, MacContextClickIds.ScheduleCanvasSelector);

        await ReplayMacCtrlClickAsync(
            MacContextClickIds.ScheduleCanvasSelector, MacContextClickIds.ScheduleProbeX, MacContextClickIds.ScheduleProbeY, withMouseUp);

        Assert.That(await MenuOpensAsync(MacContextClickIds.AnyContextMenuSelector), Is.True,
            "The emulated Mac Ctrl+Click must open the schedule context menu (reference control).");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Availability_MacCtrlClick_DoesNotChangeCellOrLeaveDragArmed(bool withMouseUp)
    {
        await SetUpBrowserAsync(emulateMac: true);
        using var recorder = _actions.RecordRequests(MacContextClickIds.AvailabilityBulkUrlPart, MacContextClickIds.HttpPostMethod);
        await OpenAsync(MacContextClickIds.AvailabilityRoute, MacContextClickIds.AvailabilitySurfaceSelector);

        var restorer = await SnapshotAvailabilityAsync();
        try
        {
            await EnsureCellAvailableAsync(recorder);
            var writesBefore = recorder.Count;

            var signatureBefore = await ReadAvailabilitySignatureAsync();
            await ReplayMacCtrlClickAsync(
                MacContextClickIds.AvailabilitySurfaceSelector, MacContextClickIds.AvailabilityCellX, MacContextClickIds.AvailabilityCellY, withMouseUp);
            await _actions.HoverByCssSelectorAtPosition(
                MacContextClickIds.AvailabilitySurfaceSelector, MacContextClickIds.AvailabilityNeighbourCellX, MacContextClickIds.AvailabilityCellY);
            await Task.Delay(MacContextClickIds.DebounceSettleMs);

            Assert.That(recorder.Count, Is.EqualTo(writesBefore),
                "A Mac Ctrl+Click, and the mouse movement after it, must not write any availability change.");
            Assert.That(await ReadAvailabilitySignatureAsync(), Is.EqualTo(signatureBefore),
                "The availability cells must look exactly as before the Mac Ctrl+Click.");
        }
        finally
        {
            await restorer.RestoreAsync(TouchedDaysOf(recorder));
        }
    }

    [Test]
    public async Task Availability_NonMacCtrlClick_StillUnchecksCell()
    {
        await SetUpBrowserAsync(emulateMac: false);
        using var recorder = _actions.RecordRequests(MacContextClickIds.AvailabilityBulkUrlPart, MacContextClickIds.HttpPostMethod);
        await OpenAsync(MacContextClickIds.AvailabilityRoute, MacContextClickIds.AvailabilitySurfaceSelector);

        var restorer = await SnapshotAvailabilityAsync();
        try
        {
            await EnsureCellAvailableAsync(recorder);
            var writesBefore = recorder.Count;

            await _actions.ControlClickByCssSelectorAtPosition(
                MacContextClickIds.AvailabilitySurfaceSelector, MacContextClickIds.AvailabilityCellX, MacContextClickIds.AvailabilityCellY);
            await Task.Delay(MacContextClickIds.DebounceSettleMs);

            Assert.That(recorder.Count, Is.GreaterThan(writesBefore),
                "On a non-Mac platform Ctrl+Click must keep unchecking the availability cell.");
            Assert.That(LastValueOf(recorder.Bodies[^1]), Is.False);
        }
        finally
        {
            await restorer.RestoreAsync(TouchedDaysOf(recorder));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AbsenceGantt_MacCtrlClickOnBar_OpensMenuAndLeavesNoDragState(bool withMouseUp)
    {
        await SetUpBrowserAsync(emulateMac: true);
        await OpenAsync(MacContextClickIds.AbsenceRoute, MacContextClickIds.AbsenceCanvasSelector);

        var bar = await FindAbsenceBarAsync();
        if (bar == null)
        {
            Assert.Inconclusive("No absence bar was found on the visible gantt canvas.");
            return;
        }

        await ReplayMacCtrlClickAsync(MacContextClickIds.AbsenceCanvasSelector, bar.Value.X, bar.Value.Y, withMouseUp);

        Assert.That(await MenuOpensAsync(MacContextClickIds.AbsenceContextMenuSelector), Is.True,
            "The emulated Mac Ctrl+Click on a bar must open the gantt context menu.");
        Assert.That(await _actions.ReadBodyCursorStyle(), Is.Not.EqualTo(MacContextClickIds.WResizeCursor),
            "A Mac Ctrl+Click must not leave the gantt in the resize/drag cursor state.");

        await DispatchOnAbsenceCanvasAsync(
            MacContextClickIds.MousemoveEvent, bar.Value.X + MacContextClickIds.BarDragDistancePx, bar.Value.Y,
            MacContextClickIds.PrimaryButton, MacContextClickIds.NoButtonsMask, false);
        await DispatchOnAbsenceCanvasAsync(
            MacContextClickIds.MousemoveEvent, bar.Value.X + MacContextClickIds.BarDragDistancePx, bar.Value.Y,
            MacContextClickIds.PrimaryButton, MacContextClickIds.PrimaryButtonsMask, false);
        await Task.Delay(MacContextClickIds.UiSettleMs);

        var barAfter = await FindAbsenceBarAsync();
        TestContext.Out.WriteLine($"BAR before={bar} after={barAfter}");
        if (barAfter == null || Math.Abs(barAfter.Value.X - bar.Value.X) > MacContextClickIds.BarPositionTolerancePx)
        {
            await CaptureFailureEvidenceAsync("absence-bar");
        }

        Assert.That(barAfter, Is.Not.Null, "The absence bar disappeared after the mouse movement.");
        Assert.That(Math.Abs(barAfter!.Value.X - bar.Value.X), Is.LessThanOrEqualTo(MacContextClickIds.BarPositionTolerancePx),
            "Mouse movement after a Mac Ctrl+Click must not move or resize the absence bar.");
        Assert.That(await _actions.ReadBodyCursorStyle(), Is.Not.EqualTo(MacContextClickIds.WResizeCursor));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TimeRuler_MacCtrlClick_OpensMenuOnlyOverShiftAndKeepsSelection(bool withMouseUp)
    {
        await SetUpBrowserAsync(emulateMac: true);
        await using var testData = new ContainerTemplateTestData(
            Environment.GetEnvironmentVariable(ContainerTemplateTestDataIds.ApiBaseUrlEnvVar) ?? ContainerTemplateTestDataIds.DefaultApiBaseUrl,
            _userName,
            _password,
            ResolveConnectionString());
        await testData.CreateAsync();

        await OpenAsync(MacContextClickIds.ContainerTemplateRoutePrefix + testData.ContainerId, MacContextClickIds.TimeRulerCanvasSelector);

        var shift = await _actions.FindUniformRunOnCanvas(
            MacContextClickIds.TimeRulerCanvasSelector,
            MacContextClickIds.RulerScanFromY,
            MacContextClickIds.RulerScanToY,
            MacContextClickIds.RulerScanStepY,
            MacContextClickIds.RulerMinRunPx,
            edgeBackground: true);
        Assert.That(shift, Is.Not.Null, "No shift rectangle was found on the time ruler canvas.");

        await ReplayMacCtrlClickAsync(MacContextClickIds.TimeRulerCanvasSelector, MacContextClickIds.RulerEmptyAreaX, shift!.Value.Y, withMouseUp);
        Assert.That(await MenuOpensAsync(MacContextClickIds.AnyContextMenuSelector), Is.False,
            "The emulated Mac Ctrl+Click beside the shift rectangles must not open a context menu.");

        await ReplayMacCtrlClickAsync(MacContextClickIds.TimeRulerCanvasSelector, shift.Value.X, shift.Value.Y, withMouseUp);
        Assert.That(await MenuOpensAsync(MacContextClickIds.AnyContextMenuSelector), Is.True,
            "The emulated Mac Ctrl+Click over a shift rectangle must open the shift context menu.");

        var signatureAfterMenu = await ReadTimeRulerSignatureAsync();
        for (var y = shift.Value.Y; y < MacContextClickIds.RulerHoverToY; y += MacContextClickIds.RulerHoverStepPx)
        {
            await _actions.HoverByCssSelectorAtPosition(MacContextClickIds.TimeRulerCanvasSelector, MacContextClickIds.RulerHoverX, y);
        }

        await Task.Delay(MacContextClickIds.UiSettleMs);

        Assert.That(await ReadTimeRulerSignatureAsync(), Is.EqualTo(signatureAfterMenu),
            "Mouse movement after a Mac Ctrl+Click must not extend the block selection (paint-select must not stay armed).");

        var secondShift = await _actions.FindUniformRunOnCanvas(
            MacContextClickIds.TimeRulerCanvasSelector,
            shift.Value.Y + MacContextClickIds.RulerSecondShiftScanOffsetY,
            MacContextClickIds.RulerScanToY,
            MacContextClickIds.RulerScanStepY,
            MacContextClickIds.RulerMinRunPx,
            edgeBackground: true);
        Assert.That(secondShift, Is.Not.Null, "The second shift rectangle was not found on the time ruler canvas.");

        var signatureBeforeClick = await ReadTimeRulerSignatureAsync();
        await _actions.ClickByCssSelectorAtPosition(
            MacContextClickIds.TimeRulerCanvasSelector, MacContextClickIds.RulerHoverX, secondShift!.Value.Y + MacContextClickIds.RulerInsideShiftOffsetY);
        await Task.Delay(MacContextClickIds.UiSettleMs);

        Assert.That(await ReadTimeRulerSignatureAsync(), Is.Not.EqualTo(signatureBeforeClick),
            "A plain click on the second shift after a Mac Ctrl+Click must select it (the click must not be swallowed).");
    }

    private async Task SetUpBrowserAsync(bool emulateMac)
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
        var headless = bool.Parse(configuration["PlaywrightConfig:HeadLess"] ?? "false");
        _baseUrl = (Environment.GetEnvironmentVariable(MacContextClickIds.BaseUrlEnvVar) ?? MacContextClickIds.DefaultBaseUrl).TrimEnd('/') + "/";

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            Locale = MacContextClickIds.LocaleCode,
            ViewportSize = new ViewportSize { Width = MacContextClickIds.ViewportWidth, Height = MacContextClickIds.ViewportHeight },
        });

        if (emulateMac)
        {
            await _context.AddInitScriptAsync(MacContextClickIds.PlatformOverrideScript);
        }

        if (!string.Equals(_baseUrl, MacContextClickIds.DefaultBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            await _context.RouteAsync(MacContextClickIds.ApiRoutePattern, AllowCrossOriginApiAsync);
        }

        _page = await _context.NewPageAsync();
        _actions = new Wrapper(_page);
        _pageEvents.Clear();
        _page.Console += (_, message) => RecordPageEvent($"console.{message.Type}: {message.Text}");
        _page.PageError += (_, error) => RecordPageEvent($"pageerror: {error}");
        _page.RequestFailed += (_, request) => RecordPageEvent($"requestfailed: {request.Method} {request.Url} {request.Failure}");
        _page.Response += (_, response) =>
        {
            if (response.Status >= MacContextClickIds.HttpErrorStatusFrom)
            {
                RecordPageEvent($"http {response.Status}: {response.Request.Method} {response.Url}");
            }
        };

        await _page.GotoAsync(_baseUrl + MacContextClickIds.LoginPath);
        await _actions.Wait500();
        await _actions.FillInputById(LogInIds.InputEmailId, _userName);
        await _actions.FillInputById(LogInIds.InputPasswordId, _password);
        await _actions.ClickButtonById(LogInIds.ButtonSumitId);
        await _actions.WaitForSpinnerToDisappear();
        await _page.WaitForURLAsync(url => !url.Contains(MacContextClickIds.LoginPath), new() { Timeout = MacContextClickIds.LoginTimeoutMs });

        var platform = await _actions.ReadNavigatorPlatform();
        Assert.That(string.Equals(platform, MacContextClickIds.MacPlatform, StringComparison.Ordinal), Is.EqualTo(emulateMac),
            $"navigator.platform was '{platform}', the platform override is not in effect as requested.");
    }

    private static async Task AllowCrossOriginApiAsync(IRoute route)
    {
        var origin = route.Request.Headers.GetValueOrDefault(MacContextClickIds.OriginHeader) ?? string.Empty;
        var corsHeaders = new Dictionary<string, string>
        {
            [MacContextClickIds.AllowOriginHeader] = origin,
            [MacContextClickIds.AllowCredentialsHeader] = MacContextClickIds.TrueValue,
            [MacContextClickIds.VaryHeader] = MacContextClickIds.OriginHeader,
        };

        if (string.Equals(route.Request.Method, MacContextClickIds.OptionsMethod, StringComparison.OrdinalIgnoreCase))
        {
            corsHeaders[MacContextClickIds.AllowMethodsHeader] = MacContextClickIds.AllMethods;
            corsHeaders[MacContextClickIds.AllowHeadersHeader] =
                route.Request.Headers.GetValueOrDefault(MacContextClickIds.RequestHeadersHeader) ?? string.Empty;
            await route.FulfillAsync(new RouteFulfillOptions { Status = MacContextClickIds.PreflightStatus, Headers = corsHeaders });
            return;
        }

        var response = await route.FetchAsync();
        var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value);
        foreach (var header in corsHeaders)
        {
            headers[header.Key] = header.Value;
        }

        await route.FulfillAsync(new RouteFulfillOptions { Response = response, Headers = headers });
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

        await Task.Delay(MacContextClickIds.GridLoadSettleMs);
    }

    private void RecordPageEvent(string text)
    {
        lock (_pageEvents)
        {
            _pageEvents.Add($"{DateTime.UtcNow:HH:mm:ss.fff} {text}");
            if (_pageEvents.Count > MacContextClickIds.PageEventLimit)
            {
                _pageEvents.RemoveAt(0);
            }
        }
    }

    private async Task CaptureFailureEvidenceAsync(string label)
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{MacContextClickIds.EvidenceFilePrefix}{label}-{DateTime.UtcNow:HHmmssfff}.png");
        await _actions.TakeScreenshotAsync(path);
        TestContext.Out.WriteLine($"EVIDENCE url={_actions.ReadCurrentUrl()}");
        lock (_pageEvents)
        {
            foreach (var entry in _pageEvents)
            {
                TestContext.Out.WriteLine($"EVIDENCE {entry}");
            }
        }
    }

    private async Task ReplayMacCtrlClickAsync(string selector, float x, float y, bool withMouseUp)
    {
        await _actions.DispatchMouseEventByCssSelectorAtPosition(
            selector, MacContextClickIds.MousedownEvent, x, y,
            MacContextClickIds.PrimaryButton, MacContextClickIds.PrimaryButtonsMask, true);
        await _actions.DispatchMouseEventByCssSelectorAtPosition(
            selector, MacContextClickIds.ContextMenuEvent, x, y,
            MacContextClickIds.SecondaryButton, MacContextClickIds.PrimaryButtonsMask, true);

        if (withMouseUp)
        {
            await _actions.DispatchMouseEventByCssSelectorAtPosition(
                selector, MacContextClickIds.MouseupEvent, x, y,
                MacContextClickIds.PrimaryButton, MacContextClickIds.NoButtonsMask, true);
        }
    }

    private Task DispatchOnAbsenceCanvasAsync(string eventType, float x, float y, int button, int buttons, bool ctrlKey)
    {
        return _actions.DispatchMouseEventByCssSelectorAtPosition(MacContextClickIds.AbsenceCanvasSelector, eventType, x, y, button, buttons, ctrlKey);
    }

    private async Task<bool> MenuOpensAsync(string menuSelector)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(MacContextClickIds.ContextMenuTimeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await _actions.CountElementsBySelector(menuSelector) > 0)
            {
                return true;
            }

            await Task.Delay(MacContextClickIds.UiSettleMs / 4);
        }

        return false;
    }

    private Task<string> ReadAvailabilitySignatureAsync()
    {
        return _actions.ReadCanvasRegionSignature(
            MacContextClickIds.AvailabilitySurfaceSelector,
            0,
            MacContextClickIds.AvailabilityRegionTop,
            MacContextClickIds.AvailabilityRegionWidth,
            MacContextClickIds.AvailabilityRegionHeight);
    }

    private async Task<(float X, float Y)?> FindAbsenceBarAsync()
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(MacContextClickIds.BarStableTimeoutMs);
        (float X, float Y)? previous = null;
        (float X, float Y)? current = null;
        while (DateTime.UtcNow < deadline)
        {
            current = await _actions.FindUniformRunOnCanvas(
                MacContextClickIds.AbsenceCanvasSelector,
                MacContextClickIds.BarScanFromY,
                MacContextClickIds.BarScanToY,
                MacContextClickIds.BarScanStepY,
                MacContextClickIds.BarMinRunPx);

            if (current != null && previous != null
                && Math.Abs(current.Value.X - previous.Value.X) <= MacContextClickIds.BarPositionTolerancePx
                && Math.Abs(current.Value.Y - previous.Value.Y) <= MacContextClickIds.BarPositionTolerancePx)
            {
                return current;
            }

            previous = current;
            await Task.Delay(MacContextClickIds.BarStablePollMs);
        }

        return current;
    }

    private Task<string> ReadTimeRulerSignatureAsync()
    {
        return _actions.ReadCanvasRegionSignature(
            MacContextClickIds.TimeRulerCanvasSelector, 0, 0, MacContextClickIds.ViewportWidth, MacContextClickIds.ViewportHeight);
    }

    private async Task EnsureCellAvailableAsync(RequestRecorder recorder)
    {
        await ClickCellAndWaitAsync(recorder, MacContextClickIds.AvailabilityCellX);
        if (!LastValueOf(recorder.Bodies[^1]))
        {
            await ClickCellAndWaitAsync(recorder, MacContextClickIds.AvailabilityCellX);
        }

        Assert.That(LastValueOf(recorder.Bodies[^1]), Is.True, "Precondition: the probe cell must be checked before the Ctrl+Click test.");
    }

    private async Task ClickCellAndWaitAsync(RequestRecorder recorder, float x)
    {
        var before = recorder.Count;
        await _actions.ClickByCssSelectorAtPosition(MacContextClickIds.AvailabilitySurfaceSelector, x, MacContextClickIds.AvailabilityCellY);
        await Task.Delay(MacContextClickIds.DebounceSettleMs);
        Assert.That(recorder.Count, Is.GreaterThan(before), "A plain click on an availability cell must persist a change.");
    }

    private static async Task<AvailabilityRowRestorer> SnapshotAvailabilityAsync()
    {
        var restorer = new AvailabilityRowRestorer(ResolveConnectionString());
        await restorer.SnapshotAsync();
        return restorer;
    }

    private static IEnumerable<(Guid ClientId, DateOnly Date)> TouchedDaysOf(RequestRecorder recorder)
    {
        foreach (var body in recorder.Bodies)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var item in document.RootElement.GetProperty(MacContextClickIds.AvailabilityItemsJsonKey).EnumerateArray())
            {
                yield return (item.GetProperty("clientId").GetGuid(), DateOnly.Parse(item.GetProperty("date").GetString()!.AsSpan(0, MacContextClickIds.IsoDateLength)));
            }
        }
    }

    private static string ResolveConnectionString()
    {
        return Environment.GetEnvironmentVariable(MacContextClickIds.DatabaseUrlEnvVar) ?? MacContextClickIds.DefaultConnectionString;
    }

    private static IEnumerable<(string Key, bool Value)> ItemsOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        foreach (var item in document.RootElement.GetProperty(MacContextClickIds.AvailabilityItemsJsonKey).EnumerateArray())
        {
            var key = $"{item.GetProperty("clientId").GetString()}_{item.GetProperty("date").GetString()}_{item.GetProperty("hour").GetInt32()}";
            yield return (key, item.GetProperty(MacContextClickIds.AvailabilityIsAvailableJsonKey).GetBoolean());
        }
    }

    private static bool LastValueOf(string body) => ItemsOf(body).Last().Value;
}
