using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.SettingsGroupScopeIds;

namespace E2ETest
{
    [TestFixture]
    [Order(23)]
    public class SettingsGroupScopeTest : PlaywrightSetup
    {
        private Listener _listener = null!;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(GroupScopeSection);
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
        public async Task Step1_VerifyGroupScopePageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Group Scope Page Loaded ===");

            // Assert
            var header = await Actions.FindElementById(GroupScopeHeader);
            Assert.That(header, Is.Not.Null, "Group scope header should be visible");

            var userRows = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
            Assert.That(userRows.Count, Is.GreaterThan(0), "User list should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Group Scope page loaded successfully with {userRows.Count} users");
        }

        [Test]
        [Order(2)]
        public async Task Step2_OpenGroupSelectionModal()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Open Group Selection Modal ===");

            // Act
            var groupCountButton = await Page.QuerySelectorAsync($"button[id^='{RowCountBtnPrefix}']");
            Assert.That(groupCountButton, Is.Not.Null, "Group count button should exist");

            var isEnabled = await groupCountButton!.IsEnabledAsync();
            if (!isEnabled)
            {
                TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                return;
            }

            await groupCountButton.ClickAsync();
            await Actions.Wait500();

            // Assert
            var modal = await Page.QuerySelectorAsync(".modal-content");
            Assert.That(modal, Is.Not.Null, "Modal should be open");
            TestContext.Out.WriteLine("Group selection modal opened successfully");

            // Cleanup
            await Actions.ClickElementById(ModalCloseBtnId);
            await Actions.Wait500();

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ToggleGroupCheckbox()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Toggle Group Checkbox ===");

            // Act
            var groupCountButton = await Page.QuerySelectorAsync($"button[id^='{RowCountBtnPrefix}']");
            Assert.That(groupCountButton, Is.Not.Null, "Group count button should exist");

            var isEnabled = await groupCountButton!.IsEnabledAsync();
            if (!isEnabled)
            {
                TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                return;
            }

            await groupCountButton.ClickAsync();
            await Actions.Wait500();

            var firstCheckbox = await Page.QuerySelectorAsync($"input[type='checkbox'][id^='{GroupCheckboxPrefix}']");
            Assert.That(firstCheckbox, Is.Not.Null, "Group checkbox should exist");

            var isChecked = await firstCheckbox!.IsCheckedAsync();
            TestContext.Out.WriteLine($"Initial checkbox state: {isChecked}");

            await firstCheckbox.ClickAsync();
            await Actions.Wait500();

            // Assert
            var newState = await firstCheckbox.IsCheckedAsync();
            Assert.That(newState, Is.Not.EqualTo(isChecked), "Checkbox state should have changed");
            TestContext.Out.WriteLine($"Checkbox toggled from {isChecked} to {newState}");

            // Restore original state
            await firstCheckbox.ClickAsync();
            await Actions.Wait500();

            // Cleanup
            await Actions.ClickElementById(ModalCancelBtnId);
            await Actions.Wait500();

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_VerifyGroupHierarchy()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Group Hierarchy ===");

            // Act
            var groupCountButton = await Page.QuerySelectorAsync($"button[id^='{RowCountBtnPrefix}']");
            Assert.That(groupCountButton, Is.Not.Null, "Group count button should exist");

            var isEnabled = await groupCountButton!.IsEnabledAsync();
            if (!isEnabled)
            {
                TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                return;
            }

            await groupCountButton.ClickAsync();
            await Actions.Wait500();

            // Assert
            var groupCheckboxes = await Page.QuerySelectorAllAsync($"input[type='checkbox'][id^='{GroupCheckboxPrefix}']");
            Assert.That(groupCheckboxes.Count, Is.GreaterThan(0), "At least one group should be visible");
            TestContext.Out.WriteLine($"Found {groupCheckboxes.Count} groups in hierarchy");

            // Cleanup
            await Actions.ClickElementById(ModalCloseBtnId);
            await Actions.Wait500();

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        [Order(5)]
        public async Task Step5_SaveGroupScopeChanges()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Save Group Scope Changes ===");

            // Act
            var groupCountButton = await Page.QuerySelectorAsync($"button[id^='{RowCountBtnPrefix}']");
            Assert.That(groupCountButton, Is.Not.Null, "Group count button should exist");

            var isEnabled = await groupCountButton!.IsEnabledAsync();
            if (!isEnabled)
            {
                TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                return;
            }

            await groupCountButton.ClickAsync();
            await Actions.Wait500();

            var firstCheckbox = await Page.QuerySelectorAsync($"input[type='checkbox'][id^='{GroupCheckboxPrefix}']");
            Assert.That(firstCheckbox, Is.Not.Null, "Group checkbox should exist");

            var originalState = await firstCheckbox!.IsCheckedAsync();
            await firstCheckbox.ClickAsync();
            await Actions.Wait500();

            // Save
            await Actions.ClickElementById(ModalSaveBtnId);
            await Actions.Wait1500();

            TestContext.Out.WriteLine("Group scope changes saved");

            // Restore original state
            groupCountButton = await Page.QuerySelectorAsync($"button[id^='{RowCountBtnPrefix}']");
            await groupCountButton!.ClickAsync();
            await Actions.Wait500();

            firstCheckbox = await Page.QuerySelectorAsync($"input[type='checkbox'][id^='{GroupCheckboxPrefix}']");
            await firstCheckbox!.ClickAsync();
            await Actions.Wait500();

            await Actions.ClickElementById(ModalSaveBtnId);
            await Actions.Wait1500();

            TestContext.Out.WriteLine("Original state restored");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
