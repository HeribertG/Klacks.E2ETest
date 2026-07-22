// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Multi-LLM reliability test for Klacksy creating a client (employee) end-to-end via the
 * chat UI. Each test case runs against a specific LLM model and verifies clean data in the
 * database: system-generated id_number, unique id_number, type=Employee, membership, email,
 * phone (with correct prefix/value split), address with country=CH and non-empty state.
 * Hard-deletes test clients between runs; restores the original default LLM model on teardown.
 * @param model - The api_model_id of the LLM model under test (drives default-model switching)
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Order(59)]
[Explicit("Multi-LLM reliability benchmark (11 models x single/multi-turn create) plus rotating edit ops; " +
    "irreducibly flaky in normal runs (model-capability variance, multi-turn nondeterminism, LLM-driven " +
    "creation latency). Run on demand via explicit --filter (release gate); stays out of the default suite " +
    "like the other chatbot benchmark fixtures. [Explicit] (not [Ignore]) so a name filter actually runs it.")]
[Category("Klacksy")]
public class ChatbotCreateEmployeeTest : ChatbotTestBase
{
    private const string SkillCreateEmployee = "create_employee";
    private const string SkillAddClientPhone = "add_client_phone";
    private const int CreateTimeoutMs = 120000;
    private const int MaxConfirmTurns = 6;

    private const int CommTypePhone = 1;
    private const int CommTypeEmail = 4;
    private const int ClientTypeEmployee = 0;
    private const int MembershipCountExpected = 1;
    private const int DelayAfterModelSwitchMs = 1000;
    private const int ConfirmLoopDelayMs = 2500;
    private const int TurnDelayMs = 1500;

    private const string ModelGeminiFlash25 = "gemini-2.5-flash";
    private const string ModelGeminiFlash35 = "gemini-3.5-flash";
    private const string ModelClaudeHaiku = "claude-haiku-4-5-20251001";
    private const string ModelClaudeSonnet = "claude-sonnet-4-6";
    private const string ModelDeepseekPro = "deepseek-v4-pro";
    private const string ModelSwissAiApertus = "swissai-apertus-70b";
    private const string ModelGroqLlama33 = "llama-3.3-70b-versatile";
    private const string ModelGptOss120b = "openai/gpt-oss-120b";
    private const string ModelGptOss20b = "openai/gpt-oss-20b";
    private const string ModelGptOssSafeguard20b = "openai/gpt-oss-safeguard-20b";
    private const string ModelQwen332b = "qwen/qwen3-32b";

    private const string PhonePrefix = "+41";
    private const string CountryCodeCh = "CH";

    private static string _originalDefaultModel = string.Empty;

    public static readonly string[] LlmModels =
    {
        ModelGeminiFlash25,
        ModelGeminiFlash35,
        ModelClaudeHaiku,
        ModelClaudeSonnet,
        ModelDeepseekPro,
        ModelSwissAiApertus,
        ModelGroqLlama33,
        ModelGptOss120b,
        ModelGptOss20b,
        ModelGptOssSafeguard20b,
        ModelQwen332b
    };

    private static readonly string[] EditOps = { "phone", "email", "note", "group_add", "group_remove", "birthdate", "gender", "assign_contract" };
    private static readonly string[] FirstNamePool =
        { "Heribert", "Anna", "Marco", "Lisa", "Tobias", "Sara", "Yasmine", "Pascal", "Nadia", "Bruno" };
    private static readonly Random Rnd = new();

    [OneTimeSetUp]
    public async Task SnapshotDefaultModel()
    {
        var sql = "SELECT api_model_id FROM llm_models WHERE is_default = true LIMIT 1";
        _originalDefaultModel = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        TestContext.Out.WriteLine($"[matrix] original default model: '{_originalDefaultModel}'");
    }

    [OneTimeTearDown]
    public async Task RestoreDefaultModel()
    {
        if (string.IsNullOrEmpty(_originalDefaultModel))
            return;

        var esc = Escape(_originalDefaultModel);
        var sql = $"UPDATE llm_models SET is_default = (api_model_id = '{esc}') WHERE is_enabled = true;";
        await DbHelper.ExecuteSqlAsync(sql);
        TestContext.Out.WriteLine($"[matrix] restored default model to: '{_originalDefaultModel}'");
    }

