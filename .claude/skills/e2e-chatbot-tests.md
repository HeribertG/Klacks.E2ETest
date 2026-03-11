# E2E Chatbot Tests

## Übersicht

Alle LLM Chatbot E2E Tests liegen in `Klacks.E2ETest\Chatbot\`.
Playwright + NUnit, sequentiell via `[Order]` Attribut.

## Voraussetzungen

- **API:** `https://localhost:5001` (dotnet run)
- **UI:** `http://localhost:4200` (npm start, ~90s Kompilierung)
- **DB:** PostgreSQL Port 5434 (Integration Test DB), User: postgres, PW: admin

## Test-Klassen

| Klasse | Order | Testet |
|--------|-------|--------|
| `ChatbotKimiProviderTest` | 48 | LLM Provider/Model Setup |
| `ChatbotDiagnosticTest` | 49 | Chat-Initialisierung |
| `ChatbotSettingsGeneralTest` | 50 | `get_general_settings`, `update_general_settings` (UiAction) |
| `ChatbotNavigationTest` | 51 | `navigate_to` (UiAction) |
| `ChatbotSoulMemoryTest` | 52 | `get_ai_soul`, `update_ai_soul`, `add_ai_memory` |
| `ChatbotBranchesTest` | 53 | Branch CRUD (UiPassthrough) |
| `ChatbotMacrosTest` | 54 | Macro CRUD |
| `ChatbotAiGuidelinesTest` | 55 | `get_ai_guidelines`, `update_ai_guidelines` |
| `ChatbotSystemInfoPermissionsTest` | 56 | `get_system_info`, `get_user_permissions` |
| `ChatbotOwnerAddressTest` | 57 | `get_owner_address`, `update_owner_address` (UiAction) |
| `ChatbotUserAdministrationTest` | 58 | User CRUD (UiPassthrough) |
| `ChatbotCalendarRulesJapanTest` | 59 | Calendar Rules CRUD |
| `ChatbotEmailSettingTest` | 60 | `get/update_email_settings`, `get/update_imap_settings` (UiAction) |
| `ChatbotEmailSetupWizardTest` | 61 | Email Wizard mit SMTP/IMAP |

## Test-Infrastruktur

### ChatbotTestBase

**Pfad:** `Chatbot/Helpers/ChatbotTestBase.cs`

Basis-Klasse für alle Chatbot-Tests:
- `SendChatMessage(message)` — Nachricht senden
- `WaitForBotResponse(timeout)` — Bot-Antwort abwarten
- `AssertSkillEnabled(skillName)` — Prüft ob Skill aktiv + korrekter Typ
- Chat-Selektoren werden aus DB geladen (nicht hardcoded)

### DbHelper

**Pfad:** `Chatbot/Helpers/DbHelper.cs`

Direkte PostgreSQL-Abfragen (Port 5434):
- `IsSkillEnabledAsync(skillName)` — Prüft Skill-Status
- `GetSkillInfoAsync(skillName)` — Holt Skill-Metadaten (Name, Category, ExecutionType)

## Tests ausführen

```bash
# Alle Chatbot-Tests
dotnet test --filter "FullyQualifiedName~Klacks.E2ETest.Chatbot"

# Einzelne Test-Klasse
dotnet test --filter "FullyQualifiedName~ChatbotSettingsGeneralTest"

# Einzelner Test-Step
dotnet test --filter "FullyQualifiedName~ChatbotSettingsGeneralTest.Step4_ChangeAppName"
```

## Test-Patterns

### Typischer Settings-Test

```csharp
[Test, Order(1)]
public async Task Step1_OpenChat() { /* Chat öffnen */ }

[Test, Order(2)]
public async Task Step2_VerifyAdminRights()
{
    await AssertSkillEnabled("get_user_permissions");
    await SendChatMessage("Welche Rechte habe ich?");
    var response = await WaitForBotResponse();
    Assert.That(response, Does.Contain("Admin").IgnoreCase);
}

[Test, Order(3)]
public async Task Step3_ReadCurrentSettings()
{
    await AssertSkillEnabled("get_email_settings");
    await SendChatMessage("Zeige mir die aktuellen Email-Einstellungen.");
    var response = await WaitForBotResponse();
    // Verify current values
}

[Test, Order(4)]
public async Task Step4_UpdateSettings()
{
    await AssertSkillEnabled("update_email_settings"); // type=UiAction
    await SendChatMessage("Setze SMTP Server auf mail.test.ch...");
    var response = await WaitForBotResponse();
    Assert.That(response, Does.Contain("✅").Or.Contain("updated").IgnoreCase);
}
```

### Retry-Pattern für unzuverlässige LLM-Antworten

```csharp
for (int attempt = 1; attempt <= 3; attempt++)
{
    await SendChatMessage(prompt);
    var response = await WaitForBotResponse();
    if (VerifyCondition(response)) break;
    if (attempt == 3) Assert.Fail("After 3 attempts...");
}
```

## Nicht abgedeckte UiAction Skills (Stand März 2026)

Folgende Skills haben noch keine dedizierten E2E Chatbot-Tests:
- `update_work_settings`
- `update_scheduling_defaults`
- `update_deepl_settings`
- `create/update/delete_llm_provider` (nur Settings-Page-Tests, nicht via Chat)
- `create/update/delete_llm_model` (nur Settings-Page-Tests, nicht via Chat)
- `create/update/delete_scheduling_rule` (nur Settings-Page-Tests, nicht via Chat)
- `update_branch` (Branch-Test nutzt UiPassthrough create/delete, nicht update)
