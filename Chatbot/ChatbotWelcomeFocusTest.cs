// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot;

/// <summary>
/// Verifies the welcome focus toast on an empty installation: the toast headline is the
/// "no orders yet" question rather than the neutral "what do you want to do", and its first
/// button starts the setup consultation in the chat instead of navigating away.
/// </summary>
[TestFixture]
[Order(53)]
[Explicit]
[Category("Klacksy")]
public class ChatbotWelcomeFocusTest : ChatbotTestBase
{
    private const string CssReplyChipBtn = ".reply-chip-btn";
    private const string CssToastText = ".toast-text";
    private const string NeutralPromptKeywordDe = "Was möchtest du tun";
    private const string NeutralPromptKeywordEn = "What would you like to do";
    // The prompt text varies by display language: DE "Bestellungen", EN "orders".
    private const string SetupPromptKeywordDe = "Bestellung";
    private const string SetupPromptKeywordEn = "order";
    private const string WorkCountSql = "SELECT count(*) FROM work WHERE is_deleted = false";
    private const string EmptyInstallationWorkCount = "0";
    private const int ToastWaitSeconds = 20;
    private const int MessageWaitSeconds = 60;

    [Test, Order(1)]
    public async Task WelcomeFocus_EmptyInstallation_AsksAboutTheMissingOrdersAndStartsTheConsultation()
    {
        var workCount = (await DbHelper.ExecuteSqlAsync(WorkCountSql)).Trim();
        if (workCount != EmptyInstallationWorkCount)
        {
            Assert.Inconclusive(
                $"This test needs an installation without work assignments; the database reports '{workCount}'.");
            return;
        }

        TestContext.Out.WriteLine("=== Test: welcome focus toast on an empty installation ===");

        await Actions.Reload();
        await Actions.Wait2000();
        await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
        await Actions.Wait3000();

        var toastText = await WaitForToastTextAsync();
        TestContext.Out.WriteLine($"Toast headline: '{toastText}'");

        Assert.That(toastText, Is.Not.Empty, "No interactive toast appeared within the wait window.");
        Assert.That(
            toastText.Contains(SetupPromptKeywordDe, StringComparison.OrdinalIgnoreCase)
            || toastText.Contains(SetupPromptKeywordEn, StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Expected the setup question in the toast headline. Got: '{toastText}'");
        Assert.That(
            toastText.Contains(NeutralPromptKeywordDe, StringComparison.OrdinalIgnoreCase)
            || toastText.Contains(NeutralPromptKeywordEn, StringComparison.OrdinalIgnoreCase),
            Is.False,
            "The neutral prompt is still shown, so the focus never reached the toast.");

        var urlBefore = Actions.ReadCurrentUrl();
        var messagesBefore = await GetMessageCount();

        var chips = await Actions.QuerySelectorAll(CssReplyChipBtn);
        Assert.That(chips.Count, Is.GreaterThan(0), "The focus toast carried no option chips.");
        var firstChipLabel = (await Actions.GetElementText(chips[0]))?.Trim() ?? string.Empty;
        TestContext.Out.WriteLine($"Clicking the first chip: '{firstChipLabel}'");
        await chips[0].ClickAsync();

        var messagesAfter = await WaitForMoreMessagesAsync(messagesBefore);
        var urlAfter = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"URL before: {urlBefore} / after: {urlAfter}");

        Assert.That(messagesAfter, Is.GreaterThan(messagesBefore),
            "The consultation button produced no new assistant message.");
        Assert.That(urlAfter, Is.EqualTo(urlBefore),
            "The consultation button must start a chat flow, not navigate away.");
        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API errors expected. Error: {TestListener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("PASS: the focus toast asked about the missing orders and started the consultation");
    }

    private async Task<string> WaitForToastTextAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(ToastWaitSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var texts = await Actions.QuerySelectorAll(CssToastText);
            foreach (var element in texts)
            {
                var text = (await Actions.GetElementText(element))?.Trim() ?? string.Empty;
                if (text.Length > 0)
                {
                    return text;
                }
            }

            await Actions.Wait1000();
        }

        return string.Empty;
    }

    private async Task<int> WaitForMoreMessagesAsync(int previousCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(MessageWaitSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var current = await GetMessageCount();
            if (current > previousCount)
            {
                return current;
            }

            await Actions.Wait1000();
        }

        return await GetMessageCount();
    }
}
