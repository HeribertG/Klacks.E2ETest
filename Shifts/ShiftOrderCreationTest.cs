using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Microsoft.Playwright;

namespace E2ETest.Shifts;

[TestFixture]
public class ShiftOrderCreationTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private string _testOrderName = string.Empty;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        _testOrderName = $"E2E Bestellung {DateTime.Now:yyyyMMdd-HHmmss}";

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
    public async Task Step1_CreateShiftOrder()
    {
        TestContext.Out.WriteLine("=== Step 1: Create Shift Order (Bestellung erstellen) ===");
        TestContext.Out.WriteLine($"Order Name: {_testOrderName}");

        // Step 1: Navigate to Shift page
        TestContext.Out.WriteLine("Navigating to /workplace/shift...");
        await Page.GotoAsync($"{BaseUrl}workplace/shift");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Step 2: WICHTIG - Selektiere "Original" Radio-Button zuerst!
        // Nur dann ist der Create-Button sichtbar im DOM
        TestContext.Out.WriteLine("Selecting 'Original' filter (shift-filter-original)...");
        var originalRadio = await Page.QuerySelectorAsync("#shift-filter-original");
        Assert.That(originalRadio, Is.Not.Null,
            "Original filter radio button should exist in all-shift-nav");
        await originalRadio!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Arrange: Prüfe ob bereits eine Bestellung mit diesem Namen existiert
        var existingShift = await Page.QuerySelectorAsync($"tr:has-text('{_testOrderName}')");
        if (existingShift != null)
        {
            TestContext.Out.WriteLine($"Order '{_testOrderName}' already exists, skipping creation");
            Assert.Pass("Order already exists");
            return;
        }

        // Step 3: Jetzt ist der Create-Button sichtbar - Klicke ihn
        TestContext.Out.WriteLine("Clicking '+ Dienst erfassen' button (shift-create-btn)...");
        var createButton = await Page.QuerySelectorAsync("#shift-create-btn");
        Assert.That(createButton, Is.Not.Null,
            "Create button should exist after selecting Original filter");
        await createButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Assert: Prüfe ob wir auf der Edit-Seite sind
        await Page.WaitForURLAsync(url => url.Contains("workplace/edit-shift"), new() { Timeout = 5000 });
        TestContext.Out.WriteLine("Navigated to edit-shift page");

        // Fill Abbreviation (Pflichtfeld)
        TestContext.Out.WriteLine("Filling Abbreviation...");
        var abbreviationInput = await Page.QuerySelectorAsync("#abbreviation");
        Assert.That(abbreviationInput, Is.Not.Null, "Abbreviation input should exist");
        await abbreviationInput!.FillAsync("E2E");
        await Actions.Wait500();

        // Fill Name (Pflichtfeld)
        TestContext.Out.WriteLine("Filling Name...");
        var nameInput = await Page.QuerySelectorAsync("#name");
        Assert.That(nameInput, Is.Not.Null, "Name input should exist");
        await nameInput!.FillAsync(_testOrderName);
        await Actions.Wait500();

        // Fill Valid From (Pflichtfeld)
        TestContext.Out.WriteLine("Filling Valid From...");
        var fromDateInput = await Page.QuerySelectorAsync("#validFrom");
        if (fromDateInput != null)
        {
            var tomorrow = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            await fromDateInput.FillAsync(tomorrow);
            await Actions.Wait500();
        }

        // Fill Valid Until (optional - 7 Tage später)
        TestContext.Out.WriteLine("Filling Valid Until...");
        var untilDateInput = await Page.QuerySelectorAsync("#validUntil");
        if (untilDateInput != null)
        {
            var nextWeek = DateTime.Now.AddDays(8).ToString("dd.MM.yyyy");
            await untilDateInput.FillAsync(nextWeek);
            await Actions.Wait500();
        }

        // Speichern
        TestContext.Out.WriteLine("Clicking Save button...");
        var saveButton = await Page.QuerySelectorAsync("#shift-save-btn");
        Assert.That(saveButton, Is.Not.Null, "Save button should exist");
        await saveButton!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1500();

        // Assert: Prüfe auf API-Fehler
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"✅ Order created successfully: {_testOrderName}");
    }

    [Test, Order(2)]
    public async Task Step2_VerifyOrderInList()
    {
        TestContext.Out.WriteLine("=== Step 2: Verify Order in List ===");

        // Arrange: Navigiere zur Shift-Liste
        await Page.GotoAsync($"{BaseUrl}workplace/shift");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // WICHTIG: Selektiere "Original" Filter
        TestContext.Out.WriteLine("Selecting 'Original' filter...");
        var originalRadio = await Page.QuerySelectorAsync("#shift-filter-original");
        Assert.That(originalRadio, Is.Not.Null, "Original filter should exist");
        await originalRadio!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Act: Suche nach der erstellten Bestellung
        TestContext.Out.WriteLine($"Looking for order: {_testOrderName}");
        var orderRow = await Page.QuerySelectorAsync($"tr:has-text('{_testOrderName}')");

        // Assert: Bestellung sollte in der Liste sein
        Assert.That(orderRow, Is.Not.Null,
            $"Order '{_testOrderName}' should be visible in the list");

        TestContext.Out.WriteLine($"✅ Order found in list: {_testOrderName}");
    }

    [Test, Order(3)]
    public async Task Step3_VerifyOrderDetails()
    {
        TestContext.Out.WriteLine("=== Step 3: Verify Order Details ===");

        // Arrange: Zur Liste navigieren
        await Page.GotoAsync($"{BaseUrl}workplace/shift");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // WICHTIG: Selektiere "Original" Filter
        TestContext.Out.WriteLine("Selecting 'Original' filter...");
        var originalRadio = await Page.QuerySelectorAsync("#shift-filter-original");
        Assert.That(originalRadio, Is.Not.Null, "Original filter should exist");
        await originalRadio!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Act: Bestellung öffnen
        TestContext.Out.WriteLine($"Opening order: {_testOrderName}");
        var orderRow = await Page.QuerySelectorAsync($"tr:has-text('{_testOrderName}')");
        Assert.That(orderRow, Is.Not.Null, "Order should exist in list");
        await orderRow!.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Assert: Prüfe ob wir auf der Edit-Seite sind
        await Page.WaitForURLAsync(url => url.Contains("workplace/edit-shift"), new() { Timeout = 5000 });

        // Prüfe Abbreviation
        var abbreviationInput = await Page.QuerySelectorAsync("#abbreviation");
        if (abbreviationInput != null)
        {
            var abbreviationValue = await abbreviationInput.InputValueAsync();
            Assert.That(abbreviationValue, Is.EqualTo("E2E"), "Abbreviation should be 'E2E'");
            TestContext.Out.WriteLine($"✓ Abbreviation: {abbreviationValue}");
        }

        // Prüfe Name
        var nameInput = await Page.QuerySelectorAsync("#name");
        if (nameInput != null)
        {
            var nameValue = await nameInput.InputValueAsync();
            Assert.That(nameValue, Is.EqualTo(_testOrderName),
                $"Name should be '{_testOrderName}'");
            TestContext.Out.WriteLine($"✓ Name: {nameValue}");
        }

        // Prüfe Status Badge (sollte "OriginalOrder" oder ähnlich sein)
        var statusBadge = await Page.QuerySelectorAsync("span.badge, .status-badge");
        if (statusBadge != null)
        {
            var statusText = await statusBadge.TextContentAsync();
            TestContext.Out.WriteLine($"✓ Status: {statusText}");
        }

        TestContext.Out.WriteLine($"✅ Order details verified: {_testOrderName}");
    }
}
