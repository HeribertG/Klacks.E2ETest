// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the data-driven recipe "dienst-in-gruppe-aufnehmen" (add a shift/order to a group). The
 * recipe chain is ask shiftName -> search search_shifts (capture shiftId) -> ask groupName ->
 * search list_groups (capture groupId) -> mutate add_shift_to_group. DB-asserted via the group_item row
 * that links the shift to the group. Single-turn (extraction): the opening message names the shift and the
 * group, so both search slots resolve and the chain runs end to end without asking. Explicit: LLM-driven
 * and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
public class ChatbotRecipeAddShiftToGroupTest : ChatbotTestBase
{
    private const string SkillSearchShifts = "search_shifts";
    private const string SkillListGroups = "list_groups";
    private const string SkillAddShiftToGroup = "add_shift_to_group";

    private const string NoActionNoticeMarker = "keine Aktion ausgeführt";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 3000;
    private const int SettleMaxPolls = 12;

    private string _shiftId = string.Empty;
    private string _groupId = string.Empty;

    [TearDown]
    public async Task RemoveTestLink()
    {
        if (_shiftId.Length > 0 && _groupId.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                $"UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
                $"AND shift_id='{Escape(_shiftId)}' AND group_id='{Escape(_groupId)}'");
        }
    }

    [Test]
    public async Task Extraction_From_Opening_Message_Links_Shift_To_Group()
    {
        await AssertSkillEnabled(SkillSearchShifts);
        await AssertSkillEnabled(SkillListGroups);
        await AssertSkillEnabled(SkillAddShiftToGroup);

        var (shiftName, shiftId) = await UniqueShiftAsync();
        var (groupName, groupId) = await UniqueGroupAsync();
        _shiftId = shiftId;
        _groupId = groupId;
        Assert.That(shiftName, Is.Not.Empty, "a uniquely searchable shift is required");
        Assert.That(groupName, Is.Not.Empty, "a uniquely searchable group is required");
        TestContext.Out.WriteLine($"[shift→group] shift='{shiftName}' group='{groupName}'");

        await ResetLink(shiftId, groupId);
        var beforeAdd = await SuccessCallCountAsync(SkillAddShiftToGroup);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Füge den Dienst {shiftName} zur Gruppe {groupName} hinzu.");
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (shift→group): {Trim(response)}");

        await WaitForLinkAsync(shiftId, groupId);

        var linkCount = await LinkCountAsync(shiftId, groupId);
        var addCalls = await SuccessCallCountAsync(SkillAddShiftToGroup) - beforeAdd;
        Assert.Multiple(() =>
        {
            Assert.That(linkCount, Is.EqualTo(1),
                "the shift must be linked to the group in a single turn (extraction filled all slots)");
            Assert.That(addCalls, Is.GreaterThanOrEqualTo(1),
                "add_shift_to_group must have run at least once");
            Assert.That(response, Does.Not.Contain(NoActionNoticeMarker),
                "a completed recipe must not emit the no-action notice");
        });
    }

    private async Task WaitForLinkAsync(string shiftId, string groupId)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await LinkCountAsync(shiftId, groupId) >= 1)
            {
                return;
            }
        }
    }

    private static async Task ResetLink(string shiftId, string groupId)
    {
        await DbHelper.ExecuteSqlAsync(
            $"UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
            $"AND shift_id='{Escape(shiftId)}' AND group_id='{Escape(groupId)}'");
    }

    private static async Task<int> LinkCountAsync(string shiftId, string groupId) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM group_item WHERE shift_id='{Escape(shiftId)}' " +
            $"AND group_id='{Escape(groupId)}' AND NOT is_deleted");

    // A shift DEFINITION (status 0) — these are the only shifts search_shifts (GetTruncatedListQuery)
    // surfaces; sealed orders / split parts are not searchable. Picks one whose name resolves to exactly
    // one definition under a Contains match, so the recipe's search step captures a single shiftId.
    private static async Task<(string Name, string Id)> UniqueShiftAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT s.name, s.id FROM shift s " +
            "WHERE NOT s.is_deleted AND s.analyse_token IS NULL AND s.status=0 AND s.name IS NOT NULL AND s.name<>'' " +
            "AND (SELECT count(*) FROM shift x WHERE NOT x.is_deleted AND x.analyse_token IS NULL AND x.status=0 " +
            "AND x.name ILIKE '%'||s.name||'%')=1 " +
            "ORDER BY length(s.name) DESC LIMIT 1")).Trim();
        var parts = result.Split('\n')[0].Split('|');
        return parts.Length >= 2 ? (parts[0].Trim(), parts[1].Trim()) : (string.Empty, string.Empty);
    }

    private static async Task<(string Name, string Id)> UniqueGroupAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT g.name, g.id FROM \"group\" g WHERE NOT g.is_deleted AND g.name IS NOT NULL AND g.name<>'' " +
            "AND (SELECT count(*) FROM \"group\" x WHERE NOT x.is_deleted AND x.name ILIKE '%'||g.name||'%')=1 " +
            "ORDER BY length(g.name) DESC, g.name LIMIT 1")).Trim();
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
