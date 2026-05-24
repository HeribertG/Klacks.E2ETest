// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

/// <summary>
/// Reliability test for Klacksy creating and updating a client (employee) end-to-end via the
/// chat UI. Verifies each run against the database: client with system-generated id_number,
/// mandatory membership, email + phone communications, correct entity type, and (for the edit
/// flow) a newly added phone. Hard-deletes the test client between runs so each iteration is clean.
/// </summary>
[TestFixture]
[Order(59)]
public class ChatbotCreateEmployeeTest : ChatbotTestBase
{
    private const string SkillCreateEmployee = "create_employee";
    private const int CreateTimeoutMs = 120000;
    private const int Iterations = 3;
    private const int EditIterations = 2;
    private const int MaxConfirmTurns = 6;

    private const int CommTypePhone = 1;
    private const int CommTypeEmail = 4;

    [Test]
    public async Task Klacksy_CreatesEmployee_SingleMessage_Reliably()
    {
        await AssertSkillEnabled(SkillCreateEmployee);
        await RunReliabilityLoopAsync("single-message", Iterations, CreateSingleMessageAsync, AssertFullOnboardingAsync);
    }

    [Test]
    public async Task Klacksy_CreatesEmployee_MultiTurn_Reliably()
    {
        await AssertSkillEnabled(SkillCreateEmployee);
        await RunReliabilityLoopAsync("multi-turn", EditIterations, CreateMultiTurnAsync, AssertFullOnboardingAsync);
    }

    private static readonly string[] EditOps = { "phone", "email", "note", "group_add", "group_remove", "birthdate", "gender", "assign_contract" };
    private static readonly string[] FirstNamePool =
        { "Heribert", "Anna", "Marco", "Lisa", "Tobias", "Sara", "Yasmine", "Pascal", "Nadia", "Bruno" };
    private static readonly Random Rnd = new();

    [Test]
    public async Task Klacksy_EditOperations_Rotating_Reliably()
    {
        await AssertSkillEnabled("add_client_phone");

        var groupName = await GetRandomGroupNameAsync();
        Assert.That(groupName, Is.Not.Empty, "Need at least one real group in the database for the group ops.");
        TestContext.Out.WriteLine($"[edit-rot] using real group: '{groupName}'");

        var contractName = await GetRandomContractNameAsync();
        TestContext.Out.WriteLine($"[edit-rot] using real contract: '{(string.IsNullOrEmpty(contractName) ? "(none)" : contractName)}'");

        var successes = 0;
        var failures = new List<string>();

        for (var i = 0; i < EditOps.Length; i++)
        {
            var op = EditOps[i];
            var firstName = FirstNamePool[Rnd.Next(FirstNamePool.Length)];
            var lastName = $"Etest{Rnd.Next(10000, 99999)}";
            TestContext.Out.WriteLine($"=== [edit-rot] {i + 1}/{EditOps.Length} op={op} '{firstName} {lastName}' ===");

            await CleanupClientAsync(lastName);

            if (!await CreateBaseClientAsync(firstName, lastName))
            {
                failures.Add($"{op}: base client not created");
                await CleanupClientAsync(lastName);
                continue;
            }

            var (ok, detail) = await RunEditOpAsync(op, firstName, lastName, groupName, contractName);
            if (ok)
            {
                successes++;
                TestContext.Out.WriteLine($"[edit-rot] op={op}: SUCCESS ({detail})");
            }
            else
            {
                failures.Add($"{op}: {detail}");
                TestContext.Out.WriteLine($"[edit-rot] op={op}: FAILED ({detail})");
            }

            await CleanupClientAsync(lastName);
        }

        TestContext.Out.WriteLine($"[edit-rot] {successes}/{EditOps.Length} edit ops succeeded.");
        Assert.That(successes, Is.EqualTo(EditOps.Length),
            $"All rotating edit ops must succeed. Failures: {string.Join(" | ", failures)}");
    }

