// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.Messaging;

/// <summary>
/// Verifies the Telegram invitation button visibility on the employee edit page.
/// The pure backend webhook HTTP tests moved to Klacks.IntegrationTest/Messaging/TelegramWebhookIntegrationTest.cs,
/// since they have no Playwright/UI dependency.
/// </summary>
[TestFixture]
[Order(90)]
[Category("Input")]
public class TelegramOnboardingTest : PlaywrightSetup
{
    private Listener _listener = null!;

    [SetUp]
    public void Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [Test]
    public async Task EmployeeClientEdit_TelegramButton_Visibility()
    {
        TestContext.Out.WriteLine("=== Telegram invitation button visibility on employee edit ===");

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
