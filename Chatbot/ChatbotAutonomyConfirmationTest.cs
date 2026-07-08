// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(62)]
    [Ignore("Chatbot tests depend on external LLM (KIMI/OpenRouter); flaky in fresh-DB CI runs. Live-verified 7/7 green 2026-06-10.")]
    [Category("Klacksy")]
    public class ChatbotAutonomyConfirmationTest : ChatbotTestBase
    {
        private const string SkillSetAutonomyLevel = "set_autonomy_level";
        private const string SkillGetAutonomyLevel = "get_autonomy_level";
        private const int BotTimeoutMs = 120000;

        private const string TestMemoryMarker = "E2E-Autonomie-Roundtrip-Marker";

        private int _messageCountBefore;

        [OneTimeSetUp]
        public async Task ResetAutonomyState()
        {
            await DbHelper.ExecuteSqlAsync("DELETE FROM agent_autonomy_preferences");
            await DbHelper.ExecuteSqlAsync(
                $"DELETE FROM agent_memories WHERE content LIKE '%{TestMemoryMarker}%'");
        }

        [OneTimeTearDown]
        public async Task CleanupAutonomyState()
        {
            await DbHelper.ExecuteSqlAsync("DELETE FROM agent_autonomy_preferences");
            await DbHelper.ExecuteSqlAsync(
                $"DELETE FROM agent_memories WHERE content LIKE '%{TestMemoryMarker}%'");
        }

        [Test, Order(1)]
        public async Task Step1_OpenChat_AndVerifySkills()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat + verify autonomy skills ===");
            await AssertSkillEnabled(SkillGetAutonomyLevel);
            await AssertSkillEnabled(SkillSetAutonomyLevel);

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");
        }

        [Test, Order(2)]
        public async Task Step2_DefaultLevel_IsAutonomous()
        {
            TestContext.Out.WriteLine("=== Step 2: Default autonomy level is 2 (Autonomous) ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie hoch ist deine aktuelle Autonomiestufe?");
            var response = await WaitForBotResponse(_messageCountBefore, BotTimeoutMs);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should report its autonomy level");
            Assert.That(
                response.Contains("2") || response.Contains("Autonomous", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Default level should be 2/Autonomous. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
        }

        [Test, Order(3)]
        public async Task Step3_SetLevelZero_RequiresConfirmation()
        {
            TestContext.Out.WriteLine("=== Step 3: set_autonomy_level is sensitive -> confirmation question, no change yet ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze deine Autonomiestufe auf 0.");
            var response = await WaitForBotResponse(_messageCountBefore, BotTimeoutMs);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should ask for confirmation");

            var rows = await DbHelper.ExecuteSqlAsync(
                "SELECT level FROM agent_autonomy_preferences WHERE is_deleted = false");
            Assert.That(rows.Contains("0"), Is.False,
                $"Level must NOT be changed before the user confirmed. DB: {rows}");
        }

        [Test, Order(4)]
        public async Task Step4_ConfirmSetLevelZero_PersistsLevel()
        {
            TestContext.Out.WriteLine("=== Step 4: explicit confirmation -> level 0 persisted ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Ja, ich bestätige: setze die Autonomiestufe auf 0.");
            var response = await WaitForBotResponse(_messageCountBefore, BotTimeoutMs);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm the level change");

            var rows = await DbHelper.ExecuteSqlAsync(
                "SELECT level FROM agent_autonomy_preferences WHERE is_deleted = false");
            Assert.That(rows.Contains("0"), Is.True,
                $"Level 0 must be persisted after explicit confirmation. DB: {rows}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
        }

        [Test, Order(5)]
        public async Task Step5_WriteActionAtLevelZero_IsHeldForConfirmation()
        {
            TestContext.Out.WriteLine("=== Step 5: at level 0 even a reversible write is held ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Speichere dauerhaft in deinem Gedächtnis als Notiz: {TestMemoryMarker}.");
            var response = await WaitForBotResponse(_messageCountBefore, BotTimeoutMs);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should ask for confirmation before writing");

            var rows = await DbHelper.ExecuteSqlAsync(
                $"SELECT id FROM agent_memories WHERE content LIKE '%{TestMemoryMarker}%' AND is_deleted = false");
            Assert.That(string.IsNullOrWhiteSpace(rows) || !rows.Contains("-"), Is.True,
                $"Memory must NOT be written before confirmation. DB: {rows}");
        }

        [Test, Order(6)]
        public async Task Step6_ConfirmWriteAction_ExecutesIt()
        {
            TestContext.Out.WriteLine("=== Step 6: confirmation -> memory written ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Ja, ich bestätige: speichere die Notiz '{TestMemoryMarker}' jetzt dauerhaft in deinem Gedächtnis.");
            var response = await WaitForBotResponse(_messageCountBefore, BotTimeoutMs);

            TestContext.Out.WriteLine($"Bot response: {response}");

            var rows = await DbHelper.ExecuteSqlAsync(
                $"SELECT id FROM agent_memories WHERE content LIKE '%{TestMemoryMarker}%' AND is_deleted = false");
            Assert.That(rows.Contains("-"), Is.True,
                $"Memory must exist after explicit confirmation. DB: {rows}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
        }

        [Test, Order(7)]
        public async Task Step7_RestoreLevelTwo()
        {
            TestContext.Out.WriteLine("=== Step 7: restore level 2 (confirm immediately) ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Setze deine Autonomiestufe wieder auf 2.");
            await WaitForBotResponse(_messageCountBefore, BotTimeoutMs);

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Ja, ich bestätige: setze die Autonomiestufe auf 2.");
            var response = await WaitForBotResponse(_messageCountBefore, BotTimeoutMs);

            TestContext.Out.WriteLine($"Bot response: {response}");

            var rows = await DbHelper.ExecuteSqlAsync(
                "SELECT level FROM agent_autonomy_preferences WHERE is_deleted = false");
            Assert.That(rows.Contains("2"), Is.True,
                $"Level must be back at 2. DB: {rows}");
        }
    }
}
