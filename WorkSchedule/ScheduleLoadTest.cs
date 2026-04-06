using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.PageObjects;
using Microsoft.Playwright;

namespace Klacks.E2ETest.WorkSchedule;

/// <summary>
/// Quick diagnostic test to verify the schedule page loads without freezing.
/// </summary>
[TestFixture]
public class ScheduleLoadTest : PlaywrightSetup
{
    [Test]
    public async Task Schedule_ShouldLoadWithoutFreezing()
    {
        var schedule = new SchedulePage(Page, Actions, BaseUrl);

        await schedule.NavigateToScheduleAsync(enableTestMode: true);

        try
        {
            await schedule.WaitForGridLoadAsync(timeoutMs: 15000);
            TestContext.Out.WriteLine("SUCCESS: Schedule grid loaded within 15 seconds");
            Assert.Pass("Schedule loaded successfully");
        }
        catch (TimeoutException)
        {
            var url = Page.Url;
            TestContext.Out.WriteLine($"FAIL: Schedule grid did not load. Current URL: {url}");

            var consoleErrors = new List<string>();
            Page.Console += (_, msg) =>
            {
                if (msg.Type == "error")
                    consoleErrors.Add(msg.Text);
            };
            await Task.Delay(2000);

            foreach (var error in consoleErrors)
            {
                TestContext.Out.WriteLine($"Console Error: {error}");
            }

            Assert.Fail("Schedule page froze - grid canvas did not appear within 15 seconds");
        }
    }
}
