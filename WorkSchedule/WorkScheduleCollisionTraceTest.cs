// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.WorkSchedule;

/// <summary>
/// Traces the full collision chain from HTTP response through SignalR to frontend display.
/// Captures browser console logs with [COLLISION-TRACE-FE] prefix.
/// </summary>
[TestFixture]
[Order(104)]
public class WorkScheduleCollisionTraceTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private readonly List<string> _consoleLogs = [];

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
        _consoleLogs.Clear();

        Page.Console += (_, msg) =>
        {
            var text = msg.Text;
            if (text.Contains("COLLISION-TRACE-FE"))
            {
                _consoleLogs.Add(text);
            }
        };
    }

    [TearDown]
    public async Task TearDown()
    {
        await _listener.WaitForResponseHandlingAsync();
    }

    [Test, Order(1)]
    public async Task Step1_LoadAlleGruppen_CheckCollisionChain()
    {
        TestContext.Out.WriteLine("=== Collision Trace: Load 'Alle Gruppen' ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        TestContext.Out.WriteLine("Schedule loaded. Waiting 10s for collision data via SignalR...");
        await Task.Delay(10000);

        TestContext.Out.WriteLine($"\n=== Frontend Console Logs ({_consoleLogs.Count} entries) ===");
        foreach (var log in _consoleLogs)
        {
            TestContext.Out.WriteLine($"  {log}");
        }

        var collisionReceived = _consoleLogs.Any(l => l.Contains("collisionsDetected$"));
        var validationReceived = _consoleLogs.Any(l => l.Contains("scheduleValidationsDetected$"));
        var spinnerOff = _consoleLogs.Any(l => l.Contains("spinner OFF"));
        var errorEntries = _consoleLogs.Where(l => l.Contains("refreshEntries")).ToList();

        TestContext.Out.WriteLine($"\n=== Summary ===");
        TestContext.Out.WriteLine($"  Collision SignalR received: {collisionReceived}");
        TestContext.Out.WriteLine($"  Validation SignalR received: {validationReceived}");
        TestContext.Out.WriteLine($"  Spinner turned OFF: {spinnerOff}");
        TestContext.Out.WriteLine($"  refreshEntries calls: {errorEntries.Count}");
        if (errorEntries.Count > 0)
        {
            TestContext.Out.WriteLine($"  Last refreshEntries: {errorEntries.Last()}");
        }

        Assert.That(spinnerOff, Is.True, "Spinner should have turned off");
    }

    [Test, Order(2)]
    public async Task Step2_SwitchGroup_ThenBackToAlle_CheckCollisions()
    {
        TestContext.Out.WriteLine("=== Collision Trace: Switch group then back to Alle ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        _consoleLogs.Clear();

        var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
        Assert.That(dropdown, Is.Not.Null);

        await dropdown!.ClickAsync();
        await Actions.Wait500();
        var options = await Page.QuerySelectorAllAsync(".group-option-button");
        TestContext.Out.WriteLine($"Found {options.Count} group options");

        if (options.Count > 1)
        {
            TestContext.Out.WriteLine("Clicking group 1...");
            await options[1].ClickAsync();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait3000();
        }

        _consoleLogs.Clear();

        dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 5000 });
        await dropdown!.ClickAsync();
        await Actions.Wait500();
        options = await Page.QuerySelectorAllAsync(".group-option-button");

        if (options.Count > 0)
        {
            TestContext.Out.WriteLine("Clicking 'Alle Gruppen' (option 0)...");
            await options[0].ClickAsync();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait3000();
        }

        TestContext.Out.WriteLine("Waiting 15s for collision data...");
        await Task.Delay(15000);

        TestContext.Out.WriteLine($"\n=== Frontend Console Logs after 'Alle Gruppen' ({_consoleLogs.Count} entries) ===");
        foreach (var log in _consoleLogs)
        {
            TestContext.Out.WriteLine($"  {log}");
        }

        var collisionLogs = _consoleLogs.Where(l => l.Contains("collisionsDetected$")).ToList();
        var refreshLogs = _consoleLogs.Where(l => l.Contains("refreshEntries")).ToList();

        TestContext.Out.WriteLine($"\n=== Summary ===");
        TestContext.Out.WriteLine($"  collisionsDetected$ events: {collisionLogs.Count}");
        foreach (var log in collisionLogs)
        {
            TestContext.Out.WriteLine($"    {log}");
        }
        TestContext.Out.WriteLine($"  refreshEntries calls: {refreshLogs.Count}");
        if (refreshLogs.Count > 0)
        {
            TestContext.Out.WriteLine($"    Last: {refreshLogs.Last()}");
        }
    }

    [Test, Order(3)]
    public async Task Step3_RapidSwitch_CheckFinalCollisions()
    {
        TestContext.Out.WriteLine("=== Collision Trace: Rapid switch then check final state ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        _consoleLogs.Clear();

        var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
        Assert.That(dropdown, Is.Not.Null);

        for (var i = 0; i < 5; i++)
        {
            try
            {
                dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 3000 });
                await dropdown!.ClickAsync();
                await Task.Delay(150);

                var options = await Page.QuerySelectorAllAsync(".group-option-button");
                if (i < options.Count)
                {
                    await options[i].ClickAsync();
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"  Switch {i} failed: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine("Rapid switches done. Waiting for recovery + collision data...");
        await Actions.WaitForSpinnerToDisappear();
        await Task.Delay(15000);

        TestContext.Out.WriteLine($"\n=== Frontend Console Logs ({_consoleLogs.Count} entries) ===");
        foreach (var log in _consoleLogs)
        {
            TestContext.Out.WriteLine($"  {log}");
        }

        var collisionLogs = _consoleLogs.Where(l => l.Contains("collisionsDetected$")).ToList();
        var clearLogs = _consoleLogs.Where(l => l.Contains("clearing collisions")).ToList();
        var refreshLogs = _consoleLogs.Where(l => l.Contains("refreshEntries")).ToList();

        TestContext.Out.WriteLine($"\n=== Summary ===");
        TestContext.Out.WriteLine($"  collisionsDetected$ events: {collisionLogs.Count}");
        TestContext.Out.WriteLine($"  Collision clears (isWorkScheduleRead): {clearLogs.Count}");
        TestContext.Out.WriteLine($"  refreshEntries calls: {refreshLogs.Count}");
        if (refreshLogs.Count > 0)
        {
            TestContext.Out.WriteLine($"    Last: {refreshLogs.Last()}");
        }

        var lastRefresh = refreshLogs.LastOrDefault() ?? "";
        TestContext.Out.WriteLine($"\n  Final error state: {lastRefresh}");
    }
}
