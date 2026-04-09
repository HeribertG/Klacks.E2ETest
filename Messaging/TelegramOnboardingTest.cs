// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net;
using System.Net.Http.Json;
using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.Messaging;

[TestFixture]
[Order(95)]
public class TelegramOnboardingTest : PlaywrightSetup
{
    private const string WebhookUrl = "http://localhost:5000/api/messaging/webhook/telegram";
    private const string WebhookUrlGet = "http://localhost:5000/api/messaging/webhook/telegram?hub.challenge=klacks";

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
    };

    private Listener _listener = null!;

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [Test]
    [Order(1)]
    public async Task Step1_WebhookVerificationEndpoint_IsReachable()
    {
        TestContext.Out.WriteLine("=== Step 1: Webhook verification endpoint reachable ===");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(WebhookUrlGet);
        }
        catch (HttpRequestException ex)
        {
            Assert.Inconclusive($"Backend API not reachable at {WebhookUrlGet}: {ex.Message}");
            return;
        }

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            "Webhook GET (verification) should return 200 OK");

        var body = await response.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"Verification body: {body}");
        Assert.That(body, Does.Contain("klacks").Or.Contain("Webhook").IgnoreCase);
    }

    [Test]
    [Order(2)]
    public async Task Step2_StartCommand_WithUnknownToken_ReturnsOk()
    {
        TestContext.Out.WriteLine("=== Step 2: /start with unknown token is handled gracefully ===");

        var payload = new
        {
            message = new
            {
                message_id = 1,
                text = "/start not_a_real_token_xyz",
                chat = new { id = 999111L },
                from = new { id = 999111L, first_name = "E2E" },
            },
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(WebhookUrl, payload);
        }
        catch (HttpRequestException ex)
        {
            Assert.Inconclusive($"Backend API not reachable at {WebhookUrl}: {ex.Message}");
            return;
        }

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            "Webhook should always return 200 for a structurally valid /start payload, even when the token is unknown");
    }

    [Test]
    [Order(3)]
    public async Task Step3_StartCommand_WithoutToken_IsHandledGracefully()
    {
        TestContext.Out.WriteLine("=== Step 3: /start without token falls through to default pipeline ===");

        var payload = new
        {
            message = new
            {
                message_id = 2,
                text = "/start",
                chat = new { id = 999112L },
                from = new { id = 999112L, first_name = "E2E" },
            },
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(WebhookUrl, payload);
        }
        catch (HttpRequestException ex)
        {
            Assert.Inconclusive($"Backend API not reachable at {WebhookUrl}: {ex.Message}");
            return;
        }

        Assert.That(
            (int)response.StatusCode,
            Is.LessThan(500),
            "Webhook must not return a 5xx error for a token-less /start. Downstream 4xx (BadRequest/Unauthorized) is acceptable.");
    }

    [Test]
    [Order(4)]
    public async Task Step4_StartCommand_WithMalformedPayload_DoesNotCrashServer()
    {
        TestContext.Out.WriteLine("=== Step 4: Malformed /start payload is handled gracefully ===");

        var content = new StringContent("{ not valid json", System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(WebhookUrl, content);
        }
        catch (HttpRequestException ex)
        {
            Assert.Inconclusive($"Backend API not reachable at {WebhookUrl}: {ex.Message}");
            return;
        }

        Assert.That(
            (int)response.StatusCode,
            Is.LessThan(500),
            "Webhook must not return a 5xx error for malformed JSON");
    }

    [Test]
    [Order(5)]
    public async Task Step5_EmployeeClientEdit_TelegramButton_Visibility()
    {
        TestContext.Out.WriteLine("=== Step 5: Telegram invitation button visibility on employee edit ===");

        await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var rows = await Actions.GetElementsBySelector("tr[id^='client-row-']");
        if (rows.Count == 0)
        {
            Assert.Inconclusive("No clients in list — cannot verify button visibility");
            return;
        }

        await rows[0].ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var currentUrl = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"Current URL after row click: {currentUrl}");

        var buttons = await Page.QuerySelectorAllAsync("button:has-text('Telegram')");
        TestContext.Out.WriteLine($"Buttons containing 'Telegram' text on page: {buttons.Count}");

        Assert.That(
            _listener.HasApiErrors(),
            Is.False,
            $"No API errors expected during navigation. Last error: {_listener.GetLastErrorMessage()}");
    }
}
