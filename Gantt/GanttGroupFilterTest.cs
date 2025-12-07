using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest
{
    [TestFixture]
    [Order(30)]
    public class GanttGroupFilterTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public void Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();
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
        public async Task Step1_NavigateToGanttPage()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Navigate to Gantt Page ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Assert
            Assert.That(Page.Url.Contains("absence"), Is.True, "Should be on gantt/absence page");
            TestContext.Out.WriteLine($"Successfully navigated to gantt page: {Page.Url}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Navigation to gantt page completed successfully");
        }

        [Test]
        public async Task Step2_OpenGroupSelectDropdown()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Open Group Select Dropdown ===");
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Act
            var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
            Assert.That(dropdown, Is.Not.Null, "Group select dropdown should exist");

            await dropdown.ClickAsync();
            await Actions.Wait500();

            // Assert
            var dropdownContent = await Page.QuerySelectorAsync(".group-select-dropdown");
            Assert.That(dropdownContent, Is.Not.Null, "Dropdown should be open");
            TestContext.Out.WriteLine("Group select dropdown opened successfully");

            await dropdown.ClickAsync();
            await Actions.Wait500();

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step3_IterateThroughAllGroups()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Iterate Through All Groups (including nested) ===");
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Act
            var dropdown = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
            await dropdown.ClickAsync();
            await Actions.Wait500();

            await Page.WaitForSelectorAsync(".group-select-dropdown", new() { Timeout = 5000 });
            await Actions.Wait500();

            await ExpandAllGroupNodes();

            var allGroupOptions = await Page.QuerySelectorAllAsync(".group-option-button");
            int totalGroupCount = allGroupOptions.Count;
            TestContext.Out.WriteLine($"Found {totalGroupCount} groups (including nested) to test");

            var groupIds = new List<string>();
            foreach (var option in allGroupOptions)
            {
                var id = await option.GetAttributeAsync("id");
                if (!string.IsNullOrEmpty(id))
                {
                    groupIds.Add(id);
                }
            }

            for (int i = 0; i < groupIds.Count; i++)
            {
                var dropdownToggle = await Page.WaitForSelectorAsync("#group-select-dropdown-toggle", new() { Timeout = 10000 });
                var ariaExpanded = await dropdownToggle.GetAttributeAsync("aria-expanded");

                if (ariaExpanded == "false")
                {
                    await dropdownToggle.ClickAsync();
                    await Actions.Wait500();

                    await Page.WaitForSelectorAsync(".group-select-dropdown", new() { Timeout = 5000 });
                    await Actions.Wait500();

                    await ExpandAllGroupNodes();
                }

                var optionId = groupIds[i];
                var optionElement = await Page.QuerySelectorAsync($"#{optionId}");

                if (optionElement != null)
                {
                    var parentNode = await optionElement.EvaluateHandleAsync("el => el.closest('.group-tree-node-item')");
                    var groupNameElement = await parentNode.AsElement()?.QuerySelectorAsync(".group-tree-node-name");
                    var groupName = groupNameElement != null ? await groupNameElement.TextContentAsync() : $"Group {i}";

                    TestContext.Out.WriteLine($"Selecting group {i + 1}/{totalGroupCount}: {groupName}");

                    await Actions.ScrollElementIntoViewInContainer(optionId, ".group-select-dropdown");
                    await Actions.Wait500();

                    await optionElement.ClickAsync();
                    await Actions.WaitForSpinnerToDisappear();
                    await Actions.Wait1000();

                    // Assert
                    Assert.That(_listener.HasApiErrors(), Is.False,
                        $"No API errors should occur for group '{groupName}'. Error: {_listener.GetLastErrorMessage()}");

                    TestContext.Out.WriteLine($"Group '{groupName}' tested successfully");
                }
            }

            TestContext.Out.WriteLine($"All {totalGroupCount} groups tested successfully");
        }

        private async Task ExpandAllGroupNodes()
        {
            var expandToggles = await Page.QuerySelectorAllAsync(".group-tree-toggle");

            foreach (var toggle in expandToggles)
            {
                var isExpanded = await toggle.GetAttributeAsync("aria-expanded");
                if (isExpanded == "false")
                {
                    var toggleId = await toggle.GetAttributeAsync("id");
                    if (!string.IsNullOrEmpty(toggleId))
                    {
                        await Actions.ScrollElementIntoViewInContainer(toggleId, ".group-select-dropdown");
                    }

                    await toggle.ClickAsync();
                    await Actions.Wait500();
                }
            }
        }
    }
}
