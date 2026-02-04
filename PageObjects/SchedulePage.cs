using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.PageObjects;

/// <summary>
/// Page Object for the WorkSchedule page.
/// Provides high-level methods for schedule-specific operations.
/// </summary>
public class SchedulePage
{
    private readonly IPage _page;
    private readonly Wrapper _actions;
    private readonly GridWrapper _grid;
    private readonly string _baseUrl;

    public SchedulePage(IPage page, Wrapper actions, string baseUrl)
    {
        _page = page;
        _actions = actions;
        _baseUrl = baseUrl.TrimEnd('/');
        _grid = new GridWrapper(page, actions);
    }

    public GridWrapper Grid => _grid;

    #region Navigation

    /// <summary>
    /// Navigates to the schedule page with test mode enabled.
    /// </summary>
    public async Task NavigateToScheduleAsync(bool enableTestMode = true)
    {
        var path = enableTestMode ? "workplace/schedule?testMode" : "workplace/schedule";
        var fullUrl = _baseUrl + "/" + path;
        
        await _page.GotoAsync(fullUrl);
        await _actions.WaitForSpinnerToDisappear();
        await _actions.Wait500();
    }

    /// <summary>
    /// Waits for the schedule grid to be fully loaded.
    /// </summary>
    public async Task WaitForGridLoadAsync(int timeoutMs = 10000)
    {
        try
        {
            // Wait for canvas
            await _page.WaitForSelectorAsync("canvas[id^='template-canvas']", new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeoutMs
            });

            // If test mode is enabled, also wait for Ghost DOM
            if (await _grid.IsTestApiAvailableAsync())
            {
                await _grid.WaitForGhostDomAsync(timeoutMs);
            }
        }
        catch (TimeoutException)
        {
            TestContext.Out.WriteLine("Warning: Grid did not load within timeout");
            throw;
        }
    }

    #endregion

    #region Client Operations

    /// <summary>
    /// Finds a client row by client ID.
    /// </summary>
    public async Task<GridCellInfo?> FindClientRowAsync(string clientId)
    {
        var cells = await _grid.FindCellsByClientAsync(clientId);
        return cells.FirstOrDefault(c => c.IsHeader || c.Column == 0);
    }

    /// <summary>
    /// Finds a cell for a specific client and date.
    /// </summary>
    public async Task<GridCellInfo?> FindClientDateCellAsync(string clientId, string date)
    {
        return await _grid.FindCellByClientAndDateViaApiAsync(clientId, date);
    }

    /// <summary>
    /// Clicks on a client's cell for a specific date.
    /// </summary>
    public async Task ClickClientDateCellAsync(string clientId, string date)
    {
        var cell = await FindClientDateCellAsync(clientId, date);
        if (cell == null)
            throw new InvalidOperationException($"Cell for client {clientId} on date {date} not found");

        await _grid.ClickCellByCoordinatesAsync(cell.Row, cell.Column);
    }

    /// <summary>
    /// Scrolls to bring a specific client into view.
    /// </summary>
    public async Task ScrollToClientAsync(string clientId)
    {
        var cell = await FindClientRowAsync(clientId);
        if (cell == null)
            throw new InvalidOperationException($"Client {clientId} not found");

        await _grid.ScrollToRowAsync(cell.Row);
    }

    #endregion

    #region Work Entry Operations

    /// <summary>
    /// Creates a work entry by double-clicking a cell and entering data.
    /// This is a simplified version - actual implementation would need to handle
    /// the shift selection dialog or context menu.
    /// </summary>
    public async Task CreateWorkEntryAsync(int row, int column, string shiftName)
    {
        // Double-click to open create dialog
        await _grid.DoubleClickOnCanvasAsync(
            column * 100 + 50,  // Approximate position
            row * 30 + 40 + 15);

        await _actions.Wait500();

        // Here you would interact with the dialog
        // This depends on your specific UI flow
    }

    /// <summary>
    /// Edits a work entry in a cell.
    /// </summary>
    public async Task EditWorkEntryAsync(int row, int column, string newValue)
    {
        // Select cell
        await _grid.SelectCellViaApiAsync(row, column);
        await _actions.Wait100();

        // Start editing
        await _grid.StartEditViaApiAsync(row, column);
        await _actions.Wait100();

        // Type new value
        await _grid.TypeIntoActiveCellAsync(newValue);
        await _grid.PressKeyInActiveCellAsync("Enter");

        await _actions.Wait500();
    }

    /// <summary>
    /// Deletes work entries using the context menu.
    /// </summary>
    public async Task DeleteWorkEntryAsync(int row, int column)
    {
        // Right-click on cell
        await _grid.ClickCellByCoordinatesAsync(row, column);
        await _actions.Wait100();

        // Open context menu via right-click on canvas
        var canvas = await _page.QuerySelectorAsync("canvas[id^='template-canvas']");
        if (canvas != null)
        {
            var box = await canvas.BoundingBoxAsync();
            if (box != null)
            {
                var x = box.X + column * 100 + 50;
                var y = box.Y + row * 30 + 40 + 15;
                await _page.Mouse.ClickAsync(x, y, new MouseClickOptions { Button = MouseButton.Right });
            }
        }

        await _actions.Wait500();

        // Click delete in context menu
        // Selector depends on your context menu implementation
        await _page.ClickAsync("text='Delete', text='Löschen'");
    }

    #endregion

    #region Period Hours Verification

    /// <summary>
    /// Gets the displayed period hours for a client from the row header.
    /// </summary>
    public async Task<PeriodHoursInfo?> GetPeriodHoursAsync(string clientId)
    {
        // This would need to be implemented based on how period hours are displayed
        // in your schedule-schedule-row-header component

        // Example: Query the row header slot for the client
        var rowHeaderSelector = $"[data-client-id='{clientId}'] .period-hours";
        var element = await _page.QuerySelectorAsync(rowHeaderSelector);

        if (element == null)
            return null;

        var text = await element.TextContentAsync();
        // Parse the text to extract hours and surcharges
        // This depends on your display format

        return new PeriodHoursInfo
        {
            ClientId = clientId,
            RawText = text ?? ""
        };
    }

    /// <summary>
    /// Waits for period hours to update after an operation.
    /// </summary>
    public async Task WaitForPeriodHoursUpdateAsync(string clientId, int timeoutMs = 5000)
    {
        var startTime = DateTime.UtcNow;
        PeriodHoursInfo? previous = null;

        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            var current = await GetPeriodHoursAsync(clientId);

            if (previous != null && current != null && previous.RawText != current.RawText)
            {
                TestContext.Out.WriteLine($"Period hours updated: {previous.RawText} -> {current.RawText}");
                return;
            }

            previous = current;
            await Task.Delay(100);
        }

        TestContext.Out.WriteLine("Warning: Period hours did not update within timeout");
    }

    #endregion

    #region Debug and Diagnostics

    /// <summary>
    /// Logs the current state of the schedule.
    /// </summary>
    public async Task LogScheduleStateAsync()
    {
        TestContext.Out.WriteLine("=== Schedule State ===");

        var cells = await _grid.GetAllCellsViaApiAsync();
        TestContext.Out.WriteLine($"Total visible cells: {cells.Count}");

        var selected = await _grid.GetSelectedCellAsync();
        TestContext.Out.WriteLine($"Selected cell: {selected}");

        var editing = await _grid.GetEditingCellAsync();
        TestContext.Out.WriteLine($"Editing cell: {editing}");

        // Log clients
        var clients = cells.Where(c => !string.IsNullOrEmpty(c.ClientId))
                           .Select(c => c.ClientId)
                           .Distinct()
                           .ToList();
        TestContext.Out.WriteLine($"Visible clients: {string.Join(", ", clients)}");
    }

    /// <summary>
    /// Takes a screenshot with debug overlay enabled.
    /// </summary>
    public async Task TakeDebugScreenshotAsync(string fileName)
    {
        await _grid.EnableDebugOverlayAsync();
        await _actions.Wait500(); // Wait for overlay to render
        await _grid.TakeGridScreenshotAsync(fileName);
        await _grid.DisableDebugOverlayAsync();
    }

    #endregion
}

/// <summary>
/// Represents period hours information for a client.
/// </summary>
public class PeriodHoursInfo
{
    public string ClientId { get; set; } = "";
    public string RawText { get; set; } = "";
    public decimal? Hours { get; set; }
    public decimal? Surcharges { get; set; }
}
