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
[Category("Klacksy")]
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
    private const int MaxReactiveTurns = 10;

    private const string CurrentLangStorageKey = "CURRENT_LANG";

    // Per-language phrase set for the reactive Multi-Turn flow: the message this test sends per slot,
    // and the keywords it looks for in Klacksy's own phrasing of that slot's question. New languages are
    // learned iteratively — run the test, see what Klacksy actually asks, extend the keyword lists —
    // exactly how the "de" entries below were built up.
    private sealed record OnboardingPhrases(
        string Opener,
        string ConfirmationYes,
        string GenderAnswer,
        string EntityTypeAnswer,
        string StartDateAnswer,
        string NoContactAnswer,
        string ContractPrefix,
        string GroupPrefix,
        string ContractKeyword,
        string GroupKeyword,
        string[] ConfirmationKeywords,
        string[] NameKeywords,
        string[] GenderKeywords,
        string[] EntityTypeKeywords,
        string[] DateKeywords,
        string[] AddressKeywords);

    private static readonly Dictionary<string, OnboardingPhrases> Phrases = new()
    {
        ["de"] = new OnboardingPhrases(
            Opener: "Ich möchte einen neuen Mitarbeiter anlegen.",
            ConfirmationYes: "Ja",
            GenderAnswer: "männlich",
            EntityTypeAnswer: "Angestellter",
            StartDateAnswer: "Eintritt am 1. Mai 2026",
            NoContactAnswer: "Keine Kontaktdaten, bitte ohne Adresse anlegen.",
            ContractPrefix: "Vertrag",
            GroupPrefix: "Gruppe",
            ContractKeyword: "vertrag",
            GroupKeyword: "gruppe",
            ConfirmationKeywords: ["richtig", "korrekt", "stimmt das"],
            NameKeywords: ["vor- und nachname", "wie heißt", "wie heisst"],
            GenderKeywords: ["geschlecht"],
            EntityTypeKeywords: ["entität", "entitaet", "mitarbeitertyp", "angestellter", "externemp", "externer mitarbeiter"],
            DateKeywords: ["eintritt", "eintrittsdatum"],
            AddressKeywords: ["adresse", "kontaktdaten", "postleitzahl", "straße", "strasse"]),
        ["fr"] = new OnboardingPhrases(
            Opener: "Je voudrais créer un nouveau collaborateur.",
            ConfirmationYes: "Oui",
            GenderAnswer: "masculin",
            EntityTypeAnswer: "Employé",
            StartDateAnswer: "Entrée le 1er mai 2026",
            NoContactAnswer: "Aucune coordonnée, merci de créer sans adresse.",
            ContractPrefix: "Contrat",
            GroupPrefix: "Groupe",
            ContractKeyword: "contrat",
            GroupKeyword: "groupe",
            ConfirmationKeywords: ["correct", "exact", "d'accord", "êtes-vous sûr"],
            NameKeywords: ["nom et prénom", "comment s'appelle", "quel est le nom", "prénom"],
            GenderKeywords: ["sexe", "genre"],
            EntityTypeKeywords: ["type de collaborateur", "employé", "externe", "client"],
            DateKeywords: ["entrée", "date de début", "date d'entrée"],
            AddressKeywords: ["adresse", "coordonnées", "code postal", "rue"]),
    };

    [TearDown]
    public async Task RemoveTestClient() => await PurgeTestClient();

    [TestCase("de")]
    [TestCase("fr")]
    public async Task Multi_Turn_Onboards_Across_All_Three_Mutations(string langCode)
    {
        var phrases = Phrases[langCode];

        await AssertSkillEnabled(SkillCreateEmployee);
        await AssertSkillEnabled(SkillAssignContract);
        await AssertSkillEnabled(SkillAddToGroup);

        var contractName = await UniqueContractAsync();
        var groupName = await UniqueGroupAsync();
        Assert.That(contractName, Is.Not.Empty, "a uniquely searchable contract is required");
        Assert.That(groupName, Is.Not.Empty, "a uniquely searchable group is required");
        TestContext.Out.WriteLine($"[multiturn/{langCode}] contract='{contractName}' group='{groupName}'");

        await SwitchUiLanguageAsync(langCode);
        await PurgeTestClient();
        await EnsureChatOpen();
        await ClearChatAndWait();

        // Turn 1: a bare opener with no specifics — the recipe must pause on the first ask, creating nothing.
        var before1 = await GetMessageCount();
        await SendChatMessage(phrases.Opener);
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

        // React to whatever Klacksy actually asks next, rather than assuming a fixed slot order —
        // the recipe/model can ask slots in a different order (or ask an extra one, e.g. entityType)
        // than a hardcoded script anticipates, and a mismatched fixed answer just gets ignored/re-asked.
        var lastBotMessage = opener;
        var allMutationsDone = await AllMutationsDoneAsync();
        for (var turn = 0; turn < MaxReactiveTurns && !allMutationsDone; turn++)
        {
            var answer = await DetermineAnswerAsync(lastBotMessage, contractName, groupName, phrases);
            Assert.That(answer, Is.Not.Null,
                $"Could not determine a reply for Klacksy's question: {Trim(lastBotMessage)}");

            var before = await GetMessageCount();
            await SendChatMessage(answer!);
            lastBotMessage = await WaitForBotResponse(before, TurnTimeoutMs);
            TestContext.Out.WriteLine($"Bot: {Trim(lastBotMessage)}");

            allMutationsDone = await AllMutationsDoneAsync();
        }

        Assert.That(allMutationsDone, Is.True,
            $"Onboarding did not complete within {MaxReactiveTurns} reactive turns. Last bot message: {Trim(lastBotMessage)}");

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

    // Switches the app's display/chat language before the recipe conversation — the assistant chat
    // forwards TranslateService's active language to the LLM request (see assistant-chat.component.ts
    // updateLLMLanguage), so this is what actually makes Klacksy converse in the target language.
    // ApplicationInitService only seeds CURRENT_LANG when absent, so writing it directly and reloading
    // is enough; no UI navigation to the profile page is needed.
    private async Task SwitchUiLanguageAsync(string langCode)
    {
        await Actions.SetLocalStorage(CurrentLangStorageKey, langCode);
        await Actions.Reload();
        await Actions.Wait1000();
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

    // Once the client exists, the recipe engine force-calls the next un-satisfied mutate skill
    // (tool_choice=required) regardless of what the bot's own prose says — Klacksy can chatter an
    // optional side-question (e.g. offering to add an address) while the engine is already forcing the
    // contract/group step underneath it, and answering the visible prose then gets fed as garbage into
    // the forced skill's parameter. DB state is ground truth for these later steps; bot-text keyword
    // matching is only needed for the earlier steps (name/gender/entityType/startDate/contactDetails),
    // where nothing has landed in the database yet to disambiguate what's being asked.
    private static async Task<string?> DetermineAnswerAsync(
        string botText, string contractName, string groupName, OnboardingPhrases phrases)
    {
        if (await ClientCountAsync() >= 1)
        {
            if (await ActiveContractCountAsync() < 1)
            {
                return $"{phrases.ContractPrefix} {contractName}";
            }

            if (await GroupMembershipCountAsync() < 1)
            {
                return $"{phrases.GroupPrefix} {groupName}";
            }
        }

        return AnswerForBotQuestion(botText, contractName, groupName, phrases);
    }

    // Picks the answer for whatever Klacksy's last message actually asks — a reply often confirms the
    // previous step ("...ohne Adresse angelegt, wie gewünscht") in the same breath as asking the next
    // one ("...Welcher Vertrag soll zugewiesen werden?"), and a confirmation clause can contain an
    // earlier slot's keyword (e.g. "Adresse") while the real, live question is a different slot
    // entirely. Checking sentences from the END finds the actual trailing question instead of matching
    // whichever slot's keyword happens to appear first in the whole message. Returns null when no
    // sentence matches any known slot, which the caller turns into a clear test failure instead of
    // silently sending an answer to the wrong question.
    private static string? AnswerForBotQuestion(
        string botText, string contractName, string groupName, OnboardingPhrases phrases)
    {
        var sentences = botText.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = sentences.Length - 1; i >= 0; i--)
        {
            var answer = AnswerForSentence(sentences[i], contractName, groupName, phrases);
            if (answer != null)
            {
                return answer;
            }
        }

        return null;
    }

    private static string? AnswerForSentence(
        string sentence, string contractName, string groupName, OnboardingPhrases phrases)
    {
        if (ContainsAny(sentence, phrases.ContractKeyword))
            return $"{phrases.ContractPrefix} {contractName}";
        if (ContainsAny(sentence, phrases.GroupKeyword))
            return $"{phrases.GroupPrefix} {groupName}";
        // A yes/no confirmation of something already said (e.g. "Onboardia ist der Vorname, richtig?")
        // — the '?' delimiter is already stripped by the sentence split, so match on the phrase alone.
        if (ContainsAny(sentence, phrases.ConfirmationKeywords))
            return phrases.ConfirmationYes;
        if (ContainsAny(sentence, phrases.NameKeywords))
            return $"{TestFirstName} {TestLastName}";
        if (ContainsAny(sentence, phrases.GenderKeywords))
            return phrases.GenderAnswer;
        if (ContainsAny(sentence, phrases.EntityTypeKeywords))
            return phrases.EntityTypeAnswer;
        if (ContainsAny(sentence, phrases.DateKeywords))
            return phrases.StartDateAnswer;
        if (ContainsAny(sentence, phrases.AddressKeywords))
            return phrases.NoContactAnswer;
        return null;
    }

    private static bool ContainsAny(string text, params string[] needles) =>
        needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static async Task<bool> AllMutationsDoneAsync() =>
        await ClientCountAsync() >= 1 && await ActiveContractCountAsync() >= 1 && await GroupMembershipCountAsync() >= 1;

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
