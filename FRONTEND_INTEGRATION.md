# Frontend Integration für C# GridWrapper

Diese Dokumentation beschreibt, wie das Frontend (Angular/TypeScript) mit dem C# GridWrapper kommuniziert.

## Architektur

```
┌─────────────────────────────────────────────────────────────────┐
│                         C# E2E Test                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  GridWrapper.cs                                         │   │
│  │  - GetCellMetadataAsync(row, col)                       │   │
│  │  - ClickCellByCoordinatesAsync(row, col)                │   │
│  │  - SelectCellViaApiAsync(row, col)                      │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│                              ▼ Playwright                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Angular Frontend                            │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Window API: window.__GRID_TEST_API__                   │   │
│  │  ├─ getCellAt(row, col) → CellInfo                      │   │
│  │  ├─ selectCell(row, col) → Position                     │   │
│  │  ├─ startEdit(row, col) → Position                      │   │
│  │  ├─ getAllCells() → CellInfo[]                          │   │
│  │  ├─ findCellsByClient(clientId) → CellInfo[]            │   │
│  │  └─ scrollToRow(row) → void                             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────┼─────────────────────────────┐   │
│  │  Ghost DOM Overlay       │     HTML Input Overlay      │   │
│  │  (non-editable cells)    │     (editable cells)        │   │
│  │  ├─ data-testid          │     ├─ data-testid          │   │
│  │  ├─ data-row             │     ├─ data-row             │   │
│  │  ├─ data-column          │     ├─ data-column          │   │
│  │  ├─ data-value           │     └─ aria-label           │   │
│  │  ├─ data-client-id       │                             │   │
│  │  └─ data-date            │                             │   │
│  └──────────────────────────┴─────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Window API Schnittstelle

Das Frontend stellt folgende API über `window.__GRID_TEST_API__` bereit:

### Cell Queries

```typescript
getCellAt(row: number, column: number): CellInfo | undefined
getCellByTestId(testId: string): CellInfo | undefined
getCellByValue(value: string): CellInfo | undefined
getCellByClientAndDate(clientId: string, date: string): CellInfo | undefined
getAllCells(): CellInfo[]
getVisibleCells(): CellInfo[]
```

### State Queries

```typescript
getSelectedCell(): { row: number; column: number } | null
getEditingCell(): { row: number; column: number } | null
isEditing(): boolean
```

### Helper Methods

```typescript
findCellsByClient(clientId: string): CellInfo[]
findCellsByDate(date: string): CellInfo[]
```

### Actions

```typescript
selectCell(row: number, column: number): { row: number; column: number }
startEdit(row: number, column: number): { row: number; column: number }
scrollToRow(row: number): void
```

### Configuration

```typescript
setEnabled(enabled: boolean): void
isEnabled(): boolean
```

## CellInfo Interface

```typescript
interface CellInfo {
  row: number;              // Row index (0-based)
  column: number;           // Column index (0-based)
  value: string;            // Display value
  testId: string;           // Unique test ID (e.g., "cell-2-3")
  isEditable: boolean;      // Whether cell can be edited
  isVisible: boolean;       // Whether cell is in viewport
  isHeader?: boolean;       // Whether cell is a header
  x?: number;               // X position in pixels
  y?: number;               // Y position in pixels
  width?: number;           // Cell width in pixels
  height?: number;          // Cell height in pixels
  
  // Schedule-specific fields
  clientId?: string;        // Client ID (UUID)
  clientName?: string;      // Client display name
  date?: string;            // Date in YYYY-MM-DD format
  entryType?: string;       // 'work' | 'break' | 'workChange' | 'expenses' | 'empty'
  shiftId?: string;         // Shift ID (UUID)
}
```

## Ghost DOM Attribute

Ghost DOM Elemente (nicht-editierbare Zellen) haben folgende Attribute:

```html
<div
  class="test-cell"
  role="gridcell"                    <!-- oder 'columnheader' für Header -->
  data-testid="cell-{row}-{column}"  <!-- z.B. "cell-2-3" -->
  data-row="{row}"
  data-column="{column}"
  data-value="{value}"
  data-client-id="{clientId}"        <!-- optional -->
  data-client-name="{clientName}"    <!-- optional -->
  data-date="{date}"                 <!-- optional, YYYY-MM-DD -->
  data-entry-type="{entryType}"      <!-- optional -->
  data-shift-id="{shiftId}"          <!-- optional -->
  aria-label="{description}"
  aria-rowindex="{row + 1}"          <!-- 1-based für A11y -->
  aria-colindex="{column + 1}"       <!-- 1-based für A11y -->
  style="left: {x}px; top: {y}px; width: {width}px; height: {height}px;"
>
  <span class="sr-only">{value}</span>
</div>
```

## Input Overlay Attribute

Das aktive Input-Feld (für editierbare Zellen) hat folgende Attribute:

```html
<input
  type="text"
  class="cell-input-overlay"
  data-testid="cell-input"
  data-row="{row}"
  data-column="{column}"
  aria-label="Editing cell {row}-{column}"
  style="left: {x}px; top: {y}px; ..."
