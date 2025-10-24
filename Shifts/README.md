# E2E Tests für Shift Cutting (Dienst Zerteilen)

**Erstellt:** 23.10.2025
**Framework:** Playwright + NUnit
**Test-Kategorien:** Basic Cutting, Nested Cutting, Batch Cutting

---

## Übersicht

Diese E2E-Tests decken die komplette Shift-Cutting-Funktionalität ab, inklusive:

1. **EBENE 0 Cutting**: Erstes Zerteilen eines Shifts (Root-Level)
2. **EBENE 1+ Cutting**: Verschachteltes Zerteilen (Nested Cutting)
3. **Batch Cutting**: Paralleles Schneiden auf mehreren Ebenen ohne Zwischenspeichern

## Test-Dateien

### 1. ShiftCutsBasicTest.cs - Grundlegende Cutting-Szenarien

**Abdeckung:**
- ✅ Create Original Order Shift (Status = 0)
- ✅ Seal Shift Order (Status 0 → 1, Backend erstellt Status 2)
- ✅ Navigate to Cut Shift Page
- ✅ Cut by Time (EBENE 0)
- ✅ Cut by Date (EBENE 0)
- ✅ Cut by Weekdays (EBENE 0)
- ✅ Verify Cuts in Database

**Wichtige Tests:**
- `Step1_CreateOriginalOrderShift`: Erstellt neuen Shift mit Status = OriginalOrder
- `Step2_SealShiftOrder`: Versiegelt Shift, Backend erstellt OriginalShift-Kopie
- `Step4_CutByTime_CreateTwoCuts`: Zerteilt Shift zeitlich (08:00-16:00 → 08:00-12:00 + 12:00-16:00)
- `Step5_VerifyCutsInDatabase`: Prüft dass alle Cuts Status = SplitShift haben

**Erwartete Tree-Struktur nach EBENE 0 Cuts:**
```
Cut 1: lft=1, rgt=2, parent_id=NULL, root_id=own ID
Cut 2: lft=1, rgt=2, parent_id=NULL, root_id=own ID
```

### 2. ShiftCutsNestedTest.cs - Verschachteltes Schneiden

**Abdeckung:**
- ✅ Create EBENE 0 Cuts (3 Cuts: 07-12, 12-15, 15-19)
- ✅ Cut EBENE 0 again to create EBENE 1 (Nested)
- ✅ Verify Nested Tree Structure
- ✅ Verify API Response for Nested Cuts

**Wichtige Tests:**
- `Step2_CreateEBENE0Cuts`: Erstellt 3 Root-Level Cuts
- `Step3_CutEBENE0AgainToCreateEBENE1`: Schneidet einen EBENE 0 Cut nochmals
- `Step4_VerifyNestedTreeStructure`: Dokumentiert erwartete Tree-Struktur

**Erwartete Tree-Struktur nach EBENE 1 Cuts:**
```
EBENE 0 (Root):
  Cut A (07-12): lft=1, rgt=6, parent_id=NULL, root_id=own ID
    └─ EBENE 1:
       Cut A1 (07-09:30): lft=2, rgt=3, parent_id=Cut A, root_id=Cut A
       Cut A2 (09:30-12): lft=4, rgt=5, parent_id=Cut A, root_id=Cut A
  Cut B (12-15): lft=1, rgt=2, parent_id=NULL, root_id=own ID
  Cut C (15-19): lft=1, rgt=2, parent_id=NULL, root_id=own ID
```

### 3. ShiftCutsBatchTest.cs - Batch/Parallel Schneiden (Szenario 2)

**Abdeckung:**
- ✅ Multiple Cuts WITHOUT Saving (Temp-IDs)
- ✅ Save All Cuts in ONE Batch Operation
- ✅ Verify Batch Save Results
- ✅ Verify Topological Sort worked

**Wichtige Tests:**
- `Step2_PerformMultipleCutsWithoutSaving`: Schneidet auf mehreren Ebenen ohne zu speichern
- `Step3_SaveAllCutsInBatch`: Speichert alle Cuts in einem Request (PostBatchCutsCommandHandler)
- `Step5_VerifyTopologicalSortWorked`: Dokumentiert wie Topological Sort funktioniert

