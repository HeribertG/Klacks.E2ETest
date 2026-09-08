// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Live proof of the read-only recipe "setup-consultation" (recipe-authoring.md §0a: a search-only
 * recipe has no DB effect, so instead of asserting a mutation this checks the DECISION the skill
 * makes). "get_setup_guidance" backs the whole recipe; called directly with phase=route and the two
 * classified answers (attribution, orderSource) it resolves Data.Route.Kind and Data.Route.ShowTarget
 * purely from those answers plus the live Installation snapshot (SetupRouteResolver.cs) — no LLM
 * involved in that decision, so calling the skill through the plain skill-execute endpoint
 * (POST api/backend/skills/execute) with fixed parameters is what actually exercises the resolver,
 * while a chat conversation could only ever re-check the bot's wording, which is explicitly out of
 * scope here. SkillUsageRecord only persists the request parameters, never the response, so DB
 * assertions cannot see Data.Route.Kind either — the skill's own live JSON response is the only
 * source of truth available to an E2E test.
 *
 * Two rules run ahead of the attribution/orderSource matrix (SetupRouteResolver.Resolve): an
 * installation that already has shifts always resolves to ShiftsAwaitingAssignment regardless of the
 * answers, and one that already has work entries reports SetupComplete=true with no Route at all. Both
 * are handled per case below rather than assumed away, because this runs against the shared dev
 * database (klacks_fresh) and its Installation state (HasOrders/HasShifts/HasWork/HasCustomers/
 * HasGroups) is read back from the same response and never hard-coded — a fixed expectation would only
 * be right by accident of whatever the database currently holds.
 *
 * Read-only: no mutation happens anywhere in this file, so there is deliberately no TearDown.
 * Explicit: hits the live API directly; run on demand.
 */

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Klacks.E2ETest.Chatbot;

[TestFixture]
[Explicit("Live API proof of the setup-consultation route decision; run on demand.")]
[Category("Klacksy")]
public class ChatbotRecipeSetupConsultationTest
{
    private const string ApiBaseUrl = "https://localhost:5001/api/backend/";
    private const string LoginEmail = "admin@test.com";
    private const string LoginPassword = "P@ssw0rt1";

    private const string SkillName = "get_setup_guidance";

    private const string ParamPhase = "phase";
    private const string ParamAttribution = "attribution";
    private const string ParamOrderSource = "orderSource";
    private const string PhaseRoute = "route";

    private const string AnswerYes = "yes";
    private const string AnswerNo = "no";
    private const string AnswerUnknown = "unknown";

    private const string RouteShiftsAwaitingAssignment = "ShiftsAwaitingAssignment";
    private const string RouteCustomerOrderNeedsCustomer = "CustomerOrderNeedsCustomer";
    private const string RouteCustomerOrderNeedsGroup = "CustomerOrderNeedsGroup";
    private const string RouteCustomerOrder = "CustomerOrder";
    private const string RouteClientlessDutyNeedsGroup = "ClientlessDutyNeedsGroup";
    private const string RouteClientlessDuty = "ClientlessDuty";
    private const string RouteErpImportContradictsClientless = "ErpImportContradictsClientless";
    private const string RouteBothRoutesUnclear = "BothRoutesUnclear";

    private const string TargetGroupList = "group-list";
    private const string TargetNewEmployee = "new-employee";
    private const string TargetNewPlannableShift = "new-plannable-shift";
    private const string TargetNewShift = "new-shift";
    private const string TargetShiftList = "shift-list";
    private const string TargetSchedule = "schedule";
    private const string TargetErpDropPoints = "erp-drop-points";

    private HttpClient _http = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        _http = new HttpClient(handler) { BaseAddress = new Uri(ApiBaseUrl) };

        var login = await _http.PostAsJsonAsync("Accounts/LoginUser",
            new { email = LoginEmail, password = LoginPassword });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login returned no body");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _http.Dispose();

    [Test]
    public async Task Customer_Attribution_Resolves_By_Customer_And_Group_Presence()
    {
        var d = await ResolveRouteAsync(AnswerYes, AnswerNo);
        if (HandledShortCircuit(d, "customer-attribution"))
        {
            return;
        }

        var expectedKind = !d.HasCustomers
            ? RouteCustomerOrderNeedsCustomer
            : !d.HasGroups
                ? RouteCustomerOrderNeedsGroup
                : RouteCustomerOrder;
        var expectedTarget = !d.HasCustomers
            ? TargetNewEmployee
            : !d.HasGroups
                ? TargetGroupList
                : TargetNewShift;

        TestContext.Out.WriteLine(
            $"[setup-consultation/customer-attribution] hasCustomers={d.HasCustomers} hasGroups={d.HasGroups} "
            + $"-> expected kind={expectedKind}, target={expectedTarget}; actual kind={d.Kind}, target={d.ShowTarget}");

        Assert.Multiple(() =>
        {
            Assert.That(d.Kind, Is.EqualTo(expectedKind),
                "attribution=yes/orderSource=no must resolve by whether the installation already has customers/groups");
            Assert.That(d.ShowTarget, Is.EqualTo(expectedTarget),
                "the navigation target must match the resolved route");
        });
    }

