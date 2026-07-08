// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the recurring (cron) task feature end to end through the real LLM: a natural-language
 * reminder is turned into a concrete schedule and persisted via schedule_recurring_task. Two turns
 * (apply preview -> confirm). The decisive assertion is that the time zone is derived from the app
 * owner's address country (globalCalendarCountry = CH -> Europe/Zurich) WITHOUT the user naming any
 * zone. DB-asserted via the scheduled_tasks row. Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live scheduled-task proof; slow and nondeterministic. Run on demand.")]
[Category("Klacksy")]
public class ChatbotScheduledTaskTest : ChatbotTestBase
{
    private const string SkillSchedule = "schedule_recurring_task";

    // A distinctive token the LLM keeps verbatim in the reminder text, so the created row is findable.
    private const string Marker = "E2EZTZREMINDER";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 3000;
    private const int SettleMaxPolls = 15;

    [SetUp]
    public async Task CleanLeftovers()
    {
        await DeleteMarkerRows();
    }

    [TearDown]
    public async Task RemoveTestTask()
    {
        await DeleteMarkerRows();
    }

    [Test]
    public async Task NaturalLanguageReminder_DerivesTimeZoneFromOwnerAddress_AndPersists()
    {
        await AssertSkillEnabled(SkillSchedule);

        var ownerCountry = await ScalarAsync(
            "SELECT value FROM settings WHERE type = 'globalCalendarCountry' LIMIT 1");
        if (!string.Equals(ownerCountry, "CH", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore($"This test expects the app owner country to be CH; found '{ownerCountry}'.");
        }

        const string expectedTimeZone = "Europe/Zurich";
        var beforeCalls = await SuccessCallCountAsync(SkillSchedule);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage($"Erinnere mich jeden Montag um 8 Uhr, {Marker} zu pruefen.");
        var preview = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (preview): {Trim(preview)}");

        var afterPreview = await GetMessageCount();
        await SendChatMessage("Ja, bitte richte das so ein.");
        var confirm = await WaitForBotResponse(afterPreview, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (confirm): {Trim(confirm)}");

        var row = await WaitForTaskRowAsync();
        Assert.That(row, Is.Not.Empty,
            "schedule_recurring_task must persist a scheduled_tasks row after the user confirms");

        var parts = row.Split('|');
        var timeZone = parts.Length > 0 ? parts[0].Trim() : string.Empty;
        var actionType = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        var cron = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        var addCalls = await SuccessCallCountAsync(SkillSchedule) - beforeCalls;
        TestContext.Out.WriteLine($"[scheduled task] tz='{timeZone}' action='{actionType}' cron='{cron}'");

        Assert.Multiple(() =>
        {
            Assert.That(timeZone, Is.EqualTo(expectedTimeZone),
                "the schedule time zone must be derived from the owner address country (CH -> Europe/Zurich) without the user naming a zone");
            Assert.That(actionType, Is.EqualTo("reminder"),
                "a 'remind me' request must produce a reminder task");
            Assert.That(cron, Does.Match("^0\\s+8\\s+\\*\\s+\\*\\s+(1|MON)$").IgnoreCase,
                "the cron must encode Mondays at 08:00");
            Assert.That(addCalls, Is.GreaterThanOrEqualTo(1),
                "schedule_recurring_task must have run successfully at least once");
        });
    }

    private async Task<string> WaitForTaskRowAsync()
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            var row = await ScalarAsync(
                "SELECT time_zone_id, action_type, cron_expression FROM scheduled_tasks " +
                $"WHERE NOT is_deleted AND message_text ILIKE '%{Marker}%' " +
                "ORDER BY create_time DESC LIMIT 1");
            if (!string.IsNullOrEmpty(row))
            {
                return row;
            }
        }

        return string.Empty;
    }

    private static async Task DeleteMarkerRows()
    {
        await DbHelper.ExecuteSqlAsync(
            $"DELETE FROM scheduled_tasks WHERE message_text ILIKE '%{Marker}%' OR name ILIKE '%{Marker}%'");
    }

    private static async Task<int> SuccessCallCountAsync(string skillName) =>
        await ScalarIntAsync(
            $"SELECT count(*) FROM skill_usage_records WHERE skill_name='{Escape(skillName)}' AND success=true");

    private static async Task<string> ScalarAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return result.StartsWith("ERROR:") ? string.Empty : result.Split('\n')[0].Trim();
    }

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = await ScalarAsync(sql);
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(220, text.Length)];
}
