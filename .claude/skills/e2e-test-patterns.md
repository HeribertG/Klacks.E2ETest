# E2E Test Patterns & Konventionen

## Projekt-Struktur

```
Klacks.E2ETest/
├── .claude/skills/          # Claude Skills (diese Dateien)
├── Client/                  # Client-Tests (Order 10-19)
├── Constants/               # ID-Konstanten fuer DOM-Elemente
├── Gantt/                   # Gantt-Tests (Order 33-34)
├── Group/                   # Group-Tests (Order 35-39)
├── Helpers/                 # PlaywrightSetup, Actions, Listener
├── Login/                   # Login-Tests (Order 1)
├── Navigation/              # Navigation-Tests (Order 2-3)
├── Settings/                # Settings + LLM-Tests (Order 20-69)
├── Shifts/                  # Shift-Tests (Order 40-49)
├── Wrappers/                # Wrapper-Klasse fuer Page-Interaktionen
├── appsettings.*.json       # Konfiguration (URLs, Headless, etc.)
└── TEST-ORDER.md            # Uebersicht aller Test-Orders
```

## Basis-Klasse: PlaywrightSetup

Alle Testklassen erben von `PlaywrightSetup`:

```csharp
[TestFixture]
[Order(63)]
public class LlmBranchesTest : PlaywrightSetup
```

Bereitgestellte Properties: `Page`, `Actions`, `BaseUrl`, `UserName`, `Password`

## Konventionen

### Immer `Actions` statt `Page` verwenden

```csharp
// RICHTIG
await Actions.ClickButtonById("my-button");
await Actions.FindElementById("my-input");
await Actions.FillInputWithDispatch("input-id", "text");

// FALSCH - niemals Page direkt
await Page.ClickAsync("#my-button");
```

Ausnahme: `Page.QuerySelectorAllAsync()` fuer DOM-Suchen wo Actions keine Methode hat.

### Test-Struktur: Arrange / Act / Assert

```csharp
[Test]
[Order(3)]
public async Task Step3_CreateBranch()
{
    // Arrange
    TestContext.Out.WriteLine("=== Step 3: Create Branch ===");
    await EnsureChatOpen();

    // Act
    _messageCountBefore = await GetMessageCount();
    await SendChatMessage("Erstelle eine Filiale...");
    var response = await WaitForBotResponse(_messageCountBefore);

    // Assert
    Assert.That(response, Is.Not.Empty);
    Assert.That(_listener.HasApiErrors(), Is.False,
        $"No API errors. Error: {_listener.GetLastErrorMessage()}");
}
```

### ID-Konstanten in Constants-Ordner

```csharp
// Constants/LlmChatIds.cs
public static class LlmChatIds
{
    public const string HeaderAssistantButton = "header-assistant-button";
    public const string ChatInput = "chat-input";
    public const string ChatSendBtn = "chat-send-btn";
    public const string ChatClearBtn = "chat-clear-btn";
    public const string ChatMessages = "chat-messages";
}
```

### Listener fuer API-Fehler

```csharp
[SetUp]
public async Task Setup()
{
    _listener = new Listener(Page);
    _listener.RecognizeApiErrors();
}

[TearDown]
public void TearDown()
{
    if (_listener.HasApiErrors())
        TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
}
```

## LLM Chat Test Patterns

### Chat oeffnen und Input pruefen

```csharp
private async Task EnsureChatOpen()
{
    var chatInput = await Actions.FindElementById(ChatInput);
    if (chatInput == null)
    {
        await Actions.ClickButtonById(HeaderAssistantButton);
        await Actions.Wait1000();
    }
    await WaitForChatInputEnabled();
}
```

### Chat-Clear vor jeder Nachricht (wichtig!)

LLM-Nondeterminismus: Lange Konversationen fuehren dazu, dass das LLM verfuegbare Tools "vergisst". Deshalb vor jeder wichtigen Nachricht den Chat leeren:

```csharp
await Actions.ClickButtonById(ChatClearBtn);
await Actions.Wait1000();
await WaitForChatInputEnabled();
```

### Bot-Antwort abwarten

```csharp
_messageCountBefore = await GetMessageCount();
await SendChatMessage("Meine Frage...");
var response = await WaitForBotResponse(_messageCountBefore, 90000);
```

### Retry-Pattern fuer LLM-Aktionen

```csharp
private async Task CreateWithRetry(string name, int maxAttempts = 3)
{
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        await EnsureChatOpen();
        await Actions.ClickButtonById(ChatClearBtn);
        await Actions.Wait1000();
        await WaitForChatInputEnabled();

        _messageCountBefore = await GetMessageCount();
        await SendChatMessage($"Erstelle '{name}'...");
        var response = await WaitForBotResponse(_messageCountBefore, 120000);

        if (await WaitForElementInDom(name))
            return;

        await Actions.Wait2000();
    }
    Assert.Fail($"'{name}' was not created after {maxAttempts} attempts");
}
```

### Loeschen per Name (nicht per ID!)

Der User kennt keine internen IDs. Loeschbefehle immer via Name formulieren:

```csharp
// RICHTIG
await SendChatMessage($"Loesche die Filiale '{branchName}'");
await SendChatMessage($"Loesche das Macro '{macroName}'");
await SendChatMessage($"Loesche den Benutzer '{userName}'");

// FALSCH - User kennt keine IDs
await SendChatMessage($"Loesche die Filiale mit der ID {branchId}");
```

### DOM-Verifizierung nach UiPassthrough

UiPassthrough-Funktionen manipulieren das DOM. Nach Create/Delete pruefen:

```csharp
// Nach Create: Warten bis Element im DOM erscheint
private async Task<bool> WaitForElementInDom(string name, int timeoutMs = 30000)
{
    var startTime = DateTime.UtcNow;
    while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
    {
        if (await ElementExistsInDom(name))
            return true;
        await Actions.Wait500();
    }
    return false;
}

// Nach Delete: Warten bis Element aus DOM verschwunden
private async Task<bool> WaitForElementRemovedFromDom(string name, int timeoutMs = 20000)
{
    var startTime = DateTime.UtcNow;
    while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
    {
        if (!await ElementExistsInDom(name))
            return true;
        await Actions.Wait500();
    }
    return false;
}
```

## Angular-spezifische Patterns

### Formulare: blur-Event fuer onNameChange

```csharp
await Actions.FillInputWithDispatch("input-id", "value");
// Angular reagiert auf blur, nicht auf input:
await Page.EvaluateAsync("document.getElementById('input-id').dispatchEvent(new FocusEvent('blur'))");
```

### Modals: Polling fuer async NgbModal

```csharp
// NgbModal rendert async - Button polling statt sofortigem Click
var startTime = DateTime.UtcNow;
while ((DateTime.UtcNow - startTime).TotalMilliseconds < 5000)
{
    var confirmBtn = await Actions.FindElementById("modal-delete-confirm");
    if (confirmBtn != null)
    {
        await Actions.ClickButtonById("modal-delete-confirm");
        break;
    }
    await Actions.Wait500();
}
```

## Datenbankzugriff in Tests

Fuer direkte DB-Operationen (z.B. Password-Hash kopieren, Daten verifizieren):

```csharp
private static async Task<string> ExecuteSql(string sql)
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"klacks_e2e_{Guid.NewGuid():N}.sql");
    await File.WriteAllTextAsync(tempFile, sql);
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = @"C:\Program Files\PostgreSQL\17\bin\psql.exe",
            Arguments = $"-h localhost -p 5434 -U postgres -d klacks -t -A -f \"{tempFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["PGPASSWORD"] = "admin";
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output.Trim();
    }
    finally { File.Delete(tempFile); }
}
```

DB-Verbindung: `localhost:5434`, User `postgres`, Password `admin`, DB `klacks`.
