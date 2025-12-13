using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.SettingsCountriesIds;

namespace E2ETest
{
    [TestFixture]
    [Order(26)]
    public class SettingsCountriesTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(CountriesSection);
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

        [Test]
        [Order(1)]
        public async Task Step1_VerifyCountriesPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Countries Page Loaded ===");

            // Assert
            var addButton = await Actions.FindElementById(AddButton);
            Assert.That(addButton, Is.Not.Null, "Add country button should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Countries page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_AddCountryRow()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Add Country Row ===");

            // Act
            await Actions.ClickButtonById(AddButton);
            await Actions.Wait500();

            // Assert
            var newRowAbbreviation = await Actions.FindElementById(NewRowAbbreviation);
            Assert.That(newRowAbbreviation, Is.Not.Null, "New country row should be added");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("New empty country row added");
        }

        [Test]
        [Order(3)]
        public async Task Step3_FillCountryAndWaitForAutoSave()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Fill Country and Wait for AutoSave ===");

            // Act
            await Actions.FillInputById(NewRowAbbreviation, TestAbbreviation);
            await Actions.Wait500();

            await Actions.FillInputById(NewRowNameDe, TestName);
            await Actions.Wait500();

            await Actions.FillInputById(NewRowPrefix, TestPrefix);
            await Actions.Wait500();

            // Click outside to trigger blur/change event
            await Actions.ClickElementById(CountriesHeader);

            TestContext.Out.WriteLine("Waiting for autoSave (3000ms)...");
            await Actions.Wait3000();

            // Assert - after autoSave, the row should have a real ID (not empty)
            var savedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefix, TestAbbreviation);

            Assert.That(savedRowId, Is.Not.Null, $"Country row with abbreviation '{TestAbbreviation}' should exist");
            TestContext.Out.WriteLine($"Country row found with ID: {savedRowId}");

            // Verify the ID is a real GUID (not empty)
            var countryId = savedRowId!.Replace(RowAbbreviationPrefix, "");
            Assert.That(countryId, Is.Not.Empty.And.Not.EqualTo("undefined"),
                "Country should have a real ID after autoSave");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Country saved to database with ID: {countryId}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_DeleteCountryFromDatabase()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Delete Country from Database ===");

            // Act - find the saved row via Actions helper (ngModel doesn't set value attribute)
            TestContext.Out.WriteLine("Searching for saved country row...");
            var savedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefix, TestAbbreviation);
            Assert.That(savedRowId, Is.Not.Null, $"Saved country row with abbreviation '{TestAbbreviation}' should exist");

            var countryId = savedRowId!.Replace(RowAbbreviationPrefix, "");
            TestContext.Out.WriteLine($"Found country with ID: {countryId}");

            var deleteButtonId = $"{RowDeletePrefix}{countryId}";
            TestContext.Out.WriteLine($"Clicking delete button: {deleteButtonId}");

            await Actions.ClickElementById(deleteButtonId);
            await Actions.Wait500();

            TestContext.Out.WriteLine("Clicking modal confirm button...");
            await Actions.ClickElementById(ModalIds.DeleteConfirm);

            // Wait for delete API call and autoSave to complete
            TestContext.Out.WriteLine("Waiting for delete to complete (3000ms)...");
            await Actions.Wait3000();

            // Assert - check that no row with TST value exists
            TestContext.Out.WriteLine("Checking if row was deleted...");
            var deletedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefix, TestAbbreviation);
            TestContext.Out.WriteLine($"Row after delete: {deletedRowId ?? "null"}");
            Assert.That(deletedRowId, Is.Null, "Country should be deleted from database");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Country deleted from database successfully");
        }
    }
}
