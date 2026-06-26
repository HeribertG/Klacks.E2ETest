// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the data-driven recipe "create-group" (create a new group). The recipe chain is
 * ask groupName -> ask validFrom -> ask placement (root or parent group) -> ask calendarName ->
 * search list_calendars (capture calendarId) -> search list_groups (surface parent candidates, no capture)
 * -> mutate create_group. Two flows, both DB-asserted:
 *   - Root level: the opening message asks for a top-level group; the created group must have the requested
 *     holiday calendar and NO parent.
 *   - Sub group: the opening message nests the new group under an existing parent group; the created group
 *     must carry that parent id (and the holiday calendar).
 * Single-turn (extraction): the opening message names every slot, so the chain runs without asking.
 * Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
public class ChatbotRecipeNewGroupTest : ChatbotTestBase
{
    private const string SkillCreateGroup = "create_group";

    private const string NoActionNoticeMarker = "keine Aktion ausgeführt";
    private const string ValidFromSpoken = "1. August 2026";
    private const string CalendarName = "Kanton Zürich";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 4000;
    private const int SettleMaxPolls = 30;

    private string _groupName = string.Empty;

    [TearDown]
    public async Task RemoveTestGroup()
    {
        if (_groupName.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                $"UPDATE \"group\" SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
                $"AND name='{Escape(_groupName)}'");
        }
    }

    [Test]
    public async Task Root_Level_Group_Is_Created_With_Calendar_And_No_Parent()
    {
        await AssertSkillEnabled(SkillCreateGroup);

        _groupName = "E2E-NeueGruppe-" + Guid.NewGuid().ToString("N")[..8];
        TestContext.Out.WriteLine($"[new-group/root] name='{_groupName}'");

        var beforeCreate = await SuccessCallCountAsync(SkillCreateGroup);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Erstelle eine neue Gruppe namens '{_groupName}', gültig ab {ValidFromSpoken}, " +
            $"mit dem Kalender {CalendarName}, auf oberster Ebene.");
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (new-group/root): {Trim(response)}");

        await WaitForGroupAsync(_groupName);

        var groupCount = await GroupCountAsync(_groupName);
        var groupWithCalendar = await GroupWithCalendarCountAsync(_groupName, CalendarName);
        var rootGroup = await RootGroupCountAsync(_groupName);
        var createCalls = await SuccessCallCountAsync(SkillCreateGroup) - beforeCreate;
        Assert.Multiple(() =>
        {
            Assert.That(groupCount, Is.EqualTo(1),
                "exactly one group must be created under the requested name in a single turn");
            Assert.That(groupWithCalendar, Is.EqualTo(1),
                $"the created group must have the '{CalendarName}' holiday calendar assigned");
            Assert.That(rootGroup, Is.EqualTo(1),
                "a root-level group must be created with NO parent");
            Assert.That(createCalls, Is.GreaterThanOrEqualTo(1),
                "create_group must have run at least once");
            Assert.That(response, Does.Not.Contain(NoActionNoticeMarker),
                "a completed recipe must not emit the no-action notice");
        });
    }

    [Test]
    public async Task Sub_Group_Is_Created_Under_The_Named_Parent()
    {
        await AssertSkillEnabled(SkillCreateGroup);

        var (parentName, parentId) = await UniqueGroupAsync();
        Assert.That(parentName, Is.Not.Empty, "a uniquely searchable parent group is required");
        Assert.That(parentId, Is.Not.Empty);

        _groupName = "E2E-SubGruppe-" + Guid.NewGuid().ToString("N")[..8];
        TestContext.Out.WriteLine($"[new-group/sub] name='{_groupName}' parent='{parentName}'");

        var beforeCreate = await SuccessCallCountAsync(SkillCreateGroup);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Erstelle eine neue Gruppe namens '{_groupName}', gültig ab {ValidFromSpoken}, " +
            $"mit dem Kalender {CalendarName}, als Untergruppe von {parentName}.");
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (new-group/sub): {Trim(response)}");

        await WaitForGroupAsync(_groupName);

        var groupCount = await GroupCountAsync(_groupName);
        var groupWithCalendar = await GroupWithCalendarCountAsync(_groupName, CalendarName);
        var subGroup = await SubGroupCountAsync(_groupName, parentId);
        var createCalls = await SuccessCallCountAsync(SkillCreateGroup) - beforeCreate;
        Assert.Multiple(() =>
        {
            Assert.That(groupCount, Is.EqualTo(1),
                "exactly one group must be created under the requested name in a single turn");
            Assert.That(groupWithCalendar, Is.EqualTo(1),
                $"the created group must have the '{CalendarName}' holiday calendar assigned");
            Assert.That(subGroup, Is.EqualTo(1),
                "the created group must be nested under the named parent group");
            Assert.That(createCalls, Is.GreaterThanOrEqualTo(1),
                "create_group must have run at least once");
            Assert.That(response, Does.Not.Contain(NoActionNoticeMarker),
                "a completed recipe must not emit the no-action notice");
        });
    }

    private async Task WaitForGroupAsync(string groupName)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await GroupCountAsync(groupName) >= 1)
            {
                return;
            }
        }
    }

    private static async Task<int> GroupCountAsync(string groupName) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM \"group\" WHERE name='{Escape(groupName)}' AND NOT is_deleted");

    private static async Task<int> GroupWithCalendarCountAsync(string groupName, string calendarName) =>
        await ScalarIntAsync(
            "SELECT count(*) FROM \"group\" g JOIN calendar_selection cs ON cs.id=g.calendar_selection_id " +
            $"WHERE g.name='{Escape(groupName)}' AND NOT g.is_deleted AND cs.name='{Escape(calendarName)}'");

    private static async Task<int> RootGroupCountAsync(string groupName) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM \"group\" WHERE name='{Escape(groupName)}' AND NOT is_deleted AND parent IS NULL");

    private static async Task<int> SubGroupCountAsync(string groupName, string parentId) =>
        await ScalarIntAsync(
            "SELECT count(*) FROM \"group\" c JOIN \"group\" p ON p.id=c.parent " +
            $"WHERE c.name='{Escape(groupName)}' AND NOT c.is_deleted AND c.parent='{Escape(parentId)}' " +
            "AND c.lft > p.lft AND c.rgt < p.rgt AND c.root = coalesce(p.root, p.id)");

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
