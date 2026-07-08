// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;
using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest
{
    /// <summary>
    /// Shared step sequence for a simple lookup-table Settings section (add row, fill it, wait for
    /// autoSave, delete it again), used by both States and Countries. Subclasses supply the DOM ids,
    /// the backing table name and the test row values.
    /// </summary>
    public abstract class LookupTableSettingsTestBase : PlaywrightSetup
    {
        private Listener _listener = null!;

        protected abstract string EntityLabel { get; }

        protected abstract string DbTableName { get; }

        protected abstract string SectionId { get; }

        protected abstract string HeaderId { get; }

        protected abstract string AddButtonId { get; }

        protected abstract string NewRowAbbreviationId { get; }

        protected abstract string NewRowNameDeId { get; }

        protected abstract string NewRowPrefixId { get; }

        protected abstract string RowAbbreviationPrefixId { get; }

        protected abstract string RowDeletePrefixId { get; }

        protected abstract string TestAbbreviationValue { get; }

        protected abstract string TestNameValue { get; }

        protected abstract string TestPrefixValue { get; }

        [OneTimeSetUp]
        public async Task CleanupTestRowFromDb()
        {
            await DbHelper.ExecuteSqlAsync($"DELETE FROM {DbTableName} WHERE abbreviation = '{TestAbbreviationValue}'");
        }

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(SectionId);
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
        public async Task Step1_VerifyPageLoaded()
        {
            TestContext.Out.WriteLine($"=== Step 1: Verify {EntityLabel} Page Loaded ===");

            var addButton = await Actions.FindElementById(AddButtonId);
            Assert.That(addButton, Is.Not.Null, $"Add {EntityLabel} button should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"{EntityLabel} page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_AddRow()
        {
            TestContext.Out.WriteLine($"=== Step 2: Add {EntityLabel} Row ===");

            await Actions.ClickButtonById(AddButtonId);
            await Actions.Wait500();

            var newRowAbbreviation = await Actions.FindElementById(NewRowAbbreviationId);
            Assert.That(newRowAbbreviation, Is.Not.Null, $"New {EntityLabel} row should be added");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"New empty {EntityLabel} row added");
        }

        [Test]
        [Order(3)]
        public async Task Step3_FillAndWaitForAutoSave()
        {
            TestContext.Out.WriteLine($"=== Step 3: Fill {EntityLabel} and Wait for AutoSave ===");

            await Actions.FillInputById(NewRowAbbreviationId, TestAbbreviationValue);
            await Actions.Wait500();

            await Actions.FillInputById(NewRowNameDeId, TestNameValue);
            await Actions.Wait500();

            await Actions.FillInputById(NewRowPrefixId, TestPrefixValue);
            await Actions.Wait500();

            // Click outside to trigger blur/change event
            await Actions.ClickElementById(HeaderId);

            TestContext.Out.WriteLine("Waiting for autoSave (3000ms)...");
            await Actions.Wait3000();

            // Assert - after autoSave, the row should have a real ID (not empty)
            var savedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefixId, TestAbbreviationValue);

            Assert.That(savedRowId, Is.Not.Null, $"{EntityLabel} row with abbreviation '{TestAbbreviationValue}' should exist");
            TestContext.Out.WriteLine($"{EntityLabel} row found with ID: {savedRowId}");

            // Verify the ID is a real GUID (not empty)
            var entityId = savedRowId!.Replace(RowAbbreviationPrefixId, string.Empty);
            Assert.That(entityId, Is.Not.Empty.And.Not.EqualTo("undefined"),
                $"{EntityLabel} should have a real ID after autoSave");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"{EntityLabel} saved to database with ID: {entityId}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_DeleteFromDatabase()
        {
            TestContext.Out.WriteLine($"=== Step 4: Delete {EntityLabel} from Database ===");

            TestContext.Out.WriteLine($"Searching for saved {EntityLabel} row...");
            var savedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefixId, TestAbbreviationValue);
            Assert.That(savedRowId, Is.Not.Null, $"Saved {EntityLabel} row with abbreviation '{TestAbbreviationValue}' should exist");

            var entityId = savedRowId!.Replace(RowAbbreviationPrefixId, string.Empty);
            TestContext.Out.WriteLine($"Found {EntityLabel} with ID: {entityId}");

            var deleteButtonId = $"{RowDeletePrefixId}{entityId}";
            TestContext.Out.WriteLine($"Clicking delete button: {deleteButtonId}");

            await Actions.ClickElementById(deleteButtonId);
            await Actions.Wait500();

            TestContext.Out.WriteLine("Clicking modal confirm button...");
            await Actions.ClickElementById(ModalIds.DeleteConfirm);

            TestContext.Out.WriteLine("Waiting for delete to complete (3000ms)...");
            await Actions.Wait3000();

            TestContext.Out.WriteLine("Checking if row was deleted...");
            var deletedRowId = await Actions.FindInputIdByValue(RowAbbreviationPrefixId, TestAbbreviationValue);
            TestContext.Out.WriteLine($"Row after delete: {deletedRowId ?? "null"}");
            Assert.That(deletedRowId, Is.Null, $"{EntityLabel} should be deleted from database");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"{EntityLabel} deleted from database successfully");
        }
    }
}
