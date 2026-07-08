# E2E Test-Reihenfolge

## Uebersicht

Die E2E-Tests werden in einer definierten Reihenfolge ausgefuehrt, gesteuert durch das `[Order(n)]` Attribut auf Klassen-Ebene.

## Test-Dateien sortiert nach Order

| Order | Test-Datei | Beschreibung |
|------:|------------|--------------|
| 1 | `Login/LoginTest.cs` | Login |
| - | `Login/OAuth2SsoTest.cs` | OAuth2 SSO (kein Order; laeuft nach Login) |
| 2 | `Navigation/NavigationStepsTest.cs` | Navigation Steps |
| 3 | `Navigation/NavigationTest.cs` | Navigation (Tooltips + Shortcuts) |
| 10 | `Client/ClientCreationTest.cs` | Client erstellen |
| 11 | `Client/ClientSearchTest.cs` | Client Suche |
| 12 | `Client/ClientTypeFilterTest.cs` | Client Type Filter |
| 13 | `Client/ClientAdvancedFiltersTest.cs` | Client Advanced Filters |
| 14 | `Client/ClientDeletionTest.cs` | Client loeschen |
| 23 | `Settings/SettingsGroupScopeTest.cs` | Settings Group Scope |
| 24 | `Settings/SettingsGridColorTest.cs` | Settings Grid Color |
| 25 | `Settings/SettingsStateTest.cs` | Settings State |
| 26 | `Settings/SettingsCountriesTest.cs` | Settings Countries |
| 27 | `Settings/SettingsEmailTest.cs` | Settings Email |
| 28 | `Settings/SettingsIdentityProviderTest.cs` | Settings Identity Provider |
| 29 | `Settings/SettingsAbsenceTest.cs` | Settings Absence (CRUD) |
| 31 | `Settings/SettingsLlmProvidersTest.cs` | Settings LLM Providers (CRUD) |
| 32 | `Settings/SettingsLlmModelsTest.cs` | Settings LLM Models (CRUD) |
| 33 | `Gantt/GanttGroupFilterTest.cs` | Gantt Group Filter |
| 38 | `Group/GroupTreeCreationTest.cs` | Group Tree erstellen (Create/Verify/Delete) |
| 40 | `Shifts/ShiftCutsBasicTest.cs` | Shift Cuts Basic |
| 41 | `Shifts/ShiftCutsNestedTest.cs` | Shift Cuts Nested |
| 42 | `Shifts/ShiftCutsBatchTest.cs` | Shift Cuts Batch |
| 43 | `Shifts/ShiftOrderCreationTest.cs` | Shift Order Creation |
| 50 | `Chatbot/ChatbotSettingsGeneralTest.cs` | Chat: Settings General (App-Name, Icon, Logo) |
| 51 | `Chatbot/ChatbotNavigationTest.cs` | Chat: Navigation zu Settings/Seiten via Chat |
| 52 | `Chatbot/ChatbotSoulMemoryTest.cs` | Chat: AI Soul & Memory (CRUD) |
| 53 | `Chatbot/ChatbotBranchesTest.cs` | Chat: Filialen CRUD |
| 54 | `Chatbot/ChatbotMacrosTest.cs` | Chat: Macro CRUD mit Script |
| 55 | `Chatbot/ChatbotAiGuidelinesTest.cs` | Chat: AI Guidelines |
| 57 | `Chatbot/ChatbotOwnerAddressTest.cs` | Chat: Owner Address (Validierung, CRUD) |
| 58 | `Chatbot/ChatbotUserAdministrationTest.cs` | Chat: User CRUD |
| 59 | `Chatbot/ChatbotCalendarRulesJapanTest.cs` | Chat: Japan Feiertage + Country/States CRUD |
| 61 | `Chatbot/ChatbotEmailSetupWizardTest.cs` | Chat: Email Setup Wizard (GMX, inkl. SMTP/IMAP Settings) |
| 62 | `Klacksy/KlacksyInPageNavigationE2ETests.cs` | Klacksy: In-Page Navigation Highlights |
| 64 | `Settings/UserGroupVisibilityTest.cs` | LLM Chat: Gruppen-Sichtbarkeit (Login als neuer User) |
| 70 | `Settings/SettingsWorkSettingTest.cs` | Settings Work-Setting |
| 71 | `Settings/SettingsContractsTest.cs` | Settings Contracts |
| 72 | `Settings/SettingsSchedulingDefaultsTest.cs` | Settings Scheduling Defaults |
| 73 | `Settings/SettingsSchedulingRulesTest.cs` | Settings Scheduling Rules |
| 74 | `Settings/SettingsAbsenceDetailTest.cs` | Settings Absence Detail |
| 90 | `Messaging/TelegramOnboardingTest.cs` | Telegram Button Visibility (Webhook-HTTP-Tests siehe Klacks.IntegrationTest/Messaging/) |
| 95 | `VoiceOnlyShellTests.cs` | Voice-Only Shell (Audio/Text Mode Switch) |
| 100 | `WorkSchedule/WorkScheduleBulkOperationsTest.cs` | Schedule Bulk Operations |
| 101 | `WorkSchedule/WorkScheduleGridTest.cs` | Schedule Canvas Grid |
| 102 | `WorkSchedule/WorkScheduleGroupSwitchTest.cs` | Schedule Group Switch |
| 103 | `WorkSchedule/WorkScheduleGroupSwitchStressTest.cs` | Schedule Group Switch Stress |
| 104 | `WorkSchedule/WorkScheduleCollisionTraceTest.cs` | Schedule Collision Trace |
| 110 | `WorkSchedule/WizardAutofillTest.cs` | Schedule Autofill Wizard Smoke |
| - | `WorkSchedule/WizardBenchmarkTrainingTest.cs` | `[Explicit]` Training-Benchmark (CI ueberspringt) |

