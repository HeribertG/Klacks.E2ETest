# E2E Test Environment - Start & Stop

## Voraussetzungen

- Tests laufen **nur unter Windows** (nicht in WSL)
- Playwright braucht einen laufenden Browser, der in WSL nicht verfuegbar ist
- `dotnet test` muss aus Windows heraus ausgefuehrt werden (PowerShell oder CMD)
- Backend (Port 5000) und Frontend (Port 4200) muessen laufen

## Vor dem Start: Alte Sessions pruefen und stoppen

**WICHTIG:** Vor jedem `start_klacks.ps1` MUSS geprueft werden, ob bereits alte Sessions laufen. Wenn Port 4200 schon belegt ist, kann das Frontend nicht sauber starten.

```powershell
# Aus WSL heraus pruefen und stoppen:
powershell.exe -Command "& 'C:\SourceCode\stop-backend.ps1'"
powershell.exe -Command "& 'C:\SourceCode\stop-klacks-ui.ps1'"
```

```powershell
# Aus Windows PowerShell direkt:
C:\SourceCode\stop-backend.ps1
C:\SourceCode\stop-klacks-ui.ps1
```

### Port 4200 pruefen

```powershell
# Aus Windows PowerShell:
Get-NetTCPConnection -LocalPort 4200 -ErrorAction SilentlyContinue

# Aus WSL:
powershell.exe -Command "Get-NetTCPConnection -LocalPort 4200 -ErrorAction SilentlyContinue"
```

Falls noch belegt: `stop-klacks-ui.ps1` nochmals ausfuehren oder Prozess manuell beenden.

## Services starten

```powershell
# Aus WSL heraus:
powershell.exe -Command "& 'C:\SourceCode\start_klacks.ps1'"

# Aus Windows PowerShell direkt:
C:\SourceCode\start_klacks.ps1
```

Das Skript startet:
1. **Klacks.Api** (`dotnet run` in `C:\SourceCode\Klacks.Api`) - Backend auf Port 5000
2. **Klacks.Ui** (`npm start` in `C:\SourceCode\Klacks.Ui`) - Frontend auf Port 4200

PID-Dateien werden in `%TEMP%\klacks_api.pid` und `%TEMP%\klacks_ui.pid` gespeichert.

**Warten:** Nach dem Start ca. 15-30 Sekunden warten, bis beide Services bereit sind (Frontend-Kompilierung dauert).

## Services stoppen

```powershell
# Aus WSL heraus:
powershell.exe -Command "& 'C:\SourceCode\stop-backend.ps1'"
powershell.exe -Command "& 'C:\SourceCode\stop-klacks-ui.ps1'"

# Aus Windows PowerShell direkt:
C:\SourceCode\stop-backend.ps1
C:\SourceCode\stop-klacks-ui.ps1
```

| Skript | Stoppt | Methode |
|--------|--------|---------|
| `stop-backend.ps1` | Klacks.Api | `Stop-Process -Name "Klacks.Api"` |
| `stop-klacks-ui.ps1` | Klacks.Ui | Port 4200 + ng serve Prozesse + Node in Klacks.Ui |

## Reihenfolge zusammengefasst

1. **Alte Sessions stoppen** → `stop-backend.ps1` + `stop-klacks-ui.ps1`
2. **Port 4200 pruefen** → frei? Weiter. Belegt? Nochmals stoppen.
3. **Services starten** → `start_klacks.ps1`
4. **Warten** → 15-30 Sekunden
5. **Tests ausfuehren** → siehe Skill `e2e-test-ausfuehrung.md`
6. **Services stoppen** → `stop-backend.ps1` + `stop-klacks-ui.ps1`
