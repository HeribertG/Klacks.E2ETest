using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest;

[TestFixture]
[Order(17)]
public class AddressValidationToastTest : PlaywrightSetup
{
    private const string UnverifiableStreet = "ZZ Unverifiable Street 99999";

    private const string SaveAnywaySelector =
        "app-toasts .reply-chip-btn:has-text(\"Trotzdem speichern\"), app-toasts .reply-chip-btn:has-text(\"Save anyway\")";

    private const string AllReplyChipsSelector = "app-toasts .reply-chip-btn";

    private Listener _listener = null!;

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [Test]
    [Order(1)]
    public async Task ChangedInvalidAddress_ShowsSaveAnywayToastWithSuggestions()
    {
        await OpenFirstEmployeeAsync();

        // Change ONLY the street to a value that cannot be geocoded exactly while keeping the
        // (real) zip/city. Both the client-side pre-check and the backend save-validator now
        // treat a street-present, city-only match as invalid -> the interactive "save anyway"
        // toast must appear, and it must additionally offer correction suggestions.
        await SetStreetAsync(UnverifiableStreet);

        await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();

        var saveAnywayButton = await PollForSaveAnywayChipAsync();
        Assert.That(
            saveAnywayButton,
            Is.Not.Null,
            "Saving a client with a changed, non-verifiable address must show the interactive 'save anyway' toast.");

        var chipCount = (await Actions.GetElementsBySelector(AllReplyChipsSelector)).Count;
        TestContext.Out.WriteLine($"Reply chips in toast (suggestions + save-anyway): {chipCount}");
        Assert.That(
            chipCount,
            Is.GreaterThan(1),
            "The toast must offer at least one correction suggestion in addition to 'save anyway' so the user can correct the address.");

        // Discard the unsaved change instead of persisting the invalid address.
        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
    }

    [Test]
    [Order(2)]
    public async Task SaveAndClose_WithInvalidAddress_StaysOnPageAndShowsToast()
    {
        await OpenFirstEmployeeAsync();
        var editUrl = Actions.ReadCurrentUrl();

        await SetStreetAsync(UnverifiableStreet);

        // "Save & Close" must NOT navigate away while the address is unverifiable. The page has to
        // stay open with the interactive toast so the user can correct or explicitly accept.
        await Actions.ScrollIntoViewById(SaveBarIds.SaveAndCloseButton);
        await Actions.ClickButtonById(SaveBarIds.SaveAndCloseButton);
        await Actions.WaitForSpinnerToDisappear();

        var saveAnywayButton = await PollForSaveAnywayChipAsync();
        Assert.That(
            saveAnywayButton,
            Is.Not.Null,
            "'Save & Close' with a non-verifiable address must surface the interactive 'save anyway' toast.");

        var currentUrl = Actions.ReadCurrentUrl();
        Assert.That(
            currentUrl,
            Does.Contain("workplace/edit-address"),
            "'Save & Close' must not navigate away while the address is unverified and unconfirmed.");
        Assert.That(
            currentUrl,
            Is.EqualTo(editUrl),
            "'Save & Close' must keep the same edit-address page open until the user confirms.");

        // Discard the unsaved change instead of persisting the invalid address.
        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
    }

