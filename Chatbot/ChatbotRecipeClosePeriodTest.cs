// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the guided recipe "close-payroll-period": ask period -> search list_period_issues
 * (no-capture pre-flight, always advances) -> ask goAhead -> mutate close_period. close_period seals the
 * works/breaks in range, writes the authoritative SealedDay locks and a PeriodAuditLog entry.
 *
 * close_period is on the Sensitive skill list (SkillRiskClassifier), so even after the recipe's own
 * goAhead confirmation the AutonomyGate holds the call for an explicit follow-up user confirmation and a
 * one-time token (ConfirmPendingActionSkill) before anything is actually written — the test drives that
 * confirmation the same way a user would, polling for the DB effect rather than assuming a fixed number
 * of turns.
 *
 * Everything is scoped to a single, freshly created group/shift/client/work fixture in a far-future test
 * period, so close_period can never affect real company data even if something goes wrong. As an
 * additional safety gate, the test aborts BEFORE sending any confirmation message if the pre-flight
 * list_period_issues call was not actually scoped to the test group by ID or name — an unscoped call
 * would risk sealing every period company-wide.
 *
 * Two core assertions carry the slice:
 *   - Pre-flight turn: list_period_issues runs, but the fixture work stays at LockLevel None and no
 *     SealedDay rows exist yet for the test group/period.
 *   - After explicit confirmation: the fixture work is sealed to LockLevel Closed and SealedDay rows
 *     exist for the test group/period.
 *
 * Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
[Category("Klacksy")]
public class ChatbotRecipeClosePeriodTest : ChatbotTestBase
{
    private const string SkillListPeriodIssues = "list_period_issues";
    private const string SkillClosePeriod = "close_period";

    private const string TestGroupPrefix = "E2E-ClosePeriod-Group-";
    private const string TestShiftPrefix = "E2E-ClosePeriod-Shift-";
    private const string TestClientPrefix = "E2E-ClosePeriod-Client-";
    private const string DateSqlFormat = "yyyy-MM-dd";

    private const int TestPeriodYear = 2031;
    private const int TestPeriodMonth = 11;

    private const int LockLevelNone = 0;
    private const int LockLevelClosed = 3;

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 4000;
    private const int SettleMaxPolls = 20;
    private const int MaxConfirmTurns = 3;

    private static readonly DateOnly PeriodStart = new(TestPeriodYear, TestPeriodMonth, 1);
    private static readonly DateOnly PeriodEnd = PeriodStart.AddMonths(1).AddDays(-1);
    private static readonly Random Rnd = new();

    private static readonly string[] ConfirmMessages =
    {
        "Ja, versiegle die Periode jetzt.",
        "Ja, ich bestätige ausdrücklich: bitte jetzt sofort versiegeln, ohne weitere Rückfrage.",
        "Ja, ich bestätige die Aktion endgültig — bitte jetzt ausführen."
    };

    private Guid _groupId;
    private Guid _shiftId;
    private Guid _clientId;
    private string _groupName = string.Empty;
    private bool _fixtureCreated;

    [TearDown]
    public async Task RollbackSealAndFixture()
    {
        if (!_fixtureCreated)
        {
            return;
        }

        var sql =
            $"UPDATE work SET lock_level={LockLevelNone}, sealed_at=NULL, sealed_by=NULL " +
            $"WHERE shift_id='{_shiftId}' AND client_id='{_clientId}' AND NOT is_deleted;\n" +
            "UPDATE sealed_day SET is_deleted=true, deleted_time=now() " +
            $"WHERE NOT is_deleted AND group_id='{_groupId}' " +
            $"AND date >= '{PeriodStart.ToString(DateSqlFormat)}' AND date <= '{PeriodEnd.ToString(DateSqlFormat)}';\n" +
            "UPDATE period_audit_log SET is_deleted=true, deleted_time=now() " +
            $"WHERE NOT is_deleted AND group_id='{_groupId}';\n" +
            "UPDATE group_item SET is_deleted=true, deleted_time=now() " +
            $"WHERE NOT is_deleted AND group_id='{_groupId}';\n" +
            "UPDATE work SET is_deleted=true, deleted_time=now() " +
            $"WHERE NOT is_deleted AND shift_id='{_shiftId}' AND client_id='{_clientId}';\n" +
            "UPDATE membership SET is_deleted=true, deleted_time=now() " +
            $"WHERE NOT is_deleted AND client_id='{_clientId}';\n" +
            $"UPDATE shift SET is_deleted=true, deleted_time=now() WHERE id='{_shiftId}' AND NOT is_deleted;\n" +
            $"UPDATE client SET is_deleted=true, deleted_time=now() WHERE id='{_clientId}' AND NOT is_deleted;\n" +
            $"UPDATE \"group\" SET is_deleted=true, deleted_time=now() WHERE id='{_groupId}' AND NOT is_deleted;";

        await DbHelper.ExecuteSqlAsync(sql);
    }

