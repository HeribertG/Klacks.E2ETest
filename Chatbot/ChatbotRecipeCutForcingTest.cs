// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Proof of the "dienst-aus-bestellung-schneiden" recipe forcing spine. Two single-turn flows, both
 * DB-asserted (one SealedOrder, zero leftover OriginalShifts, exactly three SplitShifts sharing the
 * order's original_id, all is_time_range=true, create_shift exactly once, cut_shift at least once):
 *   - GUID path (proof #1): the request already carries the customer as a clientId GUID; the spine
 *     forces create_shift -> cut_shift.
 *   - Name path (increment 2): the request names the customer; the spine forces
 *     find_customer_candidates, deterministically captures the lone matching clientId, injects it into
 *     create_shift, then forces cut_shift — and the created order is billed to exactly that customer.
 * Each sends ONE message with no nudge loop, so a green run proves the forcing, not operator re-asking.
 * Explicit: LLM-driven and slow, run on demand, ideally 5x.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-forcing proof; slow and nondeterministic. Run on demand, ideally 5x.")]
[Category("Klacksy")]
public class ChatbotRecipeCutForcingTest : ChatbotTestBase
{
    private const string SkillFindCustomer = "find_customer_candidates";
    private const string SkillCreateShift = "create_shift";
    private const string SkillCutShift = "cut_shift";

    private const string OrderNameGuid = "E2E RecipeForce 24-7";
    private const string OrderNameName = "E2E RecipeName 24-7";
    private const string FromDate = "2026-06-01";

    private const int CreateTimeoutMs = 180000;
    private const int SettlePollMs = 4000;
    private const int SettleMaxPolls = 15;

    [TearDown]
    public async Task RemoveTestOrders()
    {
        await SoftDeleteOrderAsync(OrderNameGuid);
        await SoftDeleteOrderAsync(OrderNameName);
    }

    [Test]
    public async Task Klacksy_Forces_Create_Then_Cut_With_Known_ClientId()
    {
        await AssertSkillEnabled(SkillCreateShift);
        await AssertSkillEnabled(SkillCutShift);
        await SoftDeleteOrderAsync(OrderNameGuid);

        var clientId = await MostUsedCustomerIdAsync();
        Assert.That(clientId, Is.Not.Empty, "a valid customer clientId is required to supply to the recipe");

        var beforeCreate = await SuccessCallCountAsync(SkillCreateShift);
        var beforeCut = await SuccessCallCountAsync(SkillCutShift);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Erstelle eine 24/7-Bestellung (rund um die Uhr) mit Namen '{OrderNameGuid}' fuer clientId={clientId}, " +
            $"gueltig ab {FromDate}, und schneide sie anschliessend direkt mit cut_shift in genau 3 Dienste auf: " +
            "Fruehdienst 07:00-15:00, Spaetdienst 15:00-23:00, Nachtdienst 23:00-07:00. " +
            "Fuehre den Schnitt sofort aus, navigiere nicht zur Zuschnitt-Seite und lege keinen neuen Kunden an.");
        var response = await WaitForBotResponse(before, CreateTimeoutMs);
        TestContext.Out.WriteLine($"Bot (guid): {Trim(response)}");

        await WaitForThreeSplitsAsync(OrderNameGuid);

        var createCalls = await SuccessCallCountAsync(SkillCreateShift) - beforeCreate;
        var cutCalls = await SuccessCallCountAsync(SkillCutShift) - beforeCut;
        AssertCutFlow(OrderNameGuid, createCalls, cutCalls);
    }

    [Test]
    public async Task Klacksy_Resolves_Customer_By_Name_Then_Creates_And_Cuts()
    {
        await AssertSkillEnabled(SkillFindCustomer);
        await AssertSkillEnabled(SkillCreateShift);
        await AssertSkillEnabled(SkillCutShift);
        await SoftDeleteOrderAsync(OrderNameName);

        var (customerName, expectedClientId) = await UniqueCustomerAsync();
        Assert.That(customerName, Is.Not.Empty, "a uniquely searchable customer name is required for the name path");
        TestContext.Out.WriteLine($"[recipe-name] customer='{customerName}' expectedClientId={expectedClientId}");

        var beforeFind = await SuccessCallCountAsync(SkillFindCustomer);
        var beforeCreate = await SuccessCallCountAsync(SkillCreateShift);
        var beforeCut = await SuccessCallCountAsync(SkillCutShift);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Erstelle eine 24/7-Bestellung (rund um die Uhr) mit Namen '{OrderNameName}' fuer den Kunden {customerName}, " +
            $"gueltig ab {FromDate}, und schneide sie anschliessend direkt mit cut_shift in genau 3 Dienste auf: " +
            "Fruehdienst 07:00-15:00, Spaetdienst 15:00-23:00, Nachtdienst 23:00-07:00. " +
            "Fuehre den Schnitt sofort aus und navigiere nicht zur Zuschnitt-Seite.");
        var response = await WaitForBotResponse(before, CreateTimeoutMs);
        TestContext.Out.WriteLine($"Bot (name): {Trim(response)}");

        await WaitForThreeSplitsAsync(OrderNameName);

        var findCalls = await SuccessCallCountAsync(SkillFindCustomer) - beforeFind;
        var createCalls = await SuccessCallCountAsync(SkillCreateShift) - beforeCreate;
        var cutCalls = await SuccessCallCountAsync(SkillCutShift) - beforeCut;

        var billedToExpected = await ScalarIntAsync(
            $"SELECT count(*) FROM shift WHERE name='{Escape(OrderNameName)}' AND status=1 AND NOT is_deleted " +
            $"AND client_id='{Escape(expectedClientId)}'");

        TestContext.Out.WriteLine($"[recipe-name] find_calls={findCalls} billedToExpected={billedToExpected}");
        AssertCutFlow(OrderNameName, createCalls, cutCalls);
        Assert.Multiple(() =>
        {
            Assert.That(findCalls, Is.GreaterThanOrEqualTo(1),
                "find_customer_candidates must have been forced as the first step");
            Assert.That(billedToExpected, Is.EqualTo(1),
                "the order must be billed to exactly the customer the name deterministically resolved to");
        });
    }

    private void AssertCutFlow(string orderName, int createCalls, int cutCalls)
    {
        var sealedCount = ScalarIntSync(
            $"SELECT count(*) FROM shift WHERE name='{Escape(orderName)}' AND status=1 AND NOT is_deleted");
        var leftoverOriginals = ScalarIntSync(
            $"SELECT count(*) FROM shift WHERE name='{Escape(orderName)}' AND status=2 AND NOT is_deleted");
        var splitCount = ScalarIntSync(
            "SELECT count(*) FROM shift s WHERE s.status=3 AND NOT s.is_deleted AND s.original_id IN " +
            $"(SELECT id FROM shift WHERE name='{Escape(orderName)}' AND status=1 AND NOT is_deleted)");
        var splitNotTimeRange = ScalarIntSync(
            "SELECT count(*) FROM shift s WHERE s.status=3 AND NOT s.is_deleted AND s.is_time_range=false AND s.original_id IN " +
            $"(SELECT id FROM shift WHERE name='{Escape(orderName)}' AND status=1 AND NOT is_deleted)");

        TestContext.Out.WriteLine(
            $"[recipe-force {orderName}] sealed={sealedCount} leftoverOriginals={leftoverOriginals} splits={splitCount} " +
            $"splitNotTimeRange={splitNotTimeRange} create_shift_calls={createCalls} cut_shift_calls={cutCalls}");

        Assert.Multiple(() =>
        {
            Assert.That(sealedCount, Is.EqualTo(1), "exactly one SealedOrder (the immutable Bestellung) must exist");
            Assert.That(splitCount, Is.EqualTo(3), "the order must be cut into exactly 3 SplitShifts");
            Assert.That(leftoverOriginals, Is.EqualTo(0), "no OriginalShift may be left over");
            Assert.That(splitNotTimeRange, Is.EqualTo(0), "every split part must be is_time_range=true");
            Assert.That(createCalls, Is.EqualTo(1),
                "create_shift must run exactly once (the forcing spine prevents the old 3x create_shift)");
            Assert.That(cutCalls, Is.GreaterThanOrEqualTo(1),
                "cut_shift must have been forced after create_shift in the same turn");
        });
    }

    private async Task WaitForThreeSplitsAsync(string orderName)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await ThreeSplitsExistAsync(orderName))
            {
                return;
            }
        }
    }

    private static async Task<bool> ThreeSplitsExistAsync(string orderName)
    {
        var count = await ScalarIntAsync(
            "SELECT count(*) FROM shift s WHERE s.status=3 AND NOT s.is_deleted AND s.original_id IN " +
            $"(SELECT id FROM shift WHERE name='{Escape(orderName)}' AND status=1 AND NOT is_deleted)");
        return count >= 3;
    }

    private static async Task<string> MostUsedCustomerIdAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT client_id FROM shift WHERE status=1 AND NOT is_deleted AND client_id IS NOT NULL " +
            "GROUP BY client_id ORDER BY count(*) DESC, client_id LIMIT 1")).Trim();
        return result.Split('\n')[0].Trim();
    }

    // A type-2 (Customer) client whose last name resolves to exactly ONE candidate under
    // find_customer_candidates' (company OR first_name OR name) filter — so the name path is single-turn.
    private static async Task<(string Name, string Id)> UniqueCustomerAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT c.name, c.id FROM client c WHERE c.type=2 AND c.name IS NOT NULL AND c.name<>'' AND NOT c.is_deleted " +
            "AND (SELECT count(*) FROM client x WHERE x.type=2 AND NOT x.is_deleted AND (x.company ILIKE '%'||c.name||'%' " +
            "OR x.first_name ILIKE '%'||c.name||'%' OR x.name ILIKE '%'||c.name||'%'))=1 ORDER BY c.name LIMIT 1")).Trim();
        var parts = result.Split('\n')[0].Split('|');
        return parts.Length >= 2 ? (parts[0].Trim(), parts[1].Trim()) : (string.Empty, string.Empty);
    }

    private static async Task SoftDeleteOrderAsync(string orderName)
    {
        var esc = Escape(orderName);
        var family =
            $"SELECT id FROM shift WHERE name='{esc}' " +
            $"UNION SELECT id FROM shift WHERE original_id IN (SELECT id FROM shift WHERE name='{esc}')";
        var sql =
            $"UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND shift_id IN ({family});" +
            $"UPDATE shift SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND id IN ({family});";
        await DbHelper.ExecuteSqlAsync(sql);
    }

    private static async Task<int> SuccessCallCountAsync(string skillName) =>
        await ScalarIntAsync($"SELECT count(*) FROM skill_usage_records WHERE skill_name='{Escape(skillName)}' AND success=true");

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static int ScalarIntSync(string sql) => ScalarIntAsync(sql).GetAwaiter().GetResult();

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(200, text.Length)];
}
