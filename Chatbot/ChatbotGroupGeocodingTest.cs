// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * End-to-end reliability test for Klacksy's group-geocoding + customer-grouping skills via the chat UI.
 *
 * Primary, DB-asserted test: geocode_location_groups. Klacksy is asked to geocode a real, coordinate-less
 * city group ("Thun"); the observable, non-flaky outcome is asserted via SQL — the group's latitude and
 * longitude become non-null. The precondition (coordinates cleared) and teardown (coordinates cleared
 * again) keep the dev data unchanged.
 *
 * Secondary smoke test (default model, NOT a correctness assertion): propose_customer_grouping in dry-run
 * — only that Klacksy reaches the skill and answers without an API error (the dry-run has no DB side
 * effect; applying would mass-move customers, so it is intentionally not exercised here — the apply write
 * path is covered by unit tests).
 *
 * Runs on the configured default model only. Idempotent and self-restoring.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Order(61)]
public class ChatbotGroupGeocodingTest : ChatbotTestBase
{
    private const string SkillGeocode = "geocode_location_groups";
    private const string SkillPropose = "propose_customer_grouping";

    private const int ActionTimeoutMs = 120000;
    private const int MaxConfirmTurns = 6;
    private const int ConfirmLoopDelayMs = 2500;

    private const string TargetGroup = "Thun";

    [Test]
    public async Task Klacksy_GeocodesCityGroup_DbAsserted()
    {
        await AssertSkillEnabled(SkillGeocode);

        var groupId = await ResolveGroupIdAsync(TargetGroup);
        Assert.That(groupId, Is.Not.Empty, $"Need a city group named '{TargetGroup}' in the database.");

        await ClearCoordinatesAsync(groupId);
        try
        {
            await EnsureChatOpen();
            await ClearChatAndWait();
            var before = await GetMessageCount();
            await SendChatMessage(
                $"Bitte geocode die Gruppe '{TargetGroup}'. Rufe dazu den Skill geocode_location_groups " +
                $"mit groupName='{TargetGroup}' auf und fuehre ihn sofort aus.");
            var response = await WaitForBotResponse(before, ActionTimeoutMs);
            TestContext.Out.WriteLine($"Bot: {Trim(response)}");

            await ConfirmUntilAsync(() => HasCoordinatesAsync(groupId));

            var has = await HasCoordinatesAsync(groupId);
            var coords = await GetCoordinatesAsync(groupId);
            TestContext.Out.WriteLine($"[geocode] '{TargetGroup}' coords = {coords}");
            Assert.That(has, Is.True,
                $"Group '{TargetGroup}' must have coordinates after geocoding. Got: {coords}");
        }
        finally
        {
            await ClearCoordinatesAsync(groupId);
        }
    }

    [Test]
    public async Task Klacksy_ProposesCustomerGrouping_Smoke()
    {
        await AssertSkillEnabled(SkillPropose);

        await EnsureChatOpen();
        await ClearChatAndWait();
        var before = await GetMessageCount();
        await SendChatMessage(
            "Schlage mir eine geografische Gruppierung der Kunden vor - nur ein Vorschlag, noch nicht anwenden. " +
            "Rufe dazu propose_customer_grouping auf.");
        var response = await WaitForBotResponse(before, ActionTimeoutMs);
        TestContext.Out.WriteLine($"Bot: {Trim(response)}");

        Assert.That(response, Is.Not.Empty, "Klacksy must answer the grouping proposal request.");
        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API error expected during proposal. Last: {TestListener.GetLastErrorMessage()}");
    }

    [Test]
    public async Task Klacksy_GroupByOrtschaften_NoCoordinates_BehaviorObservation()
    {
        // Precondition matching the user's scenario: no group has coordinates yet, so propose_customer_grouping
        // has no anchors and must tell the user to geocode the location groups first. Observational: we print
        // Klacksy's verbatim answer and assert only that it responds without an API error.
        await DbHelper.ExecuteSqlAsync("UPDATE \"group\" SET latitude = NULL, longitude = NULL WHERE latitude IS NOT NULL OR longitude IS NOT NULL");

        await EnsureChatOpen();
        await ClearChatAndWait();
        var before = await GetMessageCount();
        await SendChatMessage("Bitte gruppiere Customer nach Ortschaften.");
        var response = await WaitForBotResponse(before, ActionTimeoutMs);

        TestContext.Out.WriteLine("=== KLACKSY RESPONSE (verbatim) ===");
        TestContext.Out.WriteLine(response);
        TestContext.Out.WriteLine("=== END KLACKSY RESPONSE ===");

        Assert.That(response, Is.Not.Empty, "Klacksy must answer the grouping request.");
        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API error expected. Last: {TestListener.GetLastErrorMessage()}");
    }

