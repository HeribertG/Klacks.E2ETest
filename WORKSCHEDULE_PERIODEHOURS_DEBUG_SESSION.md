# WorkSchedule PeriodHours Debug Session

**Datum:** 23. Januar 2026
**Status:** In Bearbeitung - Fortsetzung morgen
**Problem:** WorkSchedule zeigt falsche PeriodHours (Hours und Surcharges) in der UI, obwohl Backend korrekte Werte zurückgibt

---

## Zusammenfassung des Problems

### Was funktioniert ✅
1. **Integration Tests (8/8 Tests bestanden)**
   - `BulkAddWorksIntegrationTests.cs`: Erstellt Works für Sa/So/Mo
   - `BulkDeleteWorksIntegrationTests.cs`: Löscht Works
   - **Ergebnisse korrekt:**
     - Samstag: 8h WorkTime, 0.8 Surcharges (10%)
     - Sonntag: 8h WorkTime, 0.8 Surcharges (10%)
     - Montag: 8h WorkTime, 0 Surcharges
     - **Total: 24h, 1.6 Surcharges** ✅

2. **Backend-API**
   - `BulkAddWorksCommandHandler` gibt korrekte `periodHours` zurück
   - Macro "AllShift" berechnet Zuschläge korrekt
   - `PeriodHoursService` summiert korrekt

3. **Backend-Code-Fixes durchgeführt**
   - `BulkAddWorksCommandHandler.cs`: Setzt `StartTime`/`EndTime` von Shift
   - `WorkRepository.cs`: Summiert `Work.Surcharges` korrekt
   - `PeriodHoursService.cs`: Summiert `Work.Surcharges` korrekt

### Was NICHT funktioniert ❌
- **Frontend-UI zeigt falsche PeriodHours**
- User berichtet: "Im realen Leben sind Hours und Surcharges falsch"
- **Diskrepanz zwischen Backend-Response und UI-Anzeige**

---

## Was heute erreicht wurde

### 1. Integration Tests erstellt und erfolgreich
**Dateien:**
- `/mnt/c/SourceCode/IntegrationTest/WorkSchedule/BulkAddWorksIntegrationTests.cs` ✅
- `/mnt/c/SourceCode/IntegrationTest/WorkSchedule/BulkDeleteWorksIntegrationTests.cs` ✅

**Test-Setup:**
- Echte Macro "AllShift" mit vollständigem VB-Script
- Echte Contract mit Surcharge-Raten (SaRate: 10%, SoRate: 10%)
- Echte Shift (8:00-16:00) mit MacroId
- Client mit ClientContract-Verknüpfung

**Alle Tests bestehen:**
```
BulkAddWorks_Saturday_ShouldReturnCorrectPeriodHours ✅
BulkAddWorks_Sunday_ShouldReturnCorrectPeriodHours ✅
BulkAddWorks_Monday_ShouldReturnCorrectPeriodHours ✅
BulkAddWorks_SaturdaySundayMonday_ShouldReturnCorrectPeriodHours ✅
BulkDelete_ShouldReturnCorrectPeriodHours_WithSurcharges ✅
BulkDelete_DeleteOnlySaturday_ShouldReturnCorrectSurcharges ✅
BulkDelete_DeleteAllWorks_ShouldReturnZeroPeriodHours ✅
BulkDelete_NonExistentWork_ShouldIncreaseFailedCount ✅
```

### 2. Backend-Fixes implementiert
**Datei:** `/mnt/c/SourceCode/Klacks.Api/Application/Handlers/Works/BulkAddWorksCommandHandler.cs`

**Problem:** Works wurden ohne StartTime/EndTime erstellt → Macro konnte nicht berechnen

**Lösung:**
```csharp
// ADDED: IShiftRepository injizieren
private readonly IShiftRepository _shiftRepository;

// ADDED: Shift fetchen vor Work-Erstellung
var shift = await _shiftRepository.Get(command.Request.ShiftId);

// ADDED: StartTime/EndTime von Shift setzen
var work = new Work
{
    // ...
    StartTime = shift.StartShift,  // ADDED
    EndTime = shift.EndShift,      // ADDED
};
```

### 3. E2E-Test erstellt (noch nicht produktiv nutzbar)
**Datei:** `/mnt/c/SourceCode/E2ETest/WorkSchedule/WorkScheduleBulkOperationsTest.cs`

