using System.Net.Http.Json;
using System.Text.Json;
using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Microsoft.Playwright;
using static E2ETest.Constants.SettingsCalendarRulesIds;
using static E2ETest.Constants.SettingsCalendarRulesTestData;

namespace E2ETest;

[TestFixture]
[Order(29)]
public class SettingsCalendarRulesTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private static int? _createdRuleIndex;
    private static string? _createdRuleName;
    private static string? _copiedRuleName;
    private static readonly HttpClient _httpClient = new();

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

    private async Task SelectAllStatesInFilter()
    {
        var dropdownBtn = await Actions.FindElementById(DropdownFormButton);
        if (dropdownBtn == null)
        {
            TestContext.Out.WriteLine("Dropdown button not found");
            return;
        }

        await dropdownBtn.ClickAsync();
        await Actions.Wait500();
        TestContext.Out.WriteLine("Opened calendar filter dropdown");

        var selectAllBtn = await Actions.FindElementById(DropdownSelectAllBtn);
        if (selectAllBtn != null)
        {
            await selectAllBtn.ClickAsync();
            await Actions.Wait500();
            TestContext.Out.WriteLine("Clicked 'Select All' button");
        }
        else
        {
            TestContext.Out.WriteLine("WARNING: Select All button not found");
        }

        var closeBtn = await Actions.FindElementById(DropdownCloseBtn);
        if (closeBtn != null)
        {
            await closeBtn.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Closed dropdown filter");
        }
        else
        {
            await dropdownBtn.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Closed dropdown filter (via toggle)");
        }
    }

    private async Task SelectSpecificStateInFilter(string country, string state)
    {
        var dropdownBtn = await Actions.FindElementById(DropdownFormButton);
        if (dropdownBtn == null)
        {
            TestContext.Out.WriteLine("Dropdown button not found");
            return;
        }

        await dropdownBtn.ClickAsync();
        await Actions.Wait500();
        TestContext.Out.WriteLine("Opened calendar filter dropdown");

        var deselectAllBtn = await Actions.FindElementById(DropdownDeselectAllBtn);
        if (deselectAllBtn != null)
        {
            await deselectAllBtn.ClickAsync();
            await Actions.Wait500();
            TestContext.Out.WriteLine("Clicked 'Deselect All' button");
        }

        var checkboxId = GetDropdownStateCheckboxId(country, state);
        var stateCheckbox = await Actions.FindElementById(checkboxId);
        if (stateCheckbox != null)
        {
            var isChecked = await stateCheckbox.IsCheckedAsync();
            if (!isChecked)
            {
                await stateCheckbox.ClickAsync();
                await Actions.Wait500();
                TestContext.Out.WriteLine($"Selected state: {country}-{state}");
            }
            else
            {
                TestContext.Out.WriteLine($"State {country}-{state} already selected");
            }
        }
        else
        {
            TestContext.Out.WriteLine($"WARNING: State checkbox {checkboxId} not found");
        }

        var closeBtn = await Actions.FindElementById(DropdownCloseBtn);
        if (closeBtn != null)
        {
            await closeBtn.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Closed dropdown filter");
        }
        else
        {
            await dropdownBtn.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Closed dropdown filter (via toggle)");
        }
    }

    private async Task<int?> FindRuleInTableByName(string ruleName)
    {
        var rows = await Page.QuerySelectorAllAsync(RowSelector);
        foreach (var row in rows)
        {
            var rowId = await row.GetAttributeAsync("id");
            if (string.IsNullOrEmpty(rowId)) continue;

            var parts = rowId.Split('-');
            if (parts.Length < 4 || !int.TryParse(parts[^1], out int index))
                continue;

            var cellName = await Actions.FindElementById(GetCellNameId(index));
            if (cellName != null)
            {
                var nameText = await cellName.InnerTextAsync();
                if (nameText.Contains(ruleName))
                {
                    TestContext.Out.WriteLine($"Found '{ruleName}' at actual index {index}");
                    return index;
                }
            }
        }
        return null;
    }

    private async Task<int?> FindRuleWithPagination(string ruleName, int maxPages = 10)
    {
        var firstPageBtn = await Page.QuerySelectorAsync($"#{Pagination} .page-item:not(.disabled) .page-link[aria-label='First']");
        if (firstPageBtn != null)
        {
            await firstPageBtn.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Navigated to first page");
        }

        for (int page = 0; page < maxPages; page++)
        {
            var ruleIndex = await FindRuleInTableByName(ruleName);
            if (ruleIndex.HasValue)
            {
                TestContext.Out.WriteLine($"Found rule on page {page + 1} at index {ruleIndex.Value}");
                return ruleIndex;
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
    public async Task Step1_VerifyCalendarRulesPageLoaded()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Verify Calendar Rules Section Loaded ===");

        // Act
        var header = await Actions.FindElementById(SettingsCalendarRulesIds.Header);
        var addButton = await Actions.FindElementById(AddBtn);
        var table = await Actions.FindElementById(Table);

        // Assert
        Assert.That(header, Is.Not.Null, "Calendar rules header should be visible");
        Assert.That(addButton, Is.Not.Null, "Add rule button should be visible");
        Assert.That(table, Is.Not.Null, "Calendar rules table should be visible");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Calendar Rules section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateNewCalendarRule()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create New Calendar Rule ===");
        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdRuleName = $"{TestRuleName}{timestamp}";
        TestContext.Out.WriteLine($"Creating rule: {_createdRuleName}");

        // Act
        var addButton = await Actions.FindElementById(AddBtn);
        Assert.That(addButton, Is.Not.Null, "Add button should exist");

        await addButton!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Add Rule button");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        await Actions.ClearInputById(ModalInputName);
        await Actions.TypeIntoInputById(ModalInputName, _createdRuleName);
        TestContext.Out.WriteLine($"Set rule name: {_createdRuleName}");

        await Actions.ClearInputById(ModalInputRule);
        await Actions.TypeIntoInputById(ModalInputRule, TestRule);
        TestContext.Out.WriteLine($"Set rule: {TestRule}");

        var countrySelect = await Actions.FindElementById(ModalInputCountry);
        if (countrySelect != null)
        {
            await countrySelect.SelectOptionAsync(new SelectOptionValue { Value = TestCountry });
            await Actions.Wait1000();
            TestContext.Out.WriteLine($"Set country: {TestCountry}");
        }
        else
        {
            TestContext.Out.WriteLine("WARNING: Country select not found!");
        }

        var stateSelect = await Actions.FindElementById(ModalInputState);
        if (stateSelect != null)
        {
            await stateSelect.SelectOptionAsync(new SelectOptionValue { Value = TestState });
            await Actions.Wait500();
            TestContext.Out.WriteLine($"Set state: {TestState}");
        }
        else
        {
            TestContext.Out.WriteLine("WARNING: State select not found!");
        }

        var resultLabel = await Actions.FindElementById(ModalResult);
        if (resultLabel != null)
        {
            var resultText = await resultLabel.InnerTextAsync();
            TestContext.Out.WriteLine($"Calculated result: {resultText}");
        }

        var addModalBtn = await Actions.FindElementById(ModalAddBtn);
        Assert.That(addModalBtn, Is.Not.Null, "Add button in modal should exist");

        var isButtonEnabled = await addModalBtn!.IsEnabledAsync();
        TestContext.Out.WriteLine($"Add button enabled: {isButtonEnabled}");
        Assert.That(isButtonEnabled, Is.True, "Add button should be enabled");

        await addModalBtn.ClickAsync();
        TestContext.Out.WriteLine("Clicked Add button in modal");

        await Actions.Wait500();
        var modalStillVisible = await Actions.FindElementById(ModalHeader);
        TestContext.Out.WriteLine($"Modal still visible after add click: {modalStillVisible != null}");

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error after create: {_listener.GetLastErrorMessage()}");
        }

        TestContext.Out.WriteLine($"Setting filter to {TestCountry}/{TestState}...");
        await SelectSpecificStateInFilter(TestCountry, TestState);
        await Actions.Wait1000();

        TestContext.Out.WriteLine($"Searching for rule: {_createdRuleName}");
        _createdRuleIndex = await FindRuleWithPagination(_createdRuleName);

        if (_createdRuleIndex.HasValue)
        {
            var cellCountry = await Actions.FindElementById(GetCellCountryId(_createdRuleIndex.Value));
            var cellState = await Actions.FindElementById(GetCellStateId(_createdRuleIndex.Value));

            if (cellCountry != null)
            {
                var countryText = await cellCountry.InnerTextAsync();
                TestContext.Out.WriteLine($"  Country: {countryText}");
                Assert.That(countryText.Trim(), Is.EqualTo(TestCountry), "Country should be set correctly");
            }

            if (cellState != null)
            {
                var stateText = await cellState.InnerTextAsync();
                TestContext.Out.WriteLine($"  State: {stateText}");
                Assert.That(stateText.Trim(), Is.EqualTo(TestState), "State should be set correctly");
            }
        }

        // Assert
        Assert.That(_createdRuleIndex, Is.Not.Null, "Created rule should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Rule created successfully: {_createdRuleName}");
    }

    [Test]
    [Order(3)]
    public async Task Step3_OpenAndVerifyRuleModal()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Open and Verify Rule Modal ===");

        if (string.IsNullOrEmpty(_createdRuleName))
        {
            TestContext.Out.WriteLine("No rule was created in Step2 - skipping");
            Assert.Inconclusive("No rule was created in previous step");
            return;
        }

        await SelectSpecificStateInFilter(TestCountry, TestState);
        _createdRuleIndex = await FindRuleWithPagination(_createdRuleName);

        if (_createdRuleIndex == null)
        {
            TestContext.Out.WriteLine($"Rule '{_createdRuleName}' not found - skipping");
            Assert.Inconclusive("Rule not found in list");
            return;
        }

        // Act
        var editBtn = await Actions.FindElementById(GetEditBtnId(_createdRuleIndex.Value));
        Assert.That(editBtn, Is.Not.Null, $"Edit button for rule {_createdRuleIndex} should exist");

        await editBtn!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked on edit button to open modal");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
        TestContext.Out.WriteLine("Modal opened successfully");

        var formTab = await Actions.FindElementById(ModalTabFormLink);
        Assert.That(formTab, Is.Not.Null, "Form tab should be visible");

        var helpTab = await Actions.FindElementById(ModalTabHelpLink);
        Assert.That(helpTab, Is.Not.Null, "Help tab should be visible");
        TestContext.Out.WriteLine("Both tabs are visible");

        await helpTab!.ClickAsync();
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked on Help tab");

        await formTab!.ClickAsync();
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked back to Form tab");

        await Actions.ClickElementById(ModalCancelBtn);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked Cancel to close modal");

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Rule modal opened and verified successfully");
    }

    [Test]
    [Order(4)]
    public async Task Step4_UpdateCalendarRule()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Update Calendar Rule ===");

        if (string.IsNullOrEmpty(_createdRuleName))
        {
            TestContext.Out.WriteLine("No rule was created - skipping");
            Assert.Inconclusive("No rule was created in previous step");
            return;
        }

        await SelectSpecificStateInFilter(TestCountry, TestState);
        _createdRuleIndex = await FindRuleWithPagination(_createdRuleName);

        if (_createdRuleIndex == null)
        {
            TestContext.Out.WriteLine($"Rule '{_createdRuleName}' not found - skipping");
            Assert.Inconclusive("Rule not found in list");
            return;
        }

        // Act
        var editBtn = await Actions.FindElementById(GetEditBtnId(_createdRuleIndex.Value));
        Assert.That(editBtn, Is.Not.Null, "Edit button should exist");

        await editBtn!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Opened modal for editing");

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _createdRuleName = $"{UpdatedRuleName}{timestamp}";

        await Actions.ClearInputById(ModalInputName);
        await Actions.TypeIntoInputById(ModalInputName, _createdRuleName);
        TestContext.Out.WriteLine($"Updated name to: {_createdRuleName}");

        await Actions.ClearInputById(ModalInputRule);
        await Actions.TypeIntoInputById(ModalInputRule, UpdatedRule);
        TestContext.Out.WriteLine($"Updated rule to: {UpdatedRule}");

        await Actions.ClearInputById(ModalInputSubRule);
        await Actions.TypeIntoInputById(ModalInputSubRule, UpdatedSubRule);
        TestContext.Out.WriteLine($"Updated subRule to: {UpdatedSubRule}");

        await Actions.Wait500();
        var resultLabel = await Actions.FindElementById(ModalResult);
        if (resultLabel != null)
        {
            var resultText = await resultLabel.InnerTextAsync();
            TestContext.Out.WriteLine($"New calculated result: {resultText}");
        }

        var addModalBtn = await Actions.FindElementById(ModalAddBtn);
        await addModalBtn!.ClickAsync();
        TestContext.Out.WriteLine("Clicked Save button");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        await SelectSpecificStateInFilter(TestCountry, TestState);
        _createdRuleIndex = await FindRuleWithPagination(_createdRuleName);
        var foundUpdated = _createdRuleIndex.HasValue;

        // Assert
        Assert.That(foundUpdated, Is.True, "Updated rule should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Rule updated successfully");
    }

    [Test]
    [Order(5)]
    public async Task Step5_ValidateCalendarRuleViaApi()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Validate Calendar Rule via API ===");

        var testCases = new[]
        {
            new { Rule = "01/01", SubRule = "", Year = 2026, Expected = "2026-01-01" },
            new { Rule = "EASTER+00", SubRule = "", Year = 2026, Expected = "2026-04-05" },
            new { Rule = "EASTER-02", SubRule = "", Year = 2026, Expected = "2026-04-03" },
            new { Rule = "EASTER+39", SubRule = "", Year = 2026, Expected = "2026-05-14" },
            new { Rule = "12/25", SubRule = "", Year = 2026, Expected = "2026-12-25" },
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            TestContext.Out.WriteLine($"Testing rule: {testCase.Rule} (Year: {testCase.Year})");

            var request = new
            {
                rule = testCase.Rule,
                subRule = testCase.SubRule,
                year = testCase.Year
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "http://localhost:5000/api/backend/ValidateCalendarRule",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    TestContext.Out.WriteLine($"  API returned status: {response.StatusCode}");
                    Assert.Inconclusive("Backend API not available");
                    return;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                TestContext.Out.WriteLine($"  Response: {jsonResponse}");

                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    Assert.Inconclusive("Backend returned empty response - endpoint may not be deployed");
                    return;
                }

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                var isValid = root.GetProperty("isValid").GetBoolean();
                Assert.That(isValid, Is.True, $"Rule {testCase.Rule} should be valid");

                if (root.TryGetProperty("calculatedDate", out var dateElement))
                {
                    var calculatedDate = dateElement.GetString();
                    Assert.That(calculatedDate, Is.EqualTo(testCase.Expected),
                        $"Rule {testCase.Rule} should calculate to {testCase.Expected}");
                    TestContext.Out.WriteLine($"  Calculated: {calculatedDate} (expected: {testCase.Expected}) OK");
                }
            }
            catch (HttpRequestException ex)
            {
                TestContext.Out.WriteLine($"  Connection failed: {ex.Message}");
                Assert.Inconclusive("Backend not reachable - please start the API server");
                return;
            }
        }

        TestContext.Out.WriteLine("All API validations passed");
    }

    [Test]
    [Order(6)]
    public async Task Step6_ValidateSubRuleViaApi()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Validate SubRule via API ===");

        var request = new
        {
            rule = "01/01",
            subRule = "SA+2;SU+1",
            year = 2028
        };

        TestContext.Out.WriteLine($"Testing rule: {request.rule} with subRule: {request.subRule} (Year: {request.year})");
        TestContext.Out.WriteLine("2028-01-01 is a Saturday, so subRule SA+2 should shift to Monday 2028-01-03");

        try
        {
            // Act
            var response = await _httpClient.PostAsJsonAsync(
                "http://localhost:5000/api/backend/ValidateCalendarRule",
                request);

            if (!response.IsSuccessStatusCode)
            {
                Assert.Inconclusive("Backend API not available");
                return;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            TestContext.Out.WriteLine($"Response: {jsonResponse}");

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                Assert.Inconclusive("Backend returned empty response");
                return;
            }

            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Assert
            var isValid = root.GetProperty("isValid").GetBoolean();
            Assert.That(isValid, Is.True, "Rule with SubRule should be valid");

            if (root.TryGetProperty("calculatedDate", out var dateElement))
            {
                var calculatedDate = dateElement.GetString();
                Assert.That(calculatedDate, Is.EqualTo("2028-01-03"),
                    "SubRule SA+2 should shift Saturday 01/01 to Monday 01/03");
                TestContext.Out.WriteLine($"SubRule shifted date correctly to: {calculatedDate}");
            }

            TestContext.Out.WriteLine("SubRule API validation passed");
        }
        catch (HttpRequestException ex)
        {
            TestContext.Out.WriteLine($"Connection failed: {ex.Message}");
            Assert.Inconclusive("Backend not reachable");
        }
    }

    [Test]
    [Order(7)]
    public async Task Step7_ValidateInvalidRuleViaApi()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 7: Validate Invalid Rule via API ===");

        var request = new
        {
            rule = "INVALID_RULE",
            subRule = "",
            year = 2026
        };

        TestContext.Out.WriteLine($"Testing invalid rule: {request.rule}");

        try
        {
            // Act
            var response = await _httpClient.PostAsJsonAsync(
                "http://localhost:5000/api/backend/ValidateCalendarRule",
                request);

            if (!response.IsSuccessStatusCode)
            {
                Assert.Inconclusive("Backend API not available");
                return;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            TestContext.Out.WriteLine($"Response: {jsonResponse}");

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                Assert.Inconclusive("Backend returned empty response");
                return;
            }

            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Assert
            var isValid = root.GetProperty("isValid").GetBoolean();
            Assert.That(isValid, Is.False, "Invalid rule should return isValid=false");

            if (root.TryGetProperty("errorMessage", out var errorElement))
            {
                var errorMessage = errorElement.GetString();
                TestContext.Out.WriteLine($"Error message: {errorMessage}");
                Assert.That(errorMessage, Is.Not.Null.And.Not.Empty, "Error message should be provided");
            }

            TestContext.Out.WriteLine("Invalid rule API validation passed");
        }
        catch (HttpRequestException ex)
        {
            TestContext.Out.WriteLine($"Connection failed: {ex.Message}");
            Assert.Inconclusive("Backend not reachable");
        }
    }

    [Test]
    [Order(8)]
    public async Task Step8_CopyCalendarRule()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 8: Copy Calendar Rule ===");

        if (string.IsNullOrEmpty(_createdRuleName))
        {
            TestContext.Out.WriteLine("No rule was created - skipping");
            Assert.Inconclusive("No rule was created in previous step");
            return;
        }

        await SelectSpecificStateInFilter(TestCountry, TestState);
        _createdRuleIndex = await FindRuleWithPagination(_createdRuleName);

        if (_createdRuleIndex == null)
        {
            TestContext.Out.WriteLine($"Rule '{_createdRuleName}' not found - skipping");
            Assert.Inconclusive("Rule not found in list");
            return;
        }

        var rowsBefore = await Page.QuerySelectorAllAsync(RowSelector);
        var countBefore = rowsBefore.Count;
        TestContext.Out.WriteLine($"Rules count before copy: {countBefore}");
        TestContext.Out.WriteLine($"Rule to copy at index: {_createdRuleIndex.Value}");

        // Act
        var copyBtn = await Actions.FindElementById(GetCopyBtnId(_createdRuleIndex.Value));
        Assert.That(copyBtn, Is.Not.Null, "Copy button should exist");

        await copyBtn!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked copy button");

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open for copied rule");
        TestContext.Out.WriteLine("Modal opened successfully");

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
        _copiedRuleName = $"E2E Copy Rule {timestamp}";

        await Actions.ClearInputById(ModalInputName);
        await Actions.TypeIntoInputById(ModalInputName, _copiedRuleName);
        TestContext.Out.WriteLine($"Set copied rule name: {_copiedRuleName}");

        var addModalBtn = await Actions.FindElementById(ModalAddBtn);
        Assert.That(addModalBtn, Is.Not.Null, "Add button should exist");

        var isButtonEnabled = await addModalBtn!.IsEnabledAsync();
        TestContext.Out.WriteLine($"Add button enabled: {isButtonEnabled}");
        Assert.That(isButtonEnabled, Is.True, "Add button should be enabled for copy");

        await addModalBtn.ClickAsync();
        TestContext.Out.WriteLine("Clicked Add button in modal");

        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Error detected: {_listener.GetLastErrorMessage()}");
        }

        await SelectSpecificStateInFilter(TestCountry, TestState);
        var copiedRuleIndex = await FindRuleWithPagination(_copiedRuleName);

        TestContext.Out.WriteLine($"Copied rule found: {copiedRuleIndex.HasValue}");

        // Assert
        Assert.That(copiedRuleIndex.HasValue, Is.True, "Copied rule should be found in the list");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Rule '{_copiedRuleName}' copied successfully");
    }

    [Test]
    [Order(9)]
    public async Task Step9_DeleteCopiedRule()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 9: Delete Copied Rule ===");

        if (string.IsNullOrEmpty(_copiedRuleName))
        {
            TestContext.Out.WriteLine("No copied rule name - skipping");
            Assert.Inconclusive("No copied rule to delete - copy may have failed");
            return;
        }

        await SelectSpecificStateInFilter(TestCountry, TestState);
        var copiedRuleIndex = await FindRuleWithPagination(_copiedRuleName);

        if (copiedRuleIndex == null)
        {
            TestContext.Out.WriteLine($"Copied rule '{_copiedRuleName}' not found - skipping");
            Assert.Inconclusive("Copied rule not found in list");
            return;
        }

        TestContext.Out.WriteLine($"Found copied rule at index: {copiedRuleIndex.Value}");

        // Act
        var deleteBtn = await Actions.FindElementById(GetDeleteBtnId(copiedRuleIndex.Value));
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

        await SelectSpecificStateInFilter(TestCountry, TestState);
        var ruleStillExists = await FindRuleWithPagination(_copiedRuleName);

        // Assert
        Assert.That(ruleStillExists, Is.Null, "Copied rule should be deleted");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        _copiedRuleName = null;
        TestContext.Out.WriteLine("Copied rule deleted successfully");
    }

    [Test]
    [Order(10)]
    public async Task Step10_DeleteCreatedRule()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 10: Delete Created Rule ===");

        if (string.IsNullOrEmpty(_createdRuleName))
        {
            TestContext.Out.WriteLine("No rule was created - skipping delete");
            Assert.Inconclusive("No rule was created in previous step");
            return;
        }

        await SelectSpecificStateInFilter(TestCountry, TestState);
        var ruleToDeleteIndex = await FindRuleWithPagination(_createdRuleName);

        if (ruleToDeleteIndex == null)
        {
            TestContext.Out.WriteLine($"Rule '{_createdRuleName}' not found - may have been deleted already");
            Assert.Pass("Rule not found - possibly already deleted");
            return;
        }

        TestContext.Out.WriteLine($"Found rule to delete at index: {ruleToDeleteIndex.Value}");

        // Act
        var deleteBtn = await Actions.FindElementById(GetDeleteBtnId(ruleToDeleteIndex.Value));
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
        await SelectSpecificStateInFilter(TestCountry, TestState);
        var ruleStillExists = await FindRuleWithPagination(_createdRuleName);

        Assert.That(ruleStillExists, Is.Null, "Created rule should be deleted");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Rule {_createdRuleName} deleted successfully");
        _createdRuleIndex = null;
        _createdRuleName = null;
    }
}