**Backend-Flow bei Batch Save:**
```typescript
// 1. Frontend sendet List<CutOperation> mit temp-IDs
{
  operations: [
    { type: "UPDATE", shiftId: "real-1", parentId: "sealed-order-id", data: {...} },
    { type: "CREATE", tempId: "temp-1", parentId: "real-1", data: {...} },
    { type: "CREATE", tempId: "temp-2", parentId: "real-1", data: {...} },
    { type: "CREATE", tempId: "temp-3", parentId: "temp-1", data: {...} }, // ← Dependency!
    { type: "CREATE", tempId: "temp-4", parentId: "temp-2", data: {...} }  // ← Dependency!
  ]
}

// 2. Backend macht Topological Sort (Parents vor Children)

// 3. Backend löst temp-IDs zu echten IDs auf

// 4. Backend ruft ShiftTreeService für tree-Felder auf

// 5. Alle Cuts gespeichert in einer Transaction
```

---

## Test-Ausführung

### Voraussetzungen

1. **Backend läuft** auf `https://localhost:5001` oder `http://157.180.42.127:5000`
2. **Frontend läuft** auf konfigurierter BaseUrl (siehe `appsettings.json`)
3. **Playwright** ist installiert
4. **User Credentials** sind in `appsettings.json` oder User Secrets konfiguriert

### Konfiguration (appsettings.json)

```json
{
  "user": "your-email@example.com",
  "password": "your-password",
  "PlaywrightConfig": {
    "BaseUrl": "https://localhost:4200/",
    "HeadLess": true,
    "RecordVideo": false,
    "RecordAllTests": false
  }
}
```

### Tests ausführen

**Alle Shift-Cut Tests:**
```bash
cd /mnt/c/SourceCode/E2ETest
dotnet test --filter "FullyQualifiedName~ShiftCuts"
```

**Nur Basic Tests:**
```bash
dotnet test --filter "FullyQualifiedName~ShiftCutsBasicTest"
```

**Nur Nested Tests:**
```bash
dotnet test --filter "FullyQualifiedName~ShiftCutsNestedTest"
```

**Nur Batch Tests:**
```bash
dotnet test --filter "FullyQualifiedName~ShiftCutsBatchTest"
```

**Mit Video-Recording (nur bei Failures):**
```json
{
  "PlaywrightConfig": {
    "RecordVideo": true,
    "RecordAllTests": false
  }
}
```

**Mit Video-Recording (alle Tests):**
```json
{
  "PlaywrightConfig": {
    "RecordVideo": true,
    "RecordAllTests": true
  }
}
```

---

## Test-Szenarien im Detail

### Szenario 1: Basic Time Cut (EBENE 0)

```
User Flow:
1. Create Shift "Test 1" (08:00-16:00)
2. Seal Shift (Backend erstellt OriginalShift copy)
3. Navigate to Cut Page
4. Cut by Time at 12:00
5. Save

Expected Result:
- 2 SplitShifts: (08:00-12:00) + (12:00-16:00)
- Beide: Status=3, lft=1, rgt=2, parent_id=NULL, root_id=own ID
- SealedOrder: is_deleted=true
```

### Szenario 2: Nested Cutting (EBENE 0 → EBENE 1)

```
User Flow:
1. Create Shift "Test 2" (07:00-19:00)
2. Seal Shift
3. Cut at 12:00 and 15:00 → Creates 3 EBENE 0 cuts
4. Save
5. Select first cut (07:00-12:00)
6. Cut again at 09:30 → Creates 2 EBENE 1 cuts
7. Save

Expected Result:
- 5 SplitShifts total:
  - 2 EBENE 0: (12:00-15:00), (15:00-19:00)
  - 1 EBENE 0 (Parent): (07:00-12:00) with lft=1, rgt=6
  - 2 EBENE 1 (Children): (07:00-09:30), (09:30-12:00)
    - Both have: parent_id=Parent ID, root_id=Parent ID
```