## Gruppierung

| Range | Kategorie | Beschreibung |
|-------|-----------|--------------|
| 1-9 | Login & Navigation | Grundlegende Anmeldung und Navigation |
| 10-19 | Client | Client-Verwaltung |
| 20-32 | Settings | Settings (Users, Absence, CalendarRules, LLM Providers/Models) |
| 33-34 | Gantt | Gantt-Diagramm Tests |
| 35-39 | Group | Gruppen-Verwaltung |
| 40-49 | Shifts | Shift-Verwaltung (Cuts, Orders) |
| 48-61 | Chatbot | Chatbot-Tests (Settings, AI Soul, Branches, Macros, Email) |
| 62-69 | Klacksy / Visibility | In-Page Nav, Group Visibility |
| 70-74 | Settings (Advanced) | Work-Setting, Contracts, Scheduling |
| 90-99 | Messaging / Shell | Telegram, Voice Shell |
| 100-119 | WorkSchedule | Schedule Canvas Grid, Group Switch, Wizard |

## Hinweise

- Tests innerhalb einer Klasse haben ebenfalls `[Order(n)]` Attribute fuer die interne Reihenfolge
- Die Order-Nummern haben Luecken, um spaeter neue Tests einfuegen zu koennen
- Login muss immer zuerst laufen (Order 1), da alle anderen Tests eine aktive Session benoetigen
- Chatbot-Tests (Order 48-69) nutzen den LLM und sind durch LLM-Nondeterminismus bedingt instabiler.
  Sie verwenden Retry-Logik, Chat-Clear und KIMI-Provider als deterministischen Default.
- Tests laufen nur unter Windows, nicht in WSL (siehe `.claude/skills/e2e-test-environment.md`)
- `[Explicit]` markierte Tests (z.B. `WizardBenchmarkTrainingTest`) werden NICHT automatisch ausgefuehrt
- `[Ignore]` markierte Tests in `SettingsIdentityProviderTest` benoetigen einen Live-LDAP-Server (Zflexldap)
