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

            // Scroll container into viewport
            var container = await Page.QuerySelectorAsync(".container-box");
            if (container != null)
            {
                await container.ScrollIntoViewIfNeededAsync();
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
            var addButton = await Actions.FindElementById("state-add-btn");
            Assert.That(addButton, Is.Not.Null, "Add country button should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("State page loaded successfully");
        }

        [Test]
        public async Task Step2_AddNewCountry()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Add New Country ===");
            var timestamp = DateTime.Now.Ticks.ToString();

            // Act - Click Add button
            var addButton = await Actions.FindElementById("state-add-btn");
            if (addButton != null)
            {
                await addButton.ClickAsync();
                await Actions.Wait500();

                TestContext.Out.WriteLine("Add button clicked - new country row should be added");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step3_EditExistingCountry()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Edit Existing Country ===");

            // Act - Find first country row inputs by looking for first input with state-row pattern
            var firstAbbreviation = await Actions.FindElementByCssSelector("input[id^='state-row-abbreviation-']");
            if (firstAbbreviation != null)
            {
                var originalValue = await firstAbbreviation.InputValueAsync();
                var newValue = $"{originalValue}_edit";

                await firstAbbreviation.FillAsync(newValue);
                await Actions.Wait500();

                // Verify change
                var currentValue = await firstAbbreviation.InputValueAsync();
                Assert.That(currentValue, Is.EqualTo(newValue), "Country abbreviation should be updated");

                // Restore original value
                await firstAbbreviation.FillAsync(originalValue);
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

            // Act - Find delete button
            var deleteButton = await Actions.FindElementByCssSelector("span[id^='state-row-delete-']");
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

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
