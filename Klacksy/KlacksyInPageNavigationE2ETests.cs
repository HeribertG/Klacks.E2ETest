// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// E2E tests verifying that Klacksy in-page navigation scrolls to and highlights the correct
/// settings card after the user asks for it via the chat panel.
/// </summary>

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Klacksy;

[TestFixture]
[Order(62)]
public class KlacksyInPageNavigationE2ETests : ChatbotTestBase
{
    private const string SkillNavigateTo = "navigate_to";
    private const string RouteSettings = "workplace/settings";

    private const string TargetLlmProvider = "llm-provider";
    private const string TargetUserManagement = "user-management";
    private const string TargetEmailConfig = "email-config";

    private const string CssKlacksyHighlight = "klacksy-highlight";
    private const string AttrKlacksyTarget = "data-klacksy-target";

    private const int ScrollCheckTimeoutMs = 5000;
    private const int HighlightPollIntervalMs = 200;

    private int _messageCountBefore;

    [Test, Order(1)]
    public async Task Step1_OpenChat()
    {
        TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

        await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
        await Actions.Wait1000();

        var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
        Assert.That(chatInput, Is.Not.Null, "Chat input should be visible after toggle");

        TestContext.Out.WriteLine("Chat panel opened successfully");
    }

    [Test, Order(2)]
    public async Task Step2_Navigate_To_Settings_Via_Chat()
    {
        TestContext.Out.WriteLine("=== Step 2: Navigate to Settings page via chat ===");
        await AssertSkillEnabled(SkillNavigateTo);
        await EnsureChatOpen();

        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Navigiere zu den Einstellungen");
        await WaitForBotResponse(_messageCountBefore);
        await Actions.Wait2000();

        var currentUrl = Actions.ReadCurrentUrl();
        TestContext.Out.WriteLine($"Current URL: {currentUrl}");
        Assert.That(currentUrl, Does.Contain(RouteSettings),
            $"URL should contain '{RouteSettings}'. Got: {currentUrl}");

        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Navigation to settings successful");
    }

    [Test, Order(3)]
    public async Task Step3_Klacksy_Shows_LlmProvider_Scrolls_And_Highlights()
    {
        TestContext.Out.WriteLine("=== Step 3: In-page navigation to LLM Provider card ===");
        await EnsureChatOpen();
        await ClearChatAndWait();

        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Klacksy zeig mir LLM Provider");
        await WaitForBotResponse(_messageCountBefore);
        await Actions.Wait2000();

        var currentUrl = Actions.ReadCurrentUrl();
        Assert.That(currentUrl, Does.Contain(RouteSettings),
            $"URL should still point to settings. Got: {currentUrl}");

        var targetSelector = $"[{AttrKlacksyTarget}=\"{TargetLlmProvider}\"]";
        var targetElement = await Actions.QuerySelector(targetSelector);
        Assert.That(targetElement, Is.Not.Null,
            $"Target element '[{AttrKlacksyTarget}=\"{TargetLlmProvider}\"]' must exist in the DOM");

        var wasHighlighted = await WaitForHighlightClass(targetSelector);
        Assert.That(wasHighlighted, Is.True,
            $"Target element should receive '{CssKlacksyHighlight}' class during navigation");

        var box = await targetElement!.BoundingBoxAsync();
        Assert.That(box, Is.Not.Null, "Target element must have a bounding box (must be visible)");
        Assert.That(box!.Y, Is.LessThan(800),
            $"Target should be scrolled into the viewport. Y={box.Y} is too far down");

        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"LLM Provider card scrolled into view at Y={box.Y} and highlighted");
    }

    [Test, Order(4)]
    public async Task Step4_Klacksy_Shows_UserManagement_Scrolls_And_Highlights()
    {
        TestContext.Out.WriteLine("=== Step 4: In-page navigation to User Management card ===");
        await EnsureChatOpen();
        await ClearChatAndWait();

        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Klacksy zeig mir Benutzerverwaltung");
        await WaitForBotResponse(_messageCountBefore);
        await Actions.Wait2000();

        var currentUrl = Actions.ReadCurrentUrl();
        Assert.That(currentUrl, Does.Contain(RouteSettings),
            $"URL should point to settings. Got: {currentUrl}");

        var targetSelector = $"[{AttrKlacksyTarget}=\"{TargetUserManagement}\"]";
        var targetElement = await Actions.QuerySelector(targetSelector);
        Assert.That(targetElement, Is.Not.Null,
            $"Target element '[{AttrKlacksyTarget}=\"{TargetUserManagement}\"]' must exist in the DOM");

        var wasHighlighted = await WaitForHighlightClass(targetSelector);
        Assert.That(wasHighlighted, Is.True,
            $"Target element should receive '{CssKlacksyHighlight}' class during navigation");

        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("User Management card scrolled and highlighted");
    }

    [Test, Order(5)]
    public async Task Step5_Klacksy_Shows_EmailConfig_Scrolls_And_Highlights()
    {
        TestContext.Out.WriteLine("=== Step 5: In-page navigation to Email Config card ===");
        await EnsureChatOpen();
        await ClearChatAndWait();

        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Klacksy zeig mir E-Mail Konfiguration");
        await WaitForBotResponse(_messageCountBefore);
        await Actions.Wait2000();

        var currentUrl = Actions.ReadCurrentUrl();
        Assert.That(currentUrl, Does.Contain(RouteSettings),
            $"URL should point to settings. Got: {currentUrl}");

        var targetSelector = $"[{AttrKlacksyTarget}=\"{TargetEmailConfig}\"]";
        var targetElement = await Actions.QuerySelector(targetSelector);
        Assert.That(targetElement, Is.Not.Null,
            $"Target element '[{AttrKlacksyTarget}=\"{TargetEmailConfig}\"]' must exist in the DOM");

        var wasHighlighted = await WaitForHighlightClass(targetSelector);
        Assert.That(wasHighlighted, Is.True,
            $"Target element should receive '{CssKlacksyHighlight}' class during navigation");

        Assert.That(TestListener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Email Config card scrolled and highlighted");
    }

    /// <summary>
    /// Polls for the klacksy-highlight CSS class on the element matching cssSelector.
    /// Returns true as soon as the class is observed, false if the timeout is exceeded.
    /// The highlight is transient (removed after HIGHLIGHT_MS), so polling must be fast.
    /// </summary>
    private async Task<bool> WaitForHighlightClass(string cssSelector)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(ScrollCheckTimeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var hasClass = await Actions.QuerySelector(cssSelector + $".{CssKlacksyHighlight}");
            if (hasClass != null)
                return true;

            await Task.Delay(HighlightPollIntervalMs);
        }

        return false;
    }
}
