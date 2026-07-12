// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// E2E test for the Klacksy first-run setup tour on a fresh install WITHOUT a live LLM provider:
/// the offer chips must appear anyway (scripted tour), accepting starts station 1 (app name ask),
/// the answer is persisted as the APP_NAME setting, station 2 (branding) navigates to the settings
/// page, and ending the tour dismisses it. Tear-down restores ONBOARDING_STATE to 'pending' and
/// the APP_NAME setting to its pre-test state so the tour stays fresh for a real user.
/// </summary>

using Microsoft.Playwright;
using Klacks.E2ETest.Chatbot.Helpers;
using Klacks.E2ETest.Helpers;

namespace Klacks.E2ETest.Klacksy;

[TestFixture]
[Order(63)]
[Category("Klacksy")]
public class OnboardingSetupTourTest : PlaywrightSetup
{
    private const string PageKeyLlmChat = "llm-chat";
    private const string ControlKeyToggleBtn = "toggle-btn";
    private const string ControlKeyInput = "input";
    private const string ControlKeySendBtn = "send-btn";
    private const string ControlKeyMessages = "messages";

    private const string CssReplyChipBtn = ".reply-chip-btn";
    private const string CssAssistantMessageText = ".message-wrapper.assistant .message-text";

    private const string SessionOfferKey = "klacksy.onboarding.offeredSession";
    private const string OnboardingStateSettingType = "ONBOARDING_STATE";
    private const string AppNameSettingType = "APP_NAME";
    private const string OnboardingStatusPending = "pending";
    private const string AppNameTestValue = "E2E Tour AG";
    private const string SettingsRouteSegment = "workplace/settings";

    private const int OfferTimeoutMs = 30000;
    private const int OfferOpenChatFallbackMs = 8000;
    private const int MessageTimeoutMs = 15000;
    private const int PresenceProbeTimeoutMs = 2000;

    // Chip labels vary with the display language; match DE and EN variants.
    private static readonly string[] AcceptChipLabels = { "Ja, los", "Yes, let's go" };
    private static readonly string[] SnoozeChipLabels = { "Später", "Later" };
    private static readonly string[] DismissChipLabels = { "Nein danke", "No thanks" };
    private static readonly string[] EndTourChipLabels = { "Tour beenden", "End tour" };

    private static readonly string[] Station1ExplainFragments = { "Zahnrad", "gear icon" };
    private static readonly string[] Station1AskFragments = { "Anzeigename", "display name" };
    private static readonly string[] SavedFragments = { "gespeichert", "saved" };
    private static readonly string[] Station2BrandingFragments = { "Logo", "logo" };

    private Dictionary<string, string> _chatSelectors = new();
    private string? _originalAppName;
    private bool _appNameExistedBefore;

    [OneTimeSetUp]
    public async Task TourOneTimeSetUp()
    {
        _chatSelectors = await DbHelper.GetUiControlSelectorsAsync(PageKeyLlmChat);
        Assert.That(_chatSelectors, Is.Not.Empty, "Chat selectors must be loaded from ui_controls");

        var appName = await DbHelper.ExecuteSqlAsync(
            $"SELECT value FROM settings WHERE type = '{AppNameSettingType}' LIMIT 1");
        _appNameExistedBefore = appName.Length > 0 && !appName.StartsWith("ERROR:");
        _originalAppName = _appNameExistedBefore ? appName : null;
        TestContext.Out.WriteLine($"APP_NAME before test: {(_appNameExistedBefore ? $"'{_originalAppName}'" : "<no row>")}");

        await DbHelper.ExecuteSqlAsync(
            $"UPDATE settings SET value = '{OnboardingStatusPending}' WHERE type = '{OnboardingStateSettingType}'");

        var liveProviders = await DbHelper.ExecuteSqlAsync(
            "SELECT count(*) FROM llm_providers WHERE is_deleted = false AND is_enabled = true AND api_key IS NOT NULL AND api_key <> ''");
        TestContext.Out.WriteLine($"Enabled LLM providers with API key: {liveProviders}");
        Assert.That(liveProviders.Trim(), Is.EqualTo("0"),
            "This test verifies the no-LLM-key scenario; the DB must not contain an enabled provider with an API key");
    }

