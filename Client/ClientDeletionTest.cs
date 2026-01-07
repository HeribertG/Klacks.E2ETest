using System.Text.RegularExpressions;
using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Npgsql;
using static E2ETest.Constants.ClientDeletionIds;
using static E2ETest.Constants.ClientFilterIds;
using static E2ETest.Constants.PaginationIds;

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

    private async Task<int> GetPaginationTotalCount()
    {
        var labelText = await Actions.GetTextContentById(TotalCountLabel);
        var match = Regex.Match(labelText, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int count))
        {
            TestContext.Out.WriteLine($"Pagination total count: {count}");
            return count;
        }

        TestContext.Out.WriteLine($"Could not parse pagination count from: '{labelText}'");
        return 0;
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

    [Test]
    [Order(2)]
    public async Task Step2_VerifyDeletedClientsInFilter()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Verify Deleted Clients in 'Gelöschte Adressen' Filter ===");

        // Act
        TestContext.Out.WriteLine("Clicking 'Gelöschte Adressen' checkbox...");
        await Actions.ClickCheckBoxById(FilterShowDeletedId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var totalCount = await GetPaginationTotalCount();
        TestContext.Out.WriteLine($"Found {totalCount} deleted addresses");

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        Assert.That(totalCount, Is.GreaterThanOrEqualTo(ClientTestData.Clients.Length),
            $"Should find at least {ClientTestData.Clients.Length} deleted clients. Found: {totalCount}");

        TestContext.Out.WriteLine($"=== Verified: {totalCount} deleted addresses found (expected at least {ClientTestData.Clients.Length}) ===");

        // Reset filter for clean state
        TestContext.Out.WriteLine("Resetting 'Gelöschte Adressen' filter...");
        await Actions.ClickCheckBoxById(FilterShowDeletedId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [Test]
    [Order(3)]
    public async Task Step3_PhysicallyDeleteTestClientsFromDatabase()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Physically Delete Test Clients from Database ===");

        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5434;Database=klacks;Username=postgres;Password=admin";

        var deletedCount = 0;

        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        TestContext.Out.WriteLine($"Connected to database");

        foreach (var client in ClientTestData.Clients)
        {
            var clientName = $"{client.FirstName} {client.LastName}";
            TestContext.Out.WriteLine($"Physically deleting: {clientName}");

            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM client
                WHERE first_name = @firstName
                AND name = @lastName", connection);

            cmd.Parameters.AddWithValue("firstName", client.FirstName);
            cmd.Parameters.AddWithValue("lastName", client.LastName);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            {
                deletedCount += rowsAffected;
                TestContext.Out.WriteLine($"  -> Deleted {rowsAffected} row(s)");
            }
            else
            {
                TestContext.Out.WriteLine($"  -> No rows found (already deleted or never existed)");
            }
        }

        // Assert
        TestContext.Out.WriteLine($"\n=== Physically deleted {deletedCount} client(s) from database ===");

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