    [Test]
    [Order(3)]
    public async Task SaveAnyway_PersistsAndUnchangedAddressDoesNotReValidate()
    {
        await OpenFirstEmployeeAsync();

        await Actions.ScrollIntoViewById(ClientIds.InputStreet);
        var originalStreet = await Actions.ReadInput(ClientIds.InputStreet);
        await Actions.ScrollIntoViewById(ClientIds.InputFirstName);
        var originalFirstName = await Actions.ReadInput(ClientIds.InputFirstName);
        var probeFirstName = originalFirstName + "X";

        try
        {
            // 1) Accept path: 'save anyway' must actually persist the unverifiable address.
            await SetStreetAsync(UnverifiableStreet);
            await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
            await Actions.ClickButtonById(SaveBarIds.SaveButton);
            await Actions.WaitForSpinnerToDisappear();

            var saveAnyway = await PollForSaveAnywayChipAsync();
            Assert.That(saveAnyway, Is.Not.Null, "Save with an unverifiable street must show the 'save anyway' toast.");
            await Actions.ClickElement(saveAnyway!);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.Reload();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.ScrollIntoViewById(ClientIds.InputStreet);
            var persistedStreet = await Actions.ReadInput(ClientIds.InputStreet);
            Assert.That(
                persistedStreet,
                Is.EqualTo(UnverifiableStreet),
                "'Save anyway' must persist the unverifiable address (accept path must function, not just render).");

            // 2) Regression guard: with the unverifiable address now persisted and UNCHANGED,
            // editing a non-address field must NOT re-validate the address (mirrors the backend
            // AddressGeocodingValidator's unchanged-skip). Otherwise every later edit of such a
            // client would be wrongly blocked by the validation toast.
            await Actions.ScrollIntoViewById(ClientIds.InputFirstName);
            await Actions.ClearInputById(ClientIds.InputFirstName);
            await Actions.FillInputById(ClientIds.InputFirstName, probeFirstName);
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
            await Actions.ClickButtonById(SaveBarIds.SaveButton);
            await Actions.WaitForSpinnerToDisappear();

            var unexpectedToast = await PollForSaveAnywayChipAsync(6);
            Assert.That(
                unexpectedToast,
                Is.Null,
                "Editing a non-address field on a client whose persisted address is unchanged must not re-trigger address validation.");

            await Actions.Reload();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
            await Actions.ScrollIntoViewById(ClientIds.InputFirstName);
            var persistedFirstName = await Actions.ReadInput(ClientIds.InputFirstName);
            Assert.That(
                persistedFirstName,
                Is.EqualTo(probeFirstName),
                "The non-address change must have been saved without being blocked by address validation.");
        }
        finally
        {
            await RestoreClientAsync(originalFirstName, originalStreet);
        }
    }

    private async Task RestoreClientAsync(string originalFirstName, string originalStreet)
    {
        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(ClientIds.InputFirstName);
        await Actions.ClearInputById(ClientIds.InputFirstName);
        await Actions.FillInputById(ClientIds.InputFirstName, originalFirstName);
        await SetStreetAsync(originalStreet);

        await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();

        // The original street should validate cleanly; if not, accept anyway so restore completes.
        var restoreToast = await PollForSaveAnywayChipAsync(6);
        if (restoreToast != null)
        {
            await Actions.ClickElement(restoreToast);
            await Actions.WaitForSpinnerToDisappear();
        }
    }

    private async Task SetStreetAsync(string street)
    {
        await Actions.ScrollIntoViewById(ClientIds.InputStreet);
        await Actions.ClearInputById(ClientIds.InputStreet);
        await Actions.FillInputById(ClientIds.InputStreet, street);
        await Actions.Wait500();
    }

    private async Task<IElementHandle?> PollForSaveAnywayChipAsync(int maxAttempts = 12)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            var chip = await Actions.FindElementByCssSelector(SaveAnywaySelector);
            if (chip != null)
            {
                return chip;
            }

            await Actions.Wait500();
        }

        return null;
    }

    private async Task OpenFirstEmployeeAsync()
    {
        await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        for (var i = 0; i < 60; i++)
        {
            var rows = await Actions.GetElementsBySelector(ClientIds.TableRowSelector);
            if (rows.Count > 0)
            {
                break;
            }
            await Actions.Wait500();
        }

        await Actions.ScrollIntoViewById("client-edit-button-0");
        await Actions.ClickElementById("client-edit-button-0");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        Assert.That(
            Actions.ReadCurrentUrl(),
            Does.Contain("workplace/edit-address"),
            "Clicking the client edit button should open the edit-address page.");
    }
}
