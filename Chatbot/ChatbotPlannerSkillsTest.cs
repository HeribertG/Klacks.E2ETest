// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Multi-LLM reliability test for Klacksy's planner skills end-to-end via the chat UI.
 *
 * Primary, DB-asserted test: cover_absence. Klacksy is given an absent employee (with a seeded work
 * on a date), a group and an absence type, and must call cover_absence (a single internal-orchestration
 * tool call, R1). The observable, non-flaky outcome is asserted via SQL: under the new scenario's token
 * a Break for the absent employee exists (+ an AnalyseScenario named "Absence cover ...") — never the
 * chat wording.
 *
 * Secondary smoke test (default model, NOT a correctness assertion): read_schedule_state + detect_conflicts
 * — only that Klacksy responds without an API error, because read skills have no DB side effect and the
 * streaming path does not log skill executions.
 *
 * Seed reuses a real (group, shift) pair and inserts only two prefixed test clients + memberships +
 * group items + one absent work; cleanup is idempotent and keyed on the test prefix + the scenario name.
 * Restores the original default LLM model on teardown.
 *
 * @param model - The api_model_id of the LLM model under test (drives default-model switching)
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Order(60)]
[Category("Klacksy")]
public class ChatbotPlannerSkillsTest : ChatbotTestBase
{
    private const string SkillCoverAbsence = "cover_absence";
    private const string SkillReadSchedule = "read_schedule_state";
    private const string SkillDetectConflicts = "detect_conflicts";

    private const int ActionTimeoutMs = 120000;
    private const int MaxConfirmTurns = 6;
    private const int DelayAfterModelSwitchMs = 1000;
    private const int ConfirmLoopDelayMs = 2500;

    private const string TestPrefix = "ETESTPLAN_";
    private const string AbsenceDate = "2099-08-17";
    private const string ScenarioNameLike = "Absence cover%";

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

    private static string _originalDefaultModel = string.Empty;
    private static readonly Random Rnd = new();

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
    [Explicit("Sweeps 11 LLM models sequentially (~10-15 min each); run on demand for model-reliability comparisons, not in default suite runs.")]
    public async Task Klacksy_CoversAbsence_PerModel(string model)
    {
        await AssertModelEnabled(model);
        await AssertSkillEnabled(SkillCoverAbsence);
        await SwitchDefaultModelAsync(model);

        var (groupId, shiftId, groupName) = await ResolveRealGroupAndShiftAsync();
        Assert.That(groupId, Is.Not.Empty, "Need a real (group, shift) pair in the database.");
        var absenceId = await ResolveAbsenceIdAsync();
        Assert.That(absenceId, Is.Not.Empty, "Need at least one absence type in the database.");

        var absentId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        TestContext.Out.WriteLine($"=== [cover_absence/{model}] group='{groupName}' absent={absentId} candidate={candidateId} ===");

        await CleanupAsync(groupId);
        try
        {
            await SeedAbsenceScenarioAsync(groupId, shiftId, absentId, candidateId);

            await EnsureChatOpen();
            await ClearChatAndWait();
            var before = await GetMessageCount();
            await SendChatMessage(
                $"Der Mitarbeiter mit der ID {absentId} ist am {AbsenceDate} abwesend (Abwesenheitstyp {absenceId}). " +
                $"Organisiere bitte direkt einen Ersatz in der Gruppe mit der ID {groupId}. " +
                "Rufe dazu den Skill cover_absence auf und führe ihn sofort aus.");
            var response = await WaitForBotResponse(before, ActionTimeoutMs);
            TestContext.Out.WriteLine($"Bot: {TrimText(response)}");

            await ConfirmUntilAsync(() => ScenarioBreakExistsAsync(absentId));

            var (ok, detail) = await AssertCoverOutcomeAsync(absentId, groupId);
            TestContext.Out.WriteLine($"[cover_absence/{model}] result: ok={ok} detail={detail}");
            Assert.That(ok, Is.True, $"[{model}] cover_absence outcome check failed: {detail}");
        }
        finally
        {
            await CleanupAsync(groupId);
        }
    }