**Aktueller Stand:**
- Öffnet Browser auf WorkSchedule-Seite ✅
- Fängt Console-Logs ab ✅
- Fängt API POST/DELETE Responses ab ✅
- Wartet 60 Sekunden auf manuelle User-Interaktion ✅
- **Problem:** Keine Logs gefangen (0 Console Logs, 0 API Responses) ❌

**Warum keine Logs:**
- WorkSchedule ist Canvas-basiert (kein HTML)
- Keine automatisierte Interaktion möglich mit Standard-E2E
- User wusste nicht, dass er manuell interagieren sollte

### 4. WorkSchedule Canvas-Architektur analysiert
**Wichtige Erkenntnisse:**
- **Canvas-Rendering:** `GridSurfaceTemplateComponent` + `BaseDrawScheduleService`
- **Click-Erkennung:** Koordinaten → Row/Column Berechnung
- **Fill-Handle:** 12px Hit-Area am Bottom-Right, Auto-Scroll, Drag-Handling
- **Console-Logs vorhanden:**
  ```typescript
  // In work-schedule-crud.service.ts
  console.log('BulkDelete Response:', response);
  console.log('PeriodHours from backend:', response.periodHours);
  console.log(`Setting periodHours for client ${clientId}:`, hours);
  ```

### 5. Dokumentation aktualisiert
**Dateien:**
- `/mnt/c/SourceCode/CLAUDE_QUICKREF.md` ✅
  - **E2E-Regel:** NIEMALS `Page` direkt verwenden, IMMER `Actions`
  - Server Start/Stop-Befehle dokumentiert
- `/mnt/c/SourceCode/E2ETest/Constants/WorkScheduleIds.cs` ✅

---

## Nächste Schritte (für morgen)

### Priority 1: Frontend-Analyse 🔴
**HAUPTPROBLEM:** Warum zeigt die UI falsche PeriodHours?

**Zu untersuchen:**
1. **Wo wird PeriodHours angezeigt?**
   - `schedule-schedule-row-header.component.ts` (Slot 3)
   - Welcher Signal/Observable wird verwendet?
   - `WorkScheduleLoaderService.periodHours`

2. **API-Response-Handling:**
   - `WorkScheduleCrudService.addWorkScheduleEntry()`: Zeile 52-57
   ```typescript
   if (response.periodHours) {
       this.workScheduleLoader.periodHours.set(params.clientId, response.periodHours);
   }
   ```
   - Wird `periodHours` korrekt aus Response extrahiert?
   - Wird es korrekt im Signal gespeichert?

3. **UI-Update-Mechanismus:**
   - `WorkScheduleLoaderService.periodHours` Signal
   - Wie reagiert die UI auf Änderungen?
   - Race Conditions möglich?

**Dateien zu prüfen:**
- `/mnt/c/SourceCode/Klacks.Ui/src/app/presentation/workplace/schedule/schedule-section/schedule-schedule-row-header/schedule-schedule-row-header.component.ts`
- `/mnt/c/SourceCode/Klacks.Ui/src/app/domain/services/schedule/work-schedule-loader.service.ts`
- `/mnt/c/SourceCode/Klacks.Ui/src/app/domain/services/schedule/work-schedule-crud.service.ts` (Zeilen 52-57, 68-71, 82-94)

### Priority 2: E2E-Test verbessern 🟡

**Option A: Manueller Test mit besserer Anleitung**
- On-Screen-Hinweis im Browser zeigen
- Längere Wartezeit (120 Sekunden)
- Besseres Feedback während des Wartens

**Option B: Automatisierter Test mit Browser Console**
```csharp
await Page.EvaluateAsync(@"
    const service = ng.probe(document.querySelector('app-schedule-section'))
        .injector.get(DataManagementScheduleService);

    service.addWorkScheduleEntry({
        clientId: '<client-id>',
        date: new Date('2025-01-18'),
        shiftId: '<shift-id>',
        workTime: 480,
        startTime: '08:00:00',
        endTime: '16:00:00'
    });
");
```

**Option C: Kontextmenü-basiert**
```csharp
await Page.ClickAsync("canvas#scheduleCanvas", new() { Button = MouseButton.Right });
await Page.ClickAsync("text='Shift Name'");
```

### Priority 3: Debugging-Session vorbereiten 🟢

**Setup für Live-Debugging morgen:**
1. Browser DevTools öffnen
2. Breakpoints setzen in:
   - `work-schedule-crud.service.ts:54` (Response-Handling)
   - `work-schedule-loader.service.ts` (Signal-Update)
   - `schedule-schedule-row-header.component.ts` (UI-Rendering)