    [Test]
    [TestCaseSource(nameof(LlmModels))]
    public async Task Klacksy_CreatesEmployee_SingleMessage_PerModel(string model)
    {
        await AssertModelEnabled(model);
        await AssertSkillEnabled(SkillCreateEmployee);
        await SwitchDefaultModelAsync(model);

        var firstName = FirstNamePool[Rnd.Next(FirstNamePool.Length)];
        var lastName = $"Etest{Rnd.Next(10000, 99999)}";
        TestContext.Out.WriteLine($"=== [single-msg/{model}] '{firstName} {lastName}' ===");

        await CleanupClientAsync(lastName);
        try
        {
            await CreateSingleMessageAsync(firstName, lastName);
            var (ok, detail) = await AssertCleanDataAsync(lastName);
            TestContext.Out.WriteLine($"[single-msg/{model}] result: ok={ok} detail={detail}");
            Assert.That(ok, Is.True, $"[{model}] clean-data check failed: {detail}");
        }
        finally
        {
            await CleanupClientAsync(lastName);
        }
    }

    [Test]
    [TestCaseSource(nameof(LlmModels))]
    public async Task Klacksy_CreatesEmployee_MultiTurn_PerModel(string model)
    {
        await AssertModelEnabled(model);
        await AssertSkillEnabled(SkillCreateEmployee);
        await SwitchDefaultModelAsync(model);

        var firstName = FirstNamePool[Rnd.Next(FirstNamePool.Length)];
        var lastName = $"Etest{Rnd.Next(10000, 99999)}";
        TestContext.Out.WriteLine($"=== [multi-turn/{model}] '{firstName} {lastName}' ===");

        await CleanupClientAsync(lastName);
        try
        {
            await CreateMultiTurnAsync(firstName, lastName);
            var (ok, detail) = await AssertCleanDataAsync(lastName);
            TestContext.Out.WriteLine($"[multi-turn/{model}] result: ok={ok} detail={detail}");
            Assert.That(ok, Is.True, $"[{model}] clean-data check failed: {detail}");
        }
        finally
        {
            await CleanupClientAsync(lastName);
        }
    }

    [Test]
    public async Task Klacksy_EditOperations_Rotating_Reliably()
    {
        await AssertSkillEnabled(SkillAddClientPhone);

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

    private static async Task AssertModelEnabled(string model)
    {
        var sql = $"SELECT is_enabled FROM llm_models WHERE api_model_id = '{Escape(model)}' LIMIT 1";
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        if (result != "t")
            Assert.Inconclusive($"Model '{model}' is not enabled in llm_models — skipping.");
    }

    private static async Task SwitchDefaultModelAsync(string model)
    {
        var esc = Escape(model);
        var sql = $"UPDATE llm_models SET is_default = (api_model_id = '{esc}') WHERE is_enabled = true;";
        await DbHelper.ExecuteSqlAsync(sql);
        TestContext.Out.WriteLine($"[matrix] switched default model to: '{model}'");
        await Task.Delay(DelayAfterModelSwitchMs);
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
        TestContext.Out.WriteLine($"Edit bot: {TrimText(response)}");
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
        TestContext.Out.WriteLine($"Bot: {TrimText(response)}");

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
            TestContext.Out.WriteLine($"Bot: {TrimText(response)}");
            await Task.Delay(TurnDelayMs);
        }

        await ConfirmUntilAsync(() => ClientExistsAsync(lastName));
    }

    private async Task ConfirmUntilAsync(Func<Task<bool>> done)
    {
        for (var turn = 0; turn < MaxConfirmTurns; turn++)
        {
            await Task.Delay(ConfirmLoopDelayMs);
            if (await done())
            {
                return;
            }

            var before = await GetMessageCount();
            await SendChatMessage(
                "Ja, bitte jetzt direkt ausfuehren und speichern. Frag nicht weiter nach und navigiere nicht.");
            var response = await WaitForBotResponse(before, CreateTimeoutMs);
            TestContext.Out.WriteLine($"Confirm {turn + 1}: {TrimText(response)}");
        }

        await Task.Delay(ConfirmLoopDelayMs);
    }

