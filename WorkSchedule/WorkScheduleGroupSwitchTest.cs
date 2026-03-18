// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.WorkSchedule;

/// <summary>
/// E2E Test for rapid group switching in the Schedule view.
/// Verifies that the schedule grid loads correctly after fast group changes
/// and doesn't hang with an infinite spinner.
/// </summary>
[TestFixture]
[Order(102)]
public class WorkScheduleGroupSwitchTest : PlaywrightSetup
{
    private Listener _listener = null!;

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Errors: {_listener.GetLastErrorMessage()}");
        }

        await _listener.WaitForResponseHandlingAsync();
    }

    [Test, Order(1)]
    public async Task Step1_NavigateToSchedule()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Navigate to Schedule ===");

        // Act
        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Assert
        var canvas = await Page.WaitForSelectorAsync("canvas[id^='template-canvas']", new()
        {
            State = Microsoft.Playwright.WaitForSelectorState.Visible,
            Timeout = 15000
        });
        Assert.That(canvas, Is.Not.Null, "Schedule canvas should be visible");
        TestContext.Out.WriteLine("Schedule loaded successfully");
    }

    [Test, Order(2)]
    public async Task Step2_RapidGroupSwitching_ScheduleShouldNotHang()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Rapid Group Switching ===");
        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Act - Open dropdown and collect group options
        var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
        Assert.That(dropdown, Is.Not.Null, "Group select dropdown should exist");

        await dropdown!.ClickAsync();
        await Actions.Wait500();

        await Page.WaitForSelectorAsync(".group-select-dropdown", new() { Timeout = 5000 });
        await ExpandAllGroupNodes();

        var allGroupOptions = await Page.QuerySelectorAllAsync(".group-option-button");
        var groupCount = allGroupOptions.Count;
        TestContext.Out.WriteLine($"Found {groupCount} group options");

        if (groupCount < 2)
        {
            TestContext.Out.WriteLine("Not enough groups to test rapid switching, skipping");
            Assert.Pass("Not enough groups for rapid switch test");
            return;
        }

        // Close dropdown first
        await dropdown.ClickAsync();
        await Actions.Wait500();

        // Rapid switch: click through groups quickly (max 5 groups)
        var switchCount = Math.Min(groupCount, 5);
        for (var i = 0; i < switchCount; i++)
        {
            TestContext.Out.WriteLine($"  Switching to group {i + 1}/{switchCount}...");

            // Open dropdown
            dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 5000 });
            await dropdown!.ClickAsync();
            await Task.Delay(200);

            // Click group option - quickly, no waiting for load
            var options = await Page.QuerySelectorAllAsync(".group-option-button");
            if (i < options.Count)
            {
                await options[i].ClickAsync();
                await Task.Delay(100); // Minimal wait - simulates rapid clicking
            }
        }

        TestContext.Out.WriteLine("All rapid switches done. Waiting for final load...");

        // Assert - Schedule should recover and load within 30s
        var spinnerGone = false;
        for (var wait = 0; wait < 60; wait++)
        {
            await Task.Delay(500);
            var spinner = await Page.QuerySelectorAsync(".spinner-border");
            var spinnerVisible = spinner != null && await spinner.IsVisibleAsync();
            if (!spinnerVisible)
            {
                spinnerGone = true;
                TestContext.Out.WriteLine($"  Spinner gone after {(wait + 1) * 500}ms");
                break;
            }
        }

        Assert.That(spinnerGone, Is.True, "Spinner should disappear within 30 seconds after rapid group switching");

        // Verify the grid canvas is still visible
        var canvas = await Page.QuerySelectorAsync("canvas[id^='template-canvas']");
        Assert.That(canvas, Is.Not.Null, "Schedule canvas should still be visible after group switching");

        // Verify no API errors accumulated
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No permanent API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Rapid group switching test PASSED");
    }

    [Test, Order(3)]
    public async Task Step3_AfterGroupSwitch_ShiftSectionShouldLoad()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Verify Shift Section After Group Switch ===");
        await Actions.ClickButtonById(MainNavIds.OpenSchedulesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Act - Switch to a group and wait for full load
        var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
        await dropdown!.ClickAsync();
        await Actions.Wait500();

        var options = await Page.QuerySelectorAllAsync(".group-option-button");
        if (options.Count > 1)
        {
            await options[1].ClickAsync();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
        }

        // Assert - Check shift section canvas exists (bottom grid)
        var allCanvases = await Page.QuerySelectorAllAsync("canvas[id^='template-canvas']");
        TestContext.Out.WriteLine($"Found {allCanvases.Count} canvas elements (expect 2: schedule + shift)");

        Assert.That(allCanvases.Count, Is.GreaterThanOrEqualTo(2),
            "Both schedule and shift section canvases should be visible");

        TestContext.Out.WriteLine("Shift section verification PASSED");
    }

    private async Task ExpandAllGroupNodes()
    {
        var maxIterations = 10;
        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var toggles = await Page.QuerySelectorAllAsync(".group-tree-toggle app-icon-angle-right");
            if (toggles.Count == 0) break;

            foreach (var toggle in toggles)
            {
                try
                {
                    await toggle.ClickAsync();
                    await Task.Delay(100);
                }
                catch
                {
                    // Node might have been removed during expansion
                }
            }

            await Task.Delay(200);
        }
    }
}