    [Test]
    public async Task Klacksy_ReadsAndDetects_Smoke()
    {
        // Smoke only: read skills have no DB side effect and the streaming path does not log skill
        // executions, so this asserts only that Klacksy answers without an API error — NOT correctness.
        await AssertSkillEnabled(SkillReadSchedule);
        await AssertSkillEnabled(SkillDetectConflicts);

        var (groupId, _, groupName) = await ResolveRealGroupAndShiftAsync();
        Assert.That(groupId, Is.Not.Empty, "Need a real group in the database.");
        TestContext.Out.WriteLine($"=== [read/detect smoke] group='{groupName}' ({groupId}) ===");

        await EnsureChatOpen();
        await ClearChatAndWait();
        var before = await GetMessageCount();
        await SendChatMessage(
            $"Zeig mir den aktuellen Einsatzplan der Gruppe mit der ID {groupId} fuer den Zeitraum {AbsenceDate} bis {AbsenceDate} " +
            "und pruefe ihn auf Konflikte.");
        var response = await WaitForBotResponse(before, ActionTimeoutMs);
        TestContext.Out.WriteLine($"Bot: {TrimText(response)}");

        Assert.That(response, Is.Not.Empty, "Klacksy must answer the read/detect request.");
        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API error expected during read/detect smoke. Last: {TestListener.GetLastErrorMessage()}");
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
                "Ja, bitte jetzt direkt cover_absence ausfuehren und das Szenario anlegen. Frag nicht weiter nach.");
            var response = await WaitForBotResponse(before, ActionTimeoutMs);
            TestContext.Out.WriteLine($"Confirm {turn + 1}: {TrimText(response)}");
        }

        await Task.Delay(ConfirmLoopDelayMs);
    }

    private static async Task<(string groupId, string shiftId, string groupName)> ResolveRealGroupAndShiftAsync()
    {
        var sql =
            "SELECT gi.group_id, gi.shift_id, g.name FROM group_item gi " +
            "JOIN \"group\" g ON g.id = gi.group_id " +
            "JOIN shift s ON s.id = gi.shift_id " +
            "WHERE gi.shift_id IS NOT NULL AND NOT gi.is_deleted " +
            "AND s.analyse_token IS NULL AND NOT s.is_deleted AND NOT g.is_deleted " +
            "ORDER BY random() LIMIT 1";
        var raw = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        var parts = raw.Split('|');
        return parts.Length >= 3
            ? (parts[0].Trim(), parts[1].Trim(), parts[2].Trim())
            : (string.Empty, string.Empty, string.Empty);
    }

    private static async Task<string> ResolveAbsenceIdAsync()
    {
        var sql = "SELECT id FROM absence WHERE NOT is_deleted ORDER BY random() LIMIT 1";
        return (await DbHelper.ExecuteSqlAsync(sql)).Trim();
    }

    private static async Task SeedAbsenceScenarioAsync(string groupId, string shiftId, Guid absentId, Guid candidateId)
    {
        var absentName = $"{TestPrefix}ABSENT_{Rnd.Next(10000, 99999)}";
        var candidateName = $"{TestPrefix}CAND_{Rnd.Next(10000, 99999)}";

        var sql =
            $"INSERT INTO client (id, name, gender, legal_entity, type, is_deleted, create_time) VALUES " +
            $"('{absentId}', '{Escape(absentName)}', 1, false, 0, false, now()), " +
            $"('{candidateId}', '{Escape(candidateName)}', 1, false, 0, false, now());\n" +

            $"INSERT INTO group_item (id, client_id, group_id, valid_from, is_deleted, create_time) VALUES " +
            $"(gen_random_uuid(), '{absentId}', '{Escape(groupId)}', now(), false, now()), " +
            $"(gen_random_uuid(), '{candidateId}', '{Escape(groupId)}', now(), false, now());\n" +

            $"INSERT INTO membership (id, client_id, type, valid_from, is_deleted, create_time) VALUES " +
            $"(gen_random_uuid(), '{absentId}', 0, '2000-01-01', false, now()), " +
            $"(gen_random_uuid(), '{candidateId}', 0, '2000-01-01', false, now());\n" +

            $"INSERT INTO work (id, shift_id, client_id, workday, work_time, surcharges, start_time, end_time, lock_level, is_deleted, create_time) VALUES " +
            $"(gen_random_uuid(), '{Escape(shiftId)}', '{absentId}', '{AbsenceDate}', 8, 0, '08:00', '16:00', 0, false, now());";

        var result = await DbHelper.ExecuteSqlAsync(sql);
        if (result.StartsWith("ERROR:"))
        {
            Assert.Fail($"Seed failed: {result}");
        }
    }

    private static async Task<bool> ScenarioBreakExistsAsync(Guid absentId)
    {
        var sql =
            $"SELECT count(*) FROM break WHERE client_id = '{absentId}' AND analyse_token IS NOT NULL " +
            $"AND workday = '{AbsenceDate}' AND NOT is_deleted";
        return ParseInt((await DbHelper.ExecuteSqlAsync(sql)).Trim()) > 0;
    }

    private static async Task<(bool ok, string detail)> AssertCoverOutcomeAsync(Guid absentId, string groupId)
    {
        var breakSql =
            $"SELECT count(*) FROM break WHERE client_id = '{absentId}' AND analyse_token IS NOT NULL " +
            $"AND workday = '{AbsenceDate}' AND NOT is_deleted";
        var breakCount = ParseInt((await DbHelper.ExecuteSqlAsync(breakSql)).Trim());

        var scenarioSql =
            $"SELECT count(*) FROM analyse_scenarios WHERE group_id = '{Escape(groupId)}' " +
            $"AND name LIKE '{ScenarioNameLike}' AND NOT is_deleted";
        var scenarioCount = ParseInt((await DbHelper.ExecuteSqlAsync(scenarioSql)).Trim());

        var detail = $"scenario_break(absent)={breakCount}, absence_cover_scenarios(group)={scenarioCount}";
        var ok = breakCount > 0 && scenarioCount > 0;
        return (ok, detail);
    }

    private static async Task CleanupAsync(string groupId)
    {
        var tokenSubquery =
            $"SELECT token FROM analyse_scenarios WHERE group_id = '{Escape(groupId)}' AND name LIKE '{ScenarioNameLike}'";
        var clientSubquery = $"SELECT id FROM client WHERE name LIKE '{TestPrefix}%'";

        var sql =
            // scenario-scoped cloned data (under our cover_absence scenario tokens)
            $"DELETE FROM work_change WHERE analyse_token IN ({tokenSubquery});\n" +
            $"DELETE FROM break WHERE analyse_token IN ({tokenSubquery});\n" +
            $"DELETE FROM work WHERE analyse_token IN ({tokenSubquery});\n" +
            $"DELETE FROM shift WHERE analyse_token IN ({tokenSubquery});\n" +
            $"DELETE FROM analyse_scenarios WHERE group_id = '{Escape(groupId)}' AND name LIKE '{ScenarioNameLike}';\n" +
            // our seeded test clients and everything keyed on them
            $"DELETE FROM work_change WHERE work_id IN (SELECT id FROM work WHERE client_id IN ({clientSubquery})) OR replace_client_id IN ({clientSubquery});\n" +
            $"DELETE FROM break WHERE client_id IN ({clientSubquery});\n" +
            $"DELETE FROM work WHERE client_id IN ({clientSubquery});\n" +
            $"DELETE FROM group_item WHERE client_id IN ({clientSubquery});\n" +
            $"DELETE FROM membership WHERE client_id IN ({clientSubquery});\n" +
            $"DELETE FROM client WHERE name LIKE '{TestPrefix}%';";

        var result = await DbHelper.ExecuteSqlAsync(sql);
        if (result.StartsWith("ERROR:"))
        {
            TestContext.Out.WriteLine($"Cleanup warning: {result}");
        }
    }

    private static int ParseInt(string value) => int.TryParse(value.Trim(), out var n) ? n : 0;

    private static string Escape(string value) => value.Replace("'", "''");

    private static string TrimText(string text) => text[..Math.Min(160, text.Length)];
}
