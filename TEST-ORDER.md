# E2E Test-Reihenfolge

## Übersicht

Die E2E-Tests werden in einer definierten Reihenfolge ausgeführt, gesteuert durch das `[Order(n)]` Attribut auf Klassen-Ebene.

## Test-Dateien sortiert nach Order

| Order | Test-Datei | Beschreibung |
|------:|------------|--------------|
| 1 | `Login/LoginTest.cs` | Login |
| 2 | `Navigation/NavigationStepsTest.cs` | Navigation Steps |
| 3 | `Navigation/NavigationTest.cs` | Navigation |
| 10 | `Client/ClientCreationTest.cs` | Client erstellen |
| 11 | `Client/ClientSearchTest.cs` | Client Suche |
| 12 | `Client/ClientTypeFilterTest.cs` | Client Type Filter |
| 13 | `Client/ClientAdvancedFiltersTest.cs` | Client Advanced Filters |
| 14 | `Client/ClientDeletionTest.cs` | Client löschen |
| 20 | `Settings/SettingsGeneralTest.cs` | Settings General |
| 21 | `Settings/SettingsOwnerAddressTest.cs` | Settings Owner Address |
| 22 | `Settings/SettingsUserAdministrationTest.cs` | Settings User Administration |
| 23 | `Settings/SettingsGroupScopeTest.cs` | Settings Group Scope |
| 24 | `Settings/SettingsGridColorTest.cs` | Settings Grid Color |
| 25 | `Settings/SettingsStateTest.cs` | Settings State |
| 26 | `Settings/SettingsCountriesTest.cs` | Settings Countries |
| 27 | `Settings/SettingsEmailTest.cs` | Settings Email |
| 28 | `Settings/SettingsIdentityProviderTest.cs` | Settings Identity Provider |
| 29 | `Settings/SettingsAbsenceTest.cs` | Settings Absence (CRUD) |
| 30 | `Settings/SettingsCalendarRulesTest.cs` | Settings Calendar Rules (CRUD + API Validation) |
| 32 | `Gantt/GanttGroupFilterTest.cs` | Gantt Group Filter |
| 33 | `Gantt/GanttVirtualScrollingTest.cs` | Gantt Virtual Scrolling |
| 35 | `Group/GroupCreationTest.cs` | Group erstellen |
| 36 | `Group/GroupSearchTest.cs` | Group Suche |
| 37 | `Group/GroupDeletionTest.cs` | Group löschen |
| 38 | `Group/GroupTreeCreationTest.cs` | Group Tree erstellen |
| 40 | `Shifts/ShiftCutsBasicTest.cs` | Shift Cuts Basic |
| 41 | `Shifts/ShiftCutsNestedTest.cs` | Shift Cuts Nested |
| 42 | `Shifts/ShiftCutsBatchTest.cs` | Shift Cuts Batch |
| 43 | `Shifts/ShiftOrderCreationTest.cs` | Shift Order Creation |

## Gruppierung

| Range | Kategorie | Beschreibung |
|-------|-----------|--------------|
| 1-9 | Login & Navigation | Grundlegende Anmeldung und Navigation |
| 10-19 | Client | Client-Verwaltung (Erstellen, Suche, Filter, Löschen) |
| 20-30 | Settings | Einstellungen (General, Owner, Users, Absence, CalendarRules, etc.) |
| 32-34 | Gantt | Gantt-Diagramm Tests |
| 35-39 | Group | Gruppen-Verwaltung (Erstellen, Suche, Löschen, Tree) |
| 40-49 | Shifts | Dienst-Verwaltung (Cutting, Orders) |

## Hinweise

- Tests innerhalb einer Klasse haben ebenfalls `[Order(n)]` Attribute für die interne Reihenfolge
- Die Order-Nummern haben Lücken, um später neue Tests einfügen zu können
- Login muss immer zuerst laufen (Order 1), da alle anderen Tests eine aktive Session benötigen
- Order 31 ist frei für zukünftige Settings-Tests oder als Puffer
