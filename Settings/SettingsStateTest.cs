using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.SettingsStatesIds;

namespace E2ETest
{
    [TestFixture]
    [Order(25)]
    public class SettingsStateTest : PlaywrightSetup
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

            await Actions.ScrollIntoViewById(StatesSection);
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
        public async Task Step1_VerifyStatesPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify States Page Loaded ===");

            // Assert
            var addButton = await Actions.FindElementById(AddButton);
            Assert.That(addButton, Is.Not.Null, "Add state button should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("States page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_AddStateRow()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Add State Row ===");

            // Act
            await Actions.ClickButtonById(AddButton);
            await Actions.Wait500();

            // Assert
            var newRowAbbreviation = await Actions.FindElementById(NewRowAbbreviation);
            Assert.That(newRowAbbreviation, Is.Not.Null, "New state row should be added");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("New empty state row added");
        }

        [Test]
        [Order(3)]
        public async Task Step3_FillStateAndWaitForAutoSave()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Fill State and Wait for AutoSave ===");

            // Act
            await Actions.FillInputById(NewRowAbbreviation, TestAbbreviation);
            await Actions.Wait500();

            await Actions.FillInputById(NewRowNameDe, TestName);
            await Actions.Wait500();

            await Actions.FillInputById(NewRowPrefix, TestPrefix);
            await Actions.Wait500();

            // Click outside to trigger blur/change event
            await Actions.ClickElementById(StatesHeader);

            TestContext.Out.WriteLine("Waiting for autoSave (3000ms)...");
            await Actions.Wait3000();

            // Assert - after autoSave, the row should have a real ID (not empty)
            var savedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefix, TestAbbreviation);

            Assert.That(savedRowId, Is.Not.Null, $"State row with abbreviation '{TestAbbreviation}' should exist");
            TestContext.Out.WriteLine($"State row found with ID: {savedRowId}");

            // Verify the ID is a real GUID (not empty)
            var stateId = savedRowId!.Replace(RowAbbreviationPrefix, "");
            Assert.That(stateId, Is.Not.Empty.And.Not.EqualTo("undefined"),
                "State should have a real ID after autoSave");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"State saved to database with ID: {stateId}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_DeleteStateFromDatabase()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Delete State from Database ===");

            // Act - find the saved row via Actions helper (ngModel doesn't set value attribute)
            TestContext.Out.WriteLine("Searching for saved state row...");
            var savedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefix, TestAbbreviation);
            Assert.That(savedRowId, Is.Not.Null, $"Saved state row with abbreviation '{TestAbbreviation}' should exist");

            var stateId = savedRowId!.Replace(RowAbbreviationPrefix, "");
            TestContext.Out.WriteLine($"Found state with ID: {stateId}");

            var deleteButtonId = $"{RowDeletePrefix}{stateId}";
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
            Assert.That(deletedRowId, Is.Null, "State should be deleted from database");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("State deleted from database successfully");
        }
    }
}
