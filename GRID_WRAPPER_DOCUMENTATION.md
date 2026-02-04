# GridWrapper Dokumentation

## Übersicht

Der `GridWrapper` ist eine spezialisierte Wrapper-Klasse für das Canvas-basierte Grid im WorkSchedule. Da das Grid auf HTML5 Canvas gerendert wird, sind Standard-DOM-Selektoren nicht möglich.

## Architektur

```
┌─────────────────────────────────────────────────────────────┐
│                     WorkSchedule Page                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Canvas (gerenderte Zellen)                         │   │
│  │  ┌─────┬─────┬─────┐                               │   │
│  │  │     │     │     │  ← Canvas Drawing             │   │
│  │  └─────┴─────┴─────┘                               │   │
│  └─────────────────────────────────────────────────────┘   │
│                           +                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Ghost DOM (nicht-editierbare Zellen)               │   │
│  │  ┌─────┐         ┌─────┐                           │   │
│  │  │ div │         │ div │  ← data-testid="cell-x-y" │   │
│  │  └─────┘         └─────┘                           │   │
│  └─────────────────────────────────────────────────────┘   │
│                           +                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  HTML Input (editierbare Zellen)                    │   │
│  │  ┌─────────┐                                        │   │
│  │  │ <input> │  ← data-testid="cell-input"            │   │
│  │  └─────────┘                                        │   │
│  └─────────────────────────────────────────────────────┘   │
│                           +                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Window API                                         │   │
│  │  window.__GRID_TEST_API__                           │   │
│  │  - getCellAt(row, col)                              │   │
│  │  - selectCell(row, col)                             │   │
│  │  - getAllCells()                                    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Verwendung

### Grundlegende Einrichtung

```csharp
using Klacks.E2ETest.Wrappers;
using Klacks.E2ETest.PageObjects;

[TestFixture]
public class MyTest : PlaywrightSetup
{
    private SchedulePage _schedule;

    [SetUp]
    public async Task Setup()
    {
        _schedule = new SchedulePage(Page, Actions);
        await _schedule.NavigateToScheduleAsync(enableTestMode: true);
        await _schedule.WaitForGridLoadAsync();
    }
}
```

### Ghost DOM Interaktionen

```csharp
// Auf Ghost DOM warten
await _schedule.Grid.WaitForGhostDomAsync();

// Zelle anhand von Koordinaten klicken
await _schedule.Grid.ClickCellByCoordinatesAsync(row: 2, column: 3);

// Zelle anhand von Wert finden
var cell = await _schedule.Grid.FindCellByValueAsync("09:00-17:00");

// Alle sichtbaren Ghost-Zellen holen
var cells = await _schedule.Grid.GetAllVisibleCellsAsync();
```

### Window API Nutzung

```csharp
// Prüfen ob API verfügbar
var isAvailable = await _schedule.Grid.IsTestApiAvailableAsync();

// Zell-Metadaten abrufen
var cell = await _schedule.Grid.GetCellMetadataAsync(2, 3);
// → { Row: 2, Column: 3, Value: "09:00-17:00", IsEditable: true, ... }

// Zelle via API auswählen (schneller als Click)
await _schedule.Grid.SelectCellViaApiAsync(2, 3);

// Bearbeitung starten
await _schedule.Grid.StartEditViaApiAsync(2, 3);

// Alle Zellen eines Clients finden
var clientCells = await _schedule.Grid.FindCellsByClientAsync("client-123");
```

### Editierbare Zellen

```csharp
// Editierbare Zelle bearbeiten
await _schedule.EditWorkEntryAsync(row: 2, column: 3, newValue: "Test123");

// Oder manuell:
await _schedule.Grid.StartEditViaApiAsync(2, 3);
var input = await _schedule.Grid.GetActiveCellInputAsync();
await _schedule.Grid.TypeIntoActiveCellAsync("Neuer Wert");
await _schedule.Grid.PressKeyInActiveCellAsync("Enter");
```

### Koordinaten-basiertes Klicken

```csharp
// Direkt auf Canvas klicken (Fallback)
await _schedule.Grid.ClickOnCanvasAsync(x: 150, y: 100);

// Doppelklick
await _schedule.Grid.DoubleClickOnCanvasAsync(x: 150, y: 100);
```

### Scrollen

```csharp
// Vertikal scrollen
await _schedule.Grid.ScrollVerticalAsync(rows: 5);

