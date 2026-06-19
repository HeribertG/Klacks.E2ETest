// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Local-only smoke test for the one-click AutoWizard ("autofill") on the schedule page.
/// Self-seeds a dedicated under-limit group (payment_interval=monthly, ~18 clients with open
/// memberships + ~18 open-ended OriginalShift shifts) via Npgsql against the Dev DB (port 5434),
/// because no natural Dev-DB group satisfies onAutoWizardClick's gate (shift-bearing nodes have
/// &gt;80 shifts, agent-bearing nodes have &gt;250 agents) and a fresh CI DB has zero shifts by
/// construction. After selecting the seeded group it clicks #schedule-wizard-btn and waits for the
/// scenario-selector to enter scenario mode (#scenario-selector-btn.scenario-active), the durable
/// structural proof that a real scenario was produced. The seeded group + group_items are torn down
/// in [OneTimeTearDown]. This is [Explicit]: it requires a live Dev API on https://localhost:5001,
/// UI on http://localhost:4200 and the Dev DB on 5434 — it is NEVER green in CI.
/// </summary>

using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.PageObjects;
using Klacks.E2ETest.Wrappers;
using Npgsql;

namespace Klacks.E2ETest.WorkSchedule;

[TestFixture]
[Order(110)]
[Explicit("One-click AutoWizard smoke — self-seeds an under-limit group in the Dev DB (5434) and needs a live Dev API (5001) + UI (4200); local-only, never green in CI")]
public class WizardAutofillTest : PlaywrightSetup
{
    private const string WizardButtonId = "schedule-wizard-btn";
    private const string ScenarioSelectorActiveSelector = "#scenario-selector-btn.scenario-active";
    private const string WizardSpinnerSelector = "#schedule-wizard-btn .spinner-border";

    // AutoWizard toasts are rendered via ToastShowService -> ngb-toast (app-toasts), NOT ob-alert.
    // showError() applies bg-danger; showInfo() (started/completed) applies bg-info. Gating on
    // bg-danger cleanly separates failure/early-return toasts from the success info toasts.
    private const string NotificationToastSelector = "ngb-toast.bg-danger";
    private const string NotificationToastTextSelector = "ngb-toast.bg-danger .toast-text";

    private const string GroupSelectToggleId = "group-select-dropdown-toggle";
    private const string GroupOptionIdPrefix = "group-option-";
    private const string PeriodLabelId = "dropdownSetting";

    private const string DevConnectionString =
        "Host=localhost;Port=5434;Username=postgres;Password=admin;Database=klacks";

    private const int AutoWizardTotalDeadlineMs = 960000;
    private const int AutoWizardPollCadenceMs = 250;
    private const int EarlyFailWindowMs = 10000;

    private const string SeededGroupNamePrefix = "E2E-AutoWizard-";
    private const int SeedAgentCount = 18;
    private const int SeedShiftCount = 18;
    private const int SeedPaymentInterval = 2;
    private const string SeedValidFrom = "2025-01-01";
    private const int OriginalShiftStatus = 2;

    private const string SeedUser = "e2e-autowizard";

    private Listener _listener = null!;
    private SchedulePage _schedule = null!;

    private Guid _seededGroupId;