    private async Task<bool> CreateBaseClientAsync(string firstName, string lastName)
    {
        await EnsureChatOpen();
        await ClearChatAndWait();
        var before = await GetMessageCount();
        await SendChatMessage(
            $"Erstelle einen neuen Mitarbeiter (entityType Employee): Vorname {firstName}, Nachname {lastName}, " +
            "Geschlecht maennlich, Adresse Kirchstrasse 52, 3097 Liebefeld, Kanton BE, Schweiz, " +
            $"Email {EmailFor(lastName)}, Telefon 079 555 11 22. Bitte lege ihn direkt an.");
        await WaitForBotResponse(before, CreateTimeoutMs);
        await ConfirmUntilAsync(() => ClientExistsAsync(lastName));
        return await ClientExistsAsync(lastName);
    }

    private async Task<(bool ok, string detail)> RunEditOpAsync(string op, string firstName, string lastName, string groupName, string contractName)
    {
        await EnsureChatOpen();
        await ClearChatAndWait();

        switch (op)
        {
            case "phone":
            {
                var phone = $"079{Rnd.Next(1000000, 9999999)}";
                await SendEditAsync($"Fuege bei {firstName} {lastName} die Telefonnummer {phone} hinzu. Bitte direkt speichern.");
                await ConfirmUntilAsync(() => HasPhoneAsync(lastName, phone));
                var ok = await HasPhoneAsync(lastName, phone);
                return (ok, ok ? $"phone {phone}" : $"phone {phone} not added");
            }
            case "email":
            {
                var email = $"rot{Rnd.Next(10000, 99999)}@klacks-e2e.ch";
                await SendEditAsync($"Fuege bei {firstName} {lastName} die Email {email} hinzu. Bitte direkt speichern.");
                await ConfirmUntilAsync(() => HasEmailAsync(lastName, email));
                var ok = await HasEmailAsync(lastName, email);
                return (ok, ok ? $"email {email}" : $"email {email} not added");
            }
            case "note":
            {
                var token = $"E2ENOTE{Rnd.Next(10000, 99999)}";
                await SendEditAsync($"Fuege bei {firstName} {lastName} eine Notiz hinzu mit dem Text '{token}'. Bitte direkt speichern.");
                await ConfirmUntilAsync(() => HasNoteAsync(lastName, token));
                var ok = await HasNoteAsync(lastName, token);
                return (ok, ok ? $"note {token}" : $"note {token} not added");
            }
            case "group_add":
            {
                await SendEditAsync($"Fuege {firstName} {lastName} zur Gruppe {groupName} hinzu. Bitte direkt speichern.");
                await ConfirmUntilAsync(() => HasGroupAsync(lastName, groupName));
                var ok = await HasGroupAsync(lastName, groupName);
                return (ok, ok ? $"in group {groupName}" : $"not added to group {groupName}");
            }
            case "group_remove":
            {
                await SetupGroupMembershipAsync(lastName, groupName);
                if (!await HasGroupAsync(lastName, groupName))
                {
                    return (false, "group_remove setup failed (could not pre-add membership)");
                }
                await SendEditAsync($"Entferne {firstName} {lastName} aus der Gruppe {groupName}. Bitte direkt speichern.");
                await ConfirmUntilAsync(async () => !await HasGroupAsync(lastName, groupName));
                var ok = !await HasGroupAsync(lastName, groupName);
                return (ok, ok ? $"removed from group {groupName}" : $"still in group {groupName}");
            }
            case "birthdate":
            {
                var year = Rnd.Next(1960, 2000);
                var month = Rnd.Next(1, 13);
                var day = Rnd.Next(1, 29);
                var date = $"{year:D4}-{month:D2}-{day:D2}";
                await SendEditAsync($"Setze das Geburtsdatum von {firstName} {lastName} auf {date}. Bitte direkt speichern.");
                await ConfirmUntilAsync(() => HasBirthdateAsync(lastName, date));
                var ok = await HasBirthdateAsync(lastName, date);
                return (ok, ok ? $"birthdate {date}" : $"birthdate {date} not set");
            }
            case "gender":
            {
                var isFemale = Rnd.Next(2) == 0;
                var genderWord = isFemale ? "weiblich" : "maennlich";
                var genderInt = isFemale ? 0 : 1;
                await SendEditAsync($"Setze das Geschlecht von {firstName} {lastName} auf {genderWord}. Bitte direkt speichern.");
                await ConfirmUntilAsync(() => HasGenderAsync(lastName, genderInt));
                var ok = await HasGenderAsync(lastName, genderInt);
                return (ok, ok ? $"gender {genderWord}" : $"gender {genderWord} not set");
            }
            case "assign_contract":
            {
                if (string.IsNullOrEmpty(contractName))
                {
                    return (true, "skipped (no contract in DB)");
                }
                var fromDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                await SendEditAsync($"Weise {firstName} {lastName} den Vertrag '{contractName}' zu, gueltig ab {fromDate}. Bitte direkt speichern.");
                await ConfirmUntilAsync(() => HasContractAsync(lastName, contractName));
                var ok = await HasContractAsync(lastName, contractName);
                return (ok, ok ? $"contract '{contractName}'" : $"contract '{contractName}' not assigned");
            }
            default:
                return (false, $"unknown op {op}");
        }
    }

