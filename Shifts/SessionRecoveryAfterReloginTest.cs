using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.Shifts;

/// <summary>
/// Verifies the session-recovery flow: when the refresh token becomes invalid
/// while editing a shift, the user is returned to the same edit page after a
/// re-login (Ebene A) and is offered to restore the unsaved form input (Ebene B).
/// Requires a running app and an editable (OriginalOrder/status=0) shift whose id
/// is supplied via the KLACKS_E2E_SHIFT_ID environment variable.
/// </summary>
[TestFixture]
[Order(42)]
[Ignore("Live session-recovery flow: requires the running app (frontend 4200 + backend 5001) and an editable status=0 shift via KLACKS_E2E_SHIFT_ID. Run on demand, not in fresh-DB CI.")]
public class SessionRecoveryAfterReloginTest : PlaywrightSetup
{
    private const string NameInputId = "name";
    private const string ApiBaseUrl = "https://localhost:5001/api/backend/";

    private Listener _listener = null!;

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [Test]
    public async Task ReLogin_RestoresPageAndOffersDraftRecovery()
    {
        var shiftId = Environment.GetEnvironmentVariable("KLACKS_E2E_SHIFT_ID");
        if (string.IsNullOrWhiteSpace(shiftId))
        {
            shiftId = await GetFirstShiftIdViaApi();
        }
        Assert.That(shiftId, Is.Not.Empty,
            "Test requires an editable (OriginalOrder/status=0) shift. Provide its id via KLACKS_E2E_SHIFT_ID.");

        var newName = $"E2E Recovery {TimeStamp}";

        TestContext.Out.WriteLine($"=== Open editable shift {shiftId} ===");
        await Actions.NavigateTo($"{BaseUrl}workplace/edit-shift/{shiftId}");
        await Actions.WaitForSpinnerToDisappear();

        // edit-shift is lazy-loaded; the first deep-link can compile the chunk slowly.
        var nameField = await WaitForVisibleById(NameInputId, 6);
        Assert.That(nameField, Is.Not.Null, "Shift name input should be present (editor must be loaded).");

        // Let the post-read form rebuild (prepareShift -> isReset) settle before typing.
        await Actions.Wait1000();

        TestContext.Out.WriteLine($"=== Type unsaved name change: {newName} ===");
        await Actions.FillInputById(NameInputId, newName);
        await Actions.Wait500();
        Assert.That(await Actions.ReadInput(NameInputId), Is.EqualTo(newName),
            "The unsaved change must be present in the form before forcing the logout.");

        TestContext.Out.WriteLine("=== Invalidate tokens so the next request forces a logout ===");
        await InvalidateTokens();

        TestContext.Out.WriteLine("=== Save -> 401 -> refresh fails -> redirect to login ===");
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitUntilUrlContains("login");

        TestContext.Out.WriteLine("=== Re-login ===");
        await Login();

        TestContext.Out.WriteLine("=== Ebene A: back on the same edit-shift page ===");
        await Actions.WaitUntilUrlContains($"edit-shift/{shiftId}");
        await Actions.WaitForSpinnerToDisappear();
        Assert.That(Actions.ReadCurrentUrl(), Does.Contain($"edit-shift/{shiftId}"),
            "After re-login the user should return to the shift they were editing.");

        TestContext.Out.WriteLine("=== Ebene B: the restore dialog should appear ===");
        var restoreButton = await WaitForVisibleById(DraftRecoveryIds.RestoreButton, 6);
        Assert.That(restoreButton, Is.Not.Null,
            "The 'restore unsaved work?' dialog should be offered after re-login.");

        TestContext.Out.WriteLine("=== Confirm restore and verify the typed value is back ===");
        await Actions.ClickButtonById(DraftRecoveryIds.RestoreButton);
        await Actions.Wait1000();
        Assert.That(await Actions.ReadInput(NameInputId), Is.EqualTo(newName),
            "The unsaved name change should be restored into the form.");
    }

    private async Task InvalidateTokens()
    {
        await Page.EvaluateAsync(@"() => {
            localStorage.setItem('JWT_TOKEN', 'invalid-expired-token');
            localStorage.setItem('JWT_REFRESH', 'invalid-expired-token');
        }");
    }

    private async Task<Microsoft.Playwright.IElementHandle?> WaitForVisibleById(string id, int attempts)
    {
        for (var i = 0; i < attempts; i++)
        {
            var el = await Actions.FindElementById(id);
            if (el != null)
            {
                return el;
            }
        }
        return null;
    }

    private async Task<string> GetFirstShiftIdViaApi()
    {
        var apiUrl = ApiBaseUrl + "Shifts/GetSimpleList/";
        var diag = await Page.EvaluateAsync<string>(@"async (apiUrl) => {
            const token = localStorage.getItem('JWT_TOKEN');
            const filter = {
                searchString: '', orderBy: 'name', sortOrder: 'asc',
                numberOfItemsPerPage: 5, requiredPage: 0,
                showDeleteEntries: false, activeDateRange: true,
                formerDateRange: false, futureDateRange: false, filterType: 0,
                includeClientName: false, isSealedOrder: false,
                isTimeRange: true, isSporadic: true
            };
            try {
                const res = await fetch(apiUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token },
                    body: JSON.stringify(filter)
                });
                const text = await res.text();
                let id = '';
                try {
                    const d = JSON.parse(text);
                    const shifts = d.shifts || [];
                    id = shifts.length ? shifts[0].id : '';
                } catch (e) { /* not json */ }
                return 'ID=' + id;
            } catch (e) {
                return 'FETCH_ERR=' + e.message;
            }
        }", apiUrl);

        const string idMarker = "ID=";
        return diag.StartsWith(idMarker, StringComparison.Ordinal)
            ? diag.Substring(idMarker.Length)
            : string.Empty;
    }
}