    [OneTimeSetUp]
    public async Task SeedUnderLimitGroup()
    {
        await using var conn = new NpgsqlConnection(DevConnectionString);
        await conn.OpenAsync();

        await PurgeSeedArtifactsAsync(conn);

        _seededGroupId = Guid.NewGuid();
        var groupName = SeededGroupNamePrefix + TimeStamp;

        await using (var groupCmd = new NpgsqlCommand(
            "INSERT INTO \"group\" (id, description, name, valid_from, valid_until, payment_interval, " +
            "parent, root, lft, rgt, create_time, current_user_created, is_deleted) " +
            "VALUES (@id, @descr, @name, @validFrom, NULL, @interval, NULL, @id, 1, 2, now(), @user, false)",
            conn))
        {
            groupCmd.Parameters.AddWithValue("id", _seededGroupId);
            groupCmd.Parameters.AddWithValue("descr", "E2E AutoWizard self-seed");
            groupCmd.Parameters.AddWithValue("name", groupName);
            groupCmd.Parameters.AddWithValue("validFrom", DateTime.Parse(SeedValidFrom));
            groupCmd.Parameters.AddWithValue("interval", SeedPaymentInterval);
            groupCmd.Parameters.AddWithValue("user", SeedUser);
            await groupCmd.ExecuteNonQueryAsync();
        }

        var clientIds = await SelectIdsAsync(conn,
            "SELECT c.id FROM client c " +
            "WHERE c.is_deleted = false " +
            "  AND EXISTS (SELECT 1 FROM membership m WHERE m.client_id = c.id " +
            "              AND m.is_deleted = false AND m.valid_from <= now() AND m.valid_until IS NULL) " +
            "ORDER BY c.name " +
            $"LIMIT {SeedAgentCount}");

        var shiftIds = await SelectIdsAsync(conn,
            "SELECT id FROM shift " +
            "WHERE is_deleted = false " +
            $"  AND status = {OriginalShiftStatus} " +
            "  AND until_date IS NULL " +
            $"  AND from_date <= '{SeedValidFrom}' " +
            "ORDER BY name " +
            $"LIMIT {SeedShiftCount}");

        Assert.That(clientIds, Has.Count.EqualTo(SeedAgentCount),
            "Dev DB must have at least 18 clients with an open membership");
        Assert.That(shiftIds, Has.Count.EqualTo(SeedShiftCount),
            "Dev DB must have at least 18 open-ended OriginalShift shifts");

        foreach (var clientId in clientIds)
        {
            await InsertGroupItemAsync(conn, clientId: clientId, shiftId: null);
        }
        foreach (var shiftId in shiftIds)
        {
            await InsertGroupItemAsync(conn, clientId: null, shiftId: shiftId);
        }

        TestContext.Out.WriteLine(
            $"Seeded group {groupName} ({_seededGroupId}) with {clientIds.Count} clients + {shiftIds.Count} shifts");
    }