    [Test]
    public async Task PreFlight_Then_Confirmed_Seal_Closes_Period_For_Test_Group()
    {
        await AssertSkillEnabled(SkillListPeriodIssues);
        await AssertSkillEnabled(SkillClosePeriod);

        await SeedFixtureAsync();
        TestContext.Out.WriteLine(
            $"[close-period] group='{_groupName}' period={PeriodStart.ToString(DateSqlFormat)}..{PeriodEnd.ToString(DateSqlFormat)}");

        await EnsureChatOpen();
        await ClearChatAndWait();

        var issuesCallsBefore = await SuccessCallCountAsync(SkillListPeriodIssues);

        var before1 = await GetMessageCount();
        await SendChatMessage(
            $"Schliesse die Periode vom {PeriodStart.ToString(DateSqlFormat)} bis " +
            $"{PeriodEnd.ToString(DateSqlFormat)} für die Gruppe {_groupName} ab.");
        var preflight = await WaitForBotResponse(before1, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (pre-flight): {Trim(preflight)}");

        var issuesCallsAfter = await SuccessCallCountAsync(SkillListPeriodIssues);
        var lockLevelAfterPreflight = await WorkLockLevelAsync();
        var sealedDaysAfterPreflight = await SealedDayCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(issuesCallsAfter, Is.GreaterThan(issuesCallsBefore),
                "list_period_issues must have run during the pre-flight turn");
            Assert.That(lockLevelAfterPreflight, Is.EqualTo(LockLevelNone),
                "the pre-flight check must NOT seal the fixture work yet");
            Assert.That(sealedDaysAfterPreflight, Is.EqualTo(0),
                "the pre-flight check must NOT create SealedDay rows yet");
        });

        var preflightParams = await LatestSuccessfulCallParamsAsync(SkillListPeriodIssues);
        Assert.That(IsScopedToTestGroup(preflightParams), Is.True,
            "Refusing to proceed: list_period_issues was not scoped to the test group by ID or name " +
            $"(params: {preflightParams}) — confirming a seal now would risk closing every period " +
            "company-wide. Aborting before sending any confirmation message.");

        await ConfirmSealUntilAsync();

