// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the data-driven recipe engine on the "onboard-employee" recipe — the first recipe with
 * THREE mutate steps and ask steps interleaved between them (every other recipe has a single, terminal
 * mutate). It exercises the engine path the structural gate cannot: after each mutate Observe() advances
 * the plan, the turn loop continues, and the next ask persists the plan and pauses — so the contract and
 * group steps are never silently dropped. The whole chain is verified against the database, not the bot
 * text:
 *   - a client row exists for the onboarded person,
 *   - an active client_contract links that client to the named contract,
 *   - a group_item links that client to the named group.
 * Two flows:
 *   - Multi-turn (pause/resume): a bare opener pauses on the first ask; later turns supply name, gender,
 *     start date, (no) contact details, contract and group. Asserts the opener creates NOTHING and emits
 *     no no-action notice, and that after the last slot all three mutations have landed.
 *   - Single-turn (extraction): one opener naming every slot fills the bag up front so the three mutates
 *     run end to end in one turn.
 * Explicit: LLM-driven and slow, run on demand.
 */

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("LLM-driven live recipe-engine proof; slow and nondeterministic. Run on demand.")]
public class ChatbotRecipeOnboardEmployeeTest : ChatbotTestBase
{
    private const string SkillCreateEmployee = "create_employee";
    private const string SkillAssignContract = "assign_contract_by_name";
    private const string SkillAddToGroup = "add_client_to_group_by_name";

    // Substring of MutationGuardConstants.NoActionStreamNotice — its presence on an ask-pause turn is
    // exactly the guard collision the engine must avoid (kept in sync with the backend constant).
    private const string NoActionNoticeMarker = "keine Aktion ausgeführt";

    // Distinct name unlikely to collide with seed data; purged before and after each test.
    private const string TestFirstName = "Onboardia";
    private const string TestLastName = "Rezepttest";

    private const int TurnTimeoutMs = 150000;
    private const int SettlePollMs = 3000;
    private const int SettleMaxPolls = 14;

    [TearDown]
    public async Task RemoveTestClient() => await PurgeTestClient();

