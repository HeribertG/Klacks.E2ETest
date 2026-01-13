# E2E Test Dokumentation - Klacks

## Inhaltsverzeichnis

1. [Übersicht](#übersicht)
2. [Technischer Stack](#technischer-stack)
3. [Projektstruktur](#projektstruktur)
4. [Wichtige Konzepte](#wichtige-konzepte)
5. [Test-Setup](#test-setup)
6. [Best Practices](#best-practices)
7. [Beispiele](#beispiele)
8. [Troubleshooting](#troubleshooting)

---

## Übersicht

Die E2E-Tests für Klacks verwenden Playwright mit NUnit als Test-Framework. Die Tests sind darauf ausgelegt, echte Benutzer-Workflows zu simulieren und die Funktionalität der gesamten Anwendung zu validieren.

**Wichtig:** Die Tests laufen gegen den Development Server (`npm start` auf `localhost:4200`), NICHT gegen eine gebaute Version.

---

## Technischer Stack

| Komponente | Version | Zweck |
|------------|---------|-------|
| .NET | 10.0 | Test-Runtime |
| NUnit | 5.0 | Test-Framework |
| Playwright | Latest | Browser-Automatisierung |
| Angular Dev Server | 21.x | Frontend (localhost:4200) |
| .NET API | 10.0 | Backend |

---

## Projektstruktur

```
E2ETest/
├── Client/
│   ├── ClientCreationTest.cs          # Order 10: Client-Erstellungs-Tests (5 Clients)
│   ├── ClientSearchTest.cs            # Order 11: Client-Suche-Tests
│   ├── ClientTypeFilterTest.cs        # Order 12: Client Type Filter-Tests
│   ├── ClientAdvancedFiltersTest.cs   # Order 13: Client Advanced Filters-Tests
│   └── ClientDeletionTest.cs          # Order 14: Client-Löschungs-Tests (⚠️ WIP)
├── Settings/
│   ├── SettingsGeneralTest.cs         # Order 20: General-Tests
│   ├── SettingsOwnerAddressTest.cs    # Order 21: Owner Address-Tests
│   ├── SettingsUserAdministrationTest.cs # Order 22: User Administration-Tests
│   ├── SettingsGroupScopeTest.cs      # Order 23: Group Scope-Tests
│   ├── SettingsGridColorTest.cs       # Order 24: Grid Color-Tests
│   ├── SettingsStateTest.cs           # Order 25: State-Tests
│   ├── SettingsCountriesTest.cs       # Order 26: Countries-Tests
│   ├── SettingsEmailTest.cs           # Order 27: Email-Tests
│   ├── SettingsIdentityProviderTest.cs # Order 28: Identity Provider-Tests
│   ├── SettingsAbsenceTest.cs         # Order 29: Absence-Tests (CRUD)
│   └── SettingsCalendarRulesTest.cs   # Order 30: Calendar Rules-Tests (CRUD + API)
├── Constants/
│   ├── ClientIds.cs                   # IDs für Client-Elemente
│   ├── ClientTestData.cs              # Test-Daten für Client-Erstellung
│   ├── ClientFilterIds.cs             # IDs für Client-Filter
│   ├── ClientDeletionIds.cs           # IDs für Client-Löschung
│   ├── ContractIds.cs                 # IDs für Contract-Elemente
│   ├── GroupIds.cs                    # IDs für Gruppen-Elemente
│   ├── MainNavIds.cs                  # IDs für Haupt-Navigation
│   ├── SaveBarIds.cs                  # IDs für SaveBar-Elemente
│   ├── SettingsOwnerAddressIds.cs     # IDs für Owner Address
│   ├── SettingsUserAdministrationIds.cs # IDs für User Administration
│   ├── SettingsGroupScopeIds.cs       # IDs für Group Scope
│   ├── SettingsGridColorIds.cs        # IDs für Grid Color
│   ├── SettingsCountriesIds.cs        # IDs für Countries
│   ├── SettingsStatesIds.cs           # IDs für States
│   ├── SettingsEmailIds.cs            # IDs für Email
│   ├── SettingsIdentityProviderIds.cs # IDs für Identity Provider
│   ├── SettingsAbsenceIds.cs          # IDs für Absence
│   └── SettingsCalendarRulesIds.cs    # IDs für Calendar Rules
├── Helpers/
│   └── PlaywrightSetup.cs             # Base-Klasse für Tests
├── Wrappers/
│   ├── Wrapper.cs                     # Actions-Wrapper (WICHTIG!)
│   └── Listener.cs                    # API-Error-Listener
└── E2E-DOCUMENTATION.md               # Diese Dokumentation
```

## Test-Übersicht

| Order | Test-Datei | Beschreibung | Status |
|-------|------------|--------------|--------|
| 10 | `Client/ClientCreationTest.cs` | 5 Clients erstellen | ✅ |
| 11 | `Client/ClientSearchTest.cs` | Client Suche | ✅ |
| 12 | `Client/ClientTypeFilterTest.cs` | Client Type Filter | ✅ |
| 13 | `Client/ClientAdvancedFiltersTest.cs` | Client Advanced Filters | ✅ |
| 14 | `Client/ClientDeletionTest.cs` | 5 Clients löschen | ✅ |
| 20 | `Settings/SettingsGeneralTest.cs` | General Settings | ✅ |
| 21 | `Settings/SettingsOwnerAddressTest.cs` | Owner Address | ✅ |
| 22 | `Settings/SettingsUserAdministrationTest.cs` | User Administration | ✅ |
| 23 | `Settings/SettingsGroupScopeTest.cs` | Group Scope | ✅ |
| 24 | `Settings/SettingsGridColorTest.cs` | Grid Color | ✅ |
| 25 | `Settings/SettingsStateTest.cs` | States | ✅ |
| 26 | `Settings/SettingsCountriesTest.cs` | Countries | ✅ |
| 27 | `Settings/SettingsEmailTest.cs` | Email Settings | ✅ |
| 28 | `Settings/SettingsIdentityProviderTest.cs` | Identity Provider | ✅ |
| 29 | `Settings/SettingsAbsenceTest.cs` | Absence (CRUD) | ✅ |
| 30 | `Settings/SettingsCalendarRulesTest.cs` | Calendar Rules (CRUD + API) | ✅ |

---

## Wichtige Konzepte

### 1. Actions-Wrapper Pattern

**REGEL:** Verwende IMMER `Actions.*` Methoden, NIEMALS direkte `Page.*` Aufrufe!

```csharp
// ❌ FALSCH
var element = await Page.QuerySelectorAsync("#my-button");
await element.ClickAsync();

// ✅ RICHTIG
await Actions.ClickButtonById("my-button");
```

**Warum?**
- Einheitliche Fehlerbehandlung
- Automatische Wartezeiten
- Bessere Wartbarkeit
- Konsistente Timeouts

### 2. ID-basierte Selektoren

Alle UI-Elemente sollten eindeutige IDs haben:

```html
<!-- Frontend (Angular) -->
<button id="save-button">Speichern</button>
<app-button-new id="add-contract-button"></app-button-new>
```

```csharp
// E2E Test
public static readonly string SaveButton = "save-button";
public static readonly string AddContractButton = "add-contract-button";
```

### 3. Test-Organisation

**WICHTIG:** Tests für zusammenhängende Workflows sollten NICHT getrennt werden!

```csharp
// ❌ FALSCH - Getrennte Tests
[Test, Order(1)]
public async Task CreateClient() { /* ... */ }

[Test, Order(2)]  // Wird fehlschlagen - Page Context verloren!
public async Task AddContract() { /* ... */ }

// ✅ RICHTIG - Ein zusammenhängender Test
[Test, Order(1)]
public async Task CreateClientWithContract()
{
    // Client erstellen
    // ...

    // Contract hinzufügen
    // ...

    // Speichern
    await Actions.ClickButtonById(SaveBarIds.SaveButton);
}
```

### 4. Dynamische Elemente

Für dynamische Elemente (z.B. Gruppen-Tree) mit XPath und Namen arbeiten:

```csharp
// Für Gruppen-Auswahl (dynamische IDs)
await Actions.ExpandGroupNodeByName("Deutschweiz Mitte");
await Actions.SelectGroupByName("Bern");

// Für statische Selects (feste Indizes)
await Actions.SelectNativeOptionByIndex(ContractIds.GetContractSelectId(0), 3);
```

### 5. Index-basierte IDs für Tabellen

Für Listen/Tabellen verwenden wir Index-basierte IDs statt GUIDs:

```html
<!-- Frontend (Angular) -->
<tr [id]="'client-row-' + i">
  <td [id]="'client-firstname-' + i">{{ data.firstName }}</td>
  <td [id]="'client-lastname-' + i">{{ data.name }}</td>
</tr>
```

```csharp
// E2E Test - Einfacher Zugriff
var firstName = await Actions.GetTextContentById("client-firstname-0");
var lastName = await Actions.GetTextContentById("client-lastname-0");
var rowCount = await Actions.CountElementsBySelector("tr[id^='client-row-']");
```

### 6. Test-Daten externalisieren

Test-Daten in separate Klassen auslagern für bessere Wartbarkeit:

```csharp
// Constants/ClientTestData.cs
public class ClientData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    // ... weitere Felder
}

public static class ClientTestData
{
    public static readonly ClientData[] Clients = new[]
    {
        new ClientData { FirstName = "Heribert", LastName = "Gasparoli", ... },
        new ClientData { FirstName = "Marie-Anne", LastName = "Gasparoli", ... },
    };
}
```

---

## Test-Setup

### 1. Voraussetzungen

```bash
# In WSL (Ubuntu)
# Chromium installiert
/usr/bin/chromium-browser

# npm installiert
npm --version

# .NET 9.0
dotnet --version
```

### 2. Umgebung starten

```bash
# Terminal 1: Backend starten
cd /mnt/c/SourceCode/Klacks.Api
dotnet run

# Terminal 2: Frontend starten (Dev-Server)
cd /mnt/c/SourceCode/Klacks.Ui
npm start

# Warten bis "Application bundle generation complete" erscheint
```

### 3. Tests ausführen

```bash
# Alle Client-Tests
cd /mnt/c/SourceCode/E2ETest
dotnet test --filter "FullyQualifiedName~ClientCreationTest" --logger "console;verbosity=detailed"

# Einzelner Test
dotnet test --filter "FullyQualifiedName~ClientCreationTest.Step1_NavigateToClientPage"
```

---

## Best Practices

### ✅ DO

1. **Actions-Wrapper verwenden**
   ```csharp
   await Actions.ClickButtonById("my-button");
   await Actions.FillInputById("firstname", "Max");
   await Actions.WaitForSpinnerToDisappear();
   await Actions.GetTextContentById("client-firstname-0");
   await Actions.CountElementsBySelector("tr[id^='client-row-']");
   ```

2. **IDs in Constants definieren**
   ```csharp
   public static class ClientIds
   {
       public static readonly string SaveButton = "save-button";
       public static readonly string InputFirstName = "firstname";
   }
   ```

3. **Arrange-Act-Assert Pattern**
   ```csharp
   [Test]
   public async Task MyTest()
   {
       // Arrange
       TestContext.Out.WriteLine("=== Test Setup ===");

       // Act
       await Actions.ClickButtonById("my-button");

       // Assert
       Assert.That(_listener.HasApiErrors(), Is.False);
   }
   ```

4. **Logging für Nachvollziehbarkeit**
   ```csharp
   TestContext.Out.WriteLine("Clicked 'Save' button");
   TestContext.Out.WriteLine($"Current URL: {Actions.ReadCurrentUrl()}");
   ```

5. **Zusammenhängende Flows in einem Test**
   ```csharp
   [Test]
   public async Task CreateCompleteClient()
   {
       // Persönliche Daten
       await Actions.FillInputById("firstname", "Max");

       // Adresse
       await Actions.FillInputById("street", "Hauptstrasse 1");

       // Contract
       await Actions.ClickButtonById("add-contract-button");

       // Speichern
       await Actions.ClickButtonById("save-button");
   }
   ```

6. **Navigation über MainNav**
   ```csharp
   // ✅ RICHTIG - Navigation über MainNav-Button
   await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
   await Actions.WaitForSpinnerToDisappear();

   // ❌ FALSCH - Direkte URL-Navigation
   await Page.GotoAsync($"{BaseUrl}workplace/client");
   ```

7. **Test-State zurücksetzen**
   ```csharp
   // Nach Suche: Zurücksetzen für nächsten Test
   await Actions.ClearInputById("search");
   await Actions.ClickButtonById("search-button");
   await Actions.WaitForSpinnerToDisappear();
   ```

### ❌ DON'T

1. **KEINE direkten Page-Aufrufe**
   ```csharp
   // ❌ FALSCH
   await Page.ClickAsync("#button");
   var url = Page.Url;
   ```

2. **KEINE hartcodierten Selektoren**
   ```csharp
   // ❌ FALSCH
   await Actions.ClickButtonById("button-123");

   // ✅ RICHTIG
   await Actions.ClickButtonById(SaveBarIds.SaveButton);
   ```

3. **KEINE getrennten Tests für zusammenhängende Flows**
   ```csharp
   // ❌ FALSCH
   [Test, Order(1)]
   public async Task Step1() { }

   [Test, Order(2)]  // Page Context verloren!
   public async Task Step2() { }
   ```

4. **KEINE unnötigen Kommentare**
   ```csharp
   // ❌ FALSCH - zu viele Kommentare
   // Click the button
   await Actions.ClickButtonById("save-button");

   // ✅ RICHTIG - nur wichtige Kommentare
   await Actions.ClickButtonById(SaveBarIds.SaveButton);
   ```

---

## Beispiele

### Vollständiger Client-Erstellungs-Test

```csharp
[Test, Order(2)]
public async Task Step2_CreateNewClient()
{
    // Arrange
    TestContext.Out.WriteLine("=== Step 2: Create New Client ===");

    await Actions.NavigateTo($"{BaseUrl}workplace/client");
    await Actions.WaitForSpinnerToDisappear();
    await Actions.Wait1000();

    // Act - Client erstellen
    await Actions.ClickButtonById(ClientIds.NewClientButton);
    await Actions.WaitForSpinnerToDisappear();
    await Actions.Wait1000();

    // Persönliche Daten
    await Actions.FillInputById(ClientIds.InputFirstName, "Heribert");
    await Actions.FillInputById(ClientIds.InputLastName, "Gasparoli");
    await Actions.SelectNativeOptionById(ClientIds.InputGender, "1");

    // Adresse
    await Actions.FillInputById(ClientIds.InputStreet, "Kirchstrasse 52");
    await Actions.FillInputById(ClientIds.InputZip, "3097");
    await Actions.FillInputById(ClientIds.InputCity, "Liebefeld");
    await Actions.SelectNativeOptionById(ClientIds.InputCountry, "CH");
    await Actions.SelectNativeOptionById(ClientIds.InputState, "BE");

    // Contract hinzufügen
    await Actions.ScrollIntoViewById(ContractIds.AddContractButton);
    await Actions.ClickButtonById(ContractIds.AddContractButton);
    await Actions.SelectNativeOptionByIndex(ContractIds.GetContractSelectId(0), 3);
    await Actions.ClickCheckBoxById(ContractIds.GetActiveCheckboxId(0));

    // Gruppe hinzufügen
    await Actions.ScrollIntoViewById(GroupIds.AddGroupButton);
    await Actions.ClickButtonById(GroupIds.AddGroupButton);
    await Actions.ClickButtonById(GroupIds.DropdownToggle);
    await Actions.ExpandGroupNodeByName("Deutschweiz Mitte");
    await Actions.ExpandGroupNodeByName("BE");
    await Actions.SelectGroupByName("Bern");

    // Speichern
    await Actions.ClickButtonById(SaveBarIds.SaveButton);
    await Actions.WaitForSpinnerToDisappear();

    // Assert
    Assert.That(_listener.HasApiErrors(), Is.False,
        $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
}
```

### Neue Wrapper-Methode hinzufügen

```csharp
// In Wrappers/Wrapper.cs

/// <summary>
/// Klickt auf einen Button basierend auf seinem Text.
/// </summary>
public async Task ClickButtonByText(params string[] texts)
{
    await Wait500();
    var selector = string.Join(", ", texts.Select(t => $"button:has-text('{t}')"));
    var element = await FindElementByCssSelector(selector);
    if (element != null)
    {
        await element.ClickAsync(new() { Force = true, Timeout = WrapperConstants.DEFAULT_TIMEOUT });
    }
    await Wait500();
}
```

### ID zu UI-Element hinzufügen

```html
<!-- Vorher -->
<app-button-new
  placement="top"
  (click)="addGroup()">
</app-button-new>

<!-- Nachher -->
<app-button-new
  id="add-group-button"
  placement="top"
  (click)="addGroup()">
</app-button-new>
```

```csharp
// Constants/GroupIds.cs
public static class GroupIds
{
    public static readonly string AddGroupButton = "add-group-button";
}
```

---

## Troubleshooting

### Problem: "Target page, context or browser has been closed"

**Ursache:** Tests sind getrennt, aber sollten zusammenhängen.

**Lösung:** Kombiniere die Tests in einem einzigen Test-Method.

```csharp
// ❌ Getrennt
[Test, Order(1)]
public async Task CreateClient() { }

[Test, Order(2)]  // Fehler!
public async Task AddContract() { }

// ✅ Kombiniert
[Test, Order(1)]
public async Task CreateClientWithContract()
{
    // Beide Schritte hier
}
```

### Problem: "Element not found" / Timeout

**Ursache:** Element-Selector ist falsch oder Element braucht mehr Zeit.

**Lösung:**
1. ID im Frontend prüfen
2. Wartezeit erhöhen
3. ScrollIntoView verwenden

```csharp
// Erst scrollen, dann klicken
await Actions.ScrollIntoViewById("my-button");
await Actions.Wait500();
await Actions.ClickButtonById("my-button");
```

### Problem: Angular Dev Server nicht aktuell

**Frage:** Muss ich `ng build` machen?

**Antwort:** NEIN! Der Dev-Server (`npm start`) kompiliert automatisch.

**Aber:** Wenn der Server nicht neu startet:
1. Alle `npm start` Prozesse stoppen
2. Neu starten: `cd /mnt/c/SourceCode/Klacks.Ui && npm start`
3. Warten bis "Application bundle generation complete"

### Problem: Dynamische IDs (z.B. Gruppen)

**Lösung:** Mit Namen statt IDs arbeiten.

```csharp
// ❌ FALSCH - ID ist dynamisch
await Actions.ClickButtonById("group-019a6a3f-fbec-70cd");

// ✅ RICHTIG - Name ist stabil
await Actions.ExpandGroupNodeByName("Deutschweiz Mitte");
await Actions.SelectGroupByName("Bern");
```

### Problem: DLL gesperrt / "file is being used by another process"

**Lösung:** Test-Prozess beenden.

```bash
# Windows
taskkill /F /PID <process-id>

# WSL
pkill -f "dotnet test"
```

### Client-Suche und Filter-Test

```csharp
[Test, Order(1)]
public async Task Step1_SearchForClients()
{
    // Arrange
    TestContext.Out.WriteLine("=== Step 1: Search for Clients with 'gasp' ===");
    var searchTerm = "gasp";

    // Act
    await Actions.FillInputById("search", searchTerm);
    await Actions.Wait500();
    await Actions.ClickButtonById("search-button");
    await Actions.WaitForSpinnerToDisappear();
    await Actions.Wait1000();

    var rowCount = await Actions.CountElementsBySelector("tr[id^='client-row-']");

    // Assert
    Assert.That(rowCount, Is.EqualTo(3), "Should find 3 clients with 'gasp'");

    // Reset für nächsten Test
    await Actions.ClearInputById("search");
    await Actions.ClickButtonById("search-button");
    await Actions.WaitForSpinnerToDisappear();
}

[Test, Order(2)]
public async Task Step2_VerifyClientData()
{
    // Arrange
    await Actions.FillInputById("search", "heri");
    await Actions.ClickButtonById("search-button");
    await Actions.WaitForSpinnerToDisappear();

    // Act
    var firstName = await Actions.GetTextContentById("client-firstname-0");
    var lastName = await Actions.GetTextContentById("client-lastname-0");

    // Assert
    Assert.That(firstName, Is.EqualTo("Heribert"));
    Assert.That(lastName, Is.EqualTo("Gasparoli"));
}

[Test, Order(6)]
public async Task Step6_FilterByGroup()
{
    // Arrange
    TestContext.Out.WriteLine("=== Filter by Group 'Bern' ===");

    // Act
    await Actions.ClickButtonById("group-select-dropdown-toggle");
    await Actions.Wait1000();
    await Actions.ExpandGroupNodeByName("Deutschweiz Mitte");
    await Actions.ExpandGroupNodeByName("BE");
    await Actions.SelectGroupByName("Bern");
    await Actions.WaitForSpinnerToDisappear();

    var rowCount = await Actions.CountElementsBySelector("tr[id^='client-row-']");

    // Assert
    Assert.That(rowCount, Is.EqualTo(3), "All 3 clients are in group Bern");

    // Reset
    await Actions.ClickButtonById("group-select-dropdown-toggle");
    await Actions.ClickButtonById("group-select-all-groups");
}
```

---

## Weiterführende Informationen

### Playwright Dokumentation
- https://playwright.dev/dotnet/

### NUnit Dokumentation
- https://docs.nunit.org/

### Angular Testing Best Practices
- https://angular.io/guide/testing

---

**Letzte Aktualisierung:** 13.01.2026
**Version:** 1.3
**Autor:** E2E Test Team