        var closeParams = await LatestSuccessfulCallParamsAsync(SkillClosePeriod);
        var lockLevelAfterSeal = await WorkLockLevelAsync();
        var sealedDaysAfterSeal = await SealedDayCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(IsScopedToTestGroup(closeParams), Is.True,
                $"close_period must have been scoped to the test group (params: {closeParams})");
            Assert.That(lockLevelAfterSeal, Is.EqualTo(LockLevelClosed),
                "the fixture work must be sealed to LockLevel Closed after explicit confirmation");
            Assert.That(sealedDaysAfterSeal, Is.GreaterThanOrEqualTo(1),
                "SealedDay rows must exist for the test group/period after the seal");
        });
    }

    private async Task ConfirmSealUntilAsync()
    {
        for (var turn = 0; turn < ConfirmMessages.Length; turn++)
        {
            var before = await GetMessageCount();
            await SendChatMessage(ConfirmMessages[turn]);
            try
            {
                var response = await WaitForBotResponse(before, TurnTimeoutMs);
                TestContext.Out.WriteLine($"Bot (confirm {turn + 1}): {Trim(response)}");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine(
                    $"No bot response within timeout on confirm turn {turn + 1} ({ex.Message}); " +
                    "the database effect decides, not the chat text.");
            }

            if (await SealedDayCountAsync() >= 1)
            {
                return;
            }
        }

        await WaitForSealAsync();
    }

    private async Task WaitForSealAsync()
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await SealedDayCountAsync() >= 1)
            {
                return;
            }
        }
    }

    private bool IsScopedToTestGroup(string parametersJson) =>
        parametersJson.Contains(_groupId.ToString(), StringComparison.OrdinalIgnoreCase)
        || parametersJson.Contains(_groupName, StringComparison.OrdinalIgnoreCase);

    private async Task SeedFixtureAsync()
    {
        var suffix = Rnd.Next(10000, 99999);
        _groupName = $"{TestGroupPrefix}{suffix}";
        var shiftName = $"{TestShiftPrefix}{suffix}";
        var clientName = $"{TestClientPrefix}{suffix}";

        _groupId = Guid.NewGuid();
        _shiftId = Guid.NewGuid();
        _clientId = Guid.NewGuid();
        var groupItemShiftLinkId = Guid.NewGuid();
        var groupItemClientLinkId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var workId = Guid.NewGuid();

        var sql =
            "WITH next_root AS (" +
            "  SELECT COALESCE(MAX(rgt), 0) AS max_rgt FROM \"group\" WHERE root IS NULL AND NOT is_deleted" +
            ") " +
            "INSERT INTO \"group\" (id, description, name, valid_from, payment_interval, parent, root, lft, rgt, is_deleted, create_time) " +
            $"SELECT '{_groupId}', '', '{Escape(_groupName)}', now(), 0, NULL, NULL, max_rgt+1, max_rgt+2, false, now() FROM next_root;\n" +

            "INSERT INTO shift (id, cutting_after_midnight, abbreviation, description, name, status, " +
            "after_shift, before_shift, end_shift, from_date, start_shift, briefing_time, debriefing_time, " +
            "travel_time_after, travel_time_before, is_friday, is_holiday, is_monday, is_saturday, is_sunday, " +
            "is_thursday, is_tuesday, is_wednesday, is_weekday_and_holiday, is_sporadic, sporadic_scope, " +
            "is_time_range, quantity, sum_employees, work_time, shift_type, is_deleted, create_time) VALUES (" +
            $"'{_shiftId}', false, 'E2ECP', '', '{Escape(shiftName)}', 0, " +
            "'00:00', '00:00', '16:00', '2000-01-01', '08:00', '00:00', '00:00', " +
            "'00:00', '00:00', true, false, true, false, false, " +
            "true, true, true, false, false, 0, " +
            "false, 1, 1, 8, 0, false, now());\n" +

            "INSERT INTO client (id, name, gender, legal_entity, type, is_deleted, create_time) VALUES " +
            $"('{_clientId}', '{Escape(clientName)}', 1, false, 0, false, now());\n" +

            "INSERT INTO membership (id, client_id, type, valid_from, is_deleted, create_time) VALUES " +
            $"('{membershipId}', '{_clientId}', 0, '2000-01-01', false, now());\n" +

            "INSERT INTO group_item (id, shift_id, group_id, valid_from, is_deleted, create_time) VALUES " +
            $"('{groupItemShiftLinkId}', '{_shiftId}', '{_groupId}', now(), false, now());\n" +

            "INSERT INTO group_item (id, client_id, group_id, valid_from, is_deleted, create_time) VALUES " +
            $"('{groupItemClientLinkId}', '{_clientId}', '{_groupId}', now(), false, now());\n" +

            "INSERT INTO work (id, shift_id, client_id, workday, work_time, surcharges, start_time, end_time, lock_level, is_deleted, create_time) VALUES " +
            $"('{workId}', '{_shiftId}', '{_clientId}', '{PeriodStart.ToString(DateSqlFormat)}', 8, 0, '08:00', '16:00', {LockLevelNone}, false, now());";

        var result = await DbHelper.ExecuteSqlAsync(sql);
        if (result.StartsWith("ERROR:"))
        {
            Assert.Fail($"Fixture seed failed: {result}");
        }

        _fixtureCreated = true;
    }

    private async Task<int> WorkLockLevelAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            $"SELECT lock_level FROM work WHERE shift_id='{_shiftId}' AND client_id='{_clientId}' " +
            "AND NOT is_deleted LIMIT 1")).Trim();
        return int.TryParse(result, out var level) ? level : LockLevelNone;
    }

    private async Task<int> SealedDayCountAsync() =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM sealed_day WHERE group_id='{_groupId}' " +
            $"AND date >= '{PeriodStart.ToString(DateSqlFormat)}' AND date <= '{PeriodEnd.ToString(DateSqlFormat)}' " +
            "AND NOT is_deleted");

    private static async Task<int> SuccessCallCountAsync(string skillName) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM skill_usage_records WHERE skill_name='{Escape(skillName)}' AND success=true");

    private static async Task<string> LatestSuccessfulCallParamsAsync(string skillName) =>
        (await DbHelper.ExecuteSqlAsync(
            $"SELECT parameters_json FROM skill_usage_records WHERE skill_name='{Escape(skillName)}' " +
            "AND success=true ORDER BY timestamp DESC LIMIT 1")).Trim();

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(200, text.Length)];
}
