using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest;

[TestFixture]
[Order(17)]
public class AddressValidationToastTest : PlaywrightSetup
{
    private const string SaveAnywaySelector =
        "app-toasts .reply-chip-btn:has-text(\"Trotzdem speichern\"), app-toasts .reply-chip-btn:has-text(\"Save anyway\")";

    private Listener _listener = null!;

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [Test]
    public async Task ChangedInvalidAddress_ShowsSaveAnywayToast()
    {
        await OpenFirstEmployeeAsync();

        // Change ONLY the street to a value that cannot be geocoded exactly while keeping the
        // (real) zip/city. The client-side check accepts the area but the backend geocoding
        // validator rejects the non-matching street -> the backend-error path must surface the
        // interactive "save anyway" toast.
        await Actions.ScrollIntoViewById(ClientIds.InputStreet);
        await Actions.ClearInputById(ClientIds.InputStreet);
        await Actions.FillInputById(ClientIds.InputStreet, "ZZ Unverifiable Street 99999");
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();

        var saveAnywayButton = await Actions.FindElementByCssSelector(SaveAnywaySelector);
        Assert.That(
            saveAnywayButton,
            Is.Not.Null,
            "Saving a client with a changed, non-verifiable address must show the interactive 'save anyway' toast.");

        // Discard the unsaved change instead of persisting the invalid address.
        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
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
