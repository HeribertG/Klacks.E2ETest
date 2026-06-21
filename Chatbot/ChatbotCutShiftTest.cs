// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live reliability test for Klacksy completing the 24h create -> split flow end-to-end via the
 * chat UI (the exact flow that previously failed: the model created the order, navigated to the cut
 * page and wrote manual instructions instead of calling cut_shift). Verifies the database end state:
 * one SealedOrder, zero leftover OriginalShifts, exactly three SplitShifts that share the order's
 * original_id, all is_time_range=true; and the skill trace shows create_shift exactly once plus a
 * successful cut_shift (the old failure signature was 3x create_shift / no cut_shift). Explicit:
 * LLM-driven and slow, run on demand against the default chat model (gemini-3.5-flash).
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live 24h split flow; slow and nondeterministic. Run on demand to verify cut_shift is invoked.")]
public class ChatbotCutShiftTest : ChatbotTestBase
{
    private const string SkillCreateShift = "create_shift";
    private const string SkillCutShift = "cut_shift";

    private const string GroupName = "Biel/Bienne";
    private const string CustomerName = "Biel GmbH";
    // Throwaway order name (distinct from any real delivered order) so re-runs never touch live data.
    private const string OrderName = "E2E CutTest 24-7 Biel";
    private const string FromDate = "2026-06-01";

    private const int CreateTimeoutMs = 180000;
    private const int MaxConfirmTurns = 8;
    private const int ConfirmLoopDelayMs = 4000;

    [TearDown]
    public async Task RemoveTestOrder()
    {
        await SoftDeletePreviousOrderAsync();
    }

    [Test]
    public async Task Klacksy_Creates_And_Splits_24h_Order_Into_Three_Parts()
    {
        await AssertSkillEnabled(SkillCreateShift);
        await AssertSkillEnabled(SkillCutShift);

        await SoftDeletePreviousOrderAsync();

        // Count successful calls before the run; assert the delta afterwards. Robust against the
        // ~2h timestamp skew between skill_usage_records and DB now() (a separate logging quirk).
        var beforeCreate = await SuccessCallCountAsync(SkillCreateShift);
        var beforeCut = await SuccessCallCountAsync(SkillCutShift);

        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Erstelle eine 24/7-Bestellung mit Namen '{OrderName}' fuer den bestehenden Kunden {CustomerName} " +
            $"in der Gruppe {GroupName}, gueltig ab {FromDate}, und teile sie anschliessend in genau 3 Dienste auf: " +
            "Fruehdienst 07:00-15:00, Spaetdienst 15:00-23:00, Nachtdienst 23:00-07:00. " +
            "Lege keinen neuen Kunden an. Fuehre den Schnitt direkt aus (cut_shift), navigiere nicht zur Zuschnitt-Seite.");
        var response = await WaitForBotResponse(before, CreateTimeoutMs);
        TestContext.Out.WriteLine($"Bot: {Trim(response)}");

        await ConfirmUntilAsync(ThreeSplitsExistAsync);

        var sealedCount = await ScalarIntAsync(
            $"SELECT count(*) FROM shift WHERE name='{Escape(OrderName)}' AND status=1 AND NOT is_deleted");
        var leftoverOriginals = await ScalarIntAsync(
            $"SELECT count(*) FROM shift WHERE name='{Escape(OrderName)}' AND status=2 AND NOT is_deleted");
        var splitCount = await ScalarIntAsync(
            "SELECT count(*) FROM shift s WHERE s.status=3 AND NOT s.is_deleted AND s.original_id IN " +
            $"(SELECT id FROM shift WHERE name='{Escape(OrderName)}' AND status=1 AND NOT is_deleted)");
        var splitNotTimeRange = await ScalarIntAsync(
            "SELECT count(*) FROM shift s WHERE s.status=3 AND NOT s.is_deleted AND s.is_time_range=false AND s.original_id IN " +
            $"(SELECT id FROM shift WHERE name='{Escape(OrderName)}' AND status=1 AND NOT is_deleted)");
        var orderInGroup = await ScalarIntAsync(
            "SELECT count(*) FROM group_item gi JOIN \"group\" g ON g.id=gi.group_id " +
            "JOIN shift s ON s.id=gi.shift_id " +
            $"WHERE s.name='{Escape(OrderName)}' AND s.status=1 AND NOT s.is_deleted AND NOT gi.is_deleted " +
            $"AND g.name ILIKE '%{Escape(GroupName)}%'");

        var createShiftCalls = await SuccessCallCountAsync(SkillCreateShift) - beforeCreate;
        var cutShiftCalls = await SuccessCallCountAsync(SkillCutShift) - beforeCut;

        TestContext.Out.WriteLine(
            $"[cut-flow] sealed={sealedCount} leftoverOriginals={leftoverOriginals} splits={splitCount} " +
            $"splitNotTimeRange={splitNotTimeRange} orderInGroup={orderInGroup} " +
            $"create_shift_calls={createShiftCalls} cut_shift_calls={cutShiftCalls}");

        Assert.Multiple(() =>
        {
            Assert.That(sealedCount, Is.EqualTo(1), "exactly one SealedOrder (the immutable Bestellung) must exist");
            Assert.That(splitCount, Is.EqualTo(3), "the order must be cut into exactly 3 SplitShifts");
            Assert.That(leftoverOriginals, Is.EqualTo(0),
                "no OriginalShift may be left over (the first part must be the converted plannable shift)");
            Assert.That(splitNotTimeRange, Is.EqualTo(0), "every split part must be is_time_range=true");
            Assert.That(orderInGroup, Is.GreaterThanOrEqualTo(1), $"the order must be assigned to group '{GroupName}'");
            Assert.That(createShiftCalls, Is.EqualTo(1),
                "create_shift must run exactly once (old failure signature was 3x create_shift)");
            Assert.That(cutShiftCalls, Is.GreaterThanOrEqualTo(1),
                "cut_shift must have been called by the model (the regression was: it never was)");
        });
    }

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
                "Ja, bitte jetzt direkt ausfuehren: lege die Bestellung an und schneide sie mit cut_shift in die 3 Dienste. " +
                "Frag nicht weiter nach und navigiere nicht zur Zuschnitt-Seite.");
            var response = await WaitForBotResponse(before, CreateTimeoutMs);
            TestContext.Out.WriteLine($"Confirm {turn + 1}: {Trim(response)}");
        }

        await Task.Delay(ConfirmLoopDelayMs);
    }

    private static async Task<bool> ThreeSplitsExistAsync()
    {
        var count = await ScalarIntAsync(
            "SELECT count(*) FROM shift s WHERE s.status=3 AND NOT s.is_deleted AND s.original_id IN " +
            $"(SELECT id FROM shift WHERE name='{Escape(OrderName)}' AND status=1 AND NOT is_deleted)");
        return count >= 3;
    }

    private static async Task SoftDeletePreviousOrderAsync()
    {
        // Delete the whole order family: the SealedOrder named OrderName AND every shift whose
        // original_id points at it. cut_shift RENAMES the split parts (Frühdienst/…), so a name-only
        // delete would orphan the active parts in the group (they then keep showing up in
        // find_split_shift_candidates). Match the children by original_id, not by name.
        var esc = Escape(OrderName);
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

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(200, text.Length)];
}