### Szenario 3: Batch Cutting (Paralleles Schneiden)

```
User Flow:
1. Create Shift "Test 3" (06:00-22:00)
2. Seal Shift
3. Cut at 12:00 and 17:00 → Creates 3 EBENE 0 cuts (WITHOUT SAVING!)
4. Select first cut (06:00-12:00), cut at 09:00 (WITHOUT SAVING!)
5. Select second cut (12:00-17:00), cut at 14:30 (WITHOUT SAVING!)
6. Save ALL at once

Expected Result:
- 7 SplitShifts saved in ONE transaction via PostBatchCutsCommandHandler:
  - 2 EBENE 0 Parents with children
  - 1 EBENE 0 without children
  - 4 EBENE 1 Children total
- Backend did Topological Sort to ensure correct order
- Backend resolved all temp-IDs to real IDs
```

---

## Wichtige Hinweise

### ⚠️ Timing und Waits

Die Tests nutzen verschiedene Wait-Strategien:
- `WaitForSpinnerToDisappear()`: Wartet auf Spinner (Daten-Laden)
- `Wait500()`, `Wait1000()`, etc.: Feste Delays für UI-Updates
- `WaitForURLAsync()`: Wartet auf Navigation
- `WaitForSelectorAsync()`: Wartet auf DOM-Element

### ⚠️ Element-Selection

Die Tests nutzen flexible Selektoren um verschiedene Sprachen (DE/EN) zu unterstützen:
```csharp
// Beispiel: Button-Suche
"button:has-text('Neu'), button:has-text('New')"
"button:has-text('Speichern'), button:has-text('Save')"
```

### ⚠️ API-Error-Monitoring

Alle Tests nutzen `Listener` um API-Fehler zu erkennen:
```csharp
[TearDown]
public async Task TearDown()
{
    if (_listener.HasApiErrors())
    {
        TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
    }
    await _listener.WaitForResponseHandlingAsync();
}
```

### ⚠️ Test-Reihenfolge

Die Tests sind mit `[Test, Order(n)]` markiert um sequenzielle Ausführung zu garantieren.

---

## Troubleshooting

### Test schlägt fehl: "Element not found"

**Problem:** UI-Elemente haben andere IDs/Selektoren als erwartet
**Lösung:**
1. Prüfe `ShiftIds.cs` Konstanten
2. Aktualisiere Selektoren in den Tests
3. Nutze `QuerySelectorAsync()` für flexible Suche

### Test schlägt fehl: "API Error"

**Problem:** Backend-API gibt Fehler zurück
**Lösung:**
1. Prüfe Backend-Logs
2. Prüfe `_listener.GetLastErrorMessage()`
3. Verifiziere dass Backend-Services laufen

### Test schlägt fehl: "Timeout"

**Problem:** Seite lädt zu langsam oder Spinner verschwindet nicht
**Lösung:**
1. Erhöhe Timeout in `WaitForSelectorAsync()`
2. Prüfe Netzwerk-Performance
3. Prüfe ob Spinner-Logic korrekt funktioniert

### Video zeigt unerwartetes Verhalten

**Problem:** Test-Ausführung sieht anders aus als erwartet
**Lösung:**
1. Prüfe `SlowMo` Setting in `PlaywrightSetup.cs`
2. Reduziere `HeadLess` auf `false` für Debugging
3. Erhöhe `RecordVideo` Settings

---

## Weitere Dokumentation

Für Details zur Shift-Cutting-Implementierung siehe:
- 📄 [SHIFT_DOCUMENTATION.md](/mnt/c/SourceCode/SHIFT_DOCUMENTATION.md) - Vollständige Shift-Workflow-Dokumentation
- 📄 [WORK_PLANNING_DOCUMENTATION.md](/mnt/c/SourceCode/WORK_PLANNING_DOCUMENTATION.md) - Unterschied Beschäftigungen vs. Dienste

---

**Status:** ✅ Tests erstellt und bereit für Ausführung
**Nächster Schritt:** Manuelle Ausführung und Anpassung an tatsächliche UI-Elemente
