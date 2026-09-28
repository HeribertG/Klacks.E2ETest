// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * Live proof of the guided setup recipes "create-branch" and "create-calendar-selection" plus the
 * server-side update_branch skill. Every flow is DB-asserted, never text-asserted:
 *   - Branch: the opening message only states the intent, so the recipe has to ask name, address and
 *     contact one after the other; the branch row must carry exactly the answered values.
 *   - Holiday calendar: name and regions are answered turn by turn; the calendar selection must merge
 *     exactly the canton calendars CH-BE and CH-ZH.
 *   - Branch change: a free sentence changes the phone number of an existing branch by name.
 * Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
[Category("Klacksy")]
public class ChatbotRecipeBranchCalendarSetupTest : ChatbotTestBase
{
    private const string SkillCreateBranch = "create_branch";
    private const string SkillUpdateBranch = "update_branch";
    private const string SkillCreateCalendarSelection = "create_calendar_selection";

    private const string BranchAddress = "Bahnhofstrasse 12, 8400 Winterthur";
    private const string BranchPhone = "052 123 45 67";
    private const string ChangedPhone = "052 999 88 77";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 4000;
    private const int SettleMaxPolls = 30;

    private string _branchName = string.Empty;
    private string _calendarName = string.Empty;

    [TearDown]
    public async Task RemoveTestData()
    {
        if (_branchName.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                $"UPDATE branch SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND name='{Escape(_branchName)}'");
        }

        if (_calendarName.Length > 0)
        {
            await DbHelper.ExecuteSqlAsync(
                $"UPDATE calendar_selection SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND name='{Escape(_calendarName)}'");
        }
    }

    [Test]
    public async Task Branch_Is_Created_Through_Question_And_Answer()
    {
        await AssertSkillEnabled(SkillCreateBranch);

        _branchName = "E2E-Filiale-" + Guid.NewGuid().ToString("N")[..8];
        var beforeCreate = await SuccessCallCountAsync(SkillCreateBranch);

        await EnsureChatOpen();
        await ClearChatAndWait();

        await TurnAsync("Erstelle eine neue Filiale");
        await TurnAsync(_branchName);
        await TurnAsync(BranchAddress);
        await TurnAsync($"Telefon {BranchPhone}, keine E-Mail");

        await WaitForCountAsync(() => BranchCountAsync(_branchName));

        var exact = await ScalarIntAsync(
            $"SELECT count(*) FROM branch WHERE NOT is_deleted AND name='{Escape(_branchName)}' " +
            $"AND address='{Escape(BranchAddress)}' AND phone='{Escape(BranchPhone)}'");
        var createCalls = await SuccessCallCountAsync(SkillCreateBranch) - beforeCreate;
        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.EqualTo(1), "the branch must be stored once with the answered name, address and phone");
            Assert.That(createCalls, Is.EqualTo(1), "create_branch must run exactly once for one guided dialogue");
        });
    }

    [Test]
    public async Task Holiday_Calendar_Is_Created_Through_Question_And_Answer()
    {
        await AssertSkillEnabled(SkillCreateCalendarSelection);

        _calendarName = "E2E-Kalender-" + Guid.NewGuid().ToString("N")[..8];

        await EnsureChatOpen();
        await ClearChatAndWait();

        await TurnAsync("Erstelle einen neuen Feiertagskalender");
        await TurnAsync(_calendarName);
        await TurnAsync("Kanton Bern und Kanton Zürich");

        await WaitForCountAsync(() => ScalarIntAsync(
            $"SELECT count(*) FROM calendar_selection WHERE NOT is_deleted AND name='{Escape(_calendarName)}'"));

        var merged = (await DbHelper.ExecuteSqlAsync(
            "SELECT string_agg(sc.country || '-' || sc.state, ',' ORDER BY sc.state) FROM calendar_selection cs " +
            "JOIN selected_calendar sc ON sc.calendar_selection_id=cs.id " +
            $"WHERE NOT cs.is_deleted AND cs.name='{Escape(_calendarName)}'")).Trim();
        Assert.That(merged, Is.EqualTo("CH-BE,CH-ZH"), "the new holiday calendar must merge exactly the cantons Bern and Zurich");
    }

    [Test]
    public async Task Branch_Phone_Is_Changed_By_Name()
    {
        await AssertSkillEnabled(SkillUpdateBranch);

        _branchName = "E2E-Filiale-" + Guid.NewGuid().ToString("N")[..8];
        await DbHelper.ExecuteSqlAsync(
            "INSERT INTO branch (id, name, address, phone, email, is_deleted, create_time) VALUES " +
            $"(gen_random_uuid(), '{Escape(_branchName)}', '{Escape(BranchAddress)}', '{Escape(BranchPhone)}', '', false, now())");

        await EnsureChatOpen();
        await ClearChatAndWait();

        await TurnAsync($"Die Telefonnummer der Filiale {_branchName} hat sich geändert, neu ist {ChangedPhone}.");

        await WaitForCountAsync(() => ScalarIntAsync(
            $"SELECT count(*) FROM branch WHERE NOT is_deleted AND name='{Escape(_branchName)}' AND phone='{Escape(ChangedPhone)}'"));

        var changed = await ScalarIntAsync(
            $"SELECT count(*) FROM branch WHERE NOT is_deleted AND name='{Escape(_branchName)}' " +
            $"AND phone='{Escape(ChangedPhone)}' AND address='{Escape(BranchAddress)}'");
        Assert.That(changed, Is.EqualTo(1), "only the phone number may change, the address must stay");
    }

    private async Task TurnAsync(string message)
    {
        var before = await GetMessageCount();
        await SendChatMessage(message);
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"User: {message}\nBot: {response[..Math.Min(200, response.Length)]}");
    }

    private static async Task WaitForCountAsync(Func<Task<int>> count)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            if (await count() >= 1)
            {
                return;
            }

            await Task.Delay(SettlePollMs);
        }
    }

    private static Task<int> BranchCountAsync(string name) =>
        ScalarIntAsync($"SELECT count(*) FROM branch WHERE NOT is_deleted AND name='{Escape(name)}'");

    private static async Task<int> SuccessCallCountAsync(string skillName) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM skill_usage_records WHERE skill_name='{Escape(skillName)}' AND success=true");

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static string Escape(string value) => value.Replace("'", "''");
}