// Zu bestimmter Zeile scrollen
await _schedule.Grid.ScrollToRowAsync(targetRow: 10);

// Zu Client scrollen
await _schedule.ScrollToClientAsync("client-123");
```

### Debugging

```csharp
// Grid-Zustand loggen
await _schedule.LogScheduleStateAsync();

// Debug-Overlay aktivieren (rote Rahmen)
await _schedule.Grid.EnableDebugOverlayAsync();

// Screenshot mit Overlay
await _schedule.Grid.TakeGridScreenshotAsync("debug.png");

// Debug-Overlay deaktivieren
await _schedule.Grid.DisableDebugOverlayAsync();
```

## Test-Modus aktivieren

Der Grid Test-Modus muss aktiviert sein, damit Ghost DOM und Window API funktionieren:

```csharp
// Option 1: URL Parameter
await Page.GotoAsync("workplace/schedule?testMode");

// Option 2: Über SchedulePage
await _schedule.NavigateToScheduleAsync(enableTestMode: true);
```

## Datenstrukturen

### GridCellInfo

```csharp
public class GridCellInfo
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string Value { get; set; }
    public string TestId { get; set; }
    public bool IsEditable { get; set; }
    public bool IsVisible { get; set; }
    public bool IsHeader { get; set; }
    
    // Schedule-spezifisch
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? Date { get; set; }
    public string? EntryType { get; set; }
    public string? ShiftId { get; set; }
}
```

### CellPosition

```csharp
public class CellPosition
{
    public int Row { get; set; }
    public int Column { get; set; }
}
```

## Strategien für Canvas-Testing

### Strategie 1: Ghost DOM (empfohlen für nicht-editierbare Zellen)

```csharp
// Für Header-Zellen und nicht-editierbare Inhalte
await _schedule.Grid.ClickCellByCoordinatesAsync(0, 1);
var cell = await _schedule.Grid.FindCellByValueAsync("Client Name");
```

### Strategie 2: Window API (empfohlen für Zell-Informationen)

```csharp
// Für schnelle Abfragen und Aktionen
var cell = await _schedule.Grid.GetCellMetadataAsync(2, 3);
await _schedule.Grid.SelectCellViaApiAsync(2, 3);
```

### Strategie 3: HTML Input (für editierbare Zellen)

```csharp
// Für Bearbeitung von Zellen
await _schedule.Grid.StartEditViaApiAsync(2, 3);
var input = await _schedule.Grid.GetActiveCellInputAsync();
await input.FillAsync("Neuer Wert");
```

### Strategie 4: Koordinaten (Fallback)

```csharp
// Wenn andere Methoden nicht funktionieren
await _schedule.Grid.ClickOnCanvasAsync(150, 100);
```

## Fehlerbehebung

### Ghost DOM wird nicht angezeigt

```csharp
// Prüfen ob Test-Modus aktiv
var isApiAvailable = await _schedule.Grid.IsTestApiAvailableAsync();
if (!isApiAvailable)
{
    TestContext.Out.WriteLine("Test-Modus nicht aktiv! URL muss ?testMode enthalten.");
}
```

### Zelle nicht gefunden

```csharp
// Alle sichtbaren Zellen loggen
var cells = await _schedule.Grid.GetAllCellsViaApiAsync();
foreach (var cell in cells)
{
    TestContext.Out.WriteLine($"[{cell.Row},{cell.Column}] = {cell.Value}");
}
```

### Input nicht sichtbar

```csharp
// Prüfen ob Zelle editierbar ist
var cell = await _schedule.Grid.GetCellMetadataAsync(2, 3);
if (!cell.IsEditable)
{
    TestContext.Out.WriteLine("Zelle ist nicht editierbar!");
}
```

## Best Practices

1. **Immer Test-Modus aktivieren**: `?testMode` in URL
2. **Ghost DOM bevorzugen**: Für nicht-editierbare Zellen
3. **Window API für Daten**: Schneller als DOM-Abfragen
4. **Input für Bearbeitung**: Nutzt bestehende UI
5. **Wartezeiten**: Nach Grid-Operationen `Wait100()` oder `Wait500()`
6. **Debug-Overlay**: Bei Fehlern aktivieren für Screenshots

## Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Wrappers/GridWrapper.cs` | Haupt-Wrapper-Klasse |
| `PageObjects/SchedulePage.cs` | Page Object für Schedule |
| `WorkSchedule/WorkScheduleGridTest.cs` | Beispiel-Tests |