    private async Task SendEditAsync(string message)
    {
        var before = await GetMessageCount();
        await SendChatMessage(message);
        var response = await WaitForBotResponse(before, CreateTimeoutMs);
        TestContext.Out.WriteLine($"Edit bot: {Trim(response)}");
    }

    private async Task RunReliabilityLoopAsync(
        string label,
        int iterations,
        Func<string, string, Task> flow,
        Func<string, Task<(bool ok, string detail)>> verify)
    {
        var successes = 0;
        var failures = new List<string>();

        for (var i = 1; i <= iterations; i++)
        {
            var firstName = FirstNamePool[Rnd.Next(FirstNamePool.Length)];
            var lastName = $"Etest{Rnd.Next(10000, 99999)}";
            TestContext.Out.WriteLine($"=== [{label}] Iteration {i}/{iterations}: '{firstName} {lastName}' ===");

            await CleanupClientAsync(lastName);

            await flow(firstName, lastName);
            var (ok, detail) = await verify(lastName);

            if (ok)
            {
                successes++;
                TestContext.Out.WriteLine($"[{label}] Iteration {i}: SUCCESS ({detail})");
            }
            else
            {
                failures.Add($"Iter {i}: {detail}");
                TestContext.Out.WriteLine($"[{label}] Iteration {i}: FAILED ({detail})");
            }

            await CleanupClientAsync(lastName);
        }

        TestContext.Out.WriteLine($"[{label}] Reliability: {successes}/{iterations} succeeded.");
        Assert.That(successes, Is.EqualTo(iterations),
            $"[{label}] must succeed in all {iterations} runs. Failures: {string.Join(" | ", failures)}");
    }

    private async Task CreateSingleMessageAsync(string firstName, string lastName)
    {
        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Erstelle einen neuen Mitarbeiter (entityType Employee): Vorname {firstName}, Nachname {lastName}, " +
            "Geschlecht maennlich, Geburtsdatum 1959-10-25, Adresse Kirchstrasse 52, 3097 Liebefeld, Kanton BE, Schweiz, " +
            $"Email {EmailFor(lastName)}, Telefon 079 555 11 22. Bitte lege ihn direkt an.");
        var response = await WaitForBotResponse(before, CreateTimeoutMs);
        TestContext.Out.WriteLine($"Bot: {Trim(response)}");

