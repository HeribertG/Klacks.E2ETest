# E2E Tests ausfuehren

## Voraussetzung

- Backend und Frontend muessen laufen (siehe Skill `e2e-test-environment.md`)
- Tests muessen unter **Windows** laufen, NICHT in WSL
- Playwright Browser wird automatisch gestartet (Headless-Modus per `appsettings.Development.json`)

## Tests aus WSL heraus starten

Da die Tests unter Windows laufen muessen, wird `powershell.exe` verwendet:

```bash
# Alle E2E-Tests
powershell.exe -Command "cd C:\SourceCode; dotnet test Klacks.E2ETest\Klacks.E2ETest.csproj 2>&1" | tee /tmp/e2e-results.log

# Einzelne Testklasse
powershell.exe -Command "cd C:\SourceCode; dotnet test Klacks.E2ETest\Klacks.E2ETest.csproj --filter 'FullyQualifiedName~LlmBranchesTest' 2>&1"

# Mehrere Testklassen (OR-Filter)
powershell.exe -Command "cd C:\SourceCode; dotnet test Klacks.E2ETest\Klacks.E2ETest.csproj --filter 'FullyQualifiedName~LlmMacros|FullyQualifiedName~LlmBranches' 2>&1"

# Alle LLM-Tests (Order 50-69)
powershell.exe -Command "cd C:\SourceCode; dotnet test Klacks.E2ETest\Klacks.E2ETest.csproj --filter 'FullyQualifiedName~LlmSettingsGeneral|FullyQualifiedName~LlmOwnerAddress|FullyQualifiedName~LlmSoulMemory|FullyQualifiedName~LlmUserAdmin|FullyQualifiedName~LlmBranches|FullyQualifiedName~LlmMacros|FullyQualifiedName~UserGroupVisibility' 2>&1"
```

## Tests direkt in Windows PowerShell starten

```powershell
cd C:\SourceCode
dotnet test Klacks.E2ETest\Klacks.E2ETest.csproj
dotnet test Klacks.E2ETest\Klacks.E2ETest.csproj --filter "FullyQualifiedName~LlmBranchesTest"
```

## Test-Gruppen (Order-Ranges)

| Range | Kategorie | Filter-Beispiel |
|-------|-----------|----------------|
| 1-9 | Login & Navigation | `FullyQualifiedName~LoginTest` |
| 10-19 | Client | `FullyQualifiedName~Client` |
| 20-32 | Settings | `FullyQualifiedName~Settings` (ohne Llm-Prefix) |
| 33-34 | Gantt | `FullyQualifiedName~Gantt` |
| 35-39 | Group | `FullyQualifiedName~Group` |
| 40-49 | Shifts | `FullyQualifiedName~Shift` |
| 50-69 | LLM Integration | Siehe LLM-Filter oben |

## Testklassen (Order 50-69, LLM Integration)

| Order | Klasse | Tests | Beschreibung |
|------:|--------|------:|-------------|
| 50 | LlmSettingsGeneralTest | 8 | App-Name, Icon, Logo via Chat |
| 51 | LlmOwnerAddressTest | 7 | Adress-Verwaltung via Chat |
| 60 | LlmSoulMemoryTest | 8 | AI Soul & Memory via Chat |
| 62 | LlmUserAdministrationTest | 8 | User CRUD via Chat |
| 63 | LlmBranchesTest | 9 | Filialen CRUD via Chat |
| 64 | LlmMacrosTest | 5 | Macro CRUD via Chat |
| 64 | UserGroupVisibilityTest | 8 | Gruppen-Sichtbarkeit via Chat |

## Konfiguration

| Datei | Umgebung | Headless | BaseUrl |
|-------|----------|----------|---------|
| `appsettings.Development.json` | Lokal | false | `http://localhost:4200/` |
| `appsettings.json` | Produktion | false | `http://157.180.42.127:3000/` |

Login-Credentials werden aus `appsettings.*.json` geladen (`user` / `password` Keys) oder User Secrets.

## Haeufige Probleme

| Problem | Ursache | Loesung |
|---------|---------|---------|
| Tests starten nicht | Services nicht gestartet | `start_klacks.ps1` ausfuehren |
| Connection refused | Port 4200/5000 nicht erreichbar | Services pruefen, ggf. neu starten |
| Login schlaegt fehl | Falsche Credentials | `appsettings.Development.json` pruefen |
| LLM-Tests instabil | LLM-Nondeterminismus | Chat-Clear vor jedem Versuch, Retry-Logik nutzen |
| "Browser not found" | Playwright nicht installiert | `pwsh bin/Debug/net10.0/.playwright/package/bin/playwright.ps1 install` |

## Build vor Tests

```bash
# Aus WSL (build geht in WSL):
dotnet build Klacks.E2ETest/Klacks.E2ETest.csproj

# Nur Tests muessen via powershell.exe laufen
```