/>
```

## Scroll Position

Das Frontend exportiert die aktuelle Scroll-Position für Koordinaten-Berechnungen:

```typescript
// Globale Properties (für C# Wrapper)
window.gridHorizontalScroll  // Aktuelle horizontale Scroll-Position
window.gridVerticalScroll    // Aktuelle vertikale Scroll-Position
window.__GRID_SCROLL_H__     // Interner Wert
window.__GRID_SCROLL_V__     // Interner Wert
```

## Test Mode Aktivierung

Der Test-Modus wird automatisch aktiviert wenn:

1. URL Parameter `?testMode` vorhanden ist
2. `window.Cypress` definiert ist
3. `window.__PLAYWRIGHT__` definiert ist

```typescript
// In der Komponente:
const testMode = urlParams.has('testMode') || 
                 (typeof window.Cypress !== 'undefined') ||
                 (typeof window.__PLAYWRIGHT__ !== 'undefined');
```

## Implementierte Dateien (Frontend)

| Datei | Beschreibung |
|-------|-------------|
| `test-accessibility-v2.service.ts` | Service mit Window API und Ghost DOM Logik |
| `test-overlay.component.ts` | Ghost DOM Overlay Komponente |
| `grid-surface-template.component.ts` | Grid Template mit Test-Integration |

## Implementierte Dateien (C# E2E)

| Datei | Beschreibung |
|-------|-------------|
| `GridWrapper.cs` | Playwright Wrapper für Grid-Interaktionen |
| `SchedulePage.cs` | Page Object für Schedule-Seite |
| `WorkScheduleGridTest.cs` | Beispiel-Tests |

## Beispiel: Kompletter Datenfluss

### 1. Frontend initialisiert Test-Modus

```typescript
// grid-surface-template.component.ts
initializeTestAccessibility() {
  const testMode = urlParams.has('testMode');
  this.testAccessibility.setEnabled(testMode);
  this.setupTestApiHooks();
}
```

### 2. C# Test ruft API auf

```csharp
// WorkScheduleGridTest.cs
var cell = await _schedule.Grid.GetCellMetadataAsync(2, 3);
// → Playwright führt aus: window.__GRID_TEST_API__.getCellAt(2, 3)
```

### 3. Frontend liefert Daten

```typescript
// test-accessibility-v2.service.ts
getCellAt(row, column) {
  return this._gridMetadata().cells.get(`${row}-${column}`);
}
// → Returns: { row: 2, column: 3, value: "09:00-17:00", isEditable: true, ... }
```

### 4. C# Test klickt auf Zelle

```csharp
// GridWrapper.cs
await _schedule.Grid.ClickCellByCoordinatesAsync(2, 3);
// → Playwright: page.ClickAsync("[data-testid='cell-2-3']")
```

### 5. Frontend reagiert auf Click

```typescript
// test-overlay.component.ts
onCellClick(cell) {
  this.cellClick.emit({ row: cell.row, column: cell.column });
}
// → Grid-Surface empfängt Event und selektiert Zelle
```

## Debugging

### Im Browser (DevTools)

```javascript
// Prüfen ob API verfügbar
window.__GRID_TEST_API__ !== undefined

// Alle Zellen abrufen
window.__GRID_TEST_API__.getAllCells()

// Zelle selektieren
window.__GRID_TEST_API__.selectCell(2, 3)

// Bearbeitung starten
window.__GRID_TEST_API__.startEdit(2, 3)

// Scroll-Position prüfen
window.gridHorizontalScroll
window.gridVerticalScroll
```

### Im C# Test

```csharp
// Grid-Zustand loggen
await _schedule.LogScheduleStateAsync();

// Debug-Overlay aktivieren
await _schedule.Grid.EnableDebugOverlayAsync();

// Screenshot erstellen
await _schedule.Grid.TakeGridScreenshotAsync("debug.png");
```

## Fehlerbehebung

### API nicht verfügbar

```csharp
var isAvailable = await _schedule.Grid.IsTestApiAvailableAsync();
if (!isAvailable) {
    // Prüfen: Wurde ?testMode zur URL hinzugefügt?
}
```

### Ghost DOM nicht sichtbar

```javascript
// Im Browser prüfen:
document.querySelector('[data-testid="grid-overlay"]') !== null

// Oder:
window.__GRID_TEST_API__.isEnabled()
```

### Koordinaten falsch

```javascript
// Scroll-Position prüfen:
window.gridHorizontalScroll
window.gridVerticalScroll

// Zellen-Positionen prüfen:
window.__GRID_TEST_API__.getCellAt(2, 3).x
window.__GRID_TEST_API__.getCellAt(2, 3).y
```

## Änderungen am Frontend

### Kürzlich hinzugefügt (für C# Kompatibilität)

1. **scrollToRow API** - Ermöglicht programmatisches Scrollen
2. **__GRID_SCROLL_H__ / __GRID_SCROLL_V__** - Exportierte Scroll-Positionen
3. **data-client-name Attribut** - Für bessere Lesbarkeit in Tests
4. **aria-rowindex / aria-colindex** - Für Accessibility

### Kürzlich aktualisiert

1. **Input Overlay** - data-testid, data-row, data-column Attribute
2. **Ghost DOM** - Vollständige data-* Attribute für alle Schedule-Felder
3. **Window API** - Vollständige C# GridWrapper-Kompatibilität
