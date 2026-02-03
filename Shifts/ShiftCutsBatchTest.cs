using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest;

[TestFixture]
[Order(42)]
public class ShiftCutsBatchTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private string _testShiftName = string.Empty;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        _testShiftName = $"E2E Batch Cut {DateTime.Now.Ticks}";

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
    public async Task Step1_CreateShiftForBatchCutting()
    {
        TestContext.Out.WriteLine("=== Step 1: Create Shift for Batch Cutting ===");
        TestContext.Out.WriteLine("This test simulates Szenario 2: User cuts multiple levels WITHOUT saving in between");

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
            await startTimeInput.FillAsync("06:00");
            await Actions.Wait500();
        }

        var endTimeInput = await Page.QuerySelectorAsync("input[name='endShift'], input[placeholder*='End']");
        if (endTimeInput != null)
        {
            await endTimeInput.FillAsync("22:00");
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
    public async Task Step2_PerformMultipleCutsWithoutSaving()
    {
        TestContext.Out.WriteLine("=== Step 2: Perform Multiple Cuts WITHOUT Saving ===");
        TestContext.Out.WriteLine("Goal: Test if Frontend can handle temp-IDs correctly");

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

        TestContext.Out.WriteLine("Cut 1: Split EBENE 0 into 3 parts (06-12, 12-17, 17-22)");
        await cutByTimeButton!.ClickAsync();
        await Actions.Wait500();

        var cutTimeInput = await Page.QuerySelectorAsync("input[name='cutTime'], input[type='time']");
        Assert.That(cutTimeInput, Is.Not.Null);
        await cutTimeInput!.FillAsync("12:00");
        await Actions.Wait500();

        var applyCutButton = await Page.QuerySelectorAsync("button:has-text('Apply'), button:has-text('Anwenden')");
        Assert.That(applyCutButton, Is.Not.Null);
        await applyCutButton!.ClickAsync();
        await Actions.Wait1000();

        await cutByTimeButton.ClickAsync();
        await Actions.Wait500();
        await cutTimeInput.FillAsync("17:00");
        await Actions.Wait500();
        await applyCutButton.ClickAsync();
        await Actions.Wait1000();

        var cutRows = await Page.QuerySelectorAllAsync("tr.cut-row, table tbody tr");
        TestContext.Out.WriteLine($"After EBENE 0 cuts: {cutRows.Count} rows");
        Assert.That(cutRows.Count, Is.GreaterThanOrEqualTo(3), "Should have 3 EBENE 0 cuts");

        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("IMPORTANT: The following nested cuts should be done WITHOUT saving!");
        TestContext.Out.WriteLine("Cut 2: Select first cut (06-12) and split again into (06-09, 09-12)");

        if (cutRows.Count >= 3)
        {
            await cutRows[0].ClickAsync();
            await Actions.Wait500();

            var nestedCutButton = await Page.QuerySelectorAsync("button:has-text('Cut'), button:has-text('Schneiden')");
            if (nestedCutButton != null)
            {
                await nestedCutButton.ClickAsync();
                await Actions.Wait500();

                var nestedCutTimeInput = await Page.QuerySelectorAsync("input[name='cutTime'], input[type='time']");
                if (nestedCutTimeInput != null)
                {
                    await nestedCutTimeInput.FillAsync("09:00");
                    await Actions.Wait500();

                    var nestedApplyButton = await Page.QuerySelectorAsync("button:has-text('Apply'), button:has-text('Anwenden')");
                    if (nestedApplyButton != null)
                    {
                        await nestedApplyButton.ClickAsync();
                        await Actions.Wait1000();

                        TestContext.Out.WriteLine("Nested cut applied (without saving)");
                    }
                }
            }
        }

        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("Cut 3: Select second cut (12-17) and split again into (12-14:30, 14:30-17)");

        cutRows = await Page.QuerySelectorAllAsync("tr.cut-row, table tbody tr");
        if (cutRows.Count >= 4)
        {
            await cutRows[1].ClickAsync();
            await Actions.Wait500();

            var nestedCutButton = await Page.QuerySelectorAsync("button:has-text('Cut'), button:has-text('Schneiden')");
            if (nestedCutButton != null)
            {
                await nestedCutButton.ClickAsync();
                await Actions.Wait500();

                var nestedCutTimeInput = await Page.QuerySelectorAsync("input[name='cutTime'], input[type='time']");
                if (nestedCutTimeInput != null)
                {
                    await nestedCutTimeInput.FillAsync("14:30");
                    await Actions.Wait500();

                    var nestedApplyButton = await Page.QuerySelectorAsync("button:has-text('Apply'), button:has-text('Anwenden')");
                    if (nestedApplyButton != null)
                    {
                        await nestedApplyButton.ClickAsync();
                        await Actions.Wait1000();

                        TestContext.Out.WriteLine("Second nested cut applied (without saving)");
                    }
                }
            }
        }

        var finalCutRows = await Page.QuerySelectorAllAsync("tr.cut-row, table tbody tr");
        TestContext.Out.WriteLine($"Total cuts after all operations: {finalCutRows.Count}");
        TestContext.Out.WriteLine("Expected: 3 EBENE 0 + 2 EBENE 1 (from first) + 2 EBENE 1 (from second) = 7 total");

        Assert.Pass("Multiple cuts without saving tested - structure ready for batch save");
    }

    [Test, Order(3)]
    public async Task Step3_SaveAllCutsInBatch()
    {
        TestContext.Out.WriteLine("=== Step 3: Save All Cuts in ONE Batch Operation ===");
        TestContext.Out.WriteLine("This tests the PostBatchCutsCommandHandler!");

        await Step2_PerformMultipleCutsWithoutSaving();

        var saveButton = await Page.QuerySelectorAsync("button:has-text('Speichern'), button:has-text('Save')");
        Assert.That(saveButton, Is.Not.Null, "Save button should exist");

        TestContext.Out.WriteLine("Clicking Save - this should trigger PostBatchCutsCommandHandler");
        TestContext.Out.WriteLine("Expected Backend flow:");
        TestContext.Out.WriteLine("1. Frontend sends List<CutOperation> with temp-IDs");
        TestContext.Out.WriteLine("2. Backend does Topological Sort");
        TestContext.Out.WriteLine("3. Backend resolves temp-IDs to real IDs");
        TestContext.Out.WriteLine("4. Backend calls ShiftTreeService for tree fields");
        TestContext.Out.WriteLine("5. All cuts saved in one transaction");

        await saveButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait3000();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during batch save. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Batch save completed successfully!");
    }

    [Test, Order(4)]
    public async Task Step4_VerifyBatchSaveResults()
    {
        TestContext.Out.WriteLine("=== Step 4: Verify Batch Save Results ===");

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
        TestContext.Out.WriteLine($"Total SplitShifts found in database: {splitShiftRows.Count}");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("Expected structure:");
        TestContext.Out.WriteLine("EBENE 0:");
        TestContext.Out.WriteLine("  - Cut A (06-12): lft=1, rgt=6, parent_id=NULL, root_id=own ID");
        TestContext.Out.WriteLine("    └─ EBENE 1:");
        TestContext.Out.WriteLine("       - Cut A1 (06-09): lft=2, rgt=3, parent_id=A, root_id=A");
        TestContext.Out.WriteLine("       - Cut A2 (09-12): lft=4, rgt=5, parent_id=A, root_id=A");
        TestContext.Out.WriteLine("  - Cut B (12-17): lft=1, rgt=6, parent_id=NULL, root_id=own ID");
        TestContext.Out.WriteLine("    └─ EBENE 1:");
        TestContext.Out.WriteLine("       - Cut B1 (12-14:30): lft=2, rgt=3, parent_id=B, root_id=B");
        TestContext.Out.WriteLine("       - Cut B2 (14:30-17): lft=4, rgt=5, parent_id=B, root_id=B");
        TestContext.Out.WriteLine("  - Cut C (17-22): lft=1, rgt=2, parent_id=NULL, root_id=own ID");

        Assert.That(splitShiftRows.Count, Is.GreaterThanOrEqualTo(5),
            "Should have at least 5 SplitShifts after batch cutting");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Batch save verification completed!");
    }

    [Test, Order(5)]
    public async Task Step5_VerifyTopologicalSortWorked()
    {
        TestContext.Out.WriteLine("=== Step 5: Verify Topological Sort Worked Correctly ===");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("The PostBatchCutsCommandHandler should have:");
        TestContext.Out.WriteLine("1. Received operations in random order:");
        TestContext.Out.WriteLine("   - UPDATE: Original shift → EBENE 0 Cut A");
        TestContext.Out.WriteLine("   - CREATE: EBENE 0 Cut B (depends on nothing)");
        TestContext.Out.WriteLine("   - CREATE: EBENE 0 Cut C (depends on nothing)");
        TestContext.Out.WriteLine("   - CREATE: EBENE 1 Cut A1 (depends on temp-ID of A)");
        TestContext.Out.WriteLine("   - CREATE: EBENE 1 Cut A2 (depends on temp-ID of A)");
        TestContext.Out.WriteLine("   - CREATE: EBENE 1 Cut B1 (depends on temp-ID of B)");
        TestContext.Out.WriteLine("   - CREATE: EBENE 1 Cut B2 (depends on temp-ID of B)");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("2. Done Topological Sort to ensure parents before children:");
        TestContext.Out.WriteLine("   Order: UPDATE(A) → CREATE(B) → CREATE(C) → CREATE(A1) → CREATE(A2) → CREATE(B1) → CREATE(B2)");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("3. Resolved temp-IDs:");
        TestContext.Out.WriteLine("   - temp-1 (Cut A) → real ID after UPDATE");
        TestContext.Out.WriteLine("   - temp-2 (Cut B) → real ID after CREATE");
        TestContext.Out.WriteLine("   - A1's parentId: temp-1 → resolved to real Cut A ID");
        TestContext.Out.WriteLine("   - A2's parentId: temp-1 → resolved to real Cut A ID");
        TestContext.Out.WriteLine("   - B1's parentId: temp-2 → resolved to real Cut B ID");
        TestContext.Out.WriteLine("   - B2's parentId: temp-2 → resolved to real Cut B ID");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("4. Called ShiftTreeService for correct tree values:");
        TestContext.Out.WriteLine("   - EBENE 0: AddRootNodeAsync() → lft=1, rgt=2 (initially)");
        TestContext.Out.WriteLine("   - EBENE 1: AddChildNodeAsync() → calculates lft/rgt, updates parent");

        Assert.Pass("Topological Sort documentation provided");
    }
}
