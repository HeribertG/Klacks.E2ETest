using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest;

[TestFixture]
[Category("Input")]
[Order(16)]
public class ClientQualificationsTest : PlaywrightSetup
{
    private Listener _listener = null!;

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [Test]
    public async Task AddQualificationToEmployee_PersistsAcrossReload()
    {
        await OpenFirstEmployeeAsync();

        var cardVisible = await WaitUntilVisibleAsync(QualificationsCardIds.AddButton);
        Assert.That(
            cardVisible,
            Is.True,
            "The qualifications card add button must be visible for an employee (qualifications must be defined in settings).");
        await Actions.ScrollIntoViewById(QualificationsCardIds.AddButton);

        var rowsBefore = await CountQualificationRowsAsync();
        var newIndex = rowsBefore;

        await Actions.ClickButtonById(QualificationsCardIds.AddButton);
        await Actions.Wait500();

        await Actions.SelectNativeOptionByIndex(QualificationsCardIds.SelectId(newIndex), 1);
        await Actions.Wait500();
        await Actions.SelectNativeOptionByIndex(QualificationsCardIds.LevelId(newIndex), 3);
        await Actions.Wait500();

        await SaveAsync();

        Assert.That(
            _listener.HasApiErrors(),
            Is.False,
            $"Saving an employee with an unchanged address must not fail (no address re-validation). Last error: {_listener.GetLastErrorMessage()}");

        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();
        await WaitUntilVisibleAsync(QualificationsCardIds.AddButton);
        await Actions.ScrollIntoViewById(QualificationsCardIds.AddButton);

        var rowsAfter = await CountQualificationRowsAsync();
        Assert.That(
            rowsAfter,
            Is.EqualTo(rowsBefore + 1),
            "The newly added qualification must still be present after reloading the page.");

        // Cleanup only when the employee had no prior qualifications, so we never risk
        // deleting pre-existing data. The freshly added row is then the only one (index 0).
        if (rowsBefore == 0)
        {
            await Actions.ClickElementById(QualificationsCardIds.DeleteId(0));
            await Actions.Wait500();
            await SaveAsync();
        }
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

    private async Task SaveAsync()
    {
        await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();
    }

    private async Task<int> CountQualificationRowsAsync()
    {
        var selects = await Actions.GetElementsBySelector("[id^='" + QualificationsCardIds.SelectPrefix + "']");
        return selects.Count;
    }

    private async Task<bool> WaitUntilVisibleAsync(string elementId, int maxAttempts = 20)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            if (await Actions.IsElementVisibleById(elementId))
            {
                return true;
            }
            await Actions.Wait500();
        }
        return false;
    }
}
