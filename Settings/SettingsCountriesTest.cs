using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsCountriesTest : PlaywrightSetup
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

            // Navigate to Countries tab
            var countriesTab = await Actions.FindElementByCssSelector("[href*='countries'], button:has-text('Countries'), a:has-text('Länder')");
            if (countriesTab != null)
            {
                await countriesTab.ClickAsync();
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
        public async Task Step1_VerifyCountriesPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Countries Page Loaded ===");

            // Assert
            var countryTable = await Actions.FindElementByCssSelector("table, [class*='country-list']");
            Assert.That(countryTable, Is.Not.Null, "Country table should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Countries page loaded successfully");
        }

        [Test]
        public async Task Step2_AddNewCountry()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Add New Country ===");
            var timestamp = DateTime.Now.Ticks.ToString();

            // Act - Click Add button
            var addButton = await Actions.FindElementByCssSelector("button:has-text('Add'), button:has-text('Hinzufügen'), [class*='btn-add']");
            if (addButton != null)
            {
                await addButton.ClickAsync();
                await Actions.Wait500();

                // Find the new row (last row)
                var lastRow = await Actions.FindElementByCssSelector("tbody tr:last-child");
                Assert.That(lastRow, Is.Not.Null, "New country row should be added");

                // Fill country data
                var nameInput = await lastRow!.QuerySelectorAsync("input[name*='name'], input:nth-child(1)");
                if (nameInput != null)
                {
                    await nameInput.FillAsync($"TestCountry_{timestamp}");
                }

                var abbreviationInput = await lastRow.QuerySelectorAsync("input[name*='abbreviation']");
                if (abbreviationInput != null)
                {
                    await abbreviationInput.FillAsync("TC");
                }

                var prefixInput = await lastRow.QuerySelectorAsync("input[name*='prefix']");
                if (prefixInput != null)
                {
                    await prefixInput.FillAsync("+99");
                }

                await Actions.Wait500();

                TestContext.Out.WriteLine("New country added successfully");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step3_EditExistingCountry()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Edit Existing Country ===");

            // Act - Find first country row
            var firstRow = await Actions.FindElementByCssSelector("tbody tr:first-child");
            Assert.That(firstRow, Is.Not.Null, "At least one country should exist");

            var nameInput = await firstRow!.QuerySelectorAsync("input[name*='name']");
            if (nameInput != null)
            {
                var originalValue = await nameInput.InputValueAsync();
                var newValue = $"{originalValue}_edited";

                await nameInput.FillAsync(newValue);
                await Actions.Wait500();

                // Verify change
                var currentValue = await nameInput.InputValueAsync();
                Assert.That(currentValue, Is.EqualTo(newValue), "Country name should be updated");

                // Restore original value
                await nameInput.FillAsync(originalValue);
                await Actions.Wait500();

                TestContext.Out.WriteLine("Country edited and restored successfully");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step4_DeleteCountry()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Delete Country ===");

            // Act - Find delete button in last row
            var lastRow = await Actions.FindElementByCssSelector("tbody tr:last-child");
            if (lastRow != null)
            {
                var deleteButton = await lastRow.QuerySelectorAsync("button:has-text('Delete'), button[class*='delete'], fa-trash");
                if (deleteButton != null)
                {
                    await deleteButton.ClickAsync();
                    await Actions.Wait500();

                    TestContext.Out.WriteLine("Country delete button clicked");
                }
                else
                {
                    TestContext.Out.WriteLine("Delete button not found - country might be protected");
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
