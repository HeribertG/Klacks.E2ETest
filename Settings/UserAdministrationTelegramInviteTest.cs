// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest;

/// <summary>
/// Verifies the admin-initiated Telegram invite button in the User-Administration edit modal:
/// that it renders, and that clicking it produces visible toast feedback rather than a silent
/// failure. Only asserted when a Telegram provider happens to be enabled in this environment -
/// the button is gated on that, and most environments won't have one configured.
/// </summary>
[TestFixture]
[Order(93)]
[Category("Input")]
public class UserAdministrationTelegramInviteTest : PlaywrightSetup
{
    private const string RowNameSelector = "input[id^='user-admin-row-name-']";
    private const string TelegramInviteButtonSelector = "#user-admin-modal-telegram-invite-row button";
    private const string ToastTextSelector = "ngb-toast .toast-text";

    private Listener _listener = null!;

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(SettingsUserAdministrationIds.UserAdminSection);
        await Actions.Wait500();
    }

    [Test]
    public async Task UserAdminModal_TelegramInviteButton_VisibleAndProducesToastFeedback()
    {
        TestContext.Out.WriteLine("=== User-Administration modal: Telegram invite button verification ===");

        var rows = await Actions.QuerySelectorAll(RowNameSelector);
        if (rows.Count < 2)
        {
            Assert.Inconclusive("Fewer than 2 users in the list — cannot pick a non-admin row to edit");
            return;
        }

        IElementHandle? targetInput = null;
        foreach (var row in rows)
        {
            var value = await row.InputValueAsync();
            if (!value.Contains("admin", StringComparison.OrdinalIgnoreCase))
            {
                targetInput = row;
                break;
            }
        }
        targetInput ??= rows[1];

        await targetInput.DblClickAsync();
        await Actions.Wait1000();

        var modal = await Actions.FindElementById(SettingsUserAdministrationIds.ModalForm);
        Assert.That(modal, Is.Not.Null, "User-Administration edit modal should open on double-click");

        var telegramButton = await Actions.FindElementByCssSelector(TelegramInviteButtonSelector);
        TestContext.Out.WriteLine($"Telegram invite button present: {telegramButton != null}");

        if (telegramButton == null)
        {
            Assert.Inconclusive(
                "No Telegram provider is active in this environment, so the invite button is " +
                "correctly hidden - nothing to verify here.");
            return;
        }

        Assert.That(await telegramButton.IsVisibleAsync(), Is.True, "Telegram invite button should be visible");

        await telegramButton.ClickAsync();

        var toastTextElement = await Actions.FindElementByCssSelector(ToastTextSelector);
        Assert.That(toastTextElement, Is.Not.Null,
            "Clicking the Telegram invite button should produce a visible toast (success or error), not a silent failure");

        var toastText = await Actions.GetElementText(toastTextElement!);
        TestContext.Out.WriteLine($"Toast text after click: '{toastText}'");
        Assert.That(toastText, Is.Not.Empty, "Toast should contain visible text feedback");

        Assert.That(
            _listener.HasApiErrors(),
            Is.False,
            $"No unexpected API errors expected. Last error: {_listener.GetLastErrorMessage()}");
    }
}
