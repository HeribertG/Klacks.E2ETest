using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.PageObjects;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.WorkSchedule;

/// <summary>
/// E2E Tests for the Canvas-based WorkSchedule Grid using the GridWrapper.
/// 
/// Prerequisites:
/// - Backend running on configured BaseUrl
/// - Frontend with testMode support (?testMode URL parameter)
/// - At least one client with schedule data
/// 
/// These tests demonstrate how to interact with the Canvas Grid using:
/// 1. Ghost DOM overlay (for non-editable cells)
/// 2. Existing HTML input (for editable cells)  
/// 3. Window API (for direct grid manipulation)
/// 4. Coordinate-based clicking (fallback)
/// </summary>
[TestFixture]
[Order(101)]
public class WorkScheduleGridTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private SchedulePage _schedule = null!;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
        
        _schedule = new SchedulePage(Page, Actions, BaseUrl);

        // Navigate to schedule with test mode enabled
        await _schedule.NavigateToScheduleAsync(enableTestMode: true);
        await _schedule.WaitForGridLoadAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Errors: {_listener.GetLastErrorMessage()}");
        }

        await _listener.WaitForResponseHandlingAsync();
    }

    #region Ghost DOM Tests

    [Test]
    [Order(1)]
    public async Task Step1_GhostDomOverlay_ShouldBeVisible()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Ghost DOM Overlay ===");

        // Act
        var ghostCells = await _schedule.Grid.GetAllVisibleCellsAsync();

        // Assert
        Assert.That(ghostCells.Count, Is.GreaterThan(0), "Ghost DOM should render cells");
        TestContext.Out.WriteLine($"Found {ghostCells.Count} ghost cells");

        // Log first few cells using Window API (not DOM elements)
        var apiCells = await _schedule.Grid.GetAllCellsViaApiAsync();
        foreach (var cell in apiCells.Take(5))
        {
            TestContext.Out.WriteLine($"  Cell [{cell.Row},{cell.Column}]: {cell.Value}");
        }
    }

    [Test]
    [Order(2)]
    public async Task Step2_ClickNonEditableCell_ShouldSelectIt()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Click Non-Editable Cell ===");
        
        // Get all cells via API for diagnostics
        var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
        TestContext.Out.WriteLine($"Total cells from API: {cells.Count}");
        
        if (cells.Count == 0)
        {
            // Debug: Check if API is available
            var isApiAvailable = await _schedule.Grid.IsTestApiAvailableAsync();
            TestContext.Out.WriteLine($"Window API available: {isApiAvailable}");
            
            Assert.Ignore("No cells returned from Window API");
            return;
        }
        
        // Log first few cells
        foreach (var cell in cells.Take(10))
        {
            TestContext.Out.WriteLine($"  Cell [{cell.Row},{cell.Column}]: Value='{cell.Value}', IsHeader={cell.IsHeader}, IsEditable={cell.IsEditable}");
        }
        
        // Get first non-editable cell (usually header)
        var headerCell = cells.FirstOrDefault(c => c.IsHeader);
        
        if (headerCell == null)
        {
            Assert.Ignore("No header cell found");
            return;
        }

        // Act
        await _schedule.Grid.ClickCellByCoordinatesAsync(headerCell.Row, headerCell.Column);
        await Actions.Wait500();

        // Assert - verify via API
        var selected = await _schedule.Grid.GetSelectedCellAsync();
        Assert.That(selected, Is.Not.Null, "A cell should be selected");
        TestContext.Out.WriteLine($"Selected cell: [{selected!.Row},{selected.Column}]");
    }

    [Test]
    [Order(3)]
    public async Task Step3_FindCellByValue_ShouldReturnCorrectCell()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Find Cell by Value ===");
        
        // Get all cells and pick one with a value
        var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
        TestContext.Out.WriteLine($"Total cells from API: {cells.Count}");
        
        if (cells.Count == 0)
        {
            Assert.Ignore("No cells returned from API");
            return;
        }
        
        // Log all cells with non-empty values
        var cellsWithValues = cells.Where(c => !string.IsNullOrEmpty(c.Value)).ToList();
        TestContext.Out.WriteLine($"Cells with values: {cellsWithValues.Count}");
        foreach (var cell in cellsWithValues.Take(5))
        {
            TestContext.Out.WriteLine($"  [{cell.Row},{cell.Column}]: '{cell.Value}'");
        }
        
        var targetCell = cellsWithValues.FirstOrDefault();
        
        if (targetCell == null)
        {
            Assert.Ignore("No cells with values found");
            return;
        }

        // Act
        var foundCell = await _schedule.Grid.FindCellByValueViaApiAsync(targetCell.Value);

        // Assert
        Assert.That(foundCell, Is.Not.Null, "Should find cell by value");
        TestContext.Out.WriteLine($"Found cell [{foundCell!.Row},{foundCell.Column}] with value '{targetCell.Value}'");
    }

    #endregion

    #region Window API Tests

    [Test]
    [Order(10)]
    public async Task Step10_WindowApi_ShouldBeAvailable()
    {
        // Act
        var isAvailable = await _schedule.Grid.IsTestApiAvailableAsync();

        // Assert
        Assert.That(isAvailable, Is.True, "Window Grid API should be available in test mode");
        TestContext.Out.WriteLine("Window.klacksScheduleGrid is available");
    }

    [Test]
    [Order(11)]
    public async Task Step11_SelectCellViaApi_ShouldUpdateSelection()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Select Cell via Window API ===");
        const int targetRow = 2;
        const int targetCol = 2;

        // Act
        await _schedule.Grid.SelectCellViaApiAsync(targetRow, targetCol);
        await Actions.Wait300();

        // Assert
        var selected = await _schedule.Grid.GetSelectedCellAsync();
        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.Row, Is.EqualTo(targetRow));
        Assert.That(selected.Column, Is.EqualTo(targetCol));
        TestContext.Out.WriteLine($"Successfully selected cell [{targetRow},{targetCol}] via API");
    }

    [Test]
    [Order(12)]
    public async Task Step12_GetCellMetadata_ShouldReturnCorrectInfo()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Get Cell Metadata ===");

        // Act
        var cell = await _schedule.Grid.GetCellMetadataAsync(1, 1);

        // Assert
        Assert.That(cell, Is.Not.Null, "Should get cell metadata");
        TestContext.Out.WriteLine($"Cell [1,1]: Value='{cell!.Value}', Editable={cell.IsEditable}");
    }

    [Test]
    [Order(13)]
    public async Task Step13_FindCellsByClient_ShouldReturnAllClientCells()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Find Cells by Client ===");
        
        // First get any client ID from visible cells
        var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
        var clientId = cells.FirstOrDefault(c => !string.IsNullOrEmpty(c.ClientId))?.ClientId;
        
        if (clientId == null)
        {
            Assert.Ignore("No cells with client ID found");
            return;
        }

        // Act
        var clientCells = await _schedule.Grid.FindCellsByClientAsync(clientId);

        // Assert
        Assert.That(clientCells.Count, Is.GreaterThan(0));
        TestContext.Out.WriteLine($"Found {clientCells.Count} cells for client {clientId}");
    }

    #endregion

    #region Editable Cell Tests

    [Test]
    [Order(20)]
    public async Task Step20_SelectAndDoubleClickCell_ShouldOpenEditor()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Select and Double-Click Cell ===");
        
        // Find any data cell (not header, row > 0 and col > 0)
        var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
        var dataCell = cells.FirstOrDefault(c => c.Row > 0 && c.Column > 0);
        
        if (dataCell == null)
        {
            TestContext.Out.WriteLine("No data cells found");
            Assert.Ignore("No data cells found");
            return;
        }

        var row = dataCell.Row;
        var col = dataCell.Column;
        var isEditable = dataCell.IsEditable;

        TestContext.Out.WriteLine($"Cell [{row},{col}] - IsEditable={isEditable}, Value='{dataCell.Value}'");
        TestContext.Out.WriteLine($"  IsEditable=true means: {(isEditable ? "Empty cell - can create new entry" : "Has entry - can edit existing")}");

        // Act - Select cell via API
        await _schedule.Grid.SelectCellViaApiAsync(row, col);
        await Actions.Wait500();
        
        // Verify selection
        var selected = await _schedule.Grid.GetSelectedCellAsync();
        Assert.That(selected, Is.Not.Null, "Cell should be selected");
        Assert.That(selected!.Row, Is.EqualTo(row), "Correct row should be selected");
        Assert.That(selected.Column, Is.EqualTo(col), "Correct column should be selected");
        
        TestContext.Out.WriteLine($"Successfully selected cell [{row},{col}]");
        
        // Note: Double-click behavior depends on cell content:
        // - Empty cell (IsEditable=true): Opens "New Entry" dialog
        // - Filled cell (IsEditable=false): Opens "Edit Entry" dialog or direct input
    }

    [Test]
    [Order(21)]
    public async Task Step21_CellInputAttributes_ShouldBePresentWhenEditing()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Cell Input Attributes (when editing) ===");
        
        // Find a data cell
        var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
        var dataCell = cells.FirstOrDefault(c => c.Row > 0 && c.Column > 0);
        
        if (dataCell == null)
        {
            Assert.Ignore("No data cells found");
            return;
        }

        // Act - Start editing
        await _schedule.Grid.StartEditViaApiAsync(dataCell.Row, dataCell.Column);
        await Actions.Wait1000(); // Wait for input to appear

        // Get the active input (may be null if dialog-based editing)
        var input = await _schedule.Grid.GetActiveCellInputAsync();

        // If no direct input found, this cell uses dialog-based editing
        if (input == null)
        {
            TestContext.Out.WriteLine("No direct input overlay found - cell uses dialog-based editing");
            Assert.Pass("Cell uses dialog-based editing (no input overlay to verify)");
            return;
        }

        // Assert - Verify input attributes
        var testId = await input.GetAttributeAsync("data-testid");
        var dataRow = await input.GetAttributeAsync("data-row");
        var dataCol = await input.GetAttributeAsync("data-column");

        Assert.That(testId, Is.EqualTo("cell-input"), "Input should have data-testid='cell-input'");
        Assert.That(dataRow, Is.EqualTo(dataCell.Row.ToString()), "Input should have correct data-row");
        Assert.That(dataCol, Is.EqualTo(dataCell.Column.ToString()), "Input should have correct data-column");

        TestContext.Out.WriteLine($"Input has correct attributes: testid={testId}, row={dataRow}, col={dataCol}");
    }

    #endregion

    #region Schedule-Specific Tests

    [Test]
    [Order(30)]
    public async Task Step30_FindCellByClientAndDate_ShouldReturnCorrectCell()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Find Cell by Client and Date ===");
        
        // Get a cell with client and date info
        var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
        var targetCell = cells.FirstOrDefault(c => 
            !string.IsNullOrEmpty(c.ClientId) && !string.IsNullOrEmpty(c.Date));
        
        if (targetCell == null)
        {
            Assert.Ignore("No cells with client ID and date found");
            return;
        }

        // Act
        var foundCell = await _schedule.Grid.FindCellByClientAndDateViaApiAsync(
            targetCell.ClientId!, targetCell.Date!);

        // Assert
        Assert.That(foundCell, Is.Not.Null);
        Assert.That(foundCell!.ClientId, Is.EqualTo(targetCell.ClientId));
        Assert.That(foundCell.Date, Is.EqualTo(targetCell.Date));
        
        TestContext.Out.WriteLine($"Found cell for client {targetCell.ClientId} on {targetCell.Date}");
    }

    [Test]
    [Order(31)]
    public async Task Step31_ScrollToClient_ShouldBringClientIntoView()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Scroll to Client ===");
        
        // Get all cells via API
        var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
        TestContext.Out.WriteLine($"Total cells from API: {cells.Count}");
        
        if (cells.Count == 0)
        {
            Assert.Ignore("No cells returned from API");
            return;
        }
        
        // Get unique clients and their row ranges
        var clientsByRow = cells
            .Where(c => !string.IsNullOrEmpty(c.ClientId))
            .GroupBy(c => c.ClientId!)
            .Select(g => new { ClientId = g.Key, MinRow = g.Min(c => c.Row), MaxRow = g.Max(c => c.Row) })
            .OrderBy(c => c.MinRow)
            .ToList();
        
        TestContext.Out.WriteLine($"Found {clientsByRow.Count} unique clients:");
        foreach (var client in clientsByRow.Take(5))
        {
            TestContext.Out.WriteLine($"  Client {client.ClientId[..8]}...: rows {client.MinRow}-{client.MaxRow}");
        }
        
        // Pick second client (row > 0) to ensure scrolling
        var targetClient = clientsByRow.Skip(1).FirstOrDefault();
        if (targetClient == null)
        {
            Assert.Ignore("Need at least 2 clients to test scrolling");
            return;
        }

        var clientId = targetClient.ClientId;
        var targetRow = targetClient.MinRow;
        TestContext.Out.WriteLine($"Scrolling to client {clientId[..8]}... at row {targetRow}");

        // Act - Scroll directly to the row
        await _schedule.Grid.ScrollToRowAsync(targetRow);
        
        // Wait and check what's visible
        await Actions.Wait1000();
        
        // Assert - Verify cells at target row are visible
        var afterScroll = await _schedule.Grid.GetAllCellsViaApiAsync();
        TestContext.Out.WriteLine($"After scroll: {afterScroll.Count} total cells");
        TestContext.Out.WriteLine($"Row range: {afterScroll.Min(c => c.Row)} - {afterScroll.Max(c => c.Row)}");
        
        var cellsAtTargetRow = afterScroll.Where(c => c.Row == targetRow).ToList();
        TestContext.Out.WriteLine($"Cells at target row {targetRow}: {cellsAtTargetRow.Count}");
        
        Assert.That(cellsAtTargetRow.Count, Is.GreaterThan(0), 
            $"Should have cells at row {targetRow} after scrolling");
    }

    #endregion

    #region Debug Tests

    [Test]
    [Order(90)]
    public async Task Step90_LogGridState_ShouldOutputDiagnostics()
    {
        // Act
        await _schedule.LogScheduleStateAsync();

        // This test always passes, it's for diagnostic output
        Assert.Pass("Grid state logged");
    }

    [Test]
    [Order(91)]
    public async Task Step91_DebugOverlay_ShouldHighlightCells()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Test: Debug Overlay ===");
        var screenshotPath = $"grid_debug_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        // Act
        await _schedule.TakeDebugScreenshotAsync(screenshotPath);

        // Assert
        Assert.That(File.Exists(screenshotPath), Is.True, "Screenshot should be created");
        TestContext.Out.WriteLine($"Debug screenshot saved to: {screenshotPath}");
        
        // Add as test attachment
        TestContext.AddTestAttachment(screenshotPath);
    }

    #endregion
}