    [OneTimeTearDown]
    public async Task TourOneTimeTearDown()
    {
        var stateResult = await DbHelper.ExecuteSqlAsync(
            $"UPDATE settings SET value = '{OnboardingStatusPending}' WHERE type = '{OnboardingStateSettingType}' RETURNING value");
        if (string.IsNullOrEmpty(stateResult) || stateResult.StartsWith("ERROR:"))
        {
            await DbHelper.ExecuteSqlAsync(
                $"INSERT INTO settings (id, type, value) VALUES (gen_random_uuid(), '{OnboardingStateSettingType}', '{OnboardingStatusPending}')");
        }

        if (_appNameExistedBefore)
        {
            await DbHelper.ExecuteSqlAsync(
                $"UPDATE settings SET value = '{(_originalAppName ?? string.Empty).Replace("'", "''")}' WHERE type = '{AppNameSettingType}'");
        }
        else
        {
            await DbHelper.ExecuteSqlAsync(
                $"DELETE FROM settings WHERE type = '{AppNameSettingType}'");
        }

        var stateAfter = await DbHelper.ExecuteSqlAsync(
            $"SELECT value FROM settings WHERE type = '{OnboardingStateSettingType}'");
        var appNameAfter = await DbHelper.ExecuteSqlAsync(
            $"SELECT value FROM settings WHERE type = '{AppNameSettingType}'");
        TestContext.Out.WriteLine($"Cleanup done. ONBOARDING_STATE='{stateAfter}', APP_NAME={(appNameAfter.Length == 0 ? "<no row>" : $"'{appNameAfter}'")}");
    }

    [Test, Order(1)]
    public async Task Step1_TourOffer_Appears_Without_LlmKey()
    {
        TestContext.Out.WriteLine("=== Step 1: Tour offer appears although no LLM API key exists ===");

        await Actions.RemoveSessionStorage(SessionOfferKey);
        var startedAt = DateTime.UtcNow;
        await Actions.Reload();

        var chatOpened = false;
        IElementHandle? acceptChip = null;
        List<string> chipLabels = new();
        while ((DateTime.UtcNow - startedAt).TotalMilliseconds < OfferTimeoutMs)
        {
            try
            {
                chipLabels = await GetChipLabels();
                acceptChip = await FindChipByLabels(AcceptChipLabels);
            }
            catch (PlaywrightException ex)
            {
                TestContext.Out.WriteLine($"DOM query during navigation, retrying: {ex.Message}");
            }

            if (acceptChip != null)
            {
                break;
            }

            if (!chatOpened && (DateTime.UtcNow - startedAt).TotalMilliseconds > OfferOpenChatFallbackMs)
            {
                TestContext.Out.WriteLine("Offer not shown yet - opening the chat panel as fallback trigger");
                await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
                chatOpened = true;
            }

            await Actions.Wait500();
        }

        var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;
        TestContext.Out.WriteLine($"Offer chips after {elapsed:F1}s (chat opened as fallback: {chatOpened}). Labels: [{string.Join(" | ", chipLabels)}]");

        Assert.That(acceptChip, Is.Not.Null,
            $"Onboarding offer accept chip must appear within {OfferTimeoutMs / 1000}s even without an LLM API key. Visible chips: [{string.Join(" | ", chipLabels)}]");
        Assert.That(chipLabels.Any(l => ContainsAny(l, SnoozeChipLabels)), Is.True,
            $"Snooze chip expected in offer. Visible chips: [{string.Join(" | ", chipLabels)}]");
        Assert.That(chipLabels.Any(l => ContainsAny(l, DismissChipLabels)), Is.True,
            $"Dismiss chip expected in offer. Visible chips: [{string.Join(" | ", chipLabels)}]");

        TestContext.Out.WriteLine("PASS: Offer with accept/snooze/dismiss chips shown without LLM key");
    }

    [Test, Order(2)]
    public async Task Step2_Accept_Starts_Station1_TitleAsk()
    {
        TestContext.Out.WriteLine("=== Step 2: Accepting the offer starts station 1 (app name) ===");

        var acceptChip = await FindChipByLabels(AcceptChipLabels);
        Assert.That(acceptChip, Is.Not.Null, "Accept chip must still be present from step 1");
        await acceptChip!.ClickAsync();
        await Actions.Wait1000();

        var url = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"URL after accept: {url}");
        Assert.That(url, Does.Contain(SettingsRouteSegment),
            $"Station 1 must navigate to the settings page. Got: {url}");

        await EnsureChatOpenForReading();

        Assert.That(await WaitForAssistantMessageContaining(Station1ExplainFragments), Is.True,
            "Station 1 explanation (display-name intro mentioning the gear icon) must appear in the chat");
        Assert.That(await WaitForAssistantMessageContaining(Station1AskFragments), Is.True,
            "Station 1 ask prompt (question for the display name) must appear in the chat");

        TestContext.Out.WriteLine("PASS: Station 1 explanation and app-name question shown");
    }

