// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(62)]
    [Category("Klacksy")]
    public class ChatbotPageKnowledgeTest : ChatbotTestBase
    {
        private const string SkillExplainDashboard = "explain_page_dashboard";
        private const string SkillStartGuidedTour = "start_guided_tour";

        private const string RouteDashboard = "workplace/dashboard";
        private const string RouteSettings = "workplace/settings";
        private const string RouteEmployees = "workplace/client";
        private const string RouteShifts = "workplace/shift";
        private const string RouteAvailability = "workplace/client-availability";
        private const string RoutePeriodClosing = "workplace/period-closing";

        private const string PromptPageQuestion = "Was sehe ich auf dieser Seite? Welche Karten und Diagramme gibt es hier?";
        private const string PromptStartTour = "Starte die geführte Einrichtungstour";
        private const string PromptExplainCurrentPage = "Was sehe ich auf dieser Seite? Erkläre mir bitte alle Bereiche und Elemente ausführlich.";

        private const string ChipDone = "Erledigt, weiter";
        private const string ChipSkip = "Überspringen";

        private const string CssAssistantMessageText = ".message-wrapper.assistant .message-text";
        private const string TourStationAnchor = "Anzeigename";
        private const string TourCompletedAnchor = "startklar";
        private const int MaxLlmAttempts = 3;
        private const int StationAdvanceTimeoutMs = 20000;

        private static readonly string[] DashboardAnchors =
        {
            "Kunden nach Gruppen",
            "Dienste nach Gruppen",
            "Abdeckung",
            "Ressourcen",
        };

        private static readonly string[] EmployeesPageAnchors =
        {
            "Alle Adressen",
            "Adresse erfassen",
            "Mitarbeiter",
            "Externer",
            "Kunde",
        };

        private static readonly string[] ShiftsPageAnchors =
        {
            "Alle Dienste",
            "Dienst erfassen",
            "Bestellungen",
            "Planbare Dienste",
            "Gültigkeit",
        };

        private static readonly string[] AvailabilityPageAnchors =
        {
            "VM/NM",
            "Raster",
            "Checkbox",
            "Häkchen",
            "2 Wochen",
        };

        private static readonly string[] PeriodClosingPageAnchors =
        {
            "Tage der Periode",
            "Probleme dieser Periode",
            "Protokoll",
            "Export",
            "versiegel",
        };

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
        public async Task Step2_AskPageQuestion_AnswersFromRealDashboardKnowledge()
        {
            TestContext.Out.WriteLine("=== Step 2: Page question pulls explain_page_dashboard ===");
            await AssertSkillEnabled(SkillExplainDashboard);

            await EnsureOnDashboard();
            await EnsureChatOpen();

            for (var attempt = 1; attempt <= MaxLlmAttempts; attempt++)
            {
                await ClearChatAndWait();
                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(PromptPageQuestion);
                var response = await WaitForBotResponse(_messageCountBefore);
                TestContext.Out.WriteLine($"Bot response (attempt {attempt}): {response}");

                var matchedAnchor = DashboardAnchors.FirstOrDefault(a =>
                    response.Contains(a, StringComparison.OrdinalIgnoreCase));
                if (matchedAnchor != null)
                {
                    TestContext.Out.WriteLine($"Response grounded in real dashboard knowledge (anchor: '{matchedAnchor}')");
                    Assert.That(TestListener.HasApiErrors(), Is.False,
                        $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
                    return;
                }

                TestContext.Out.WriteLine($"No dashboard anchor found in response (attempt {attempt}/{MaxLlmAttempts})");
            }

            Assert.Fail($"After {MaxLlmAttempts} attempts the answer never contained a real dashboard anchor " +
                $"({string.Join(", ", DashboardAnchors)}) — page knowledge not used, likely hallucinated.");
        }

        [Test, Order(3)]
        public async Task Step3_RequestTour_StartsGuidedTourAtFirstStation()
        {
            TestContext.Out.WriteLine("=== Step 3: Tour request triggers start_guided_tour ===");
            await AssertSkillEnabled(SkillStartGuidedTour);
            await EnsureChatOpen();

            for (var attempt = 1; attempt <= MaxLlmAttempts; attempt++)
            {
                await ClearChatAndWait();
                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(PromptStartTour);
                await WaitForBotResponse(_messageCountBefore);
                await Actions.Wait3000();

                var allAssistantText = await GetAllAssistantMessagesText();
                TestContext.Out.WriteLine($"Assistant messages (attempt {attempt}): {allAssistantText}");

                var currentUrl = Actions.ReadCurrentUrl();
                TestContext.Out.WriteLine($"Current URL: {currentUrl}");

                if (allAssistantText.Contains(TourStationAnchor, StringComparison.OrdinalIgnoreCase)
                    && currentUrl.Contains(RouteSettings, StringComparison.OrdinalIgnoreCase))
                {
                    TestContext.Out.WriteLine("Guided tour started: first station presented and settings page opened");
                    Assert.That(TestListener.HasApiErrors(), Is.False,
                        $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
                    return;
                }

                TestContext.Out.WriteLine($"Tour not started yet (attempt {attempt}/{MaxLlmAttempts})");
            }

            Assert.Fail($"After {MaxLlmAttempts} attempts the guided tour did not start " +
                $"(expected station text containing '{TourStationAnchor}' and URL containing '{RouteSettings}').");
        }

        [Test, Order(4)]
        public async Task Step4_SkipAskStations_TitleAndAddress()
        {
            TestContext.Out.WriteLine("=== Step 4: Skip both ask stations (title, address) ===");

            await AdvanceStationAndAssert(ChipSkip, "Absenderadresse");
            await AdvanceStationAndAssert(ChipSkip, "Feiertagskalender");

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
        }

        [Test, Order(5)]
        public async Task Step5_WalkSettingsStations()
        {
            TestContext.Out.WriteLine("=== Step 5: Walk calendar -> users -> group-scope -> identity-provider -> scheduling ===");

            await AdvanceStationAndAssert(ChipDone, "Benutzerkonten");
            await AdvanceStationAndAssert(ChipDone, "Teams, Standorte");
            await AdvanceStationAndAssert(ChipDone, "Identity-Provider");
            await AdvanceStationAndAssert(ChipDone, "Planungs-Grundeinstellungen");

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
        }

        [Test, Order(6)]
        public async Task Step6_EmployeesStation_WithComprehensivePageExplanation()
        {
            TestContext.Out.WriteLine("=== Step 6: Employees station + in-tour page explanation ===");

            await AdvanceStationAndAssert(ChipDone, "Adress- und Personenverwaltung", RouteEmployees);
            await AskPageQuestionAndAssertAnchors("Employees", EmployeesPageAnchors);
        }

        [Test, Order(7)]
        public async Task Step7_ShiftsStation_WithComprehensivePageExplanation()
        {
            TestContext.Out.WriteLine("=== Step 7: Shifts station + in-tour page explanation ===");

            await AdvanceStationAndAssert(ChipDone, "Schichtvorlagen", RouteShifts);
            await AskPageQuestionAndAssertAnchors("Shifts", ShiftsPageAnchors);
        }

        [Test, Order(8)]
        public async Task Step8_AvailabilityStation_WithComprehensivePageExplanation()
        {
            TestContext.Out.WriteLine("=== Step 8: Availability station + in-tour page explanation ===");

            await AdvanceStationAndAssert(ChipDone, "Verfügbarkeitsansicht", RouteAvailability);
            await AskPageQuestionAndAssertAnchors("Availability", AvailabilityPageAnchors);
        }

        [Test, Order(9)]
        public async Task Step9_AbsenceAndHolidayStations()
        {
            TestContext.Out.WriteLine("=== Step 9: Walk absence -> holidays ===");

            await AdvanceStationAndAssert(ChipDone, "Absenz-Arten");
            await AdvanceStationAndAssert(ChipDone, "Feiertagsregeln");

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
        }

        [Test, Order(10)]
        public async Task Step10_PeriodClosingStation_WithComprehensivePageExplanation()
        {
            TestContext.Out.WriteLine("=== Step 10: Period-closing station + in-tour page explanation ===");

            await AdvanceStationAndAssert(ChipDone, "Monatsabschluss-Arbeitsplatz", RoutePeriodClosing);
            await AskPageQuestionAndAssertAnchors("PeriodClosing", PeriodClosingPageAnchors);
        }

        [Test, Order(11)]
        public async Task Step11_FinalStationsAndTourCompletion()
        {
            TestContext.Out.WriteLine("=== Step 11: Walk email -> llm-klacksy -> plugins -> completed ===");

            await AdvanceStationAndAssert(ChipDone, "SMTP");
            await AdvanceStationAndAssert(ChipDone, "KI-Provider");
            await AdvanceStationAndAssert(ChipDone, "Funktions-Plugins");
            await AdvanceStationAndAssert(ChipDone, TourCompletedAnchor);

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Guided tour completed across all 16 stations");
        }

        private async Task AdvanceStationAndAssert(string chipLabel, string expectedAnchor, string? expectedRoute = null)
        {
            await Actions.Wait500();
            var before = await GetMessageCount();
            await Actions.ClickButtonByText(chipLabel);
            await WaitForBotResponse(before, StationAdvanceTimeoutMs);
            await Actions.Wait1000();
            await Actions.WaitForSpinnerToDisappear();

            var allText = await GetAllAssistantMessagesText();
            Assert.That(allText, Does.Contain(expectedAnchor).IgnoreCase,
                $"After clicking '{chipLabel}' the station text should contain '{expectedAnchor}'.");

            if (expectedRoute != null)
            {
                var currentUrl = Actions.ReadCurrentUrl();
                Assert.That(currentUrl, Does.Contain(expectedRoute).IgnoreCase,
                    $"Station should navigate to '{expectedRoute}', but URL is '{currentUrl}'.");
            }

            TestContext.Out.WriteLine($"Station presented (anchor: '{expectedAnchor}'{(expectedRoute != null ? $", route: '{expectedRoute}'" : string.Empty)})");
        }

        private async Task AskPageQuestionAndAssertAnchors(string pageName, string[] anchors)
        {
            for (var attempt = 1; attempt <= MaxLlmAttempts; attempt++)
            {
                var before = await GetMessageCount();
                await SendChatMessage(PromptExplainCurrentPage);
                var response = await WaitForBotResponse(before);
                TestContext.Out.WriteLine($"{pageName} page explanation (attempt {attempt}): {response}");
                await Actions.Wait2000();

                var matchedAnchor = anchors.FirstOrDefault(a =>
                    response.Contains(a, StringComparison.OrdinalIgnoreCase));
                if (matchedAnchor != null)
                {
                    TestContext.Out.WriteLine($"{pageName} explanation grounded in real page knowledge (anchor: '{matchedAnchor}')");
                    Assert.That(TestListener.HasApiErrors(), Is.False,
                        $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
                    return;
                }

                TestContext.Out.WriteLine($"No {pageName} anchor found in response (attempt {attempt}/{MaxLlmAttempts})");
            }

            Assert.Fail($"After {MaxLlmAttempts} attempts the {pageName} explanation never contained a real page anchor " +
                $"({string.Join(", ", anchors)}) — page knowledge not used, likely hallucinated.");
        }

        private async Task EnsureOnDashboard()
        {
            var currentUrl = Actions.ReadCurrentUrl();
            if (!currentUrl.Contains(RouteDashboard, StringComparison.OrdinalIgnoreCase))
            {
                await Actions.NavigateTo(BaseUrl + RouteDashboard);
                await Actions.Wait2000();
                await Actions.WaitForSpinnerToDisappear();
            }
        }

        private async Task<string> GetAllAssistantMessagesText()
        {
            var messagesSelector = GetChatSelector(ControlKeyMessages);
            var messageTexts = await Actions.QuerySelectorAll($"#{messagesSelector} {CssAssistantMessageText}");
            var parts = new List<string>();
            foreach (var element in messageTexts)
            {
                var text = await Actions.GetElementText(element);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    parts.Add(text.Trim());
                }
            }

            return string.Join(" | ", parts);
        }
    }
}
