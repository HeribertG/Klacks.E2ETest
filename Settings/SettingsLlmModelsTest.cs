using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsLlmModelsIds;
using static Klacks.E2ETest.Constants.SettingsLlmModelsTestData;

namespace Klacks.E2ETest;

[TestFixture]
[Order(32)]
public class SettingsLlmModelsTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private static string? _createdModelId;
    private static string? _createdModelName;

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

    private async Task<string?> FindModelRowByName(string modelName)
    {
        var rows = await Page.QuerySelectorAllAsync(RowSelector);
        TestContext.Out.WriteLine($"Searching for model '{modelName}' in {rows.Count} rows");

        foreach (var row in rows)
        {
            var displayInput = await row.QuerySelectorAsync("input[readonly]");
            if (displayInput == null)
            {
                continue;
            }

            var displayValue = await displayInput.InputValueAsync();
            var displayId = await displayInput.GetAttributeAsync("id");

            if (!string.IsNullOrEmpty(displayValue) && displayValue.Contains(modelName))
            {
                var modelId = displayId?.Replace("llm-models-row-display-", "") ?? string.Empty;
                TestContext.Out.WriteLine($"Found model '{modelName}' with ID: {modelId}");
                return modelId;
            }
        }

        return null;
    }

    private async Task<int> GetModelRowCount()
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
    public async Task Step1_VerifyLlmModelsPageLoaded()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Verify LLM Models Section Loaded ===");

        // Act
        var section = await Actions.FindElementById(Section);
        var addButton = await FindAddButton();
        var header = await Actions.FindElementById(TableHeader);

        // Assert
        Assert.That(section, Is.Not.Null, "LLM Models section should be visible");
        Assert.That(addButton, Is.Not.Null, "Add model button should be visible");
        Assert.That(header, Is.Not.Null, "Table header should be visible");

        var rowCount = await GetModelRowCount();
        TestContext.Out.WriteLine($"Found {rowCount} existing models");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("LLM Models section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateNewModel()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create New LLM Model ===");
        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdModelId = $"{TestModelId}-{timestamp}";
        _createdModelName = $"{TestModelName} {timestamp}";
        TestContext.Out.WriteLine($"Creating model: {_createdModelName} (ID: {_createdModelId})");

        var rowCountBefore = await GetModelRowCount();
        TestContext.Out.WriteLine($"Models before: {rowCountBefore}");

        // Act
        var addButton = await FindAddButton();
        Assert.That(addButton, Is.Not.Null, "Add button should exist");

        await addButton!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Add Model button");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        await Actions.ClearInputById(ModalInputModelId);
        await Actions.TypeIntoInputById(ModalInputModelId, _createdModelId);
        TestContext.Out.WriteLine($"Set model ID: {_createdModelId}");

        await Actions.ClearInputById(ModalInputModelName);
        await Actions.TypeIntoInputById(ModalInputModelName, _createdModelName);
        TestContext.Out.WriteLine($"Set model name: {_createdModelName}");

        await Actions.ClearInputById(ModalInputDescription);
        await Actions.TypeIntoInputById(ModalInputDescription, TestDescription);
        TestContext.Out.WriteLine($"Set description: {TestDescription}");

        await Actions.ClearInputById(ModalInputApiModelId);
        await Actions.TypeIntoInputById(ModalInputApiModelId, TestApiModelId);
        TestContext.Out.WriteLine($"Set API model ID: {TestApiModelId}");

        await Actions.ClearInputById(ModalInputContextWindow);
        await Actions.TypeIntoInputById(ModalInputContextWindow, TestContextWindow);
        TestContext.Out.WriteLine($"Set context window: {TestContextWindow}");

        await Actions.ClearInputById(ModalInputMaxTokens);
        await Actions.TypeIntoInputById(ModalInputMaxTokens, TestMaxTokens);
        TestContext.Out.WriteLine($"Set max tokens: {TestMaxTokens}");

        await Actions.ClearInputById(ModalInputInputCost);
        await Actions.TypeIntoInputById(ModalInputInputCost, TestInputCost);
        TestContext.Out.WriteLine($"Set input cost: {TestInputCost}");

        await Actions.ClearInputById(ModalInputOutputCost);
        await Actions.TypeIntoInputById(ModalInputOutputCost, TestOutputCost);
        TestContext.Out.WriteLine($"Set output cost: {TestOutputCost}");

        var apiKeyInput = await Actions.FindElementById(ModalInputApiKey);
        if (apiKeyInput != null)
        {
            await Actions.ClearInputById(ModalInputApiKey);
            await Actions.TypeIntoInputById(ModalInputApiKey, TestApiKey);
            TestContext.Out.WriteLine($"Set API key: {TestApiKey}");
        }

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

        var rowCountAfter = await GetModelRowCount();
        TestContext.Out.WriteLine($"Models after: {rowCountAfter}");

        // Assert
        Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore + 1), "Model count should increase by 1");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Model created successfully: {_createdModelName}");
    }

    [Test]
    [Order(3)]
    public async Task Step3_VerifyCreatedModelExists()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Verify Created Model Exists ===");

        if (string.IsNullOrEmpty(_createdModelName))
        {
            TestContext.Out.WriteLine("No model was created in Step2 - skipping");
            Assert.Inconclusive("No model was created in previous step");
            return;
        }

        // Act
        var foundModelId = await FindModelRowByName(_createdModelName);

        // Assert
        Assert.That(foundModelId, Is.Not.Null, $"Model '{_createdModelName}' should exist in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Model '{_createdModelName}' found successfully");
    }

    [Test]
    [Order(4)]
    public async Task Step4_OpenAndVerifyModelModal()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Open and Verify Model Modal ===");

        if (string.IsNullOrEmpty(_createdModelId))
        {
            TestContext.Out.WriteLine("No model was created - skipping");
            Assert.Inconclusive("No model was created in previous step");
            return;
        }

        // Act
        var displayId = GetRowDisplayId(_createdModelId);
        var displayElement = await Actions.FindElementById(displayId);

        if (displayElement == null)
        {
            TestContext.Out.WriteLine($"Display element '{displayId}' not found - trying to find by name");
            var foundId = await FindModelRowByName(_createdModelName!);
            if (foundId != null)
            {
                displayId = GetRowDisplayId(foundId);
                displayElement = await Actions.FindElementById(displayId);
            }
        }

        Assert.That(displayElement, Is.Not.Null, "Model display element should exist");

        await displayElement!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked on model to open modal");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        var modelIdInput = await Actions.FindElementById(ModalInputModelId);
        Assert.That(modelIdInput, Is.Not.Null, "Model ID input should be visible");

        var modelNameInput = await Actions.FindElementById(ModalInputModelName);
        Assert.That(modelNameInput, Is.Not.Null, "Model name input should be visible");

        var providerSelect = await Actions.FindElementById(ModalSelectProvider);
        Assert.That(providerSelect, Is.Not.Null, "Provider select should be visible");

        var descriptionInput = await Actions.FindElementById(ModalInputDescription);
        Assert.That(descriptionInput, Is.Not.Null, "Description input should be visible");

        TestContext.Out.WriteLine("All form fields are visible");

        await Actions.ClickElementById(ModalCancelBtn);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked Cancel to close modal");

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Model modal opened and verified successfully");
    }

    [Test]
    [Order(5)]
    public async Task Step5_UpdateModel()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Update Model ===");

        if (string.IsNullOrEmpty(_createdModelId))
        {
            TestContext.Out.WriteLine("No model was created - skipping");
            Assert.Inconclusive("No model was created in previous step");
            return;
        }

        // Act
        var displayId = GetRowDisplayId(_createdModelId);
        var displayElement = await Actions.FindElementById(displayId);

        if (displayElement == null)
        {
            var foundId = await FindModelRowByName(_createdModelName!);
            if (foundId != null)
            {
                displayId = GetRowDisplayId(foundId);
                displayElement = await Actions.FindElementById(displayId);
            }
        }

        Assert.That(displayElement, Is.Not.Null, "Model display element should exist");

        await displayElement!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Opened modal for editing");

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdModelName = $"{UpdatedModelName} {timestamp}";

        await Actions.ClearInputById(ModalInputModelName);
        await Actions.TypeIntoInputById(ModalInputModelName, _createdModelName);
        TestContext.Out.WriteLine($"Updated name to: {_createdModelName}");

        await Actions.ClearInputById(ModalInputDescription);
        await Actions.TypeIntoInputById(ModalInputDescription, UpdatedDescription);
        TestContext.Out.WriteLine($"Updated description to: {UpdatedDescription}");

        await Actions.ClearInputById(ModalInputContextWindow);
        await Actions.TypeIntoInputById(ModalInputContextWindow, UpdatedContextWindow);
        TestContext.Out.WriteLine($"Updated context window to: {UpdatedContextWindow}");

        await Actions.ClearInputById(ModalInputMaxTokens);
        await Actions.TypeIntoInputById(ModalInputMaxTokens, UpdatedMaxTokens);
        TestContext.Out.WriteLine($"Updated max tokens to: {UpdatedMaxTokens}");

        var saveBtn = await Actions.FindElementById(ModalSaveBtn);
        await saveBtn!.ClickAsync();
        TestContext.Out.WriteLine("Clicked Save button");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var foundUpdatedModel = await FindModelRowByName(_createdModelName);

        // Assert
        Assert.That(foundUpdatedModel, Is.Not.Null, "Updated model should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Model updated successfully");
    }

    [Test]
    [Order(6)]
    public async Task Step6_DeleteCreatedModel()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Delete Created Model ===");

        if (string.IsNullOrEmpty(_createdModelId))
        {
            TestContext.Out.WriteLine("No model was created - skipping delete");
            Assert.Inconclusive("No model was created in previous step");
            return;
        }

        var rowCountBefore = await GetModelRowCount();
        TestContext.Out.WriteLine($"Models before delete: {rowCountBefore}");

        // Act
        var deleteId = GetRowDeleteId(_createdModelId);
        var deleteBtn = await Actions.FindElementById(deleteId);

        if (deleteBtn == null)
        {
            var foundId = await FindModelRowByName(_createdModelName!);
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

        var rowCountAfter = await GetModelRowCount();
        TestContext.Out.WriteLine($"Models after delete: {rowCountAfter}");

        // Assert
        Assert.That(rowCountAfter, Is.EqualTo(rowCountBefore - 1), "Model count should decrease by 1");

        var modelStillExists = await FindModelRowByName(_createdModelName!);
        Assert.That(modelStillExists, Is.Null, "Deleted model should not exist anymore");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Model {_createdModelName} deleted successfully");
        _createdModelId = null;
        _createdModelName = null;
    }
}
