// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.WorkSchedule;

/// <summary>
/// Stress-Tests for rapid group switching in the Schedule view.
/// Tests use root groups (which have clients and shifts).
/// Verifies: both grids load data, error list shows entries, no hanging.
/// @param _listener - Monitors HTTP responses for API errors
/// @param _consoleLogs - Captures frontend COLLISION-TRACE-FE logs
/// </summary>
[TestFixture]
[Order(103)]
public class WorkScheduleGroupSwitchStressTest : PlaywrightSetup
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
    public async Task Step1_LoadRootGroup_BothGridsAndErrorList()
    {
        TestContext.Out.WriteLine("=== Step 1: Load Root Group - verify grids + error list ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var rootGroupIndex = await GetFirstRootGroupIndex();
        await OpenDropdownAndClickGroup(rootGroupIndex);

        await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        await Actions.Wait3000();

        await VerifyScheduleGridHasData("after root group load");
        await VerifyShiftGridHasData("after root group load");

        await WaitForCollisionData(TimeSpan.FromSeconds(15));

        await VerifyErrorListHasEntries("after root group load");
    }

    [Test, Order(2)]
    public async Task Step2_SwitchBetweenRootGroupsAndAlleGruppen_DataLoadsEachTime()
    {
        TestContext.Out.WriteLine("=== Step 2: Switch between root groups + Alle Gruppen ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var rootGroups = await GetRootGroupIndices();
        if (rootGroups.Count < 2)
        {
            Assert.Pass("Need at least 2 root groups");
            return;
        }

        TestContext.Out.WriteLine("  Switching to 'Alle Gruppen'...");
        await OpenDropdownAndClickGroup(0);
        await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        await Actions.Wait2000();
        await VerifyScheduleGridHasData("Alle Gruppen initial");
        await VerifyShiftGridHasData("Alle Gruppen initial");

        for (var i = 0; i < Math.Min(3, rootGroups.Count); i++)
        {
            TestContext.Out.WriteLine($"  Switching to root group {i + 1}...");
            await OpenDropdownAndClickGroup(rootGroups[i]);
            await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
            await Actions.Wait2000();

            await VerifyScheduleGridHasData($"root group {i + 1}");
            await VerifyShiftGridHasData($"root group {i + 1}");
        }

        TestContext.Out.WriteLine("  Switching back to 'Alle Gruppen'...");
        await OpenDropdownAndClickGroup(0);
        await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        await Actions.Wait2000();
        await VerifyScheduleGridHasData("Alle Gruppen after groups");
        await VerifyShiftGridHasData("Alle Gruppen after groups");
    }

    [Test, Order(3)]
    public async Task Step3_RapidRootGroupSwitch_NoHang()
    {
        TestContext.Out.WriteLine("=== Step 3: Rapid root group switching ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var rootGroups = await GetRootGroupIndices();
        if (rootGroups.Count < 2)
        {
            Assert.Pass("Need at least 2 root groups");
            return;
        }

        _consoleLogs.Clear();

        var switchTargets = new List<int> { 0 };
        switchTargets.AddRange(rootGroups);

        for (var i = 0; i < 7; i++)
        {
            var groupIndex = switchTargets[i % switchTargets.Count];
            var label = groupIndex == 0 ? "Alle Gruppen" : $"root group index {groupIndex}";
            TestContext.Out.WriteLine($"  Rapid switch {i + 1}/7 -> {label}");
            await OpenDropdownAndClickGroup(groupIndex);
            await Task.Delay(100);
        }

        TestContext.Out.WriteLine("Waiting for final load...");
        var recovered = await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        Assert.That(recovered, Is.True, "Schedule should recover within 30s");

        await Actions.Wait3000();

        await VerifyScheduleGridHasData("after rapid switch");
        await VerifyShiftGridHasData("after rapid switch");

        await WaitForCollisionData(TimeSpan.FromSeconds(15));
        await VerifyErrorListHasEntries("after rapid switch");

        var spinnerTimeout = _consoleLogs.Any(l => l.Contains("Spinner safety timeout"));
        Assert.That(spinnerTimeout, Is.False, "Spinner safety timeout should NOT fire (indicates hang)");
    }

    [Test, Order(4)]
    public async Task Step4_BackToAlleGruppen_CollisionsVisible()
    {
        TestContext.Out.WriteLine("=== Step 4: Back to 'Alle Gruppen' - collisions must show ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var rootGroups = await GetRootGroupIndices();
        if (rootGroups.Count > 0)
        {
            await OpenDropdownAndClickGroup(rootGroups[0]);
            await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
            await Actions.Wait2000();
        }

        _consoleLogs.Clear();

        TestContext.Out.WriteLine("  Switching to 'Alle Gruppen'...");
        await OpenDropdownAndClickGroup(0);
        await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        await Actions.Wait3000();

        await VerifyScheduleGridHasData("Alle Gruppen");

        await WaitForCollisionData(TimeSpan.FromSeconds(15));

        var collisionReceived = _consoleLogs.Any(l => l.Contains("collisionsDetected$"));
        var keepingCached = _consoleLogs.Any(l => l.Contains("keeping cached"));
        TestContext.Out.WriteLine($"  collisionsDetected$ via SignalR: {collisionReceived}");
        TestContext.Out.WriteLine($"  Keeping cached collisions: {keepingCached}");

        await VerifyErrorListHasEntries("Alle Gruppen");
    }

    [Test, Order(5)]
    public async Task Step5_AlleGruppenCollisionRoundtrip()
    {
        TestContext.Out.WriteLine("=== Step 5: Alle Gruppen -> Group -> Alle Gruppen collision roundtrip ===");

        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var rootGroups = await GetRootGroupIndices();
        if (rootGroups.Count == 0)
        {
            Assert.Pass("Need at least 1 root group");
            return;
        }

        TestContext.Out.WriteLine("  Phase 1: Load 'Alle Gruppen'...");
        await OpenDropdownAndClickGroup(0);
        await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        await Actions.Wait3000();
        await VerifyScheduleGridHasData("Alle Gruppen phase 1");
        await WaitForCollisionData(TimeSpan.FromSeconds(15));
        var alleGruppenEntries1 = await GetTotalErrorEntryCount();
        TestContext.Out.WriteLine($"  Alle Gruppen entries (phase 1): {alleGruppenEntries1}");
        Assert.That(alleGruppenEntries1, Is.GreaterThan(0),
            "Alle Gruppen should have collision/validation entries");

        TestContext.Out.WriteLine("  Phase 2: Switch to specific group...");
        await OpenDropdownAndClickGroup(rootGroups[0]);
        await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        await Actions.Wait3000();
        await VerifyScheduleGridHasData("specific group");
        await WaitForCollisionData(TimeSpan.FromSeconds(15));
        var groupEntries = await GetTotalErrorEntryCount();
        TestContext.Out.WriteLine($"  Group entries: {groupEntries}");

        TestContext.Out.WriteLine("  Phase 3: Back to 'Alle Gruppen'...");
        _consoleLogs.Clear();
        await OpenDropdownAndClickGroup(0);
        await WaitForScheduleRecovery(TimeSpan.FromSeconds(30));
        await Actions.Wait3000();
        await VerifyScheduleGridHasData("Alle Gruppen phase 3");
        await WaitForCollisionData(TimeSpan.FromSeconds(15));
        var alleGruppenEntries3 = await GetTotalErrorEntryCount();
        TestContext.Out.WriteLine($"  Alle Gruppen entries (phase 3): {alleGruppenEntries3}");

        Assert.That(alleGruppenEntries3, Is.GreaterThan(0),
            "Alle Gruppen should still have entries after roundtrip");
        Assert.That(alleGruppenEntries3, Is.GreaterThanOrEqualTo(groupEntries),
            "Alle Gruppen should have >= entries compared to a single group");

        await VerifyErrorListHasEntries("Alle Gruppen roundtrip");
    }

    private async Task<int> GetTotalErrorEntryCount()
    {
        var badges = await Page.QuerySelectorAllAsync("a[role='tab'] span.badge");
        var total = 0;
        foreach (var badge in badges)
        {
            var text = await badge.TextContentAsync() ?? "0";
            if (int.TryParse(text.Trim(), out var count))
                total += count;
        }
        return total;
    }

    private async Task VerifyScheduleGridHasData(string context)
    {
        var rowCanvas = await Page.QuerySelectorAsync("#scheduleRowCanvas");
        Assert.That(rowCanvas, Is.Not.Null, $"Schedule row canvas should exist ({context})");

        var box = await rowCanvas!.BoundingBoxAsync();
        TestContext.Out.WriteLine($"  Schedule row canvas: {box?.Width}x{box?.Height} ({context})");
        Assert.That(box, Is.Not.Null, $"Schedule row canvas bounding box ({context})");
        Assert.That(box!.Height, Is.GreaterThan(50),
            $"Schedule row canvas height ({box.Height}px) should be > 50px = rows rendered ({context})");
    }

    private async Task VerifyShiftGridHasData(string context)
    {
        var shiftCount = await Page.QuerySelectorAsync(".shift-row-count");
        if (shiftCount != null)
        {
            var text = await shiftCount.TextContentAsync() ?? "";
            TestContext.Out.WriteLine($"  Shift row count: {text} ({context})");
            Assert.That(text, Does.Not.Contain("(0)"),
                $"Shift section should have shifts loaded ({context})");
        }

        var shiftCanvas = await Page.QuerySelectorAsync("#shift-row-header-box canvas");
        if (shiftCanvas != null)
        {
            var box = await shiftCanvas.BoundingBoxAsync();
            TestContext.Out.WriteLine($"  Shift canvas: {box?.Width}x{box?.Height} ({context})");
            Assert.That(box, Is.Not.Null, $"Shift canvas bounding box ({context})");
            Assert.That(box!.Height, Is.GreaterThan(30),
                $"Shift canvas height should be > 30px ({context})");
        }
    }

    private async Task VerifyErrorListHasEntries(string context)
    {
        var errorTab = await Page.QuerySelectorAsync("a[role='tab'][class*='nav-link']:has(span.badge)");
        if (errorTab == null)
        {
            var tabs = await Page.QuerySelectorAllAsync("a[role='tab']");
            foreach (var tab in tabs)
            {
                var text = await tab.TextContentAsync();
                TestContext.Out.WriteLine($"  Tab: '{text?.Trim()}'");
            }

            TestContext.Out.WriteLine($"  WARNING: No error tab with badges found ({context})");
            return;
        }

        var badges = await errorTab.QuerySelectorAllAsync("span.badge");
        var badgeTexts = new List<string>();
        foreach (var badge in badges)
        {
            var text = await badge.TextContentAsync() ?? "";
            badgeTexts.Add(text.Trim());
        }

        TestContext.Out.WriteLine($"  Error tab badges: [{string.Join(", ", badgeTexts)}] ({context})");
        Assert.That(badgeTexts.Count, Is.GreaterThan(0),
            $"Error tab should have at least one badge with count ({context})");

        var totalErrors = 0;
        foreach (var badge in badgeTexts)
        {
            if (int.TryParse(badge, out var count))
                totalErrors += count;
        }

        Assert.That(totalErrors, Is.GreaterThan(0),
            $"Total error/warning/info count should be > 0 ({context})");
        TestContext.Out.WriteLine($"  Total entries: {totalErrors} ({context})");
    }

    private async Task WaitForCollisionData(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var errorBadge = await Page.QuerySelectorAsync("a[role='tab'] span.badge");
            if (errorBadge != null)
            {
                var text = await errorBadge.TextContentAsync() ?? "0";
                if (int.TryParse(text.Trim(), out var count) && count > 0)
                {
                    TestContext.Out.WriteLine($"  Collision data arrived ({count} entries visible)");
                    return;
                }
            }
            await Task.Delay(500);
        }
        TestContext.Out.WriteLine("  WARNING: Collision data did not arrive within timeout");
    }

    private async Task<bool> WaitForScheduleRecovery(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            var spinner = await Page.QuerySelectorAsync(".spinner-border");
            var spinnerVisible = spinner != null && await spinner.IsVisibleAsync();
            if (!spinnerVisible)
            {
                var elapsed = timeout - (deadline - DateTime.UtcNow);
                TestContext.Out.WriteLine($"  Recovered after {elapsed.TotalMilliseconds:F0}ms");
                return true;
            }
        }
        TestContext.Out.WriteLine($"  Did NOT recover within {timeout.TotalSeconds}s");
        return false;
    }

    private async Task<int> GetFirstRootGroupIndex()
    {
        var rootGroups = await GetRootGroupIndices();
        Assert.That(rootGroups.Count, Is.GreaterThan(0), "Need at least 1 root group");
        return rootGroups[0];
    }

    private async Task<List<int>> GetRootGroupIndices()
    {
        var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
        Assert.That(dropdown, Is.Not.Null, "Group dropdown should exist");

        await dropdown!.ClickAsync();
        await Actions.Wait500();
        await Page.WaitForSelectorAsync(".group-select-dropdown", new() { Timeout = 5000 });

        var options = await Page.QuerySelectorAllAsync(".group-option-button");
        var rootIndices = new List<int>();

        for (var i = 1; i < options.Count; i++)
        {
            var classes = await options[i].GetAttributeAsync("class") ?? "";
            var parentEl = await options[i].EvaluateHandleAsync("el => el.closest('.group-tree-node')");
            var depth = await parentEl.EvaluateAsync<int>("el => { let d=0; let p=el; while(p=p.parentElement?.closest('.group-tree-children')) d++; return d; }");

            if (depth == 0)
            {
                rootIndices.Add(i);
                var text = await options[i].TextContentAsync();
                TestContext.Out.WriteLine($"  Root group index {i}: '{text?.Trim()}'");
            }
        }

        await dropdown.ClickAsync();
        await Actions.Wait300();

        TestContext.Out.WriteLine($"Found {rootIndices.Count} root groups");
        return rootIndices;
    }

    private async Task OpenDropdownAndClickGroup(int groupIndex)
    {
        try
        {
            var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 5000 });
            if (dropdown == null) return;

            await dropdown.ClickAsync();
            await Task.Delay(150);

            var options = await Page.QuerySelectorAllAsync(".group-option-button");
            if (groupIndex < options.Count)
            {
                await options[groupIndex].ClickAsync();
            }
        }
        catch (TimeoutException)
        {
            TestContext.Out.WriteLine($"    Timeout clicking group {groupIndex}");
        }
    }
}
