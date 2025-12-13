using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.ClientDeletionIds;
using static E2ETest.Constants.ClientFilterIds;

namespace E2ETest;

[TestFixture]
[Order(14)]
public class ClientDeletionTest : PlaywrightSetup
{
    private Listener _listener = null!;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await WaitForClientTableData();
    }

    private async Task WaitForClientTableData()
    {
        const int maxAttempts = 60;
        const int delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            var tableRows = await Actions.GetElementsBySelector(ClientRowSelector);
            if (tableRows.Count > 0)
            {
                TestContext.Out.WriteLine($"Client table loaded after {i * delayMs}ms ({tableRows.Count} rows)");
                return;
            }

            await Task.Delay(delayMs);
        }

        TestContext.Out.WriteLine($"WARNING: Client table not loaded after {maxAttempts * delayMs}ms");
    }

    [TearDown]
    public void TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
        }
    }

    [Test]
    [Order(1)]
    public async Task Step1_DeleteAllTestClients()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Delete All Test Clients ===");

        var clientsToDelete = ClientTestData.Clients;
        var deletedCount = 0;

        // Act
        foreach (var client in clientsToDelete)
        {
            var clientName = $"{client.FirstName} {client.LastName}";
            TestContext.Out.WriteLine($"\n--- Deleting client: {clientName} ---");

            var deleted = await SearchAndDeleteClient(clientName);
            if (!deleted)
            {
                TestContext.Out.WriteLine($"Client '{clientName}' not found - may have been already deleted");
                continue;
            }

            deletedCount++;

            await Actions.Wait500();
            await WaitForClientTableData();
        }

        // Assert
        TestContext.Out.WriteLine($"\n=== Deleted {deletedCount} of {clientsToDelete.Length} clients ===");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
    }

    private async Task<bool> SearchAndDeleteClient(string searchTerm)
    {
        await Actions.ClearInputById(SearchInputId);
        await Actions.FillInputById(SearchInputId, searchTerm);
        await Actions.Wait500();

        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var tableRows = await Actions.GetElementsBySelector(ClientRowSelector);
        TestContext.Out.WriteLine($"Search '{searchTerm}' found {tableRows.Count} rows");

        if (tableRows.Count == 0)
        {
            return false;
        }

        var deleteButtonId = $"{DeleteButtonPrefix}0";
        var deleteButton = await Actions.FindElementById(deleteButtonId);

        if (deleteButton == null)
        {
            TestContext.Out.WriteLine($"Delete button '{deleteButtonId}' not found");
            return false;
        }

        TestContext.Out.WriteLine($"Clicking delete button: {deleteButtonId}");
        await deleteButton.ClickAsync();
        await Actions.Wait500();

        TestContext.Out.WriteLine("Confirming deletion in modal...");
        await Actions.ClickElementById(DeleteModalConfirmBtn);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ClearInputById(SearchInputId);
        await Actions.ClickButtonById(SearchButtonId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        TestContext.Out.WriteLine($"Deleted client: {searchTerm}");
        return true;
    }
}
