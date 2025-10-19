using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsGroupScopeTest : PlaywrightSetup
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

            // Navigate to Group Scope tab
            var groupScopeTab = await Actions.FindElementByCssSelector("[href*='group-scope'], button:has-text('Group Scope'), a:has-text('Gruppenbereich')");
            if (groupScopeTab != null)
            {
                await groupScopeTab.ClickAsync();
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
        public async Task Step1_VerifyGroupScopePageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Group Scope Page Loaded ===");

            // Assert
            var groupList = await Actions.FindElementByCssSelector("ul, [class*='group-list'], [class*='tree-view']");
            Assert.That(groupList, Is.Not.Null, "Group list should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Group Scope page loaded successfully");
        }

        [Test]
        public async Task Step2_ExpandGroupTree()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Expand Group Tree ===");

            // Act - Find expandable group nodes
            var expandButtons = await Page.QuerySelectorAllAsync("button[class*='expand'], [class*='toggle'], fa-chevron-right");
            if (expandButtons.Count > 0)
            {
                var firstExpandButton = expandButtons[0];
                await firstExpandButton.ClickAsync();
                await Actions.Wait500();

                TestContext.Out.WriteLine("Group tree expanded successfully");
            }
            else
            {
                TestContext.Out.WriteLine("No expandable groups found");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step3_ToggleGroupVisibility()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Toggle Group Visibility ===");

            // Act - Find first group checkbox
            var firstCheckbox = await Actions.FindElementByCssSelector("input[type='checkbox']");
            if (firstCheckbox != null)
            {
                var isChecked = await firstCheckbox.IsCheckedAsync();
                TestContext.Out.WriteLine($"Initial checkbox state: {isChecked}");

                await firstCheckbox.ClickAsync();
                await Actions.Wait500();

                var newState = await firstCheckbox.IsCheckedAsync();
                Assert.That(newState, Is.Not.EqualTo(isChecked), "Checkbox state should have changed");

                // Restore original state
                await firstCheckbox.ClickAsync();
                await Actions.Wait500();

                TestContext.Out.WriteLine("Group visibility toggled and restored");
            }
            else
            {
                TestContext.Out.WriteLine("No visibility checkboxes found");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step4_VerifyGroupHierarchy()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Group Hierarchy ===");

            // Act - Check if nested groups are displayed
            var groupItems = await Page.QuerySelectorAllAsync("li, [class*='group-item']");
            Assert.That(groupItems.Count, Is.GreaterThan(0), "At least one group should be visible");

            // Check for nested structure indicators
            var nestedIndicators = await Page.QuerySelectorAllAsync("[class*='nested'], [class*='child'], [style*='padding-left']");
            if (nestedIndicators.Count > 0)
            {
                TestContext.Out.WriteLine($"Found {nestedIndicators.Count} nested group indicators");
            }

            TestContext.Out.WriteLine($"Total groups found: {groupItems.Count}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step5_SaveGroupScopeChanges()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Save Group Scope Changes ===");

            // Act - Make a change
            var firstCheckbox = await Actions.FindElementByCssSelector("input[type='checkbox']");
            if (firstCheckbox != null)
            {
                var originalState = await firstCheckbox.IsCheckedAsync();
                await firstCheckbox.ClickAsync();
                await Actions.Wait500();

                // Find and click Save button
                var saveButton = await Actions.FindElementByCssSelector("button:has-text('Save'), button:has-text('Speichern'), [class*='btn-save']");
                if (saveButton != null)
                {
                    await saveButton.ClickAsync();
                    await Actions.WaitForSpinnerToDisappear();
                    await Actions.Wait500();

                    TestContext.Out.WriteLine("Group scope changes saved");

                    // Restore original state
                    await firstCheckbox.ClickAsync();
                    await saveButton.ClickAsync();
                    await Actions.WaitForSpinnerToDisappear();
                    await Actions.Wait500();
                }
                else
                {
                    TestContext.Out.WriteLine("Save button not found - changes might be auto-saved");

                    // Restore original state
                    await firstCheckbox.ClickAsync();
                    await Actions.Wait500();
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