3. Network-Tab beobachten:
   - POST `/api/work` Response prüfen
   - DELETE `/api/work/bulk-delete` Response prüfen

4. Console-Tab beobachten:
   - Logs nach "periodHours" filtern

---

## Wichtige Code-Stellen

### Backend (funktioniert ✅)

**BulkAddWorksCommandHandler.cs** (Zeile 114-124)
```csharp
_handler = new BulkAddWorksCommandHandler(
    workRepository,
    shiftRepository,      // ← ADDED für StartTime/EndTime
    scheduleMapper,
    unitOfWork,
    workNotificationService,
    shiftStatsNotificationService,
    shiftScheduleService,
    periodHoursService,
    mockHttpContextAccessor,
    Substitute.For<ILogger<BulkAddWorksCommandHandler>>());
```

**Work-Erstellung mit StartTime/EndTime** (Zeile ~60-70)
```csharp
var shift = await _shiftRepository.Get(command.Request.ShiftId);
var work = new Work
{
    Id = Guid.NewGuid(),
    ShiftId = command.Request.ShiftId,
    ClientId = entry.ClientId,
    CurrentDate = entry.CurrentDate,
    WorkTime = command.Request.WorkTime,
    StartTime = shift.StartShift,    // ← ADDED
    EndTime = shift.EndShift,        // ← ADDED
    IsSealed = false,
    IsDeleted = false
};
```

**PeriodHours-Response** (Zeile ~140-155)
```csharp
if (affectedClients.Count > 0)
{
    response.PeriodHours = new Dictionary<Guid, PeriodHoursResource>();

    foreach (var clientId in affectedClients)
    {
        if (clientPeriods.TryGetValue(clientId, out var period))
        {
            var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(
                clientId,
                period.Start,
                period.End);
            response.PeriodHours[clientId] = periodHours;
        }
    }
}
```

### Frontend (HIER IST DAS PROBLEM ❌)

**WorkScheduleCrudService.addWorkScheduleEntry()** (Zeile 42-58)
```typescript
addWorkScheduleEntry(params: ScheduleCellParams, workFilter: IWorkFilter): Promise<void> {
    this.updateShiftEngagedLocally(params.shiftId, params.date, 1, workFilter);

    const periodStart = this.workScheduleLoader.startDate
      ? formatDateOnly(this.workScheduleLoader.startDate)
      : formatDateOnly(new Date());
    const periodEnd = this.workScheduleLoader.endDate
      ? formatDateOnly(this.workScheduleLoader.endDate)
      : formatDateOnly(new Date());

    return this.workCrud.createWork({ ...params, periodStart, periodEnd }).then(async (response) => {
      // ← HIER: Wird periodHours korrekt aus Response gelesen?
      if (response.periodHours) {
        this.workScheduleLoader.periodHours.set(params.clientId, response.periodHours);
      }
      await this.refreshClientScheduleForDays(params.clientId, params.date);
    });
  }
```

**WorkScheduleCrudService.bulkDeleteWorkScheduleEntries()** (Zeile 77-131)
```typescript
bulkDeleteWorkScheduleEntries(entries: DeleteWorkScheduleEntryParams[], workFilter: IWorkFilter): void {
    // ...
    this.workCrud.bulkDeleteWorks(workIds).then(async (response) => {
      if (response.successCount === 0) return;

      // ← HIER: Console-Logs für Debugging
      console.log('BulkDelete Response:', response);
      console.log('PeriodHours from backend:', response.periodHours);

      if (response.periodHours) {
        for (const [clientId, hours] of Object.entries(response.periodHours)) {
          console.log(`Setting periodHours for client ${clientId}:`, hours);
          this.workScheduleLoader.periodHours.set(clientId, hours);
        }
      } else {
        console.warn('No periodHours in bulkDelete response!');
      }
      // ...
    });
  }
```

**WorkScheduleLoaderService.periodHours** (zu prüfen!)
```typescript
// Signal oder Observable?
// Wie wird UI aktualisiert?
```

**ScheduleScheduleRowHeaderComponent** (Slot 3 - Surcharges Anzeige)
```typescript
// Wo wird periodHours gelesen?
// Wie wird Slot 3 befüllt?
```

---

## Test-Daten für Debugging

### Test-Dates
- **Samstag:** 18.01.2025 (Weekday = 6, Surcharge = 10%)
- **Sonntag:** 19.01.2025 (Weekday = 7, Surcharge = 10%)
- **Montag:** 20.01.2025 (Weekday = 1, Surcharge = 0%)