    [Test, Order(3)]
    public async Task Step3_Answer_AppName_IsSavedToDb()
    {
        TestContext.Out.WriteLine("=== Step 3: Answering with the app name persists the APP_NAME setting ===");

        await EnsureChatOpenForReading();
        var inputSelector = GetChatSelector(ControlKeyInput);
        var chatInput = await Actions.FindElementById(inputSelector);
        Assert.That(chatInput, Is.Not.Null, "Chat input must be present during the ask station");

        var isDisabled = await Actions.IsDisabled(chatInput);
        Assert.That(isDisabled, Is.False,
            "BUG CANDIDATE: chat input is disabled during the onboarding ask station because no LLM API key exists "
            + "(hasNoApiKey() gates the input in assistant-chat.component.html) - the scripted tour cannot collect the app name");

        // Real keystrokes (not a synthetic fill) so the Angular inputText signal updates.
        await Actions.TypeIntoInputById(inputSelector, AppNameTestValue);

        var inputValue = await Actions.ReadInput(inputSelector);
        var sendBtn = await Actions.FindElementById(GetChatSelector(ControlKeySendBtn));
        var sendDisabled = await Actions.IsDisabled(sendBtn);
        var inputContainer = await Actions.QuerySelector(".input-container");
        var containerClass = inputContainer != null ? await Actions.GetElementAttribute(inputContainer, "class") : "<not found>";
        TestContext.Out.WriteLine($"DIAG after typing: input value='{inputValue}', send-btn disabled={sendDisabled}, input-container class='{containerClass}'");

        if (sendDisabled)
        {
            // Send button did not enable - submit with Enter on the focused textarea instead
            // (onInputKeyPress handles Enter and calls sendMessage directly).
            TestContext.Out.WriteLine("DIAG: send button disabled despite typed text - falling back to Enter key");
            var chatInputForEnter = await Actions.FindElementById(inputSelector);
            await chatInputForEnter!.PressAsync("Enter");
        }
        else
        {
            await Actions.ClickButtonById(GetChatSelector(ControlKeySendBtn));
        }

        await Actions.Wait2000();

        var messagesSelector = GetChatSelector(ControlKeyMessages);
        var userMessages = await Actions.QuerySelectorAll($"#{messagesSelector} .message-wrapper.user");
        var appNameEarly = await DbHelper.ExecuteSqlAsync(
            $"SELECT value FROM settings WHERE type = '{AppNameSettingType}' LIMIT 1");
        TestContext.Out.WriteLine($"DIAG after send: user messages={userMessages.Count}, APP_NAME row='{appNameEarly}'");

        Assert.That(await WaitForAssistantMessageContaining(SavedFragments), Is.True,
            "Saved confirmation must appear in the chat after answering the app name");

        var appName = await DbHelper.ExecuteSqlAsync(
            $"SELECT value FROM settings WHERE type = '{AppNameSettingType}' ORDER BY value LIMIT 1");
        TestContext.Out.WriteLine($"APP_NAME in DB: '{appName}'");
        Assert.That(appName, Is.EqualTo(AppNameTestValue),
            $"APP_NAME setting must contain '{AppNameTestValue}'. Got: '{appName}'");

        TestContext.Out.WriteLine("PASS: App name persisted as APP_NAME setting");
    }

    [Test, Order(4)]
    public async Task Step4_Station2_Branding_NavigatesToSettings()
    {
        TestContext.Out.WriteLine("=== Step 4: Station 2 (branding) navigates to settings and explains icon/logo ===");

        await Actions.Wait1000();
        var url = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"URL at station 2: {url}");
        Assert.That(url, Does.Contain(SettingsRouteSegment),
            $"Station 2 must keep/navigate the user on the settings page. Got: {url}");

