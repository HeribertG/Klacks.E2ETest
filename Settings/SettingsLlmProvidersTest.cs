using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsLlmProvidersIds;
using static Klacks.E2ETest.Constants.SettingsLlmProvidersTestData;

namespace Klacks.E2ETest;

[TestFixture]
[Order(31)]
public class SettingsLlmProvidersTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private static string? _createdProviderId;
    private static string? _createdProviderName;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(Section);
        await Actions.Wait500();
    }

    [TearDown]
    public void TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
        }
    }

    private async Task<string?> FindProviderRowByName(string providerName)
    {
        var rows = await Page.QuerySelectorAllAsync(RowSelector);
        TestContext.Out.WriteLine($"Searching for provider '{providerName}' in {rows.Count} rows");

        foreach (var row in rows)
        {
            var displayInput = await row.QuerySelectorAsync("input[readonly]");
            if (displayInput == null)
            {
                continue;
            }

            var displayValue = await displayInput.InputValueAsync();
            var displayId = await displayInput.GetAttributeAsync("id");

            if (!string.IsNullOrEmpty(displayValue) && displayValue.Contains(providerName))
            {
                var providerId = displayId?.Replace("llm-providers-row-display-", "") ?? string.Empty;
                TestContext.Out.WriteLine($"Found provider '{providerName}' with ID: {providerId}");
                return providerId;
            }
        }

        return null;
    }

    private async Task<int> GetProviderRowCount()
    {
        var rows = await Page.QuerySelectorAllAsync(RowSelector);
        return rows.Count;
    }

    private async Task<Microsoft.Playwright.IElementHandle?> FindAddButton()
    {
        var section = await Page.QuerySelectorAsync($"#{Section}");
        if (section == null)
        {
            return null;
        }

        return await section.QuerySelectorAsync(".add-button");
    }

    [Test]
    [Order(1)]
    public async Task Step1_VerifyLlmProvidersPageLoaded()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Verify LLM Providers Section Loaded ===");

        // Act
        var section = await Actions.FindElementById(Section);
        var addButton = await FindAddButton();
        var header = await Actions.FindElementById(TableHeader);

        // Assert
        Assert.That(section, Is.Not.Null, "LLM Providers section should be visible");
        Assert.That(addButton, Is.Not.Null, "Add provider button should be visible");
        Assert.That(header, Is.Not.Null, "Table header should be visible");

        var rowCount = await GetProviderRowCount();
        TestContext.Out.WriteLine($"Found {rowCount} existing providers");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("LLM Providers section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateNewProvider()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create New LLM Provider ===");
        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdProviderId = $"{TestProviderId}-{timestamp}";
        _createdProviderName = $"{TestProviderName} {timestamp}";
        TestContext.Out.WriteLine($"Creating provider: {_createdProviderName} (ID: {_createdProviderId})");

        var rowCountBefore = await GetProviderRowCount();
        TestContext.Out.WriteLine($"Providers before: {rowCountBefore}");

        // Act
        var addButton = await FindAddButton();
        Assert.That(addButton, Is.Not.Null, "Add button should exist");

        await addButton!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Add Provider button");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        await Actions.ClearInputById(ModalInputProviderId);
        await Actions.TypeIntoInputById(ModalInputProviderId, _createdProviderId);
        TestContext.Out.WriteLine($"Set provider ID: {_createdProviderId}");

        await Actions.ClearInputById(ModalInputProviderName);
        await Actions.TypeIntoInputById(ModalInputProviderName, _createdProviderName);
        TestContext.Out.WriteLine($"Set provider name: {_createdProviderName}");

        await Actions.ClearInputById(ModalInputBaseUrl);
        await Actions.TypeIntoInputById(ModalInputBaseUrl, TestBaseUrl);
        TestContext.Out.WriteLine($"Set base URL: {TestBaseUrl}");

        await Actions.ClearInputById(ModalInputApiVersion);
        await Actions.TypeIntoInputById(ModalInputApiVersion, TestApiVersion);
        TestContext.Out.WriteLine($"Set API version: {TestApiVersion}");

        await Actions.ClearInputById(ModalInputPriority);
        await Actions.TypeIntoInputById(ModalInputPriority, TestPriority);
        TestContext.Out.WriteLine($"Set priority: {TestPriority}");

        await Actions.ClearInputById(ModalInputApiKey);
        await Actions.TypeIntoInputById(ModalInputApiKey, TestApiKey);
        TestContext.Out.WriteLine($"Set API key: {TestApiKey}");

        var saveBtn = await Actions.FindElementById(ModalSaveBtn);
        Assert.That(saveBtn, Is.Not.Null, "Save button should exist");

        await saveBtn!.ClickAsync();
        TestContext.Out.WriteLine("Clicked Save button");

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error after create: {_listener.GetLastErrorMessage()}");
        }

        var rowCountAfter = await GetProviderRowCount();
        TestContext.Out.WriteLine($"Providers after: {rowCountAfter}");

        // Assert
        Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore + 1), "Provider count should increase by 1");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Provider created successfully: {_createdProviderName}");
    }

    [Test]
    [Order(3)]
    public async Task Step3_VerifyCreatedProviderExists()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Verify Created Provider Exists ===");

        if (string.IsNullOrEmpty(_createdProviderName))
        {
            TestContext.Out.WriteLine("No provider was created in Step2 - skipping");
            Assert.Inconclusive("No provider was created in previous step");
            return;
        }

        // Act
        var foundProviderId = await FindProviderRowByName(_createdProviderName);

        // Assert
        Assert.That(foundProviderId, Is.Not.Null, $"Provider '{_createdProviderName}' should exist in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Provider '{_createdProviderName}' found successfully");
    }

    [Test]
    [Order(4)]
    public async Task Step4_OpenAndVerifyProviderModal()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Open and Verify Provider Modal ===");

        if (string.IsNullOrEmpty(_createdProviderId))
        {
            TestContext.Out.WriteLine("No provider was created - skipping");
            Assert.Inconclusive("No provider was created in previous step");
            return;
        }

        // Act
        var displayId = GetRowDisplayId(_createdProviderId);
        var displayElement = await Actions.FindElementById(displayId);

        if (displayElement == null)
        {
            TestContext.Out.WriteLine($"Display element '{displayId}' not found - trying to find by name");
            var foundId = await FindProviderRowByName(_createdProviderName!);
            if (foundId != null)
            {
                displayId = GetRowDisplayId(foundId);
                displayElement = await Actions.FindElementById(displayId);
            }
        }

        Assert.That(displayElement, Is.Not.Null, "Provider display element should exist");

        await displayElement!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked on provider to open modal");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        var providerIdInput = await Actions.FindElementById(ModalInputProviderId);
        Assert.That(providerIdInput, Is.Not.Null, "Provider ID input should be visible");

        var providerNameInput = await Actions.FindElementById(ModalInputProviderName);
        Assert.That(providerNameInput, Is.Not.Null, "Provider name input should be visible");

        var baseUrlInput = await Actions.FindElementById(ModalInputBaseUrl);
        Assert.That(baseUrlInput, Is.Not.Null, "Base URL input should be visible");

        var apiKeyInput = await Actions.FindElementById(ModalInputApiKey);
        Assert.That(apiKeyInput, Is.Not.Null, "API key input should be visible");

        TestContext.Out.WriteLine("All form fields are visible");

        await Actions.ClickElementById(ModalCancelBtn);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked Cancel to close modal");

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Provider modal opened and verified successfully");
    }

    [Test]
    [Order(5)]
    public async Task Step5_UpdateProvider()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Update Provider ===");

        if (string.IsNullOrEmpty(_createdProviderId))
        {
            TestContext.Out.WriteLine("No provider was created - skipping");
            Assert.Inconclusive("No provider was created in previous step");
            return;
        }

        // Act
        var displayId = GetRowDisplayId(_createdProviderId);
        var displayElement = await Actions.FindElementById(displayId);

        if (displayElement == null)
        {
            var foundId = await FindProviderRowByName(_createdProviderName!);
            if (foundId != null)
            {
                displayId = GetRowDisplayId(foundId);
                displayElement = await Actions.FindElementById(displayId);
            }
        }

        Assert.That(displayElement, Is.Not.Null, "Provider display element should exist");

        await displayElement!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Opened modal for editing");

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdProviderName = $"{UpdatedProviderName} {timestamp}";

        await Actions.ClearInputById(ModalInputProviderName);
        await Actions.TypeIntoInputById(ModalInputProviderName, _createdProviderName);
        TestContext.Out.WriteLine($"Updated name to: {_createdProviderName}");

        await Actions.ClearInputById(ModalInputBaseUrl);
        await Actions.TypeIntoInputById(ModalInputBaseUrl, UpdatedBaseUrl);
        TestContext.Out.WriteLine($"Updated base URL to: {UpdatedBaseUrl}");

        await Actions.ClearInputById(ModalInputApiVersion);
        await Actions.TypeIntoInputById(ModalInputApiVersion, UpdatedApiVersion);
        TestContext.Out.WriteLine($"Updated API version to: {UpdatedApiVersion}");

        await Actions.ClearInputById(ModalInputPriority);
        await Actions.TypeIntoInputById(ModalInputPriority, UpdatedPriority);
        TestContext.Out.WriteLine($"Updated priority to: {UpdatedPriority}");

        var saveBtn = await Actions.FindElementById(ModalSaveBtn);
        await saveBtn!.ClickAsync();
        TestContext.Out.WriteLine("Clicked Save button");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var foundUpdatedProvider = await FindProviderRowByName(_createdProviderName);

        // Assert
        Assert.That(foundUpdatedProvider, Is.Not.Null, "Updated provider should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Provider updated successfully");
    }

    [Test]
    [Order(6)]
    public async Task Step6_DeleteCreatedProvider()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Delete Created Provider ===");

        if (string.IsNullOrEmpty(_createdProviderId))
        {
            TestContext.Out.WriteLine("No provider was created - skipping delete");
            Assert.Inconclusive("No provider was created in previous step");
            return;
        }

        var rowCountBefore = await GetProviderRowCount();
        TestContext.Out.WriteLine($"Providers before delete: {rowCountBefore}");

        // Act
        var deleteId = GetRowDeleteId(_createdProviderId);
        var deleteBtn = await Actions.FindElementById(deleteId);

        if (deleteBtn == null)
        {
            var foundId = await FindProviderRowByName(_createdProviderName!);
            if (foundId != null)
            {
                deleteId = GetRowDeleteId(foundId);
                deleteBtn = await Actions.FindElementById(deleteId);
            }
        }

        Assert.That(deleteBtn, Is.Not.Null, "Delete button should exist");

        await deleteBtn!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked delete button");

        var confirmBtn = await Actions.FindElementById("modal-delete-confirm");
        if (confirmBtn == null)
        {
            TestContext.Out.WriteLine("ERROR: Delete confirmation button not found!");
            Assert.Fail("Delete confirmation modal did not appear");
            return;
        }
        TestContext.Out.WriteLine("Delete modal opened, clicking confirm...");

        await confirmBtn.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();
        TestContext.Out.WriteLine("Confirmed deletion");

        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
        }

        var rowCountAfter = await GetProviderRowCount();
        TestContext.Out.WriteLine($"Providers after delete: {rowCountAfter}");

        // Assert
        Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore - 1), "Provider count should decrease by 1");

        var providerStillExists = await FindProviderRowByName(_createdProviderName!);
        Assert.That(providerStillExists, Is.Null, "Deleted provider should not exist anymore");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Provider {_createdProviderName} deleted successfully");
        _createdProviderId = null;
        _createdProviderName = null;
    }
}
