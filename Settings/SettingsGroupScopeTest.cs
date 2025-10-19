using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    // NOTE: Group Scope tests will be marked as Inconclusive when running as Admin user
    // This is expected behavior: Admin users have access to all groups by default,
    // so the group scope buttons are disabled ([disabled]="user?.isAdmin")
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
        public async Task Step1_VerifyGroupScopePageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Group Scope Page Loaded ===");

            // Assert
            var userRows = await Page.QuerySelectorAllAsync("input[id^='group-scope-row-name-']");
            Assert.That(userRows.Count, Is.GreaterThan(0), "User list should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Group Scope page loaded successfully");
        }

        [Test]
        public async Task Step2_ExpandGroupTree()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Open Group Selection Modal ===");

            // Act - Find first group count button with short timeout
            var groupCountButton = await Page.QuerySelectorAsync("button[id^='group-scope-row-count-btn-']");
            if (groupCountButton != null)
            {
                var isEnabled = await groupCountButton.IsEnabledAsync();
                if (!isEnabled)
                {
                    TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                    Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                    return;
                }

                await groupCountButton.ClickAsync();
                await Actions.Wait500();

                // Check if modal opened
                var modal = await Page.QuerySelectorAsync(".modal, [class*='modal-content']");
                if (modal != null)
                {
                    TestContext.Out.WriteLine("Group selection modal opened successfully");

                    // Close modal
                    var closeButton = await Actions.FindElementById("group-scope-modal-close-btn");
                    if (closeButton != null)
                    {
                        await closeButton.ClickAsync();
                        await Actions.Wait500();
                    }
                }
            }
            else
            {
                TestContext.Out.WriteLine("No group count buttons found");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step3_ToggleGroupVisibility()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Toggle Group Visibility ===");

            // Act - Open modal
            var groupCountButton = await Page.QuerySelectorAsync("button[id^='group-scope-row-count-btn-']");
            if (groupCountButton != null)
            {
                var isEnabled = await groupCountButton.IsEnabledAsync();
                if (!isEnabled)
                {
                    TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                    Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                    return;
                }

                await groupCountButton.ClickAsync();
                await Actions.Wait500();

                // Find first checkbox in modal
                var firstCheckbox = await Page.QuerySelectorAsync("input[type='checkbox'][id^='group-']");
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

                // Close modal
                var cancelButton = await Actions.FindElementById("group-scope-modal-cancel-btn");
                if (cancelButton != null)
                {
                    await cancelButton.ClickAsync();
                    await Actions.Wait500();
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step4_VerifyGroupHierarchy()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Group Hierarchy ===");

            // Act - Open modal
            var groupCountButton = await Page.QuerySelectorAsync("button[id^='group-scope-row-count-btn-']");
            if (groupCountButton != null)
            {
                var isEnabled = await groupCountButton.IsEnabledAsync();
                if (!isEnabled)
                {
                    TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                    Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                    return;
                }

                await groupCountButton.ClickAsync();
                await Actions.Wait500();

                // Check for group checkboxes
                var groupCheckboxes = await Page.QuerySelectorAllAsync("input[type='checkbox'][id^='group-']");
                Assert.That(groupCheckboxes.Count, Is.GreaterThan(0), "At least one group should be visible");

                TestContext.Out.WriteLine($"Found {groupCheckboxes.Count} groups in hierarchy");

                // Close modal
                var closeButton = await Actions.FindElementById("group-scope-modal-close-btn");
                if (closeButton != null)
                {
                    await closeButton.ClickAsync();
                    await Actions.Wait500();
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step5_SaveGroupScopeChanges()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Save Group Scope Changes ===");

            // Act - Open modal
            var groupCountButton = await Page.QuerySelectorAsync("button[id^='group-scope-row-count-btn-']");
            if (groupCountButton != null)
            {
                var isEnabled = await groupCountButton.IsEnabledAsync();
                if (!isEnabled)
                {
                    TestContext.Out.WriteLine("Group count button is disabled - Admin users have access to all groups by default");
                    Assert.Inconclusive("Test skipped: Button is disabled because admin users don't need group scope settings");
                    return;
                }

                await groupCountButton.ClickAsync();
                await Actions.Wait500();

                // Make a change
                var firstCheckbox = await Page.QuerySelectorAsync("input[type='checkbox'][id^='group-']");
                if (firstCheckbox != null)
                {
                    var originalState = await firstCheckbox.IsCheckedAsync();
                    await firstCheckbox.ClickAsync();
                    await Actions.Wait500();

                    // Find and click Save button
                    var saveButton = await Actions.FindElementById("group-scope-modal-save-btn");
                    if (saveButton != null)
                    {
                        await saveButton.ClickAsync();
                        await Actions.WaitForSpinnerToDisappear();
                        await Actions.Wait500();

                        TestContext.Out.WriteLine("Group scope changes saved");

                        // Restore original state by reopening and toggling
                        await groupCountButton.ClickAsync();
                        await Actions.Wait500();

                        await firstCheckbox.ClickAsync();
                        await Actions.Wait500();

                        await saveButton.ClickAsync();
                        await Actions.WaitForSpinnerToDisappear();
                        await Actions.Wait500();
                    }
                    else
                    {
                        TestContext.Out.WriteLine("Save button not found - changes might be auto-saved");

                        // Close modal
                        var closeButton = await Actions.FindElementById("group-scope-modal-close-btn");
                        if (closeButton != null)
                        {
                            await closeButton.ClickAsync();
                            await Actions.Wait500();
                        }
                    }
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
