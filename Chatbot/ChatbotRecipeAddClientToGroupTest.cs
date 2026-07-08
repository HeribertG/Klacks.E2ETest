// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the data-driven recipe engine on the "add-employee-to-group" recipe
 * (ask / search+capture / mutate, with a durable slot bag across turns). Two flows, both DB-asserted
 * via the group_item row that links the employee to the group:
 *   - Single-turn (extraction): the opening message names group, employee and start date, so the
 *     start-of-recipe extraction fills every slot and NO question is asked — the chain runs end to end
 *     in one turn (search list_groups -> capture groupId, search search_employees -> capture clientId,
 *     mutate add_client_to_group). Asserts the membership exists and the no-action notice is absent.
 *   - Multi-turn (pause/resume): a bare mutation-intent opener with no specifics pauses on the first
 *     ask. The advisor-flagged anchor is asserted here: a recipe that deliberately pauses on an ask
 *     must NOT emit the no-action notice and must NOT force-retry a tool (no membership yet). The
 *     follow-up turns supply group, employee and date; the final turn creates the membership.
 * Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
[Category("Klacksy")]
public class ChatbotRecipeAddClientToGroupTest : ChatbotTestBase
{
    private const string SkillListGroups = "list_groups";
    private const string SkillSearchEmployees = "search_employees";
    private const string SkillAddToGroup = "add_client_to_group";

    // Substring of MutationGuardConstants.NoActionStreamNotice — its presence on an ask-pause turn is
    // exactly the guard collision the engine must avoid (kept in sync with the backend constant).
    private const string NoActionNoticeMarker = "keine Aktion ausgeführt";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 3000;
    private const int SettleMaxPolls = 12;

    private string _clientId = string.Empty;
    private string _groupId = string.Empty;