    [OneTimeTearDown]
    public async Task TeardownSeedGroup()
    {
        await using var conn = new NpgsqlConnection(DevConnectionString);
        await conn.OpenAsync();
        await PurgeSeedArtifactsAsync(conn);
    }

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        _schedule = new SchedulePage(Page, Actions, BaseUrl);
        await _schedule.NavigateToScheduleAsync(enableTestMode: true);
        await _schedule.WaitForGridLoadAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine(_listener.GetLastErrorMessage());
        }
        await _listener.WaitForResponseHandlingAsync();
    }

    [Test]
    [Order(1)]
    public async Task AutoWizard_OneClick_ProducesScenario()
    {
        await SelectSeededGroupAsync();

        TestContext.Out.WriteLine("Clicking the one-click AutoWizard button");
        await Actions.ClickButtonById(WizardButtonId);

        var earlyFailToast = await PollForEarlyReturnToastAsync();
        if (earlyFailToast != null)
        {
            Assert.Fail($"AutoWizard early-return toast (seed/group-view did not satisfy the gate): {earlyFailToast}");
        }

        var completed = false;
        string? failureToast = null;
        var deadline = DateTime.UtcNow.AddMilliseconds(AutoWizardTotalDeadlineMs);
        var lastLog = DateTime.UtcNow;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (await Actions.CountElementsBySelector(ScenarioSelectorActiveSelector) > 0)
                {
                    completed = true;
                    break;
                }

                if (await Actions.CountElementsBySelector(NotificationToastSelector) > 0
                    && await Actions.CountElementsBySelector(WizardSpinnerSelector) == 0)
                {
                    failureToast = await Actions.GetTextContentBySelector(NotificationToastTextSelector, 0);
                    break;
                }
            }
            catch (Exception ex) when (IsNavigationRace(ex))
            {
                // The schedule grid re-renders while the scenario is applied; swallow the race.
            }

            if ((DateTime.UtcNow - lastLog).TotalSeconds >= 30)
            {
                var elapsed = (int)(AutoWizardTotalDeadlineMs / 1000 - (deadline - DateTime.UtcNow).TotalSeconds);
                TestContext.Out.WriteLine($"AutoWizard still running... ~{elapsed}s elapsed");
                lastLog = DateTime.UtcNow;
            }

            await Task.Delay(AutoWizardPollCadenceMs);
        }

        if (failureToast != null)
        {
            Assert.Fail($"AutoWizard failed: {failureToast}");
        }

        Assert.That(completed, Is.True,
            $"AutoWizard did not enter scenario mode ({ScenarioSelectorActiveSelector}) within {AutoWizardTotalDeadlineMs / 1000}s");

        Assert.That(await Actions.CountElementsBySelector(WizardSpinnerSelector), Is.EqualTo(0),
            "Wizard spinner did not clear after completion");

        Assert.That(_listener.HasApiErrors(), Is.False, _listener.GetLastErrorMessage());
    }

    private async Task SelectSeededGroupAsync()
    {
        await Actions.ClickButtonById(GroupSelectToggleId);

        var optionId = GroupOptionIdPrefix + _seededGroupId;
        try
        {
            await Actions.ClickElementById(optionId);
        }
        catch
        {
            await Actions.ClickByJavaScript(optionId);
        }

        await Actions.WaitForSpinnerToDisappear();
        await _schedule.WaitForGridLoadAsync();

        var period = await Actions.GetTextContentById(PeriodLabelId);
        TestContext.Out.WriteLine($"Landing period after group selection: {period}");
    }

    private async Task<string?> PollForEarlyReturnToastAsync()
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(EarlyFailWindowMs);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // A success run never produces a bg-danger toast in this early window; the running
                // spinner / scenario-active are the legitimate signals, so an error toast here means
                // a synchronous gate-miss (noPeriod/noAgents/noShifts/tooLarge).
                if (await Actions.CountElementsBySelector(ScenarioSelectorActiveSelector) > 0)
                {
                    return null;
                }
                if (await Actions.CountElementsBySelector(NotificationToastSelector) > 0)
                {
                    return await Actions.GetTextContentBySelector(NotificationToastTextSelector, 0);
                }
            }
            catch (Exception ex) when (IsNavigationRace(ex))
            {
                // Ignore mid-poll navigation races.
            }
            await Task.Delay(AutoWizardPollCadenceMs);
        }
        return null;
    }

    private async Task InsertGroupItemAsync(NpgsqlConnection conn, Guid? clientId, Guid? shiftId)
    {
        var id = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO group_item (id, client_id, shift_id, group_id, valid_from, valid_until, " +
            "create_time, current_user_created, is_deleted) " +
            "VALUES (@id, @clientId, @shiftId, @groupId, @validFrom, NULL, now(), @user, false)",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("clientId", (object?)clientId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("shiftId", (object?)shiftId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("groupId", _seededGroupId);
        cmd.Parameters.AddWithValue("validFrom", DateTime.Parse(SeedValidFrom));
        cmd.Parameters.AddWithValue("user", SeedUser);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Removes every artifact this test can create (current run + orphans from a crashed run) in
    /// FK-safe order: scenario-cloned schedule rows (matched by analyse_token) first, then the
    /// analyse_scenarios + other group foreign-key holders, then the seeded group itself. A
    /// successful AutoWizard run inserts an analyse_scenarios row referencing the seeded group, so
    /// the group cannot be deleted until those scenario rows are gone.
    /// </summary>
    private static async Task PurgeSeedArtifactsAsync(NpgsqlConnection conn)
    {
        const string tokenFilter =
            "SELECT a.token FROM analyse_scenarios a JOIN \"group\" g ON g.id = a.group_id " +
            "WHERE g.name LIKE @prefix";
        const string groupFilter =
            "SELECT id FROM \"group\" WHERE name LIKE @prefix";

        var sql =
            $"DELETE FROM work WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM work_change WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM break WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM expenses WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM shift_expenses WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM work_softening WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM client_period_hours WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM client_shift_preference WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM schedule_notes WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM schedule_commands WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM shift WHERE analyse_token IN ({tokenFilter});" +
            $"DELETE FROM analyse_scenarios WHERE group_id IN ({groupFilter});" +
            $"DELETE FROM group_item WHERE group_id IN ({groupFilter});" +
            $"DELETE FROM assigned_group WHERE group_id IN ({groupFilter});" +
            $"DELETE FROM group_visibility WHERE group_id IN ({groupFilter});" +
            "DELETE FROM \"group\" WHERE name LIKE @prefix;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("prefix", SeededGroupNamePrefix + "%");
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<Guid>> SelectIdsAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Guid>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetGuid(0));
        }
        return list;
    }

    private static bool IsNavigationRace(Exception ex)
    {
        return ex.Message.Contains("Execution context was destroyed", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("because of a navigation", StringComparison.OrdinalIgnoreCase);
    }
}