    private static async Task<(bool ok, string detail)> AssertCleanDataAsync(string lastName)
    {
        var clientSql =
            "SELECT c.id_number, c.type, " +
            "(SELECT count(*) FROM membership m WHERE m.client_id=c.id AND NOT m.is_deleted), " +
            $"(SELECT count(*) FROM communication co WHERE co.client_id=c.id AND co.type={CommTypeEmail} AND NOT co.is_deleted), " +
            $"(SELECT count(*) FROM communication co WHERE co.client_id=c.id AND co.type={CommTypePhone} AND NOT co.is_deleted), " +
            "(SELECT count(*) FROM address a WHERE a.client_id=c.id AND NOT a.is_deleted), " +
            "(SELECT COALESCE(a.country,'') FROM address a WHERE a.client_id=c.id AND NOT a.is_deleted LIMIT 1), " +
            "(SELECT COALESCE(a.state,'') FROM address a WHERE a.client_id=c.id AND NOT a.is_deleted LIMIT 1) " +
            $"FROM client c WHERE c.name='{Escape(lastName)}' AND NOT c.is_deleted LIMIT 1";

        var clientResult = (await DbHelper.ExecuteSqlAsync(clientSql)).Trim();
        var parts = clientResult.Split('|');
        if (parts.Length < 8)
        {
            return (false, $"client not found or query error (raw='{clientResult}')");
        }

        var idNumber = ParseInt(parts[0]);
        var type = ParseInt(parts[1]);
        var membershipCount = ParseInt(parts[2]);
        var emailCount = ParseInt(parts[3]);
        var phoneCount = ParseInt(parts[4]);
        var addressCount = ParseInt(parts[5]);
        var country = parts[6].Trim();
        var state = parts[7].Trim();

        string phonePrefix = string.Empty;
        string phoneValue = string.Empty;
        if (phoneCount > 0)
        {
            var phoneSql =
                $"SELECT COALESCE(co.prefix,''), COALESCE(co.value,'') FROM communication co " +
                $"JOIN client c ON c.id=co.client_id " +
                $"WHERE c.name='{Escape(lastName)}' AND co.type={CommTypePhone} AND NOT co.is_deleted LIMIT 1";
            var phoneResult = (await DbHelper.ExecuteSqlAsync(phoneSql)).Trim();
            var phoneParts = phoneResult.Split('|');
            if (phoneParts.Length >= 2)
            {
                phonePrefix = phoneParts[0].Trim();
                phoneValue = phoneParts[1].Trim();
            }
        }

        string idUniqueness = string.Empty;
        if (idNumber > 0)
        {
            var uniqueSql = $"SELECT count(*) FROM client WHERE id_number = {idNumber} AND NOT is_deleted";
            idUniqueness = (await DbHelper.ExecuteSqlAsync(uniqueSql)).Trim();
        }

        var idUnique = idUniqueness == "1";
        var phonePrefixOk = phonePrefix == PhonePrefix;
        var phoneValueOk = phoneCount == 0 || (!phoneValue.StartsWith("+") && !phoneValue.StartsWith("0"));

        var detail =
            $"id_number={idNumber}(unique_count={idUniqueness}), type={type}, membership={membershipCount}, " +
            $"email={emailCount}, phone={phoneCount}, address={addressCount}, " +
            $"country='{country}', state='{state}', " +
            $"phone_prefix='{phonePrefix}', phone_value='{phoneValue}'";

        var ok =
            idNumber > 0 &&
            idUnique &&
            type == ClientTypeEmployee &&
            membershipCount == MembershipCountExpected &&
            emailCount >= 1 &&
            phoneCount >= 1 &&
            addressCount >= 1 &&
            country == CountryCodeCh &&
            !string.IsNullOrEmpty(state) &&
            phonePrefixOk &&
            phoneValueOk;

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

    private static string TrimText(string text) => text[..Math.Min(160, text.Length)];
}