    [TearDown]
    public async Task RemoveTestMembership()
    {
        if (_clientId.Length > 0 && _groupId.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                $"UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
                $"AND client_id='{Escape(_clientId)}' AND group_id='{Escape(_groupId)}'");
        }
    }

    [Test]
    public async Task Extraction_From_Opening_Message_Runs_Without_Asking()
    {
        await AssertSkillEnabled(SkillAddToGroup);

        var (groupName, groupId) = await UniqueGroupAsync();
        var (employeeName, clientId) = await UniqueEmployeeNotInGroupAsync(groupId);
        _groupId = groupId;
        _clientId = clientId;
        Assert.That(groupName, Is.Not.Empty, "a uniquely searchable group is required");
        Assert.That(employeeName, Is.Not.Empty, "a uniquely searchable employee not yet in the group is required");
        TestContext.Out.WriteLine($"[extract] group='{groupName}' employee='{employeeName}'");

        await ResetMembership(clientId, groupId);
        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Füge den Mitarbeiter {employeeName} zur Gruppe {groupName} hinzu, gültig ab 1. Mai 2026.");
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (extract): {Trim(response)}");

        await WaitForMembershipAsync(clientId, groupId);

        var membership = await MembershipCountAsync(clientId, groupId);
        Assert.Multiple(() =>
        {
            Assert.That(membership, Is.EqualTo(1),
                "the employee must be added to the group in a single turn (extraction filled all slots)");
            Assert.That(response, Does.Not.Contain(NoActionNoticeMarker),
                "a completed recipe must not emit the no-action notice");
        });
    }

    [Test]
    public async Task Multi_Turn_Pause_And_Resume_Creates_Membership()
    {
        await AssertSkillEnabled(SkillAddToGroup);

        var (groupName, groupId) = await UniqueGroupAsync();
        var (employeeName, clientId) = await UniqueEmployeeNotInGroupAsync(groupId);
        _groupId = groupId;
        _clientId = clientId;
        Assert.That(groupName, Is.Not.Empty);
        Assert.That(employeeName, Is.Not.Empty);
        TestContext.Out.WriteLine($"[multiturn] group='{groupName}' employee='{employeeName}'");

        await ResetMembership(clientId, groupId);
        await EnsureChatOpen();
        await ClearChatAndWait();

        // Turn 1: a bare mutation-intent opener with no specifics — the recipe must pause on the first ask.
        var before1 = await GetMessageCount();
        await SendChatMessage("Ich möchte einen Mitarbeiter zu einer Gruppe hinzufügen.");
        var opener = await WaitForBotResponse(before1, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (turn1/ask): {Trim(opener)}");

        var membershipAfterOpener = await MembershipCountAsync(clientId, groupId);
        Assert.Multiple(() =>
        {
            // The advisor-flagged anchor: a deliberate ask-pause must not trip the no-action guards.
            Assert.That(opener, Does.Not.Contain(NoActionNoticeMarker),
                "an intentional ask-pause must NOT emit the no-action notice");
            Assert.That(membershipAfterOpener, Is.EqualTo(0),
                "the opener must pause for input, NOT force-retry a tool that creates a membership");
        });

        // Turn 2: name the group.
        var before2 = await GetMessageCount();
        await SendChatMessage(groupName);
        TestContext.Out.WriteLine($"Bot (turn2): {Trim(await WaitForBotResponse(before2, TurnTimeoutMs))}");

        // Turn 3: name the employee.
        var before3 = await GetMessageCount();
        await SendChatMessage(employeeName);
        TestContext.Out.WriteLine($"Bot (turn3): {Trim(await WaitForBotResponse(before3, TurnTimeoutMs))}");

        // Turn 4: supply the start date — this turn runs the guard + mutate.
        var before4 = await GetMessageCount();
        await SendChatMessage("Gültig ab 1. Mai 2026.");
        TestContext.Out.WriteLine($"Bot (turn4): {Trim(await WaitForBotResponse(before4, TurnTimeoutMs))}");

        await WaitForMembershipAsync(clientId, groupId);

        var membership = await MembershipCountAsync(clientId, groupId);
        Assert.That(membership, Is.EqualTo(1),
            "the multi-turn recipe must create exactly one membership after all slots are supplied");
    }

    private async Task WaitForMembershipAsync(string clientId, string groupId)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await MembershipCountAsync(clientId, groupId) >= 1)
            {
                return;
            }
        }
    }

    private static async Task ResetMembership(string clientId, string groupId)
    {
        await DbHelper.ExecuteSqlAsync(
            $"UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
            $"AND client_id='{Escape(clientId)}' AND group_id='{Escape(groupId)}'");
    }

    private static async Task<int> MembershipCountAsync(string clientId, string groupId) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM group_item WHERE client_id='{Escape(clientId)}' " +
            $"AND group_id='{Escape(groupId)}' AND NOT is_deleted");

    private static async Task<(string Name, string Id)> UniqueGroupAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT g.name, g.id FROM \"group\" g WHERE NOT g.is_deleted AND g.name IS NOT NULL AND g.name<>'' " +
            "AND (SELECT count(*) FROM \"group\" x WHERE NOT x.is_deleted AND x.name ILIKE '%'||g.name||'%')=1 " +
            "ORDER BY length(g.name) DESC, g.name LIMIT 1")).Trim();
        var parts = result.Split('\n')[0].Split('|');
        return parts.Length >= 2 ? (parts[0].Trim(), parts[1].Trim()) : (string.Empty, string.Empty);
    }

    // A type-1 employee whose "FirstName Name" resolves to EXACTLY ONE client under the real
    // search_employees logic (per-token Contains over FirstName/Name/Company, AND across tokens, across
    // all client types) and who is not already in the target group. Returns the full name as the search
    // term, so the recipe's search step captures a single clientId.
    private static async Task<(string Name, string Id)> UniqueEmployeeNotInGroupAsync(string groupId)
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT c.first_name || ' ' || c.name AS full_name, c.id FROM client c " +
            "WHERE c.type=1 AND NOT c.is_deleted AND c.first_name IS NOT NULL AND c.first_name<>'' " +
            "AND c.name IS NOT NULL AND c.name<>'' " +
            "AND (SELECT count(*) FROM client x WHERE NOT x.is_deleted " +
            "AND (lower(coalesce(x.first_name,'')) LIKE '%'||lower(c.first_name)||'%' OR lower(coalesce(x.name,'')) LIKE '%'||lower(c.first_name)||'%' OR lower(coalesce(x.company,'')) LIKE '%'||lower(c.first_name)||'%') " +
            "AND (lower(coalesce(x.first_name,'')) LIKE '%'||lower(c.name)||'%' OR lower(coalesce(x.name,'')) LIKE '%'||lower(c.name)||'%' OR lower(coalesce(x.company,'')) LIKE '%'||lower(c.name)||'%'))=1 " +
            $"AND NOT EXISTS (SELECT 1 FROM group_item gi WHERE gi.client_id=c.id AND gi.group_id='{Escape(groupId)}' AND NOT gi.is_deleted) " +
            "ORDER BY length(c.first_name || c.name) DESC LIMIT 1")).Trim();
        var parts = result.Split('\n')[0].Split('|');
        return parts.Length >= 2 ? (parts[0].Trim(), parts[1].Trim()) : (string.Empty, string.Empty);
    }

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(200, text.Length)];
}