    [Test]
    public async Task Multi_Turn_Onboards_Across_All_Three_Mutations()
    {
        await AssertSkillEnabled(SkillCreateEmployee);
        await AssertSkillEnabled(SkillAssignContract);
        await AssertSkillEnabled(SkillAddToGroup);

        var contractName = await UniqueContractAsync();
        var groupName = await UniqueGroupAsync();
        Assert.That(contractName, Is.Not.Empty, "a uniquely searchable contract is required");
        Assert.That(groupName, Is.Not.Empty, "a uniquely searchable group is required");
        TestContext.Out.WriteLine($"[multiturn] contract='{contractName}' group='{groupName}'");

        await PurgeTestClient();
        await EnsureChatOpen();
        await ClearChatAndWait();

        // Turn 1: a bare opener with no specifics — the recipe must pause on the first ask, creating nothing.
        var before1 = await GetMessageCount();
        await SendChatMessage("Ich möchte einen neuen Mitarbeiter anlegen.");
        var opener = await WaitForBotResponse(before1, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (turn1/ask): {Trim(opener)}");

        Assert.Multiple(() =>
        {
            Assert.That(opener, Does.Not.Contain(NoActionNoticeMarker),
                "an intentional ask-pause must NOT emit the no-action notice");
        });
        var clientAfterOpener = await ClientCountAsync();
        Assert.That(clientAfterOpener, Is.EqualTo(0),
            "the opener must pause for input, NOT create the employee yet");

        // Turns 2-6: supply each slot. The bot asks the next slot after each mutate (create -> ask contract,
        // assign -> ask group); the recipe drives the order, so plain answers suffice.
        await SendAndLog($"{TestFirstName} {TestLastName}");                 // employeeName
        await SendAndLog("männlich");                                        // gender
        await SendAndLog("Eintritt am 1. Mai 2026");                         // startDate
        await SendAndLog("Keine Kontaktdaten, bitte ohne Adresse anlegen.");  // contactDetails
        await SendAndLog($"Vertrag {contractName}");                         // contractName -> create + assign
        await SendAndLog($"Gruppe {groupName}");                             // groupName    -> add to group

        await WaitForAsync(async () => await GroupMembershipCountAsync() >= 1);

        var clientCount = await ClientCountAsync();
        var activeContracts = await ActiveContractCountAsync();
        var memberships = await GroupMembershipCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(clientCount, Is.EqualTo(1), "exactly one employee must be created");
            Assert.That(activeContracts, Is.GreaterThanOrEqualTo(1),
                "the onboarded employee must have an active contract assigned");
            Assert.That(memberships, Is.GreaterThanOrEqualTo(1),
                "the onboarded employee must be added to the group — the step after two prior mutates must not be dropped");
        });
    }

    [Test]
    public async Task Extraction_From_Opening_Runs_All_Three_Mutations()
    {
        await AssertSkillEnabled(SkillCreateEmployee);

        var contractName = await UniqueContractAsync();
        var groupName = await UniqueGroupAsync();
        Assert.That(contractName, Is.Not.Empty);
        Assert.That(groupName, Is.Not.Empty);
        TestContext.Out.WriteLine($"[extract] contract='{contractName}' group='{groupName}'");

        await PurgeTestClient();
        await EnsureChatOpen();
        await ClearChatAndWait();

        var before = await GetMessageCount();
        await SendChatMessage(
            $"Lege einen neuen Mitarbeiter an: {TestFirstName} {TestLastName}, männlich, " +
            $"Eintritt 1. Mai 2026, ohne Kontaktdaten. Vertrag {contractName}, Gruppe {groupName}.");
        var response = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot (extract): {Trim(response)}");

        await WaitForAsync(async () => await GroupMembershipCountAsync() >= 1);

        var clientCount = await ClientCountAsync();
        var activeContracts = await ActiveContractCountAsync();
        var memberships = await GroupMembershipCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(clientCount, Is.EqualTo(1), "exactly one employee must be created");
            Assert.That(activeContracts, Is.GreaterThanOrEqualTo(1), "an active contract must be assigned");
            Assert.That(memberships, Is.GreaterThanOrEqualTo(1), "the employee must be added to the group");
            Assert.That(response, Does.Not.Contain(NoActionNoticeMarker),
                "a completed recipe must not emit the no-action notice");
        });
    }

    private async Task SendAndLog(string message)
    {
        var before = await GetMessageCount();
        await SendChatMessage(message);
        var reply = await WaitForBotResponse(before, TurnTimeoutMs);
        TestContext.Out.WriteLine($"Bot: {Trim(reply)}");
    }

    private async Task WaitForAsync(Func<Task<bool>> condition)
    {
        for (var poll = 0; poll < SettleMaxPolls; poll++)
        {
            await Task.Delay(SettlePollMs);
            if (await condition())
            {
                return;
            }
        }
    }

    private static Task<int> ClientCountAsync() =>
        ScalarIntAsync(
            $"SELECT count(*) FROM client WHERE NOT is_deleted " +
            $"AND first_name='{Escape(TestFirstName)}' AND name='{Escape(TestLastName)}'");

    private static Task<int> ActiveContractCountAsync() =>
        ScalarIntAsync(
            "SELECT count(*) FROM client_contract cc JOIN client c ON cc.client_id=c.id " +
            "WHERE NOT cc.is_deleted AND cc.is_active AND NOT c.is_deleted " +
            $"AND c.first_name='{Escape(TestFirstName)}' AND c.name='{Escape(TestLastName)}'");

    private static Task<int> GroupMembershipCountAsync() =>
        ScalarIntAsync(
            "SELECT count(*) FROM group_item gi JOIN client c ON gi.client_id=c.id " +
            "WHERE NOT gi.is_deleted AND NOT c.is_deleted " +
            $"AND c.first_name='{Escape(TestFirstName)}' AND c.name='{Escape(TestLastName)}'");

    // Soft-delete the test person and everything hanging off it, matched by id WITHOUT the is_deleted
    // filter so repeated runs also clear soft-deleted leftovers. Dependents first, client last.
    private static async Task PurgeTestClient()
    {
        var ids = $"(SELECT id FROM client WHERE first_name='{Escape(TestFirstName)}' AND name='{Escape(TestLastName)}')";
        await DbHelper.ExecuteSqlAsync($"UPDATE group_item SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND client_id IN {ids}");
        await DbHelper.ExecuteSqlAsync($"UPDATE membership SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND client_id IN {ids}");
        await DbHelper.ExecuteSqlAsync($"UPDATE client_contract SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND client_id IN {ids}");
        await DbHelper.ExecuteSqlAsync($"UPDATE client SET is_deleted=true, deleted_time=now() WHERE NOT is_deleted AND first_name='{Escape(TestFirstName)}' AND name='{Escape(TestLastName)}'");
    }

    private static async Task<string> UniqueContractAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT c.name FROM contract c WHERE NOT c.is_deleted AND c.name IS NOT NULL AND c.name<>'' " +
            "AND (SELECT count(*) FROM contract x WHERE NOT x.is_deleted AND x.name ILIKE '%'||c.name||'%')=1 " +
            "ORDER BY length(c.name) DESC LIMIT 1")).Trim();
        return result.Split('\n')[0].Trim();
    }

    private static async Task<string> UniqueGroupAsync()
    {
        var result = (await DbHelper.ExecuteSqlAsync(
            "SELECT g.name FROM \"group\" g WHERE NOT g.is_deleted AND g.name IS NOT NULL AND g.name<>'' " +
            "AND (SELECT count(*) FROM \"group\" x WHERE NOT x.is_deleted AND x.name ILIKE '%'||g.name||'%')=1 " +
            "ORDER BY length(g.name) DESC LIMIT 1")).Trim();
        return result.Split('\n')[0].Trim();
    }

    private static async Task<int> ScalarIntAsync(string sql)
    {
        var result = (await DbHelper.ExecuteSqlAsync(sql)).Trim();
        return int.TryParse(result, out var n) ? n : 0;
    }

    private static string Escape(string value) => value.Replace("'", "''");

    private static string Trim(string text) => text[..Math.Min(200, text.Length)];
}
