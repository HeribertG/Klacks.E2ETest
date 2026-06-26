// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the data-driven recipe "add-absence-for-employee" (place an absence on an
 * employee). The recipe chain is ask clientName -> search search_employees (capture clientId) ->
 * ask absenceType -> ask absenceDate -> search list_absence_types (surface the type ids, no capture) ->
 * mutate add_break (the model picks the absenceId matching the type the user named). DB-asserted via the
 * break row that links the employee to a workday. Single-turn (extraction): the opening message names the
 * employee, the absence kind and the date, so every slot is filled and the chain runs end to end without
 * asking. Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
public class ChatbotRecipeAddBreakTest : ChatbotTestBase
{
    private const string SkillSearchEmployees = "search_employees";
    private const string SkillListAbsenceTypes = "list_absence_types";
    private const string SkillAddBreak = "add_break";

    private const string NoActionNoticeMarker = "keine Aktion ausgeführt";

    // A date far enough in the future that a real break on it is vanishingly unlikely to pre-exist.
    private const string BreakDateIso = "2026-07-15";
    private const string BreakDateSpoken = "15. Juli 2026";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 3000;
    private const int SettleMaxPolls = 12;

    private string _clientId = string.Empty;

    [TearDown]
    public async Task RemoveTestBreak()
    {
        if (_clientId.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                $"UPDATE break SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
                $"AND client_id='{Escape(_clientId)}' AND workday='{BreakDateIso}'");
        }
    }

    [Test]
    public async Task Extraction_From_Opening_Message_Places_The_Absence()
    {
        await AssertSkillEnabled(SkillSearchEmployees);
        await AssertSkillEnabled(SkillListAbsenceTypes);
        await AssertSkillEnabled(SkillAddBreak);

        var (employeeName, clientId) = await UniqueEmployeeAsync();
        _clientId = clientId;
        Assert.That(employeeName, Is.Not.Empty, "a uniquely searchable employee is required");
        Assert.That(clientId, Is.Not.Empty);
        TestContext.Out.WriteLine($"[break] employee='{employeeName}' clientId='{clientId}'");

        await ResetBreak(clientId);
        var beforeAddBreak = await SuccessCallCountAsync(SkillAddBreak);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Trage für den Mitarbeiter {employeeName} eine Abwesenheit wegen Ferien am {BreakDateSpoken} ein.");
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (break): {Trim(response)}");

        await WaitForBreakAsync(clientId);

        var breakCount = await BreakCountAsync(clientId);
        var addBreakCalls = await SuccessCallCountAsync(SkillAddBreak) - beforeAddBreak;
        Assert.Multiple(() =>
        {
            Assert.That(breakCount, Is.EqualTo(1),
                "the absence must be placed on the employee for the given workday in a single turn");
            Assert.That(addBreakCalls, Is.GreaterThanOrEqualTo(1),
                "add_break must have run at least once");
            Assert.That(response, Does.Not.Contain(NoActionNoticeMarker),
                "a completed recipe must not emit the no-action notice");
        });
    }

    private async Task WaitForBreakAsync(string clientId)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await BreakCountAsync(clientId) >= 1)
            {
                return;
            }
        }
    }

    private static async Task ResetBreak(string clientId)
    {
        await DbHelper.ExecuteSqlAsync(
            $"UPDATE break SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
            $"AND client_id='{Escape(clientId)}' AND workday='{BreakDateIso}'");
    }

    private static async Task<int> BreakCountAsync(string clientId) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM break WHERE client_id='{Escape(clientId)}' " +
            $"AND workday='{BreakDateIso}' AND NOT is_deleted");

    // A type-1 employee whose "FirstName Name" resolves to EXACTLY ONE client under the real
    // search_employees logic (per-token Contains over FirstName/Name/Company, AND across tokens), so the
    // recipe's search step captures a single clientId.
    private static async Task<(string Name, string Id)> UniqueEmployeeAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT c.first_name || ' ' || c.name AS full_name, c.id FROM client c " +
            "WHERE c.type=1 AND NOT c.is_deleted AND c.first_name IS NOT NULL AND c.first_name<>'' " +
            "AND c.name IS NOT NULL AND c.name<>'' " +
            "AND (SELECT count(*) FROM client x WHERE NOT x.is_deleted " +
            "AND (lower(coalesce(x.first_name,'')) LIKE '%'||lower(c.first_name)||'%' OR lower(coalesce(x.name,'')) LIKE '%'||lower(c.first_name)||'%' OR lower(coalesce(x.company,'')) LIKE '%'||lower(c.first_name)||'%') " +
            "AND (lower(coalesce(x.first_name,'')) LIKE '%'||lower(c.name)||'%' OR lower(coalesce(x.name,'')) LIKE '%'||lower(c.name)||'%' OR lower(coalesce(x.company,'')) LIKE '%'||lower(c.name)||'%'))=1 " +
            "ORDER BY length(c.first_name || c.name) DESC LIMIT 1")).Trim();
        var parts = result.Split('\n')[0].Split('|');
        return parts.Length >= 2 ? (parts[0].Trim(), parts[1].Trim()) : (string.Empty, string.Empty);
    }

    private static async Task<int> SuccessCallCountAsync(string skillName) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM skill_usage_records WHERE skill_name='{Escape(skillName)}' AND success=true");

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(200, text.Length)];
}