        await ConfirmUntilAsync(() => ClientExistsAsync(lastName));
    }

    private async Task CreateMultiTurnAsync(string firstName, string lastName)
    {
        await EnsureChatOpen();
        await ClearChatAndWait();

        var turns = new[]
        {
            "Kannst du fuer mich einen neuen Mitarbeiter (Employee) erstellen?",
            $"Name {firstName} {lastName}, Geschlecht maennlich, Geburtsdatum 1959-10-25, " +
            "Adresse Kirchstrasse 52, 3097 Liebefeld, Kanton BE, Schweiz, " +
            $"Email {EmailFor(lastName)}, Telefon 079 555 11 22."
        };

        foreach (var turn in turns)
        {
            if (await ClientExistsAsync(lastName))
            {
                break;
            }

            var before = await GetMessageCount();
            await SendChatMessage(turn);
            var response = await WaitForBotResponse(before, CreateTimeoutMs);
            TestContext.Out.WriteLine($"User: {turn}");
            TestContext.Out.WriteLine($"Bot: {Trim(response)}");
            await Task.Delay(1500);
        }

        await ConfirmUntilAsync(() => ClientExistsAsync(lastName));
    }

    private async Task ConfirmUntilAsync(Func<Task<bool>> done)
    {
        for (var turn = 0; turn < MaxConfirmTurns; turn++)
        {
            await Task.Delay(2500);
            if (await done())
            {
                return;
            }

            var before = await GetMessageCount();
            await SendChatMessage(
                "Ja, bitte jetzt direkt ausfuehren und speichern. Frag nicht weiter nach und navigiere nicht.");
            var response = await WaitForBotResponse(before, CreateTimeoutMs);
            TestContext.Out.WriteLine($"Confirm {turn + 1}: {Trim(response)}");
        }

        await Task.Delay(2500);
    }

    private static async Task<(bool ok, string detail)> AssertFullOnboardingAsync(string lastName)
    {
        var sql =
            "SELECT c.id_number, c.type, " +
            "(SELECT count(*) FROM membership m WHERE m.client_id=c.id AND NOT m.is_deleted), " +
            $"(SELECT count(*) FROM communication co WHERE co.client_id=c.id AND co.type={CommTypeEmail}), " +
            $"(SELECT count(*) FROM communication co WHERE co.client_id=c.id AND co.type={CommTypePhone}), " +
            "(SELECT count(*) FROM address a WHERE a.client_id=c.id AND NOT a.is_deleted) " +
            $"FROM client c WHERE c.name='{Escape(lastName)}' AND NOT c.is_deleted LIMIT 1";
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        var parts = result.Split('|');
        if (parts.Length < 6)
        {
            return (false, $"client not found (raw='{result}')");
        }

        var idNumber = ParseInt(parts[0]);
        var type = ParseInt(parts[1]);
        var membership = ParseInt(parts[2]);
        var emails = ParseInt(parts[3]);
        var phones = ParseInt(parts[4]);
        var addresses = ParseInt(parts[5]);

        var detail = $"id_number={idNumber}, type={type}, membership={membership}, email={emails}, phone={phones}, address={addresses}";
        var ok = idNumber > 0 && type == 0 && membership == 1 && emails >= 1 && phones >= 1 && addresses >= 1;
        return (ok, detail);
    }

    private static async Task<bool> ClientExistsAsync(string lastName)
    {
        var sql = $"SELECT COALESCE(MAX(id_number),0) FROM client WHERE name='{Escape(lastName)}' AND NOT is_deleted";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<bool> HasPhoneAsync(string lastName, string phoneFragment)
    {
        var digits = new string(phoneFragment.Where(char.IsDigit).ToArray());
        var sql =
            "SELECT count(*) FROM communication co JOIN client c ON c.id=co.client_id " +
            $"WHERE c.name='{Escape(lastName)}' AND NOT co.is_deleted " +
            $"AND regexp_replace(co.value,'[^0-9]','','g') LIKE '%{Escape(digits)}%'";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<bool> HasEmailAsync(string lastName, string email)
    {
        var sql =
            "SELECT count(*) FROM communication co JOIN client c ON c.id=co.client_id " +
            $"WHERE c.name='{Escape(lastName)}' AND NOT co.is_deleted AND lower(co.value)=lower('{Escape(email)}')";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<bool> HasNoteAsync(string lastName, string token)
    {
        var sql =
            "SELECT count(*) FROM annotation a JOIN client c ON c.id=a.client_id " +
            $"WHERE c.name='{Escape(lastName)}' AND NOT a.is_deleted AND a.note LIKE '%{Escape(token)}%'";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<bool> HasGroupAsync(string lastName, string groupName)
    {
        var sql =
            "SELECT count(*) FROM group_item gi JOIN client c ON c.id=gi.client_id JOIN \"group\" g ON g.id=gi.group_id " +
            $"WHERE c.name='{Escape(lastName)}' AND NOT gi.is_deleted AND g.name ILIKE '%{Escape(groupName)}%'";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<string> GetRandomGroupNameAsync()
    {
        var sql = "SELECT name FROM \"group\" WHERE NOT is_deleted AND name ~ '^[A-Za-z0-9 ]+$' ORDER BY random() LIMIT 1";
        return (await DbHelper.ExecuteSqlAsync(sql)).Trim();
    }

    private static async Task<string> GetRandomContractNameAsync()
    {
        var sql = "SELECT name FROM contract WHERE NOT is_deleted ORDER BY random() LIMIT 1";
        return (await DbHelper.ExecuteSqlAsync(sql)).Trim();
    }

    private static async Task<bool> HasBirthdateAsync(string lastName, string isoDate)
    {
        var sql =
            $"SELECT count(*) FROM client WHERE name='{Escape(lastName)}' AND NOT is_deleted " +
            $"AND birthdate::date = '{Escape(isoDate)}'::date";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<bool> HasGenderAsync(string lastName, int genderInt)
    {
        var sql =
            $"SELECT count(*) FROM client WHERE name='{Escape(lastName)}' AND NOT is_deleted " +
            $"AND gender = {genderInt}";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<bool> HasContractAsync(string lastName, string contractName)
    {
        var sql =
            "SELECT count(*) FROM client_contract cc " +
            "JOIN client c ON c.id = cc.client_id " +
            "JOIN contract ct ON ct.id = cc.contract_id " +
            $"WHERE c.name='{Escape(lastName)}' AND NOT c.is_deleted " +
            $"AND ct.name ILIKE '%{Escape(contractName)}%'";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task SetupGroupMembershipAsync(string lastName, string groupName)
    {
        var sql =
            "INSERT INTO group_item (id, client_id, group_id, valid_from, is_deleted, create_time) " +
            "SELECT gen_random_uuid(), c.id, g.id, now(), false, now() " +
            $"FROM client c, \"group\" g WHERE c.name='{Escape(lastName)}' AND NOT c.is_deleted " +
            $"AND g.name ILIKE '%{Escape(groupName)}%' AND NOT g.is_deleted LIMIT 1";
        await DbHelper.ExecuteSqlAsync(sql);
    }

    private static async Task CleanupClientAsync(string lastName)
    {
        var esc = Escape(lastName);
        var sql =
            $"DELETE FROM address WHERE client_id IN (SELECT id FROM client WHERE name='{esc}');\n" +
            $"DELETE FROM communication WHERE client_id IN (SELECT id FROM client WHERE name='{esc}');\n" +
            $"DELETE FROM annotation WHERE client_id IN (SELECT id FROM client WHERE name='{esc}');\n" +
            $"DELETE FROM membership WHERE client_id IN (SELECT id FROM client WHERE name='{esc}');\n" +
            $"DELETE FROM group_item WHERE client_id IN (SELECT id FROM client WHERE name='{esc}');\n" +
            $"DELETE FROM client_contract WHERE client_id IN (SELECT id FROM client WHERE name='{esc}');\n" +
            $"DELETE FROM client_period_hours WHERE client_id IN (SELECT id FROM client WHERE name='{esc}');\n" +
            $"DELETE FROM client WHERE name='{esc}';";
        var result = await DbHelper.ExecuteSqlAsync(sql);
        if (result.StartsWith("ERROR:"))
        {
            TestContext.Out.WriteLine($"Cleanup warning for '{lastName}': {result}");
        }
    }

    private static string EmailFor(string lastName) => $"{lastName.ToLowerInvariant()}@klacks-e2e.ch";

    private static int ParseInt(string value) => int.TryParse(value.Trim(), out var n) ? n : 0;

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(160, text.Length)];
}
