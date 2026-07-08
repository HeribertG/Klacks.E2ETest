// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

/// <summary>
/// Verifies that clicking a navigation suggestion chip in Klacksy's greeting toast
/// navigates directly to the target page without going through the LLM.
/// </summary>
[TestFixture]
[Order(52)]
[Explicit]
[Category("Klacksy")]
public class ChatbotGreetingNavigationTest : ChatbotTestBase
{
    private const string CssReplyChipBtn = ".reply-chip-btn";
    private const string SettingsRouteSegment = "settings";
    // Label varies by display language (e.g. "Open settings" in EN, "Einstellungen öffnen" in DE)
    private const string GreetingSuggestionKeyword = "settings";

    [Test, Order(1)]
    public async Task GreetingChip_EditSettings_NavigatesToSettingsPage()
    {
        TestContext.Out.WriteLine("=== Test: Greeting chip 'Einstellungen öffnen' navigates to settings ===");

        await Actions.Reload();
        await Actions.Wait2000();

        var toggleBtnId = GetChatSelector(ControlKeyToggleBtn);
        await Actions.ClickButtonById(toggleBtnId);
        await Actions.Wait3000();

        var urlBefore = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"URL before click: {urlBefore}");

        var chipFound = false;
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var chips = await Actions.QuerySelectorAll(CssReplyChipBtn);
            TestContext.Out.WriteLine($"Found {chips.Count} chip(s)");
            foreach (var chip in chips)
            {
                var label = (await Actions.GetElementText(chip))?.Trim() ?? string.Empty;
                TestContext.Out.WriteLine($"  chip: '{label}'");
                if (label.Contains(GreetingSuggestionKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    await chip.ClickAsync();
                    chipFound = true;
                    TestContext.Out.WriteLine($"Clicked chip: '{label}'");
                    break;
                }
            }

            if (chipFound) break;
            await Actions.Wait1000();
        }

        Assert.That(chipFound, Is.True,
            $"Could not find greeting chip containing '{GreetingSuggestionKeyword}' within 20s after opening chat");

        await Actions.Wait2000();

        var urlAfter = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"URL after click: {urlAfter}");

        Assert.That(urlAfter, Does.Contain(SettingsRouteSegment),
            $"Expected URL to contain '{SettingsRouteSegment}' after clicking greeting chip. Got: {urlAfter}");
        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API errors expected. Error: {TestListener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("PASS: Greeting chip navigated directly to settings page");
    }
}