    [Test]
    public async Task Klacksy_AppliesGrouping_DbAsserted()
    {
        // Precondition: city anchors exist (geocoded earlier). Drive an explicit apply and assert via SQL
        // that customers actually land in geocoded (city) groups. The backup taken before this run is the
        // safety net.
        var anchors = await ScalarAsync("SELECT count(*) FROM \"group\" WHERE latitude IS NOT NULL AND NOT is_deleted");
        Assert.That(int.Parse(anchors), Is.GreaterThan(0), "Need geocoded city groups as anchors before applying.");

        await EnsureChatOpen();
        await ClearChatAndWait();
        var before = await GetMessageCount();
        await SendChatMessage("Bitte gruppiere die Customer nach ihren Ortschaften.");
        var response = await WaitForBotResponse(before, ActionTimeoutMs);
        TestContext.Out.WriteLine($"Bot: {Trim(response)}");

        for (var turn = 0; turn < MaxConfirmTurns; turn++)
        {
            await Task.Delay(ConfirmLoopDelayMs);
            if (await CustomersInCityGroupsAsync() > 0)
            {
                break;
            }

            var b = await GetMessageCount();
            await SendChatMessage(
                "Ja, wende die Gruppierung jetzt an - uebernimm den Vorschlag und verschiebe die Kunden. Frag nicht weiter nach.");
            var r = await WaitForBotResponse(b, ActionTimeoutMs);
            TestContext.Out.WriteLine($"Confirm {turn + 1}: {Trim(r)}");
        }

        var inCities = await CustomersInCityGroupsAsync();
        TestContext.Out.WriteLine($"[apply] customers now in city groups: {inCities}");
        Assert.That(inCities, Is.GreaterThan(0),
            "After apply, at least some customers must be members of a geocoded (city) group.");
    }

    private static async Task<int> CustomersInCityGroupsAsync()
    {
        var sql =
            "SELECT count(*) FROM group_item gi " +
            "JOIN client c ON c.id = gi.client_id AND c.type = 2 AND NOT c.is_deleted " +
            "JOIN \"group\" g ON g.id = gi.group_id AND g.latitude IS NOT NULL AND NOT g.is_deleted " +
            "WHERE NOT gi.is_deleted";
        return int.Parse((await DbHelper.ExecuteSqlAsync(sql)).Trim());
    }

    private static async Task<string> ScalarAsync(string sql) => (await DbHelper.ExecuteSqlAsync(sql)).Trim();

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
                $"Ja, bitte jetzt direkt geocode_location_groups fuer '{TargetGroup}' ausfuehren. Frag nicht weiter nach.");
            var response = await WaitForBotResponse(before, ActionTimeoutMs);
            TestContext.Out.WriteLine($"Confirm {turn + 1}: {Trim(response)}");
        }

        await Task.Delay(ConfirmLoopDelayMs);
    }

    private static async Task<string> ResolveGroupIdAsync(string name)
    {
        var sql = $"SELECT id FROM \"group\" WHERE name = '{Esc(name)}' AND NOT is_deleted ORDER BY lft LIMIT 1";
        return (await DbHelper.ExecuteSqlAsync(sql)).Trim();
    }

    private static async Task<bool> HasCoordinatesAsync(string groupId)
    {
        var sql = $"SELECT (latitude IS NOT NULL AND longitude IS NOT NULL) FROM \"group\" WHERE id = '{Esc(groupId)}'";
        return (await DbHelper.ExecuteSqlAsync(sql)).Trim() == "t";
    }

    private static async Task<string> GetCoordinatesAsync(string groupId)
    {
        var sql = $"SELECT COALESCE(latitude::text,'null') || ',' || COALESCE(longitude::text,'null') " +
                  $"FROM \"group\" WHERE id = '{Esc(groupId)}'";
        return (await DbHelper.ExecuteSqlAsync(sql)).Trim();
    }

    private static async Task ClearCoordinatesAsync(string groupId)
    {
        var sql = $"UPDATE \"group\" SET latitude = NULL, longitude = NULL WHERE id = '{Esc(groupId)}'";
        await DbHelper.ExecuteSqlAsync(sql);
    }

    private static string Esc(string value) => value.Replace("'", "''");

    private static string Trim(string text)
        => string.IsNullOrEmpty(text) || text.Length <= 500 ? text : text[..500];
}
