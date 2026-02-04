using Microsoft.Playwright;
using System.Text.Json;

namespace Klacks.E2ETest.Wrappers;

/// <summary>
/// Specialized wrapper for Canvas-based Grid interactions.
/// Provides methods to interact with the grid via Ghost DOM, Window API, or coordinate-based clicks.
/// </summary>
/// <remarks>
/// The Schedule Grid is rendered on HTML5 Canvas, making standard DOM selectors impossible.
/// This wrapper provides multiple strategies:
/// 1. Ghost DOM: Interact with invisible HTML overlay (if testMode is enabled)
/// 2. Window API: Access grid metadata and perform actions via window.klacksScheduleGrid
/// 3. Coordinate-based: Click on specific canvas coordinates
/// 4. API-based: Direct backend calls for data verification
/// </remarks>
public sealed class GridWrapper
{
    private readonly IPage _page;
    private readonly Wrapper _actions;

    public GridWrapper(IPage page, Wrapper actions)
    {
        _page = page;
        _actions = actions;
    }

    #region Ghost DOM Interactions

    /// <summary>
    /// Waits for the Ghost DOM overlay to be present.
    /// Requires ?testMode to be appended to the URL.
    /// </summary>
    public async Task WaitForGhostDomAsync(int timeoutMs = 10000)
    {
        await _page.WaitForSelectorAsync("[data-testid='grid-overlay']", new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
    }

    /// <summary>
    /// Clicks on a cell using the Ghost DOM overlay.
    /// Works for both editable and non-editable cells.
    /// </summary>
    public async Task ClickCellByCoordinatesAsync(int row, int column)
    {
        var cellSelector = $"[data-testid='cell-{row}-{column}']";
        await _page.ClickAsync(cellSelector);
    }

    /// <summary>
    /// Gets a cell element from the Ghost DOM.
    /// Returns null if cell is not visible or Ghost DOM is not enabled.
    /// </summary>
    public async Task<IElementHandle?> GetGhostCellAsync(int row, int column)
    {
        var cellSelector = $"[data-testid='cell-{row}-{column}']";
        return await _page.QuerySelectorAsync(cellSelector);
    }

    /// <summary>
    /// Finds a cell by its displayed value in the Ghost DOM.
    /// </summary>
    public async Task<IElementHandle?> FindCellByValueAsync(string value)
    {
        var selector = $"[data-value='{value}']";
        return await _page.QuerySelectorAsync(selector);
    }

    /// <summary>
    /// Finds a cell by its displayed value via Window API.
    /// </summary>
    public async Task<GridCellInfo?> FindCellByValueViaApiAsync(string value)
    {
        var result = await _page.EvaluateAsync<JsonElement>(
            $"() => window.klacksScheduleGrid?.getCellByValue('{value}')");

        if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return DeserializeCellInfo(result);
    }

    /// <summary>
    /// Finds a schedule cell by client ID and date (schedule-specific).
    /// </summary>
    public async Task<IElementHandle?> FindCellByClientAndDateAsync(string clientId, string date)
    {
        var selector = $"[data-client-id='{clientId}'][data-date='{date}']";
        return await _page.QuerySelectorAsync(selector);
    }

    /// <summary>
    /// Gets all visible ghost cells.
    /// </summary>
    public async Task<IReadOnlyList<IElementHandle>> GetAllVisibleCellsAsync()
    {
        return await _page.QuerySelectorAllAsync("[data-testid^='cell-']");
    }

    /// <summary>
    /// Gets the active cell input (when editing an editable cell).
    /// </summary>
    public async Task<IElementHandle?> GetActiveCellInputAsync()
    {
        // Use Locator for visibility check instead of :visible pseudo-selector
        var locator = _page.Locator("[data-testid='cell-input']").Filter(new() { Has = _page.Locator("css=visible") });
        try 
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 1000 });
            return await locator.ElementHandleAsync();
        }
        catch 
        {
            // Fallback: try to find without visibility filter
            var input = await _page.QuerySelectorAsync("[data-testid='cell-input']");
            if (input != null)
            {
                var isVisible = await input.IsVisibleAsync();
                if (isVisible)
                {
                    return input;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Types into the active cell input.
    /// </summary>
    public async Task TypeIntoActiveCellAsync(string text, int maxRetries = 5)
    {
        IElementHandle? input = null;
        for (int i = 0; i < maxRetries; i++)
        {
            input = await GetActiveCellInputAsync();
            if (input != null)
            {
                break;
            }

            await Task.Delay(100);
        }
        
        if (input == null)
        {
            throw new InvalidOperationException("No active cell input found. Double-click a cell first.");
        }

        await input.FillAsync(text);
    }

    /// <summary>
    /// Presses a key in the active cell input.
    /// </summary>
    public async Task PressKeyInActiveCellAsync(string key)
    {
        var input = await GetActiveCellInputAsync();
        if (input == null)
        {
            throw new InvalidOperationException("No active cell input found.");
        }

        await input.PressAsync(key);
    }

    #endregion

    #region Window API Interactions

    /// <summary>
    /// Checks if the Grid Test API is available (window.klacksScheduleGrid).
    /// </summary>
    public async Task<bool> IsTestApiAvailableAsync()
    {
        return await _page.EvaluateAsync<bool>("() => typeof window.klacksScheduleGrid !== 'undefined'");
    }

    /// <summary>
    /// Gets cell metadata via the Window API.
    /// </summary>
    public async Task<GridCellInfo?> GetCellMetadataAsync(int row, int column)
    {
        var result = await _page.EvaluateAsync<JsonElement>(
            $"() => window.klacksScheduleGrid?.getCellAt({row}, {column})");

        if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return DeserializeCellInfo(result);
    }

    /// <summary>
    /// Finds a cell by client ID and date via Window API.
    /// </summary>
    public async Task<GridCellInfo?> FindCellByClientAndDateViaApiAsync(string clientId, string date)
    {
        var result = await _page.EvaluateAsync<JsonElement>(
            $"() => window.klacksScheduleGrid?.getCellByClientAndDate('{clientId}', '{date}')");

        if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return DeserializeCellInfo(result);
    }

    /// <summary>
    /// Gets all cells via Window API.
    /// </summary>
    public async Task<IReadOnlyList<GridCellInfo>> GetAllCellsViaApiAsync()
    {
        var result = await _page.EvaluateAsync<JsonElement>(
            "() => window.klacksScheduleGrid?.getAllCells() ?? []");

        return DeserializeCellList(result);
    }

    /// <summary>
    /// Gets the currently selected cell.
    /// </summary>
    public async Task<CellPosition?> GetSelectedCellAsync()
    {
        var result = await _page.EvaluateAsync<JsonElement>(
            "() => window.klacksScheduleGrid?.getSelectedCell()");

        if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return new CellPosition
        {
            Row = result.GetProperty("row").GetInt32(),
            Column = result.GetProperty("column").GetInt32()
        };
    }

    /// <summary>
    /// Gets the cell currently being edited.
    /// </summary>
    public async Task<CellPosition?> GetEditingCellAsync()
    {
        var result = await _page.EvaluateAsync<JsonElement>(
            "() => window.klacksScheduleGrid?.getEditingCell()");

        if (result.ValueKind == JsonValueKind.Null || result.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return new CellPosition
        {
            Row = result.GetProperty("row").GetInt32(),
            Column = result.GetProperty("column").GetInt32()
        };
    }

    /// <summary>
    /// Selects a cell via Window API (faster than clicking).
    /// </summary>
    public async Task SelectCellViaApiAsync(int row, int column)
    {
        await _page.EvaluateAsync($"" +
            $"window.klacksScheduleGrid?.selectCell({row}, {column})");
    }

    /// <summary>
    /// Starts editing a cell via Window API.
    /// </summary>
    public async Task StartEditViaApiAsync(int row, int column)
    {
        await _page.EvaluateAsync($"" +
            $"window.klacksScheduleGrid?.startEdit({row}, {column})");
    }

    /// <summary>
    /// Finds all cells for a specific client.
    /// </summary>
    public async Task<IReadOnlyList<GridCellInfo>> FindCellsByClientAsync(string clientId)
    {
        var result = await _page.EvaluateAsync<JsonElement>(
            $"() => window.klacksScheduleGrid?.findCellsByClient('{clientId}') ?? []");

        return DeserializeCellList(result);
    }

    #endregion

    #region Coordinate-based Interactions

    /// <summary>
    /// Clicks on the canvas at specific coordinates relative to the grid.
    /// </summary>
    public async Task ClickOnCanvasAsync(int x, int y)
    {
        var canvas = await _page.QuerySelectorAsync("canvas[id^='template-canvas']");
        if (canvas == null)
        {
            throw new InvalidOperationException("Canvas element not found.");
        }

        var box = await canvas.BoundingBoxAsync();
        if (box == null)
        {
            throw new InvalidOperationException("Could not get canvas bounding box.");
        }

        await _page.Mouse.ClickAsync(box.X + x, box.Y + y);
    }

    /// <summary>
    /// Clicks on a cell by calculating its position based on row/column and cell dimensions.
    /// </summary>
    public async Task ClickCellByCalculatedPositionAsync(int row, int column, int cellWidth = 100, int cellHeight = 30, int headerHeight = 40)
    {
        // Get scroll position
        var scrollPos = await _page.EvaluateAsync<JsonElement>(
            "() => ({ h: window.gridHorizontalScroll || 0, v: window.gridVerticalScroll || 0 })");

        int scrollH = scrollPos.TryGetProperty("h", out var h) ? h.GetInt32() : 0;
        int scrollV = scrollPos.TryGetProperty("v", out var v) ? v.GetInt32() : 0;

        // Calculate position
        int x = (column - scrollH) * cellWidth + (cellWidth / 2);
        int y = (row - scrollV) * cellHeight + headerHeight + (cellHeight / 2);

        await ClickOnCanvasAsync(x, y);
    }

    /// <summary>
    /// Performs a double-click on the canvas at specific coordinates.
    /// </summary>
    public async Task DoubleClickOnCanvasAsync(int x, int y)
    {
        var canvas = await _page.QuerySelectorAsync("canvas[id^='template-canvas']");
        if (canvas == null)
        {
            throw new InvalidOperationException("Canvas element not found.");
        }

        var box = await canvas.BoundingBoxAsync();
        if (box == null)
            throw new InvalidOperationException("Could not get canvas bounding box.");

        await _page.Mouse.DblClickAsync(box.X + x, box.Y + y);
    }

    #endregion

    #region Scrolling

    /// <summary>
    /// Scrolls the grid vertically.
    /// </summary>
    public async Task ScrollVerticalAsync(int rows)
    {
        await _page.Mouse.WheelAsync(0, rows * 30); // Approximate row height
        await _actions.Wait100(); // Wait for render
    }

    /// <summary>
    /// Scrolls the grid horizontally.
    /// </summary>
    public async Task ScrollHorizontalAsync(int columns)
    {
        await _page.Mouse.WheelAsync(columns * 100, 0); // Approximate column width
        await _actions.Wait100();
    }

    /// <summary>
    /// Scrolls to a specific row using the Window API if available.
    /// </summary>
    public async Task ScrollToRowAsync(int targetRow)
    {
        // Try to use API first
        var canScroll = await _page.EvaluateAsync<bool>(
            "() => typeof window.klacksScheduleGrid?.scrollToRow === 'function'");

        if (canScroll)
        {
            await _page.EvaluateAsync($"window.klacksScheduleGrid?.scrollToRow({targetRow})");
        }
        else
        {
            // Fallback: calculate and use scrollbar
            // This is a simplified version - actual implementation would need scrollbar interaction
            await ScrollVerticalAsync(targetRow);
        }
    }

    #endregion

    #region Debug Helpers

    /// <summary>
    /// Enables debug mode on the Ghost DOM overlay (shows red borders).
    /// </summary>
    public async Task EnableDebugOverlayAsync()
    {
        await _page.EvaluateAsync(
            "() => document.querySelector('.test-overlay-container')?.classList.add('debug-mode')");
    }

    /// <summary>
    /// Disables debug mode on the Ghost DOM overlay.
    /// </summary>
    public async Task DisableDebugOverlayAsync()
    {
        await _page.EvaluateAsync(
            "() => document.querySelector('.test-overlay-container')?.classList.remove('debug-mode')");
    }

    /// <summary>
    /// Takes a screenshot of the grid area.
    /// </summary>
    public async Task TakeGridScreenshotAsync(string fileName)
    {
        var canvas = await _page.QuerySelectorAsync("canvas[id^='template-canvas']");
        if (canvas != null)
        {
            await canvas.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Path = fileName
            });
        }
    }

    /// <summary>
    /// Logs the current grid state (visible cells, selected cell, etc.).
    /// </summary>
    public async Task LogGridStateAsync()
    {
        var selected = await GetSelectedCellAsync();
        var editing = await GetEditingCellAsync();
        var cells = await GetAllCellsViaApiAsync();

        TestContext.Out.WriteLine("=== Grid State ===");
        TestContext.Out.WriteLine($"Selected: {selected?.Row},{selected?.Column}");
        TestContext.Out.WriteLine($"Editing: {editing?.Row},{editing?.Column}");
        TestContext.Out.WriteLine($"Visible cells: {cells.Count}");
    }

    #endregion

    #region Helper Methods

    private GridCellInfo DeserializeCellInfo(JsonElement element)
    {
        var info = new GridCellInfo
        {
            Row = element.GetProperty("row").GetInt32(),
            Column = element.GetProperty("column").GetInt32(),
            Value = element.GetProperty("value").GetString() ?? "",
            TestId = element.GetProperty("testId").GetString() ?? "",
            IsEditable = element.TryGetProperty("isEditable", out var editable) && editable.GetBoolean(),
            IsVisible = element.TryGetProperty("isVisible", out var visible) && visible.GetBoolean(),
            IsHeader = element.TryGetProperty("isHeader", out var header) && header.GetBoolean()
        };

        if (element.TryGetProperty("clientId", out var clientId))
            info.ClientId = clientId.GetString();
        if (element.TryGetProperty("clientName", out var clientName))
            info.ClientName = clientName.GetString();
        if (element.TryGetProperty("date", out var date))
            info.Date = date.GetString();
        if (element.TryGetProperty("entryType", out var entryType))
            info.EntryType = entryType.GetString();
        if (element.TryGetProperty("shiftId", out var shiftId))
            info.ShiftId = shiftId.GetString();

        return info;
    }

    private IReadOnlyList<GridCellInfo> DeserializeCellList(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return Array.Empty<GridCellInfo>();

        var list = new List<GridCellInfo>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(DeserializeCellInfo(item));
        }
        return list;
    }

    #endregion
}

/// <summary>
/// Represents information about a grid cell.
/// </summary>
public class GridCellInfo
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string Value { get; set; } = "";
    public string TestId { get; set; } = "";
    public bool IsEditable { get; set; }
    public bool IsVisible { get; set; }
    public bool IsHeader { get; set; }

    // Schedule-specific properties
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? Date { get; set; }
    public string? EntryType { get; set; }
    public string? ShiftId { get; set; }
}

/// <summary>
/// Represents a cell position (row, column).
/// </summary>
public class CellPosition
{
    public int Row { get; set; }
    public int Column { get; set; }

    public override string ToString() => $"[{Row},{Column}]";
}
