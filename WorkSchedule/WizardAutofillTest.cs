// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// E2E smoke test for the schedule autofill wizard.
/// Verifies that the wizard dialog leaves the "Initialisierung..." state within a reasonable
/// timeout, proving the REST/SignalR handshake between frontend and backend is working.
/// </summary>

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.PageObjects;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.WorkSchedule;

[TestFixture]
[Order(110)]
public class WizardAutofillTest : PlaywrightSetup
{
    private const string WizardButtonId = "schedule-wizard-btn";
    private const string ModalSelector = "ngb-modal-window";
    private const string GenerationLabelSelector = "ngb-modal-window .progress-info span";
    private const string CloseButtonSelector = "ngb-modal-window .cancel-btn, ngb-modal-window .normal-btn";

    private const int InitialProgressTimeoutMs = 45000;

    private Listener _listener = null!;
    private SchedulePage _schedule = null!;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        _schedule = new SchedulePage(Page, Actions, BaseUrl);
        await _schedule.NavigateToScheduleAsync(enableTestMode: true);
        await _schedule.WaitForGridLoadAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await CloseDialogIfOpen();

        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API errors: {_listener.GetLastErrorMessage()}");
        }
        await _listener.WaitForResponseHandlingAsync();
    }

    [Test]
    [Order(1)]
    public async Task Wizard_OpensDialog_EmitsInitialProgressWithinTimeout()
    {
        TestContext.Out.WriteLine("Clicking wizard button");
        await Actions.ClickButtonById(WizardButtonId);

        await Actions.ElementIsVisibleByCssSelector(ModalSelector);
        TestContext.Out.WriteLine("Wizard modal is visible");

        var leftInitialization = await WaitForProgressBeyondInitialization(InitialProgressTimeoutMs);

        Assert.That(leftInitialization, Is.True,
            $"Wizard dialog stayed on 'Initialisierung...' for more than {InitialProgressTimeoutMs}ms — SignalR progress events never arrived.");
    }

    private async Task<bool> WaitForProgressBeyondInitialization(int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var labels = await Page.Locator(GenerationLabelSelector).AllInnerTextsAsync();
            var stillInitializing =
                labels.Count > 0 &&
                labels.Any(l => l.Contains("Initialisierung", StringComparison.OrdinalIgnoreCase)
                             || l.Contains("Initializing", StringComparison.OrdinalIgnoreCase));

            var hasGenerationLabel = labels.Any(l =>
                l.Contains("Generation", StringComparison.OrdinalIgnoreCase));

            var summaryBadges = await Page.Locator("ngb-modal-window .summary-badges").CountAsync();
            var errorMessage = await Page.Locator("ngb-modal-window .error-message").CountAsync();

            if (hasGenerationLabel || summaryBadges > 0 || errorMessage > 0)
            {
                TestContext.Out.WriteLine($"Wizard progressed beyond initialization. Labels: {string.Join(" | ", labels)}");
                return true;
            }

            if (!stillInitializing && labels.Count > 0)
            {
                TestContext.Out.WriteLine($"Unexpected labels: {string.Join(" | ", labels)}");
                return true;
            }

            await Task.Delay(250);
        }

        var finalLabels = await Page.Locator(GenerationLabelSelector).AllInnerTextsAsync();
        TestContext.Out.WriteLine($"Timed out. Final labels: {string.Join(" | ", finalLabels)}");
        return false;
    }

    private async Task CloseDialogIfOpen()
    {
        var closeButtons = Page.Locator(CloseButtonSelector);
        if (await closeButtons.CountAsync() > 0)
        {
            try
            {
                await closeButtons.First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
            }
            catch
            {
                // Dialog already dismissed.
            }
        }
    }
}
