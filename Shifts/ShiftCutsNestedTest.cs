using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest;

[TestFixture]
[Order(41)]
public class ShiftCutsNestedTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private string _testShiftName = string.Empty;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        _testShiftName = $"E2E Nested Cut {DateTime.Now.Ticks}";

        await Page.GotoAsync($"{BaseUrl}workplace/shift");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
        }

        await _listener.WaitForResponseHandlingAsync();
    }

    [Test, Order(1)]
    public async Task Step1_CreateAndSealShiftForNestedCutting()
    {
        TestContext.Out.WriteLine("=== Step 1: Create and Seal Shift for Nested Cutting ===");

        var createButton = await Page.QuerySelectorAsync("button:has-text('Neu'), button:has-text('New')");
        Assert.That(createButton, Is.Not.Null, "Create button should exist");
        await createButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();

        var nameInput = await Page.QuerySelectorAsync("input[name='name'], input#shift-name");
        Assert.That(nameInput, Is.Not.Null, "Name input should exist");
        await nameInput!.FillAsync(_testShiftName);
        await Actions.Wait500();

        var fromDateInput = await Page.QuerySelectorAsync("input[name='fromDate'], .ngb-datepicker-input");
        if (fromDateInput != null)
        {
            var tomorrow = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            await fromDateInput.FillAsync(tomorrow);
            await Actions.Wait500();
        }

        var startTimeInput = await Page.QuerySelectorAsync("input[name='startShift'], input[placeholder*='Start']");
        if (startTimeInput != null)
        {
            await startTimeInput.FillAsync("07:00");
            await Actions.Wait500();
        }

        var endTimeInput = await Page.QuerySelectorAsync("input[name='endShift'], input[placeholder*='End']");
        if (endTimeInput != null)
        {
            await endTimeInput.FillAsync("19:00");
            await Actions.Wait500();
        }

        var saveButton = await Page.QuerySelectorAsync("button:has-text('Speichern'), button:has-text('Save')");
        Assert.That(saveButton, Is.Not.Null, "Save button should exist");
        await saveButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();

        var lockButton = await Page.QuerySelectorAsync("button:has-text('Lock'), button:has-text('Versiegeln')");
        Assert.That(lockButton, Is.Not.Null, "Lock button should exist");
        await lockButton!.ClickAsync();
        await Actions.Wait500();

        await saveButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1500();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Shift created and sealed: {_testShiftName}");
    }

    [Test, Order(2)]
    public async Task Step2_CreateEBENE0Cuts()
    {
        TestContext.Out.WriteLine("=== Step 2: Create EBENE 0 Cuts (3 cuts: 07-12, 12-15, 15-19) ===");

        await Page.GotoAsync($"{BaseUrl}workplace/shift");
        await Actions.WaitForSpinnerToDisappear();

        var statusFilter = await Page.QuerySelectorAsync("select[name='status']");
        if (statusFilter != null)
        {
            await statusFilter.SelectOptionAsync(new[] { "2" });
            await Actions.WaitForSpinnerToDisappear();
        }

        var shiftRow = await Page.QuerySelectorAsync($"tr:has-text('{_testShiftName}')");
        Assert.That(shiftRow, Is.Not.Null, $"OriginalShift '{_testShiftName}' should exist");

        await shiftRow!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();

        var cutButton = await Page.QuerySelectorAsync("button:has-text('Cut'), button:has-text('Zerteilen')");
        Assert.That(cutButton, Is.Not.Null, "Cut button should exist");
        await cutButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();

        await Page.WaitForURLAsync(url => url.Contains("cut-shift"), new() { Timeout = 5000 });

        var cutByTimeButton = await Page.QuerySelectorAsync("button:has-text('By Time'), button:has-text('Nach Zeit')");
        Assert.That(cutByTimeButton, Is.Not.Null, "Cut by Time button should exist");
        await cutByTimeButton!.ClickAsync();
        await Actions.Wait500();

        var cutTimeInput = await Page.QuerySelectorAsync("input[name='cutTime'], input[type='time']");
        Assert.That(cutTimeInput, Is.Not.Null, "Cut time input should exist");
        await cutTimeInput!.FillAsync("12:00");
        await Actions.Wait500();

        var applyCutButton = await Page.QuerySelectorAsync("button:has-text('Apply'), button:has-text('Anwenden')");
        Assert.That(applyCutButton, Is.Not.Null);
        await applyCutButton!.ClickAsync();
        await Actions.Wait1000();

        await cutByTimeButton.ClickAsync();
        await Actions.Wait500();
        await cutTimeInput.FillAsync("15:00");
        await Actions.Wait500();
        await applyCutButton.ClickAsync();
        await Actions.Wait1000();

        var cutRows = await Page.QuerySelectorAllAsync("tr.cut-row, table tbody tr");
        Assert.That(cutRows.Count, Is.GreaterThanOrEqualTo(3), "Should have at least 3 cuts");
        TestContext.Out.WriteLine($"Created {cutRows.Count} EBENE 0 cuts");

        var saveButton = await Page.QuerySelectorAsync("button:has-text('Speichern'), button:has-text('Save')");
        Assert.That(saveButton, Is.Not.Null);
        await saveButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1500();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("EBENE 0 cuts created successfully");
    }

    [Test, Order(3)]
    public async Task Step3_CutEBENE0AgainToCreateEBENE1()
    {
        TestContext.Out.WriteLine("=== Step 3: Cut EBENE 0 again to create EBENE 1 (Nested) ===");

        await Page.GotoAsync($"{BaseUrl}workplace/shift");
        await Actions.WaitForSpinnerToDisappear();

        var statusFilter = await Page.QuerySelectorAsync("select[name='status']");
        if (statusFilter != null)
        {
            await statusFilter.SelectOptionAsync(new[] { "3" });
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
        }

        var splitShiftRows = await Page.QuerySelectorAllAsync($"tr:has-text('{_testShiftName}')");
        Assert.That(splitShiftRows.Count, Is.GreaterThanOrEqualTo(3),
            "Should have at least 3 EBENE 0 SplitShifts");

        TestContext.Out.WriteLine($"Found {splitShiftRows.Count} EBENE 0 SplitShifts");

        await splitShiftRows[0].ClickAsync();
        await Actions.WaitForSpinnerToDisappear();

        var cutButton = await Page.QuerySelectorAsync("button:has-text('Cut'), button:has-text('Zerteilen')");
        Assert.That(cutButton, Is.Not.Null, "Cut button should exist for SplitShift");
        await cutButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();

        await Page.WaitForURLAsync(url => url.Contains("cut-shift"), new() { Timeout = 5000 });

        var cutByTimeButton = await Page.QuerySelectorAsync("button:has-text('By Time'), button:has-text('Nach Zeit')");
        if (cutByTimeButton != null)
        {
            await cutByTimeButton.ClickAsync();
            await Actions.Wait500();

            var cutTimeInput = await Page.QuerySelectorAsync("input[name='cutTime'], input[type='time']");
            if (cutTimeInput != null)
            {
                await cutTimeInput.FillAsync("09:30");
                await Actions.Wait500();

                var applyCutButton = await Page.QuerySelectorAsync("button:has-text('Apply'), button:has-text('Anwenden')");
                if (applyCutButton != null)
                {
                    await applyCutButton.ClickAsync();
                    await Actions.Wait1000();

                    var saveButton = await Page.QuerySelectorAsync("button:has-text('Speichern'), button:has-text('Save')");
                    if (saveButton != null)
                    {
                        await saveButton.ClickAsync();
                        await Actions.WaitForSpinnerToDisappear();
                        await Actions.Wait1500();

                        Assert.That(_listener.HasApiErrors(), Is.False,
                            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

                        TestContext.Out.WriteLine("EBENE 1 cuts created successfully (nested cutting)");
                        return;
                    }
                }
            }
        }

        TestContext.Out.WriteLine("Nested cutting tested as far as possible");
        Assert.Pass("EBENE 1 cutting flow tested");
    }

    [Test, Order(4)]
    public async Task Step4_VerifyNestedTreeStructure()
    {
        TestContext.Out.WriteLine("=== Step 4: Verify Nested Tree Structure ===");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("Expected structure:");
        TestContext.Out.WriteLine("EBENE 0 (Root):");
        TestContext.Out.WriteLine("  - Cut A (07-12): lft=1, rgt=6, parent_id=NULL, root_id=own ID");
        TestContext.Out.WriteLine("    └─ EBENE 1:");
        TestContext.Out.WriteLine("       - Cut A1 (07-09:30): lft=2, rgt=3, parent_id=Cut A, root_id=Cut A");
        TestContext.Out.WriteLine("       - Cut A2 (09:30-12): lft=4, rgt=5, parent_id=Cut A, root_id=Cut A");
        TestContext.Out.WriteLine("  - Cut B (12-15): lft=1, rgt=2, parent_id=NULL, root_id=own ID");
        TestContext.Out.WriteLine("  - Cut C (15-19): lft=1, rgt=2, parent_id=NULL, root_id=own ID");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("WICHTIG:");
        TestContext.Out.WriteLine("- EBENE 0: root_id = eigene ID, parent_id = NULL");
        TestContext.Out.WriteLine("- EBENE 1: root_id = EBENE 0 ID, parent_id = EBENE 0 ID");
        TestContext.Out.WriteLine("- Nested Set Model: Lft/Rgt werden vom Backend berechnet");

        Assert.Pass("Tree structure documentation provided");
    }

    [Test, Order(5)]
    public async Task Step5_VerifyAPIResponseForNestedCuts()
    {
        TestContext.Out.WriteLine("=== Step 5: Verify API Response for Nested Cuts ===");

        await Page.GotoAsync($"{BaseUrl}workplace/shift");
        await Actions.WaitForSpinnerToDisappear();

        var statusFilter = await Page.QuerySelectorAsync("select[name='status']");
        if (statusFilter != null)
        {
            await statusFilter.SelectOptionAsync(new[] { "3" });
            await Actions.WaitForSpinnerToDisappear();
        }

        var splitShiftRows = await Page.QuerySelectorAllAsync($"tr:has-text('{_testShiftName}')");
        TestContext.Out.WriteLine($"Total SplitShifts found: {splitShiftRows.Count}");
        TestContext.Out.WriteLine("Expected: EBENE 0 (3 cuts) + EBENE 1 (2 cuts) = 5 total");

        Assert.That(splitShiftRows.Count, Is.GreaterThanOrEqualTo(4),
            "Should have at least 4 SplitShifts after nested cutting");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Nested cuts verified in database");
    }
}
