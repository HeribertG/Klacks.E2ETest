using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsStateTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            // Navigate to Settings
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Navigate to State tab
            var stateTab = await Actions.FindElementByCssSelector("[href*='state'], button:has-text('State'), a:has-text('Bundesland')");
            if (stateTab != null)
            {
                await stateTab.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait500();
            }
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

        [Test]
        public async Task Step1_VerifyStatePageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify State Page Loaded ===");

            // Assert
            var stateTable = await Actions.FindElementByCssSelector("table, [class*='state-list']");
            Assert.That(stateTable, Is.Not.Null, "State table should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("State page loaded successfully");
        }

        [Test]
        public async Task Step2_AddNewState()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Add New State ===");
            var timestamp = DateTime.Now.Ticks.ToString();

            // Act - Click Add button
            var addButton = await Actions.FindElementByCssSelector("button:has-text('Add'), button:has-text('Hinzufügen'), [class*='btn-add']");
            if (addButton != null)
            {
                await addButton.ClickAsync();
                await Actions.Wait500();

                // Find the new row (last row)
                var lastRow = await Actions.FindElementByCssSelector("tbody tr:last-child");
                Assert.That(lastRow, Is.Not.Null, "New state row should be added");

                // Fill state data
                var nameInput = await lastRow!.QuerySelectorAsync("input[name*='name'], input:nth-child(1)");
                if (nameInput != null)
                {
                    await nameInput.FillAsync($"TestState_{timestamp}");
                }

                var abbreviationInput = await lastRow.QuerySelectorAsync("input[name*='abbreviation']");
                if (abbreviationInput != null)
                {
                    await abbreviationInput.FillAsync("TS");
                }

                // Select country from dropdown
                var countryDropdown = await lastRow.QuerySelectorAsync("select, [class*='country-select']");
                if (countryDropdown != null)
                {
                    await countryDropdown.SelectOptionAsync(new[] { "1" });
                }

                await Actions.Wait500();

                TestContext.Out.WriteLine("New state added successfully");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step3_EditExistingState()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Edit Existing State ===");

            // Act - Find first state row
            var firstRow = await Actions.FindElementByCssSelector("tbody tr:first-child");
            Assert.That(firstRow, Is.Not.Null, "At least one state should exist");

            var nameInput = await firstRow!.QuerySelectorAsync("input[name*='name']");
            if (nameInput != null)
            {
                var originalValue = await nameInput.InputValueAsync();
                var newValue = $"{originalValue}_edited";

                await nameInput.FillAsync(newValue);
                await Actions.Wait500();

                // Verify change
                var currentValue = await nameInput.InputValueAsync();
                Assert.That(currentValue, Is.EqualTo(newValue), "State name should be updated");

                // Restore original value
                await nameInput.FillAsync(originalValue);
                await Actions.Wait500();

                TestContext.Out.WriteLine("State edited and restored successfully");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step4_ChangeStateCountry()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Change State Country ===");

            // Act - Find first state row
            var firstRow = await Actions.FindElementByCssSelector("tbody tr:first-child");
            if (firstRow != null)
            {
                var countryDropdown = await firstRow.QuerySelectorAsync("select, [class*='country-select']");
                if (countryDropdown != null)
                {
                    var originalValue = await countryDropdown.InputValueAsync();
                    TestContext.Out.WriteLine($"Original country ID: {originalValue}");

                    // Get all options
                    var options = await countryDropdown.QuerySelectorAllAsync("option");
                    if (options.Count > 1)
                    {
                        // Select different country
                        var newOption = options[1];
                        var newValue = await newOption.GetAttributeAsync("value");

                        await countryDropdown.SelectOptionAsync(new[] { newValue! });
                        await Actions.Wait500();

                        // Restore original
                        await countryDropdown.SelectOptionAsync(new[] { originalValue });
                        await Actions.Wait500();

                        TestContext.Out.WriteLine("Country changed and restored successfully");
                    }
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step5_DeleteState()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Delete State ===");

            // Act - Find delete button in last row
            var lastRow = await Actions.FindElementByCssSelector("tbody tr:last-child");
            if (lastRow != null)
            {
                var deleteButton = await lastRow.QuerySelectorAsync("button:has-text('Delete'), button[class*='delete'], fa-trash");
                if (deleteButton != null)
                {
                    await deleteButton.ClickAsync();
                    await Actions.Wait500();

                    TestContext.Out.WriteLine("State delete button clicked");
                }
                else
                {
                    TestContext.Out.WriteLine("Delete button not found - state might be protected");
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