    [Test]
    public async Task Clientless_Attribution_Resolves_By_Group_Presence()
    {
        var d = await ResolveRouteAsync(AnswerNo, AnswerNo);
        if (HandledShortCircuit(d, "clientless-attribution"))
        {
            return;
        }

        var expectedKind = !d.HasGroups ? RouteClientlessDutyNeedsGroup : RouteClientlessDuty;
        var expectedTarget = !d.HasGroups ? TargetGroupList : TargetNewPlannableShift;

        TestContext.Out.WriteLine(
            $"[setup-consultation/clientless-attribution] hasGroups={d.HasGroups} "
            + $"-> expected kind={expectedKind}, target={expectedTarget}; actual kind={d.Kind}, target={d.ShowTarget}");

        Assert.Multiple(() =>
        {
            Assert.That(d.Kind, Is.EqualTo(expectedKind),
                "attribution=no/orderSource=no must resolve by whether the installation already has groups");
            Assert.That(d.ShowTarget, Is.EqualTo(expectedTarget),
                "the navigation target must match the resolved route");
        });
    }

    [Test]
    public async Task Clientless_Attribution_With_External_Orders_Is_A_Contradiction()
    {
        var d = await ResolveRouteAsync(AnswerNo, AnswerYes);
        if (HandledShortCircuit(d, "erp-contradicts-clientless"))
        {
            return;
        }

        TestContext.Out.WriteLine(
            $"[setup-consultation/erp-contradicts-clientless] -> actual kind={d.Kind}, target={d.ShowTarget}");

        Assert.Multiple(() =>
        {
            Assert.That(d.Kind, Is.EqualTo(RouteErpImportContradictsClientless),
                "attribution=no/orderSource=yes is a self-contradiction (no customer, yet orders come from an ERP) " +
                "and must always flag it, independent of the installation state");
            Assert.That(d.ShowTarget, Is.EqualTo(TargetErpDropPoints),
                "the contradiction must point at the ERP drop points, not offer either create route");
        });
    }

    [Test]
    public async Task Unclear_Answers_Never_Assemble_Into_A_Handoff()
    {
        var d = await ResolveRouteAsync(AnswerUnknown, AnswerUnknown);
        if (HandledShortCircuit(d, "both-routes-unclear"))
        {
            return;
        }

        var expectedTarget = d.HasCustomers ? TargetNewShift : TargetShiftList;

        TestContext.Out.WriteLine(
            $"[setup-consultation/both-routes-unclear] hasCustomers={d.HasCustomers} "
            + $"-> expected target={expectedTarget}; actual kind={d.Kind}, target={d.ShowTarget}, handoff={d.HandoffPresent}");

        Assert.Multiple(() =>
        {
            Assert.That(d.Kind, Is.EqualTo(RouteBothRoutesUnclear),
                "an unclear attribution must never resolve into a concrete create-offer route");
            Assert.That(d.ShowTarget, Is.EqualTo(expectedTarget),
                "the navigation target must still match the installation's existing customer state");
            Assert.That(d.HandoffPresent, Is.False,
                "an unclear attribution/orderSource pair must never assemble into a follow-up recipe handoff");
        });
    }

    private static bool HandledShortCircuit(RouteDecision d, string caseLabel)
    {
        if (d.SetupComplete)
        {
            Assert.Ignore(
                $"[setup-consultation/{caseLabel}] installation already has work entries, so " +
                "get_setup_guidance reports SetupComplete=true and resolves no route at all — the " +
                "matrix under test does not apply to this installation state.");
            return true;
        }

        if (d.HasShifts)
        {
            TestContext.Out.WriteLine(
                $"[setup-consultation/{caseLabel}] installation already has shifts -> the " +
                "ShiftsAwaitingAssignment short-circuit must win over the attribution/orderSource matrix");
            Assert.Multiple(() =>
            {
                Assert.That(d.Kind, Is.EqualTo(RouteShiftsAwaitingAssignment),
                    "an installation with existing shifts must always resolve to ShiftsAwaitingAssignment, " +
                    "regardless of the attribution/orderSource answers");
                Assert.That(d.ShowTarget, Is.EqualTo(TargetSchedule),
                    "the ShiftsAwaitingAssignment route must point at the schedule");
            });
            return true;
        }

        return false;
    }

    private async Task<RouteDecision> ResolveRouteAsync(string attribution, string orderSource)
    {
        var request = new SkillExecuteRequest(
            SkillName,
            new Dictionary<string, object>
            {
                [ParamPhase] = PhaseRoute,
                [ParamAttribution] = attribution,
                [ParamOrderSource] = orderSource
            });

        var response = await _http.PostAsJsonAsync("skills/execute", request);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var data = doc.RootElement.GetProperty("data");

        var setupComplete = data.GetProperty("setupComplete").GetBoolean();
        var installation = data.GetProperty("installation");
        var hasShifts = installation.GetProperty("hasShifts").GetBoolean();
        var hasCustomers = installation.GetProperty("hasCustomers").GetBoolean();
        var hasGroups = installation.GetProperty("hasGroups").GetBoolean();

        if (setupComplete)
        {
            return new RouteDecision(true, hasShifts, hasCustomers, hasGroups, string.Empty, string.Empty, false);
        }

        var route = data.GetProperty("route");
        var kind = route.GetProperty("kind").GetString() ?? string.Empty;
        var showTarget = route.GetProperty("showTarget").GetString() ?? string.Empty;
        var handoffPresent = data.TryGetProperty("handoff", out var handoff)
            && handoff.ValueKind != JsonValueKind.Null;

        return new RouteDecision(false, hasShifts, hasCustomers, hasGroups, kind, showTarget, handoffPresent);
    }

    private sealed record LoginResponse(string Token);

    private sealed record SkillExecuteRequest(string SkillName, Dictionary<string, object> Parameters);

    private sealed record RouteDecision(
        bool SetupComplete,
        bool HasShifts,
        bool HasCustomers,
        bool HasGroups,
        string Kind,
        string ShowTarget,
        bool HandoffPresent);
}
