// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(55)]
    public class ChatbotAiGuidelinesTest : ChatbotTestBase
    {
        private const string SkillGetGuidelines = "get_ai_guidelines";
        private const string SkillUpdateGuidelines = "update_ai_guidelines";

        private const string TestGuideline = "Antworte immer hoeflich und verwende die Anrede 'Sie'. Gib bei Fehlern immer eine Loesung vor.";
        private const string SecondGuideline = "Verwende kurze Saetze. Keine Emojis. Antworte auf Deutsch.";
        private const string ResetGuideline = "Sei ein hilfreicher Assistent.";

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
        public async Task Step2_GetCurrentGuidelines()
        {
            TestContext.Out.WriteLine("=== Step 2: Get Current AI Guidelines ===");
            await AssertSkillEnabled(SkillGetGuidelines);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen KI-Richtlinien");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return current guidelines");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Current guidelines retrieved successfully");
        }

        [Test, Order(3)]
        public async Task Step3_UpdateGuidelines()
        {
            TestContext.Out.WriteLine("=== Step 3: Update AI Guidelines ===");
            await AssertSkillEnabled(SkillUpdateGuidelines);
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Aktualisiere die KI-Richtlinien auf: {TestGuideline}");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm guidelines update");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI guidelines updated successfully");
        }

        [Test, Order(4)]
        public async Task Step4_VerifyUpdatedGuidelines()
        {
            TestContext.Out.WriteLine("=== Step 4: Verify Updated AI Guidelines ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Zeige mir die aktuellen KI-Richtlinien");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return updated guidelines");
            Assert.That(
                response.Contains("hoeflich", StringComparison.OrdinalIgnoreCase)
                || response.Contains("höflich", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Sie", StringComparison.Ordinal),
                Is.True,
                $"Response should contain updated guideline text. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Updated guidelines verified successfully");
        }

        [Test, Order(5)]
        public async Task Step5_UpdateGuidelinesAgain()
        {
            TestContext.Out.WriteLine("=== Step 5: Update Guidelines with Different Text ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze die KI-Richtlinien auf: {SecondGuideline}");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm second guidelines update");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Guidelines updated again successfully");
        }

        [Test, Order(6)]
        public async Task Step6_VerifySecondUpdate()
        {
            TestContext.Out.WriteLine("=== Step 6: Verify Second Guidelines Update ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Was sind die aktuellen KI-Richtlinien?");
            var response = await WaitForBotResponse(_messageCountBefore);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should return current guidelines");
            Assert.That(
                response.Contains("kurze", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Deutsch", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Emojis", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"Response should contain second update text. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Second guidelines update verified");
        }

        [Test, Order(7)]
        public async Task Step7_ResetGuidelines()
        {
            TestContext.Out.WriteLine("=== Step 7: Reset AI Guidelines ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Setze die KI-Richtlinien zurueck auf: {ResetGuideline}");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should confirm guidelines reset");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("AI guidelines reset successfully");
        }
    }
}
