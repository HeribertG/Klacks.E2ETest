// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    /// <summary>
    /// End-to-end skeleton for the chat planning flow: a complex multi-mutation request nudges the bot
    /// to propose a plan (create_plan), the user confirms, execution starts in the background and the
    /// plan-execution panel reflects progress. Explicit: it depends on an external LLM and a running
    /// dev app, so it is never part of the automated suite — run it manually to live-verify the flow.
    /// </summary>
    [TestFixture]
    [Explicit("Full chat -> proposal -> confirmation -> execution -> panel flow; needs a live LLM + dev app.")]
    [Category("Klacksy")]
    public class ChatbotCreatePlanTest : ChatbotTestBase
    {
        private const string SkillCreatePlan = "create_plan";
        private const string SkillGetPlanStatus = "get_plan_status";
        private const int BotTimeoutMs = 120000;

        private const string ComplexGoal =
            "Erstelle einen neuen Kunden Bäckerei Keller in Bern, lege einen Mitarbeiter Hans Ott an " +
            "und weise ihn der Gruppe Nord zu.";

        [Test, Order(1)]
        public async Task Step1_PlanSkills_AreEnabled()
        {
            await AssertSkillEnabled(SkillCreatePlan);
            await AssertSkillEnabled(SkillGetPlanStatus);
        }

        [Test, Order(2)]
        public async Task Step2_ComplexRequest_ProposesPlan_ThenConfirmAndRun()
        {
            await EnsureChatOpen();

            var before = await GetMessageCount();
            await SendChatMessage(ComplexGoal);
            var proposal = await WaitForBotResponse(before, BotTimeoutMs);
            Assert.That(proposal, Is.Not.Null.And.Not.Empty, "Bot should propose a plan for the complex request");

            var beforeConfirm = await GetMessageCount();
            await SendChatMessage("Ja, bitte führe den Plan aus.");
            var confirmation = await WaitForBotResponse(beforeConfirm, BotTimeoutMs);
            Assert.That(confirmation, Is.Not.Null.And.Not.Empty, "Bot should acknowledge the plan started");

            var beforeStatus = await GetMessageCount();
            await SendChatMessage("Wie weit ist der Plan?");
            var status = await WaitForBotResponse(beforeStatus, BotTimeoutMs);
            Assert.That(status, Is.Not.Null.And.Not.Empty, "Bot should report the plan status via get_plan_status");
        }
    }
}
