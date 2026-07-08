// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the bulk recipe "bulk-add-employees-to-group": fill a named group with all
 * employees matching a criterion (here: a canton), with a dry-run preview before applying. The flow is
 * ask groupName -> ask criteria -> mutate fill_group_by_criteria(apply=false) for a preview, then on the
 * user's confirmation the skill is called again with apply=true and self-verifies every new membership
 * by re-reading it from the database (rolling back the whole batch on any mismatch).
 *
 * Two assertions carry the slice:
 *   - Preview turn: the recipe runs but persists NOTHING (membership count unchanged).
 *   - Apply turn: memberships appear for the matched employees. Because the skill rolls back the entire
 *     batch when the database re-read does not confirm the writes, ANY persisted membership is itself
 *     proof that the server-side verification passed — a false success would have left zero rows.
 *
 * Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
[Category("Klacksy")]
public class ChatbotRecipeBulkFillGroupTest : ChatbotTestBase
{
    private const string SkillFill = "fill_group_by_criteria";
    private const string NoActionNoticeMarker = "keine Aktion ausgeführt";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 4000;
    private const int SettleMaxPolls = 20;

    private string _groupId = string.Empty;
    private string _canton = string.Empty;

    [TearDown]
    public async Task RemoveBulkMemberships()
    {
        if (_groupId.Length > 0 && _canton.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                "UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
                $"AND group_id='{Escape(_groupId)}' AND client_id IN (" +
                CantonEmployeeIdsSql(_canton) + ")");
        }
    }

    [Test]
    public async Task Bulk_Preview_Then_Apply_Fills_Group_And_Self_Verifies()
    {
        await AssertSkillEnabled(SkillFill);

        var (groupName, groupId) = await UniqueGroupAsync();
        Assert.That(groupName, Is.Not.Empty, "a uniquely searchable target group is required");
        _groupId = groupId;

        var (canton, candidateCount) = await BestCantonAsync(groupId);
        Assert.That(canton, Is.Not.Empty, "a canton with employees not yet in the group is required");
        Assert.That(candidateCount, Is.GreaterThanOrEqualTo(1));
        _canton = canton;
        TestContext.Out.WriteLine($"[bulk-fill] group='{groupName}' canton='{canton}' candidates={candidateCount}");

        await ResetMemberships(groupId, canton);
        var baseline = await MembershipCountAsync(groupId, canton);
        Assert.That(baseline, Is.EqualTo(0), "the canton's employees must start outside the target group");

        await EnsureChatOpen();
        await ClearChatAndWait();

        // Turn 1: full info in one message -> recipe extraction runs the chain to the preview (apply=false).
        var before1 = await GetMessageCount();
        await SendChatMessage(
            $"Füge alle Mitarbeiter aus dem Kanton {canton} in die Gruppe {groupName} ein.");
        var preview = await WaitForBotResponse(before1, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (preview): {Trim(preview)}");

        var afterPreview = await MembershipCountAsync(groupId, canton);
        Assert.Multiple(() =>
        {
            Assert.That(afterPreview, Is.EqualTo(0),
                "the preview (apply=false) must NOT persist any membership");
            Assert.That(preview, Does.Not.Contain(NoActionNoticeMarker),
                "a completed preview must not emit the no-action notice");
        });

        // Turn 2: confirm -> the model calls fill_group_by_criteria with apply=true, which self-verifies.
        var before2 = await GetMessageCount();
        await SendChatMessage("Ja, bitte wende das an und füge sie wirklich ein.");
        var applied = await WaitForBotResponse(before2, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (apply): {Trim(applied)}");

        await WaitForMembershipsAsync(groupId, canton);

        var afterApply = await MembershipCountAsync(groupId, canton);
        Assert.That(afterApply, Is.GreaterThanOrEqualTo(1),
            "after apply the matched employees must be members — and because the skill rolls the whole " +
            "batch back on a failed database re-read, any persisted membership proves verification passed");
    }

    private async Task WaitForMembershipsAsync(string groupId, string canton)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await MembershipCountAsync(groupId, canton) >= 1)
            {
                return;
            }
        }
    }

    private static async Task ResetMemberships(string groupId, string canton) =>
        await DbHelper.ExecuteSqlAsync(
            "UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
            $"AND group_id='{Escape(groupId)}' AND client_id IN ({CantonEmployeeIdsSql(canton)})");

    private static async Task<int> MembershipCountAsync(string groupId, string canton) =>
        await ScalarIntAsync(
            "SELECT count(*) FROM group_item gi WHERE NOT gi.is_deleted " +
            $"AND gi.group_id='{Escape(groupId)}' AND gi.client_id IN ({CantonEmployeeIdsSql(canton)})");

    // type-0 employees whose address is in the given canton (mirrors the SearchAsync canton filter:
    // c.Addresses.Any(a => a.State == canton)).
    private static string CantonEmployeeIdsSql(string canton) =>
        "SELECT c.id FROM client c WHERE c.type=0 AND NOT c.is_deleted AND EXISTS (" +
        "SELECT 1 FROM address a WHERE a.client_id=c.id AND NOT a.is_deleted " +
        $"AND upper(a.state)=upper('{Escape(canton)}'))";

    private static async Task<(string Name, string Id)> UniqueGroupAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT g.name, g.id FROM \"group\" g WHERE NOT g.is_deleted AND g.name IS NOT NULL AND g.name<>'' " +
            "AND g.name NOT LIKE 'E2E-%' " +
            "AND (SELECT count(*) FROM \"group\" x WHERE NOT x.is_deleted AND x.name ILIKE '%'||g.name||'%')=1 " +
            "ORDER BY length(g.name) DESC, g.name LIMIT 1")).Trim();
        var parts = result.Split('\n')[0].Split('|');
        return parts.Length >= 2 ? (parts[0].Trim(), parts[1].Trim()) : (string.Empty, string.Empty);
    }

    // The canton (address state code) with the most type-0 employees that are not already in the group.
    private static async Task<(string Canton, int Count)> BestCantonAsync(string groupId)
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT upper(a.state) AS canton, count(DISTINCT c.id) AS n " +
            "FROM client c JOIN address a ON a.client_id=c.id AND NOT a.is_deleted " +
            "WHERE c.type=0 AND NOT c.is_deleted AND a.state IS NOT NULL AND a.state<>'' " +
            "AND NOT EXISTS (SELECT 1 FROM group_item gi WHERE gi.client_id=c.id " +
            $"AND gi.group_id='{Escape(groupId)}' AND NOT gi.is_deleted) " +
            "GROUP BY upper(a.state) ORDER BY n DESC, canton LIMIT 1")).Trim();
        var parts = result.Split('\n')[0].Split('|');
        if (parts.Length < 2)
        {
            return (string.Empty, 0);
        }

        return (parts[0].Trim(), int.TryParse(parts[1].Trim(), out var n) ? n : 0);
    }

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(200, text.Length)];
}
