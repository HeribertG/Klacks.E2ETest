using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.SettingsAbsenceDetailIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(74)]
    public class SettingsAbsenceDetailTest : PlaywrightSetup
    {
        private Listener _listener = null!;
        private static string? _createdDetailName;

        private const string TestDetailName = "E2E Test Halbtag";
        private const string TestDurationHours = "4";
        private const string AddBtnId = AddBtn;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById("absence-detail-card");
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
        public async Task Step1_VerifyAbsenceDetailSectionLoaded()
        {
            TestContext.Out.WriteLine("=== Step 1: Verify AbsenceDetail Section Loaded ===");

            await Actions.ScrollPageDown(scrollToBottom: true);
            await Actions.Wait500();

            var addButton = await Actions.FindElementById(AddBtnId);
            Assert.That(addButton, Is.Not.Null, "Add button for absence details should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AbsenceDetail section loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_CreateNewAbsenceDetail()
        {
            TestContext.Out.WriteLine("=== Step 2: Create New Absence Detail ===");
            var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
            _createdDetailName = $"{TestDetailName} {timestamp}";
            TestContext.Out.WriteLine($"Creating absence detail: {_createdDetailName}");

            var originalDeeplKey = await Actions.ReadSettingViaApi("DEEPL_API_KEY");
            if (!string.IsNullOrEmpty(originalDeeplKey))
            {
                await Actions.SaveSettingViaApi("DEEPL_API_KEY", "");
                TestContext.Out.WriteLine("Temporarily cleared DeepL API key to avoid translation errors");
            }

            try
            {
                await Actions.ScrollPageDown(scrollToBottom: true);
                await Actions.Wait500();

                await Actions.ClickElementById(AddBtnId);
                await Actions.Wait1000();

                var modalSaveBtn = await Actions.FindElementById(ModalSaveBtn);
                Assert.That(modalSaveBtn, Is.Not.Null, "Modal should be open");

                var dropdown = await Actions.FindElementById(ModalAbsenceDropdown);
                Assert.That(dropdown, Is.Not.Null, "Absence dropdown should exist");
                await dropdown!.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                await Actions.Wait500();

                await Actions.ClickButtonById(ModeDuration);
                await Actions.Wait500();

                await Actions.ClearInputById(DurationHours);
                await Actions.TypeIntoInputById(DurationHours, TestDurationHours);

                await Actions.ClearInputById(ModalDetailName);
                await Actions.TypeIntoInputById(ModalDetailName, _createdDetailName);

                await Actions.ClickButtonById(ModalSaveBtn);
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait2000();

                Assert.That(_listener.HasApiErrors(), Is.False,
                    $"API error after create: {_listener.GetLastErrorMessage()}");

                var createdRow = await FindDetailRowByName(_createdDetailName);
                Assert.That(createdRow, Is.Not.Null, "Created absence detail should appear in the list");

                TestContext.Out.WriteLine($"Absence detail created successfully: {_createdDetailName}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalDeeplKey))
                {
                    await Actions.SaveSettingViaApi("DEEPL_API_KEY", originalDeeplKey);
                    TestContext.Out.WriteLine("Restored DeepL API key");
                }
            }
        }

        [Test]
        [Order(3)]
        public async Task Step3_VerifyCreatedDetailInList()
        {
            TestContext.Out.WriteLine("=== Step 3: Verify Created Detail In List ===");

            if (string.IsNullOrEmpty(_createdDetailName))
            {
                TestContext.Out.WriteLine("No detail was created in Step2 - skipping");
                Assert.Inconclusive("No absence detail was created in previous step");
                return;
            }

            await Actions.ScrollPageDown(scrollToBottom: true);
            await Actions.Wait500();

            var createdRow = await FindDetailRowByName(_createdDetailName);
            Assert.That(createdRow, Is.Not.Null, $"Absence detail '{_createdDetailName}' should be visible in the list");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Absence detail '{_createdDetailName}' verified in list");
        }

        [Test]
        [Order(4)]
        public async Task Step4_DeleteCreatedAbsenceDetail()
        {
            TestContext.Out.WriteLine("=== Step 4: Delete Created Absence Detail ===");

            if (string.IsNullOrEmpty(_createdDetailName))
            {
                TestContext.Out.WriteLine("No detail was created - skipping delete");
                Assert.Inconclusive("No absence detail was created in previous step");
                return;
            }

            await Actions.ScrollPageDown(scrollToBottom: true);
            await Actions.Wait500();

            var deleteButtons = await Page.QuerySelectorAllAsync($"[id^='{RowDeletePrefix}']");
            TestContext.Out.WriteLine($"Found {deleteButtons.Count} delete buttons");

            IElementHandle? targetDeleteBtn = null;
            foreach (var btn in deleteButtons)
            {
                var btnId = await btn.GetAttributeAsync("id");
                if (string.IsNullOrEmpty(btnId)) continue;

                var rowId = btnId.Replace(RowDeletePrefix, RowNamePrefix);
                var nameElement = await Page.QuerySelectorAsync($"[id='{rowId}']");
                if (nameElement != null)
                {
                    var nameText = await nameElement.InputValueAsync();
                    if (nameText != null && nameText.Contains(_createdDetailName))
                    {
                        targetDeleteBtn = btn;
                        TestContext.Out.WriteLine($"Found delete button for '{_createdDetailName}': {btnId}");
                        break;
                    }
                }
            }

            Assert.That(targetDeleteBtn, Is.Not.Null, $"Delete button for '{_createdDetailName}' should exist");

            await targetDeleteBtn!.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Clicked delete button");

            var confirmBtn = await Actions.FindElementById(ModalIds.DeleteConfirm);
            if (confirmBtn == null)
            {
                TestContext.Out.WriteLine("Delete confirmation button not found");
                Assert.Fail("Delete confirmation modal did not appear");
                return;
            }

            await confirmBtn.ClickAsync();
            TestContext.Out.WriteLine("Confirmed deletion");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            var deletedRow = await FindDetailRowByName(_createdDetailName);

            Assert.That(deletedRow, Is.Null, "Deleted absence detail should no longer exist in the list");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            _createdDetailName = null;
            TestContext.Out.WriteLine("Absence detail deleted successfully");
        }

        private async Task<IElementHandle?> FindDetailRowByName(string detailName)
        {
            var nameElements = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            foreach (var element in nameElements)
            {
                var value = await element.InputValueAsync();
                if (value != null && value.Contains(detailName))
                {
                    return element;
                }
            }
            return null;
        }
    }
}
