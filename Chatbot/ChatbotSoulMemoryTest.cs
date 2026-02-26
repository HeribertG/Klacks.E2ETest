// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(52)]
    public class ChatbotSoulMemoryTest : ChatbotTestBase
    {
        private const string SkillUpdateSoul = "update_ai_soul";
        private const string SkillGetSoul = "get_ai_soul";
        private const string SkillAddMemory = "add_ai_memory";
        private const string SkillGetMemories = "get_ai_memories";

        private const string TestSoulText = "Du bist Klacks, ein freundlicher Planungsassistent. Du sprichst locker aber professionell.";
        private const string ResetSoulText = "Du bist ein hilfreicher KI-Assistent.";
        private const string MemoryHerbert = "Der Hauptadministrator heisst Herbert und bevorzugt die deutsche Sprache.";
        private const string MemoryCompany = "Das Unternehmen hat 50 Mitarbeiter und sitzt in Bern.";

        private int _messageCountBefore;

        [Test, Order(1)]
        public async Task Step1_OpenChat()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test, Order(2)]
        public async Task Step2_SetAiSoul()
        {
            TestContext.Out.WriteLine("=== Step 2: Set AI Soul ===");
            await AssertSkillEnabled(SkillUpdateSoul);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze die KI-Persoenlichkeit auf: {TestSoulText}");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm soul update");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI soul set successfully");
        }

        [Test, Order(3)]
        public async Task Step3_GetAiSoul()
        {
            TestContext.Out.WriteLine("=== Step 3: Get AI Soul ===");
            await AssertSkillEnabled(SkillGetSoul);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuelle KI-Persoenlichkeit");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return the soul");
            Assert.That(response.Contains("Klacks", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should contain 'Klacks'. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI soul retrieved successfully");
        }

        [Test, Order(4)]
        public async Task Step4_AddMemory()
        {
            TestContext.Out.WriteLine("=== Step 4: Add AI Memory ===");
            await AssertSkillEnabled(SkillAddMemory);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Merke dir: {MemoryHerbert}");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm memory was added");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI memory added successfully");
        }

        [Test, Order(5)]
        public async Task Step5_GetMemories()
        {
            TestContext.Out.WriteLine("=== Step 5: Get AI Memories ===");
            await AssertSkillEnabled(SkillGetMemories);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige alle gespeicherten Erinnerungen");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return memories");
            Assert.That(response.Contains("Herbert", StringComparison.OrdinalIgnoreCase), Is.True,
                $"Response should mention 'Herbert'. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI memories retrieved successfully");
        }

        [Test, Order(6)]
        public async Task Step6_AddSecondMemory()
        {
            TestContext.Out.WriteLine("=== Step 6: Add Second Memory ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Merke dir auch: {MemoryCompany}");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm second memory was added");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Second AI memory added successfully");
        }

        [Test, Order(7)]
        public async Task Step7_VerifyMultipleMemories()
        {
            TestContext.Out.WriteLine("=== Step 7: Verify Multiple Memories ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Wie viele Erinnerungen hast du gespeichert? Liste sie auf.");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should list memories");
            Assert.That(
                response.Contains("Herbert", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Bern", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should mention stored facts. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Multiple memories verified successfully");
        }

        [Test, Order(8)]
        public async Task Step8_ResetSoul()
        {
            TestContext.Out.WriteLine("=== Step 8: Reset AI Soul ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze die KI-Persoenlichkeit auf: {ResetSoulText}");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm soul reset");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI soul reset successfully");
        }
    }
}