### Erwartete Werte (für 8h WorkTime)
| Tag | WorkTime | Surcharge | Berechnung |
|-----|----------|-----------|------------|
| Sa  | 8h       | 0.8h      | 8 × 10% = 0.8 |
| So  | 8h       | 0.8h      | 8 × 10% = 0.8 |
| Mo  | 8h       | 0h        | 8 × 0% = 0 |
| **Total** | **24h** | **1.6h** | 0.8 + 0.8 + 0 |

### Macro AllShift - Rates
```csharp
SaRate = 0.1m      // 10% Saturday
SoRate = 0.1m      // 10% Sunday
NightRate = 0.1m   // 10% Night (23:00-06:00)
HolidayRate = 0.15m // 15% Holiday
```

---

## Offene Fragen

1. **Wo genau wird PeriodHours in der UI angezeigt?**
   - Komponente?
   - Template?
   - Signal/Observable-Binding?

2. **Gibt es ein Timing-Problem?**
   - Wird `refreshClientScheduleForDays()` NACH `periodHours.set()` aufgerufen?
   - Überschreibt der Refresh die PeriodHours?

3. **Wird die richtige Client-ID verwendet?**
   - `params.clientId` vs. `response.periodHours[clientId]`
   - Guid-String-Konvertierung korrekt?

4. **Response-Format korrekt?**
   - Backend gibt `Dictionary<Guid, PeriodHoursResource>`
   - Frontend erwartet `Map<string, PeriodHours>`?

---

## Befehle für morgen

### Integration Tests ausführen
```bash
cd /mnt/c/SourceCode/IntegrationTest
dotnet test --filter "BulkAddWorksIntegrationTests"
dotnet test --filter "BulkDeleteWorksIntegrationTests"
```

### Server starten
```bash
# WICHTIG: Immer zuerst alles stoppen!
powershell.exe -File "C:\SourceCode\stop_klacks.ps1"

# Dann neu starten
powershell.exe -File "C:\SourceCode\start_klacks.ps1"

# Warten: Backend ~20s, Frontend ~40-60s
```

### E2E-Test ausführen
```bash
cd /mnt/c/SourceCode/E2ETest
dotnet test --filter "WorkScheduleBulkOperationsTest"

# Während 60 Sekunden:
# 1. Im Browser: Schedule öffnen
# 2. Client finden
# 3. Works erstellen (Sa, So, Mo)
# 4. Works löschen
# 5. Test analysiert Logs
```

### Frontend-Debugging
```bash
cd /mnt/c/SourceCode/Klacks.Ui

# Browser öffnen mit DevTools
# Breakpoints setzen in:
# - work-schedule-crud.service.ts:54
# - work-schedule-loader.service.ts (periodHours update)

# Network-Tab: POST/DELETE Responses prüfen
# Console-Tab: "periodHours" logs suchen
```

---

## Git-Status

**Commits erstellt:**
1. `IntegrationTest`: BulkAdd/BulkDelete Tests mit echtem Macro
2. `Klacks.Api`: BulkAddWorksCommandHandler - Set StartTime/EndTime from Shift
3. `E2ETest`: WorkSchedule BulkOperations E2E Test

**Noch nicht committed:**
- Diese Dokumentation
- `CLAUDE_QUICKREF.md` Änderungen
- `appsettings.Development.json`

---

## Kontakt/Kontext für morgen

**Wenn du morgen weitermachst:**
1. Lies diese Dokumentation komplett durch ✅
2. Lies `CLAUDE_QUICKREF.md` für Projekt-Konventionen ✅
3. Lies `/mnt/c/SourceCode/Klacks.Api/MACRO_ZUSCHLAEGE_DOCUMENTATION.md` für Macro-Details ✅
4. Fokus auf **Frontend-Analyse** (Priority 1)

**Wichtigste Erkenntnis:**
> Backend funktioniert korrekt (Integration Tests beweisen es).
> Das Problem ist im Frontend: PeriodHours wird nicht korrekt aus der API-Response gelesen/gespeichert/angezeigt.

**Nächster Schritt:**
> Finde heraus, wo `periodHours` in `schedule-schedule-row-header.component.ts` (Slot 3) angezeigt wird und warum der Wert falsch ist.

---

**Erstellt am:** 23. Januar 2026, 20:50 Uhr
**Session-ID:** 2e07b020-b249-47f6-95f2-f16e81571c0e
**Agent-ID:** a288f1e (für Resume der Canvas-Analyse)
