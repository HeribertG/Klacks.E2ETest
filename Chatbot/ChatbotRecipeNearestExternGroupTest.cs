// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the data-driven recipe "add-extern-employee-to-nearest-group" (add an external
 * employee to the geographically nearest group). The recipe chain is ask clientName ->
 * search search_employees(entityType=ExternEmp) (capture clientId) -> mutate add_client_to_nearest_group.
 * DB-asserted via the group_item row that links the external employee to a group carrying coordinates
 * (geocoded from the group's city name). Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
public class ChatbotRecipeNearestExternGroupTest : ChatbotTestBase
{
    private const string SkillSearchEmployees = "search_employees";
    private const string SkillAddToNearest = "add_client_to_nearest_group";

    private const string NoActionNoticeMarker = "keine Aktion ausgeführt";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 3000;
    private const int SettleMaxPolls = 12;

    private string _clientId = string.Empty;

    [TearDown]
    public async Task RemoveTestLinks()
    {
        if (_clientId.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                "UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
                $"AND client_id='{Escape(_clientId)}' AND group_id IN " +
                "(SELECT id FROM \"group\" WHERE latitude IS NOT NULL AND longitude IS NOT NULL)");
        }
    }

    [Test]
    public async Task External_Employee_Is_Added_To_The_Nearest_Geocoded_Group()
    {
        await AssertSkillEnabled(SkillSearchEmployees);
        await AssertSkillEnabled(SkillAddToNearest);

        var geocodedGroups = await ScalarIntAsync(
            "SELECT count(*) FROM \"group\" WHERE NOT is_deleted AND latitude IS NOT NULL AND longitude IS NOT NULL");
        Assert.That(geocodedGroups, Is.GreaterThan(0),
            "the happy path needs at least one group with coordinates (geocoded from its city name)");

        var (externName, clientId) = await UniqueExternEmpWithCoordsAsync();
        _clientId = clientId;
        Assert.That(externName, Is.Not.Empty, "a uniquely searchable external employee with a geocoded address is required");
        Assert.That(clientId, Is.Not.Empty);
        TestContext.Out.WriteLine($"[nearest-extern] externEmp='{externName}' clientId='{clientId}'");

        await ResetCoordGroupLinks(clientId);
        var beforeAdd = await SuccessCallCountAsync(SkillAddToNearest);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Füge den externen Mitarbeiter {externName} zur nächstgelegenen Gruppe hinzu.");
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (nearest-extern): {Trim(response)}");

        await WaitForCoordGroupLinkAsync(clientId);

        var linkCount = await CoordGroupLinkCountAsync(clientId);
        var addCalls = await SuccessCallCountAsync(SkillAddToNearest) - beforeAdd;
        Assert.Multiple(() =>
        {
            Assert.That(linkCount, Is.EqualTo(1),
                "the external employee must be linked to exactly one geocoded (nearest) group");
            Assert.That(addCalls, Is.GreaterThanOrEqualTo(1),
                "add_client_to_nearest_group must have run at least once");
            Assert.That(response, Does.Not.Contain(NoActionNoticeMarker),
                "a completed recipe must not emit the no-action notice");
        });
    }

    private async Task WaitForCoordGroupLinkAsync(string clientId)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await CoordGroupLinkCountAsync(clientId) >= 1)
            {
                return;
            }
        }
    }

    private static async Task ResetCoordGroupLinks(string clientId)
    {
        await DbHelper.ExecuteSqlAsync(
            "UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted " +
            $"AND client_id='{Escape(clientId)}' AND group_id IN " +
            "(SELECT id FROM \"group\" WHERE latitude IS NOT NULL AND longitude IS NOT NULL)");
    }

    private static async Task<int> CoordGroupLinkCountAsync(string clientId) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM group_item gi JOIN \"group\" g ON g.id=gi.group_id " +
            $"WHERE gi.client_id='{Escape(clientId)}' AND NOT gi.is_deleted " +
            "AND g.latitude IS NOT NULL AND g.longitude IS NOT NULL");

    // A type-1 external employee with a geocoded address whose "FirstName Name" resolves to EXACTLY ONE
    // external employee under a per-token Contains match over first_name/name/company, so the recipe's
    // search step captures a single clientId.
    private static async Task<(string Name, string Id)> UniqueExternEmpWithCoordsAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT c.first_name || ' ' || c.name AS full_name, c.id FROM client c " +
            "JOIN address a ON a.client_id=c.id " +
            "WHERE c.type=1 AND NOT c.is_deleted AND NOT a.is_deleted " +
            "AND a.latitude IS NOT NULL AND a.longitude IS NOT NULL " +
            "AND coalesce(c.first_name,'')<>'' AND coalesce(c.name,'')<>'' " +
            "AND (SELECT count(DISTINCT x.id) FROM client x WHERE x.type=1 AND NOT x.is_deleted " +
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
