using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Debug
{
    [TestFixture]
    public class DebugGroupSelectTest : PlaywrightSetup
    {
        [Test]
        public async Task DebugGroupSelectVisibility()
        {
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            TestContext.Out.WriteLine($"Current URL: {Page.Url}");

            var pageContent = await Page.ContentAsync();
            var hasGroupSelectComponent = pageContent.Contains("app-group-select");
            TestContext.Out.WriteLine($"Page contains app-group-select: {hasGroupSelectComponent}");

            var allGroupSelects = await Page.QuerySelectorAllAsync("app-group-select");
            TestContext.Out.WriteLine($"Found {allGroupSelects.Count} app-group-select elements");

            var groupSelectToggle = await Page.QuerySelectorAsync("#group-select-dropdown-toggle");
            TestContext.Out.WriteLine($"Found #group-select-dropdown-toggle: {groupSelectToggle != null}");

            if (groupSelectToggle == null)
            {
                var allDivs = await Page.QuerySelectorAllAsync("div[id*='group']");
                TestContext.Out.WriteLine($"\nAll divs with 'group' in ID:");
                foreach (var div in allDivs)
                {
                    var id = await div.GetAttributeAsync("id");
                    TestContext.Out.WriteLine($"  - {id}");
                }
            }
        }
    }
}
