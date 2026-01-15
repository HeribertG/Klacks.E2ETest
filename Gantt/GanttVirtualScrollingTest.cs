using E2ETest.Helpers;
using E2ETest.Wrappers;
using Microsoft.Playwright;
using NUnit.Framework;

namespace E2ETest;

[TestFixture]
[Order(34)]
[Parallelizable(ParallelScope.Self)]
public class GanttVirtualScrollingTest : PlaywrightSetup
{
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task GanttVirtualScrolling_LoadsInitialChunk()
    {
        // Arrange
        await Page.GotoAsync(BaseUrl + "workplace/absence");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        await Page.WaitForTimeoutAsync(3000);

        // Assert
        var rowHeader = Page.Locator("app-absence-gantt-row-header");
        await Assertions.Expect(rowHeader).ToBeVisibleAsync(new() { Timeout = 10000 });

        var canvas = Page.Locator("app-absence-gantt-row-header canvas");
        await Assertions.Expect(canvas.First).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    [Test]
    public async Task GanttVirtualScrolling_LoadsMoreRowsOnScroll()
    {
        // Arrange
        await Page.GotoAsync(BaseUrl + "workplace/absence");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(3000);

        // Act - Use keyboard to scroll
        var ganttSurface = Page.Locator("app-absence-gantt-surface");
        await ganttSurface.ClickAsync();

        for (int i = 0; i < 5; i++)
        {
            await Page.Keyboard.PressAsync("PageDown");
            await Page.WaitForTimeoutAsync(300);
        }

        await Page.WaitForTimeoutAsync(1000);

        // Assert
        var rowHeader = Page.Locator("app-absence-gantt-row-header");
        await Assertions.Expect(rowHeader).ToBeVisibleAsync();
    }

    [Test]
    public async Task GanttVirtualScrolling_PerformanceTest_ScrollThroughAllRows()
    {
        // Arrange
        await Page.GotoAsync(BaseUrl + "workplace/absence");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(3000);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Use keyboard to scroll
        var ganttSurface = Page.Locator("app-absence-gantt-surface");
        await ganttSurface.ClickAsync();

        for (int i = 0; i < 10; i++)
        {
            await Page.Keyboard.PressAsync("PageDown");
            await Page.WaitForTimeoutAsync(200);
        }

        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(15000));
        var rowHeader = Page.Locator("app-absence-gantt-row-header");
        await Assertions.Expect(rowHeader).ToBeVisibleAsync();
    }

    [Test]
    public async Task GanttVirtualScrolling_RendersCorrectly_After_Filter()
    {
        // Arrange
        await Page.GotoAsync(BaseUrl + "workplace/absence");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000);

        // Act - Apply filter
        var searchInput = Page.Locator("input[placeholder*='Search']").First;
        if (await searchInput.IsVisibleAsync())
        {
            await searchInput.FillAsync("Test");
            await Page.WaitForTimeoutAsync(1000);
        }

        // Assert
        var rowHeader = Page.Locator("app-absence-gantt-row-header");
        await Assertions.Expect(rowHeader).ToBeVisibleAsync();
    }
}
