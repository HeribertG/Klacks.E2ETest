using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.SettingsAbsenceIds;
using static E2ETest.Constants.SettingsAbsenceTestData;

namespace E2ETest;

[TestFixture]
[Order(29)]
public class SettingsAbsenceTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private static int? _createdAbsenceIndex;
    private static string? _createdAbsenceName;
    private static string? _copiedAbsenceName;

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

    private async Task<int?> FindAbsenceInTableByName(string absenceName)
    {
        var rows = await Page.QuerySelectorAllAsync(RowSelector);
        foreach (var row in rows)
        {
            var rowId = await row.GetAttributeAsync("id");
            if (string.IsNullOrEmpty(rowId)) continue;

            var parts = rowId.Split('-');
            if (parts.Length < 3 || !int.TryParse(parts[^1], out int index))
                continue;

            var cellName = await Actions.FindElementById(GetCellNameId(index));
            if (cellName != null)
            {
                var nameText = await cellName.InnerTextAsync();
                if (nameText.Contains(absenceName))
                {
                    TestContext.Out.WriteLine($"Found '{absenceName}' at index {index}");
                    return index;
                }
            }
        }
        return null;
    }

    private async Task<int?> FindAbsenceWithPagination(string absenceName, int maxPages = 10)
    {
        var paginationElement = await Actions.FindElementById(Pagination);
        if (paginationElement != null)
        {
            var firstPageBtn = await Page.QuerySelectorAsync($"#{Pagination} .page-item:not(.disabled) .page-link[aria-label='First']");
            if (firstPageBtn != null)
            {
                await firstPageBtn.ClickAsync();
                await Actions.Wait1000();
                TestContext.Out.WriteLine("Navigated to first page");
            }
        }

        for (int page = 0; page < maxPages; page++)
        {
            var absenceIndex = await FindAbsenceInTableByName(absenceName);
            if (absenceIndex.HasValue)
            {
                TestContext.Out.WriteLine($"Found absence on page {page + 1} at index {absenceIndex.Value}");
                return absenceIndex;
            }

            var nextPageBtn = await Page.QuerySelectorAsync($"#{Pagination} .page-item:not(.disabled) .page-link[aria-label='Next']");
            if (nextPageBtn == null)
            {
                TestContext.Out.WriteLine($"No more pages, searched {page + 1} pages");
                break;
            }

            await nextPageBtn.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine($"Navigated to page {page + 2}");
        }

        return null;
    }

    [Test]
    [Order(1)]
    public async Task Step1_VerifyAbsencePageLoaded()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Verify Absence Section Loaded ===");

        // Act
        var header = await Actions.FindElementById(Header);
        var addButton = await Actions.FindElementById(AddBtn);
        var table = await Actions.FindElementById(Table);

        // Assert
        Assert.That(header, Is.Not.Null, "Absence header should be visible");
        Assert.That(addButton, Is.Not.Null, "Add absence button should be visible");
        Assert.That(table, Is.Not.Null, "Absence table should be visible");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Absence section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateNewAbsence()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create New Absence ===");
        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdAbsenceName = $"{TestAbsenceName} {timestamp}";
        TestContext.Out.WriteLine($"Creating absence: {_createdAbsenceName}");

        // Act
        var addButton = await Actions.FindElementById(AddBtn);
        Assert.That(addButton, Is.Not.Null, "Add button should exist");

        await addButton!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Add Absence button");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        await Actions.ClearInputById(ModalInputName);
        await Actions.TypeIntoInputById(ModalInputName, _createdAbsenceName);
        TestContext.Out.WriteLine($"Set absence name: {_createdAbsenceName}");

        await Actions.ClearInputById(ModalInputDescription);
        await Actions.TypeIntoInputById(ModalInputDescription, TestDescription);
        TestContext.Out.WriteLine($"Set description: {TestDescription}");

        await Actions.ClearInputById(ModalInputDefaultLength);
        await Actions.TypeIntoInputById(ModalInputDefaultLength, TestDefaultLength.ToString());
        TestContext.Out.WriteLine($"Set default length: {TestDefaultLength}");

        await Actions.ClearInputById(ModalInputDefaultValue);
        await Actions.TypeIntoInputById(ModalInputDefaultValue, TestDefaultValue.ToString());
        TestContext.Out.WriteLine($"Set default value: {TestDefaultValue}");

        var colorInput = await Actions.FindElementById(ModalInputColorText);
        if (colorInput != null)
        {
            await colorInput.FillAsync(TestColor);
            TestContext.Out.WriteLine($"Set color: {TestColor}");
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

        TestContext.Out.WriteLine($"Searching for absence: {_createdAbsenceName}");
        _createdAbsenceIndex = await FindAbsenceWithPagination(_createdAbsenceName);

        // Assert
        Assert.That(_createdAbsenceIndex, Is.Not.Null, "Created absence should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Absence created successfully: {_createdAbsenceName}");
    }

    [Test]
    [Order(3)]
    public async Task Step3_OpenAndVerifyAbsenceModal()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Open and Verify Absence Modal ===");

        if (string.IsNullOrEmpty(_createdAbsenceName))
        {
            TestContext.Out.WriteLine("No absence was created in Step2 - skipping");
            Assert.Inconclusive("No absence was created in previous step");
            return;
        }

        _createdAbsenceIndex = await FindAbsenceWithPagination(_createdAbsenceName);

        if (_createdAbsenceIndex == null)
        {
            TestContext.Out.WriteLine($"Absence '{_createdAbsenceName}' not found - skipping");
            Assert.Inconclusive("Absence not found in list");
            return;
        }

        // Act
        var editBtn = await Actions.FindElementById(GetEditBtnId(_createdAbsenceIndex.Value));
        Assert.That(editBtn, Is.Not.Null, $"Edit button for absence {_createdAbsenceIndex} should exist");

        await editBtn!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked on edit button to open modal");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        var nameInput = await Actions.FindElementById(ModalInputName);
        Assert.That(nameInput, Is.Not.Null, "Name input should be visible");

        var descInput = await Actions.FindElementById(ModalInputDescription);
        Assert.That(descInput, Is.Not.Null, "Description input should be visible");

        var lengthInput = await Actions.FindElementById(ModalInputDefaultLength);
        Assert.That(lengthInput, Is.Not.Null, "Default length input should be visible");

        var valueInput = await Actions.FindElementById(ModalInputDefaultValue);
        Assert.That(valueInput, Is.Not.Null, "Default value input should be visible");

        var saturdayCheckbox = await Actions.FindElementById(ModalCheckboxSaturday);
        Assert.That(saturdayCheckbox, Is.Not.Null, "Saturday checkbox should be visible");

        var sundayCheckbox = await Actions.FindElementById(ModalCheckboxSunday);
        Assert.That(sundayCheckbox, Is.Not.Null, "Sunday checkbox should be visible");

        var holidayCheckbox = await Actions.FindElementById(ModalCheckboxHoliday);
        Assert.That(holidayCheckbox, Is.Not.Null, "Holiday checkbox should be visible");

        TestContext.Out.WriteLine("All form fields are visible");

        await Actions.ClickElementById(ModalCancelBtn);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked Cancel to close modal");

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Absence modal opened and verified successfully");
    }

    [Test]
    [Order(4)]
    public async Task Step4_UpdateAbsence()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Update Absence ===");

        if (string.IsNullOrEmpty(_createdAbsenceName))
        {
            TestContext.Out.WriteLine("No absence was created - skipping");
            Assert.Inconclusive("No absence was created in previous step");
            return;
        }

        _createdAbsenceIndex = await FindAbsenceWithPagination(_createdAbsenceName);

        if (_createdAbsenceIndex == null)
        {
            TestContext.Out.WriteLine($"Absence '{_createdAbsenceName}' not found - skipping");
            Assert.Inconclusive("Absence not found in list");
            return;
        }

        // Act
        var editBtn = await Actions.FindElementById(GetEditBtnId(_createdAbsenceIndex.Value));
        Assert.That(editBtn, Is.Not.Null, "Edit button should exist");

        await editBtn!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Opened modal for editing");

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdAbsenceName = $"{UpdatedAbsenceName} {timestamp}";

        await Actions.ClearInputById(ModalInputName);
        await Actions.TypeIntoInputById(ModalInputName, _createdAbsenceName);
        TestContext.Out.WriteLine($"Updated name to: {_createdAbsenceName}");

        await Actions.ClearInputById(ModalInputDescription);
        await Actions.TypeIntoInputById(ModalInputDescription, UpdatedDescription);
        TestContext.Out.WriteLine($"Updated description to: {UpdatedDescription}");

        await Actions.ClearInputById(ModalInputDefaultLength);
        await Actions.TypeIntoInputById(ModalInputDefaultLength, UpdatedDefaultLength.ToString());
        TestContext.Out.WriteLine($"Updated default length to: {UpdatedDefaultLength}");

        await Actions.ClearInputById(ModalInputDefaultValue);
        await Actions.TypeIntoInputById(ModalInputDefaultValue, UpdatedDefaultValue.ToString());
        TestContext.Out.WriteLine($"Updated default value to: {UpdatedDefaultValue}");

        var colorInput = await Actions.FindElementById(ModalInputColorText);
        if (colorInput != null)
        {
            await colorInput.FillAsync(UpdatedColor);
            TestContext.Out.WriteLine($"Updated color to: {UpdatedColor}");
        }

        var saveBtn = await Actions.FindElementById(ModalSaveBtn);
        await saveBtn!.ClickAsync();
        TestContext.Out.WriteLine("Clicked Save button");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        _createdAbsenceIndex = await FindAbsenceWithPagination(_createdAbsenceName);
        var foundUpdated = _createdAbsenceIndex.HasValue;

        // Assert
        Assert.That(foundUpdated, Is.True, "Updated absence should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Absence updated successfully");
    }

    [Test]
    [Order(5)]
    public async Task Step5_CopyAbsence()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Copy Absence ===");

        if (string.IsNullOrEmpty(_createdAbsenceName))
        {
            TestContext.Out.WriteLine("No absence was created - skipping");
            Assert.Inconclusive("No absence was created in previous step");
            return;
        }

        _createdAbsenceIndex = await FindAbsenceWithPagination(_createdAbsenceName);

        if (_createdAbsenceIndex == null)
        {
            TestContext.Out.WriteLine($"Absence '{_createdAbsenceName}' not found - skipping");
            Assert.Inconclusive("Absence not found in list");
            return;
        }

        TestContext.Out.WriteLine($"Absence to copy at index: {_createdAbsenceIndex.Value}");

        // Act
        var copyBtn = await Actions.FindElementById(GetCopyBtnId(_createdAbsenceIndex.Value));
        Assert.That(copyBtn, Is.Not.Null, "Copy button should exist");

        await copyBtn!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked copy button");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open for copied absence");
        TestContext.Out.WriteLine("Modal opened successfully");

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _copiedAbsenceName = $"E2E Copy Absence {timestamp}";

        await Actions.ClearInputById(ModalInputName);
        await Actions.TypeIntoInputById(ModalInputName, _copiedAbsenceName);
        TestContext.Out.WriteLine($"Set copied absence name: {_copiedAbsenceName}");

        var saveBtn = await Actions.FindElementById(ModalSaveBtn);
        Assert.That(saveBtn, Is.Not.Null, "Save button should exist");

        await saveBtn!.ClickAsync();
        TestContext.Out.WriteLine("Clicked Save button");

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error detected: {_listener.GetLastErrorMessage()}");
        }

        var copiedAbsenceIndex = await FindAbsenceWithPagination(_copiedAbsenceName);

        TestContext.Out.WriteLine($"Copied absence found: {copiedAbsenceIndex.HasValue}");

        // Assert
        Assert.That(copiedAbsenceIndex.HasValue, Is.True, "Copied absence should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Absence '{_copiedAbsenceName}' copied successfully");
    }

    [Test]
    [Order(6)]
    public async Task Step6_DeleteCopiedAbsence()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Delete Copied Absence ===");

        if (string.IsNullOrEmpty(_copiedAbsenceName))
        {
            TestContext.Out.WriteLine("No copied absence name - skipping");
            Assert.Inconclusive("No copied absence to delete - copy may have failed");
            return;
        }

        var copiedAbsenceIndex = await FindAbsenceWithPagination(_copiedAbsenceName);

        if (copiedAbsenceIndex == null)
        {
            TestContext.Out.WriteLine($"Copied absence '{_copiedAbsenceName}' not found - skipping");
            Assert.Inconclusive("Copied absence not found in list");
            return;
        }

        TestContext.Out.WriteLine($"Found copied absence at index: {copiedAbsenceIndex.Value}");

        // Act
        var deleteBtn = await Actions.FindElementById(GetDeleteBtnId(copiedAbsenceIndex.Value));
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

        await confirmBtn.ClickAsync();
        TestContext.Out.WriteLine("Confirmed deletion");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var absenceStillExists = await FindAbsenceWithPagination(_copiedAbsenceName);

        // Assert
        Assert.That(absenceStillExists, Is.Null, "Copied absence should be deleted");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        _copiedAbsenceName = null;
        TestContext.Out.WriteLine("Copied absence deleted successfully");
    }

    [Test]
    [Order(7)]
    public async Task Step7_DeleteCreatedAbsence()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 7: Delete Created Absence ===");

        if (string.IsNullOrEmpty(_createdAbsenceName))
        {
            TestContext.Out.WriteLine("No absence was created - skipping delete");
            Assert.Inconclusive("No absence was created in previous step");
            return;
        }

        var absenceToDeleteIndex = await FindAbsenceWithPagination(_createdAbsenceName);

        if (absenceToDeleteIndex == null)
        {
            TestContext.Out.WriteLine($"Absence '{_createdAbsenceName}' not found - may have been deleted already");
            Assert.Pass("Absence not found - possibly already deleted");
            return;
        }

        TestContext.Out.WriteLine($"Found absence to delete at index: {absenceToDeleteIndex.Value}");

        // Act
        var deleteBtn = await Actions.FindElementById(GetDeleteBtnId(absenceToDeleteIndex.Value));
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

        // Assert
        var absenceStillExists = await FindAbsenceWithPagination(_createdAbsenceName);

        Assert.That(absenceStillExists, Is.Null, "Created absence should be deleted");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Absence {_createdAbsenceName} deleted successfully");
        _createdAbsenceIndex = null;
        _createdAbsenceName = null;
    }
}
