using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Debug
{
    [TestFixture]
    public class DebugGroupSelectTest2 : PlaywrightSetup
    {
        [Test]
        public async Task DebugGroupSelectInnerHTML()
        {
            await Actions.ClickButtonById(MainNavIds.OpenAbsenceId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            TestContext.Out.WriteLine($"Current URL: {Page.Url}");

            var groupSelectComponent = await Page.QuerySelectorAsync("app-group-select");
            TestContext.Out.WriteLine($"app-group-select element exists: {groupSelectComponent != null}");

            if (groupSelectComponent != null)
            {
                var innerHTML = await groupSelectComponent.InnerHTMLAsync();
                TestContext.Out.WriteLine($"\napp-group-select innerHTML length: {innerHTML.Length}");
                TestContext.Out.WriteLine($"app-group-select innerHTML:\n{innerHTML}");
            }

            var groupSelectContainer = await Page.QuerySelectorAsync(".group-select-container");
            TestContext.Out.WriteLine($"\n.group-select-container exists: {groupSelectContainer != null}");

            var groupSelectToggle = await Page.QuerySelectorAsync("#group-select-dropdown-toggle");
            TestContext.Out.WriteLine($"#group-select-dropdown-toggle exists: {groupSelectToggle != null}");
        }
    }
}