        Assert.That(await WaitForAssistantMessageContaining(Station2BrandingFragments), Is.True,
            "Station 2 branding explanation (icon/logo card) must appear in the chat");

        var endChip = await WaitForChipByLabels(EndTourChipLabels, MessageTimeoutMs);
        Assert.That(endChip, Is.Not.Null, "Station chips (incl. end-tour) must be shown for station 2");

        TestContext.Out.WriteLine("PASS: Station 2 explanation shown on settings page with station chips");
    }

    [Test, Order(5)]
    public async Task Step5_EndTour_PersistsDismissedState()
    {
        TestContext.Out.WriteLine("=== Step 5: Ending the tour persists the dismissed state ===");

        var endChip = await WaitForChipByLabels(EndTourChipLabels, PresenceProbeTimeoutMs);
        Assert.That(endChip, Is.Not.Null, "End-tour chip must be present from step 4");
        await endChip!.ClickAsync();
        await Actions.Wait2000();

        var state = await DbHelper.ExecuteSqlAsync(
            $"SELECT value FROM settings WHERE type = '{OnboardingStateSettingType}'");
        TestContext.Out.WriteLine($"ONBOARDING_STATE after end: '{state}'");
        Assert.That(state, Does.Contain("dismissed"),
            $"Ending the tour must persist a dismissed state. Got: '{state}'");

        TestContext.Out.WriteLine("PASS: Tour ended and dismissed state persisted (tear-down resets it to pending)");
    }

    private string GetChatSelector(string controlKey)
    {
        Assert.That(_chatSelectors.ContainsKey(controlKey), Is.True,
            $"Chat selector '{controlKey}' not found in ui_controls for page '{PageKeyLlmChat}'");
        return _chatSelectors[controlKey];
    }

    private async Task EnsureChatOpenForReading()
    {
        var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput), PresenceProbeTimeoutMs);
        if (chatInput == null)
        {
            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();
        }
    }

    private async Task<List<string>> GetChipLabels()
    {
        var labels = new List<string>();
        var chips = await Actions.QuerySelectorAll(CssReplyChipBtn);
        foreach (var chip in chips)
        {
            var label = (await Actions.GetElementText(chip))?.Trim() ?? string.Empty;
            if (label.Length > 0)
            {
                labels.Add(label);
            }
        }

        return labels;
    }

    private async Task<IElementHandle?> FindChipByLabels(string[] labels)
    {
        var chips = await Actions.QuerySelectorAll(CssReplyChipBtn);
        foreach (var chip in chips)
        {
            var label = (await Actions.GetElementText(chip))?.Trim() ?? string.Empty;
            if (ContainsAny(label, labels))
            {
                return chip;
            }
        }

        return null;
    }

    private async Task<IElementHandle?> WaitForChipByLabels(string[] labels, int timeoutMs)
    {
        var startedAt = DateTime.UtcNow;
        while ((DateTime.UtcNow - startedAt).TotalMilliseconds < timeoutMs)
        {
            var chip = await FindChipByLabels(labels);
            if (chip != null)
            {
                return chip;
            }

            await Actions.Wait500();
        }

        return null;
    }

    private async Task<bool> WaitForAssistantMessageContaining(string[] fragments, int timeoutMs = MessageTimeoutMs)
    {
        var messagesSelector = GetChatSelector(ControlKeyMessages);
        var startedAt = DateTime.UtcNow;
        var lastTexts = new List<string>();
        while ((DateTime.UtcNow - startedAt).TotalMilliseconds < timeoutMs)
        {
            lastTexts.Clear();
            var messageTexts = await Actions.QuerySelectorAll($"#{messagesSelector} {CssAssistantMessageText}");
            foreach (var messageText in messageTexts)
            {
                var text = (await Actions.GetElementText(messageText))?.Trim() ?? string.Empty;
                lastTexts.Add(text);
                if (ContainsAny(text, fragments))
                {
                    return true;
                }
            }

            await Actions.Wait500();
        }

        TestContext.Out.WriteLine($"No assistant message containing [{string.Join(" | ", fragments)}] within {timeoutMs / 1000}s. Messages seen:");
        foreach (var text in lastTexts)
        {
            TestContext.Out.WriteLine($"  - {Truncate(text, 120)}");
        }

        return false;
    }

    private static bool ContainsAny(string text, string[] fragments) =>
        fragments.Any(fragment => text.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
