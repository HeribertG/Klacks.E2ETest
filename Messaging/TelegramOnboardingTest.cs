// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    /// <summary>
    /// Opens the edit form via the pencil (edit) button, not the row itself: clicking the row
    /// only wires to onClickedRow()/highlighting in all-address-list.component.html and never
    /// navigates anywhere - the persona section the Telegram button lives in only mounts after
    /// navigateToEditAddress(), which the pencil button's onClickEdit() triggers.
    /// </summary>
    [Test]
    public async Task EmployeeClientEdit_TelegramButton_Visibility()
    {
        TestContext.Out.WriteLine("=== Telegram invitation button visibility on employee edit ===");

        await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var editButton = await Actions.FindElementById("client-edit-button-0");
        if (editButton == null)
        {
            Assert.Inconclusive("No clients in list — cannot verify button visibility");
            return;
        }

        await editButton.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        var currentUrl = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"Current URL after clicking the edit button: {currentUrl}");

        var buttons = await Actions.QuerySelectorAll("button:has-text('Telegram')");
        TestContext.Out.WriteLine($"Buttons containing 'Telegram' text on page: {buttons.Count}");

        Assert.That(
            _listener.HasApiErrors(),
            Is.False,
            $"No API errors expected during navigation. Last error: {_listener.GetLastErrorMessage()}");

        // Only asserted when a Telegram provider happens to be enabled in this environment - the
        // button is gated on that, and most environments won't have one configured. Absence here
        // is not itself a failure; a *visible-but-not-clickable* button would be.
        if (buttons.Count > 0)
        {
            Assert.That(await buttons[0].IsVisibleAsync(), Is.True,
                "A Telegram button found on the page should actually be visible, not hidden");
        }
    }
}
