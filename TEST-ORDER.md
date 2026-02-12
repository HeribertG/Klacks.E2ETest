# E2E Test-Reihenfolge

## Uebersicht

Die E2E-Tests werden in einer definierten Reihenfolge ausgefuehrt, gesteuert durch das `[Order(n)]` Attribut auf Klassen-Ebene.

## Test-Dateien sortiert nach Order

| Order | Test-Datei | Tests | Beschreibung |
|------:|------------|------:|--------------|
| 1 | `Login/LoginTest.cs` | | Login |
| 2 | `Navigation/NavigationStepsTest.cs` | | Navigation Steps |
| 3 | `Navigation/NavigationTest.cs` | | Navigation |
| 10 | `Client/ClientCreationTest.cs` | | Client erstellen |
| 11 | `Client/ClientSearchTest.cs` | | Client Suche |
| 12 | `Client/ClientTypeFilterTest.cs` | | Client Type Filter |
| 13 | `Client/ClientAdvancedFiltersTest.cs` | | Client Advanced Filters |
| 14 | `Client/ClientDeletionTest.cs` | | Client loeschen |
| 20 | `Settings/SettingsGeneralTest.cs` | | Settings General |
| 21 | `Settings/SettingsOwnerAddressTest.cs` | | Settings Owner Address |
| 22 | `Settings/SettingsUserAdministrationTest.cs` | | Settings User Administration |
| 23 | `Settings/SettingsGroupScopeTest.cs` | | Settings Group Scope |
| 24 | `Settings/SettingsGridColorTest.cs` | | Settings Grid Color |
| 25 | `Settings/SettingsStateTest.cs` | | Settings State |
| 26 | `Settings/SettingsCountriesTest.cs` | | Settings Countries |
| 27 | `Settings/SettingsEmailTest.cs` | | Settings Email |
| 28 | `Settings/SettingsIdentityProviderTest.cs` | | Settings Identity Provider |
| 29 | `Settings/SettingsAbsenceTest.cs` | | Settings Absence (CRUD) |
| 30 | `Settings/SettingsCalendarRulesTest.cs` | | Settings Calendar Rules (CRUD + API Validation) |
| 31 | `Settings/SettingsLlmProvidersTest.cs` | | Settings LLM Providers (CRUD) |
| 32 | `Settings/SettingsLlmModelsTest.cs` | | Settings LLM Models (CRUD) |
| 33 | `Gantt/GanttGroupFilterTest.cs` | | Gantt Group Filter |
| 34 | `Gantt/GanttVirtualScrollingTest.cs` | | Gantt Virtual Scrolling |
| 35 | `Group/GroupCreationTest.cs` | | Group erstellen |
| 36 | `Group/GroupSearchTest.cs` | | Group Suche |
| 37 | `Group/GroupDeletionTest.cs` | | Group loeschen |
| 38 | `Group/GroupTreeCreationTest.cs` | | Group Tree erstellen |
| 40 | `Shifts/ShiftCutsBasicTest.cs` | | Shift Cuts Basic |
| 41 | `Shifts/ShiftCutsNestedTest.cs` | | Shift Cuts Nested |
| 42 | `Shifts/ShiftCutsBatchTest.cs` | | Shift Cuts Batch |
| 43 | `Shifts/ShiftOrderCreationTest.cs` | | Shift Order Creation |
| 48 | `Settings/LlmKimiProviderTest.cs` | 7 | KIMI Provider: Create/Verify Provider + Model, Chat Smoke-Test (bleibt fuer Order 50-69) |
| 50 | `Settings/LlmSettingsGeneralTest.cs` | 8 | LLM Chat: Settings General (App-Name, Icon, Logo) |
| 51 | `Settings/LlmOwnerAddressTest.cs` | 7 | LLM Chat: Owner Address (Validierung, CRUD) |
| 60 | `Settings/LlmSoulMemoryTest.cs` | 8 | LLM Chat: AI Soul & Memory (CRUD) |
| 62 | `Settings/LlmUserAdministrationTest.cs` | 8 | LLM Chat: User CRUD (Create 3, Verify, Delete) |
| 63 | `Settings/LlmBranchesTest.cs` | 9 | LLM Chat: Filialen CRUD (Zuerich + Lausanne) |
| 64 | `Settings/LlmMacrosTest.cs` | 5 | LLM Chat: Macro CRUD mit Script |
| 64 | `Settings/UserGroupVisibilityTest.cs` | 8 | LLM Chat: Gruppen-Sichtbarkeit (Login as new user) |
| 65 | `Settings/LlmAiGuidelinesTest.cs` | 7 | LLM Chat: AI Guidelines (Lesen, Aktualisieren, Verifizieren) |
| 66 | `Settings/LlmNavigationSettingsTest.cs` | 8 | LLM Chat: Navigation zu Settings/Seiten via Chat |
| 67 | `Settings/LlmSystemInfoPermissionsTest.cs` | 8 | LLM Chat: Systeminfo & Berechtigungen abfragen |
| 68 | `Settings/LlmCalendarRulesJapanTest.cs` | 13 | LLM Chat: Japan Feiertage recherchieren + Country/States/CalendarRules CRUD |

## Gruppierung

| Range | Kategorie | Beschreibung |
|-------|-----------|--------------|
| 1-9 | Login & Navigation | Grundlegende Anmeldung und Navigation |
| 10-19 | Client | Client-Verwaltung (Erstellen, Suche, Filter, Loeschen) |
| 20-32 | Settings | Einstellungen (General, Owner, Users, Absence, CalendarRules, LLM Providers/Models) |
| 33-34 | Gantt | Gantt-Diagramm Tests |
| 35-39 | Group | Gruppen-Verwaltung (Erstellen, Suche, Loeschen, Tree) |
| 40-49 | Shifts | Dienst-Verwaltung (Cutting, Orders) |
| 50-59 | LLM Settings | LLM-Chat-Tests fuer Settings + Owner Address |
| 60-69 | LLM Features | LLM-Chat-Tests fuer AI Soul/Memory, Users, Branches, Macros, GroupVisibility |

## Hinweise

- Tests innerhalb einer Klasse haben ebenfalls `[Order(n)]` Attribute fuer die interne Reihenfolge
- Die Order-Nummern haben Luecken, um spaeter neue Tests einfuegen zu koennen
- Login muss immer zuerst laufen (Order 1), da alle anderen Tests eine aktive Session benoetigen
- LLM-Tests (Order 50-69) nutzen den Chat-LLM und sind durch LLM-Nondeterminismus bedingt instabiler
- LLM-Tests verwenden Retry-Logik und Chat-Clear vor jeder Nachricht
- Tests laufen nur unter Windows, nicht in WSL (siehe `.claude/skills/e2e-test-environment.md`)
