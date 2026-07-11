// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.Chatbot.Helpers;

public abstract class ChatbotTestBase : PlaywrightSetup
{
    private const string PageKeyLlmChat = "llm-chat";
    protected const string ControlKeyToggleBtn = "toggle-btn";
    protected const string ControlKeyInput = "input";
    protected const string ControlKeySendBtn = "send-btn";
    protected const string ControlKeyMessages = "messages";
    protected const string ControlKeyClearBtn = "clear-btn";

    private const string CssAssistantMessage = ".message-wrapper.assistant";
    private const string CssTypingIndicator = ".typing-indicator";
    private const string CssMessageText = ".message-text";

    private const int MaxInputRetries = 3;
    private const int MaxNavigationRetries = 5;
    private const int InputEnabledTimeoutMs = 10000;
    private const int DefaultBotResponseTimeoutMs = 90000;

    // AsideComponent only renders the classic aside panel (#assistant-chat-input etc.) when
    // outputMode is NOT audio/both-auto — those route the chat into the floating voice shell
    // instead, leaving the selectors these tests drive absent/hidden. Force text mode for the
    // fixture's duration so the suite runs deterministically regardless of the current
    // Klacksy voice-output setting, and restore it afterward since that setting is also used
    // interactively outside these tests.
    private const string OutputModeSettingType = "ASSISTANT_OUTPUT_MODE";
    private const string NonFloatingOutputMode = "text";

    private string? _originalOutputMode;

    protected Dictionary<string, string> ChatSelectors { get; private set; } = new();
    protected Listener TestListener { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task ChatbotOneTimeSetUp()
    {
        ChatSelectors = await DbHelper.GetUiControlSelectorsAsync(PageKeyLlmChat);
        Assert.That(ChatSelectors, Is.Not.Empty, "Chat selectors must be loaded from ui_controls");

        await ForceNonFloatingOutputModeAsync();
        await ResetChatAsync();
    }

    [OneTimeTearDown]
    public async Task ChatbotOneTimeTearDown() => await RestoreOutputModeAsync();

    private async Task ForceNonFloatingOutputModeAsync()
    {
        var current = (await DbHelper.ExecuteSqlAsync(
            $"SELECT value FROM settings WHERE type = '{OutputModeSettingType}'")).Trim();
        var rowExists = current.Length > 0 && !current.StartsWith("ERROR:");
        _originalOutputMode = rowExists ? current : null;

        if (_originalOutputMode == NonFloatingOutputMode)
            return;

        var sql = rowExists
            ? $"UPDATE settings SET value = '{NonFloatingOutputMode}' WHERE type = '{OutputModeSettingType}'"
            : $"INSERT INTO settings (id, type, value) VALUES (gen_random_uuid(), '{OutputModeSettingType}', '{NonFloatingOutputMode}')";
        await DbHelper.ExecuteSqlAsync(sql);

        await Actions.Reload();
        await Actions.Wait1000();
    }

    private async Task RestoreOutputModeAsync()
    {
        if (_originalOutputMode == null || _originalOutputMode == NonFloatingOutputMode)
            return;

        await DbHelper.ExecuteSqlAsync(
            $"UPDATE settings SET value = '{Escape(_originalOutputMode)}' WHERE type = '{OutputModeSettingType}'");
    }

    private static string Escape(string value) => value.Replace("'", "''");

    [SetUp]
    public async Task ChatbotSetUp()
    {
        TestListener = new Listener(Page);
        TestListener.RecognizeApiErrors();
    }

    [TearDown]
    public void ChatbotTearDown()
    {
        if (TestListener.HasApiErrors())
            TestContext.Out.WriteLine($"API Error: {TestListener.GetLastErrorMessage()}");
    }

    protected string GetChatSelector(string controlKey)
    {
        Assert.That(ChatSelectors.ContainsKey(controlKey), Is.True,
            $"Chat selector '{controlKey}' not found in ui_controls for page '{PageKeyLlmChat}'");
        return ChatSelectors[controlKey];
    }

    protected async Task EnsureChatOpen()
    {
        var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
        if (chatInput == null)
        {
            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();
        }

        await WaitForChatInputEnabled();
    }

    protected async Task CloseChatIfOpen()
    {
        var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
        if (chatInput != null)
        {
            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait500();
        }
    }

    protected async Task WaitForChatInputEnabled()
    {
        var inputSelector = GetChatSelector(ControlKeyInput);
        var toggleSelector = GetChatSelector(ControlKeyToggleBtn);

        for (var attempt = 0; attempt < MaxInputRetries; attempt++)
        {
            var isEnabled = await WaitForInputEnabled(inputSelector, InputEnabledTimeoutMs);
            if (isEnabled)
                return;

            TestContext.Out.WriteLine($"Chat input disabled (attempt {attempt + 1}/{MaxInputRetries}), refreshing page...");
            await Actions.Reload();
            await Actions.Wait2000();

            await Actions.ClickButtonById(toggleSelector);
            await Actions.Wait1000();
        }

        Assert.Fail("Chat input remained disabled after multiple refresh attempts");
    }

    protected async Task SendChatMessage(string message)
    {
        TestContext.Out.WriteLine($"Sending message: {message}");
        await Actions.FillInputWithDispatch(GetChatSelector(ControlKeyInput), message);
        await Actions.ClickButtonById(GetChatSelector(ControlKeySendBtn));
    }

    protected async Task<int> GetMessageCount()
    {
        var messagesSelector = GetChatSelector(ControlKeyMessages);
        for (var attempt = 0; attempt < MaxNavigationRetries; attempt++)
        {
            try
            {
                var messages = await Actions.QuerySelectorAll($"#{messagesSelector} {CssAssistantMessage}");
                return messages.Count;
            }
            catch (Exception ex) when (IsNavigationRace(ex))
            {
                await Actions.Wait500();
            }
        }

        return 0;
    }

    private static bool IsNavigationRace(Exception ex) =>
        ex.Message.Contains("Execution context was destroyed", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("because of a navigation", StringComparison.OrdinalIgnoreCase);

    protected async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = DefaultBotResponseTimeoutMs)
    {
        TestContext.Out.WriteLine("Waiting for bot response...");
        var messagesSelector = GetChatSelector(ControlKeyMessages);

        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var typingIndicator = await Actions.QuerySelector($"#{messagesSelector} {CssTypingIndicator}");
                var currentMessages = await Actions.QuerySelectorAll($"#{messagesSelector} {CssAssistantMessage}");

                if (typingIndicator == null && currentMessages.Count > previousMessageCount)
                {
                    var lastMessage = currentMessages[currentMessages.Count - 1];
                    var messageText = await Actions.QueryChildSelector(lastMessage, CssMessageText);
                    if (messageText != null)
                    {
                        var text = await Actions.GetElementText(messageText);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            TestContext.Out.WriteLine($"Bot responded after {(DateTime.UtcNow - startTime).TotalSeconds:F1}s");
                            return text.Trim();
                        }
                    }
                }
            }
            catch (Exception ex) when (IsNavigationRace(ex))
            {
                TestContext.Out.WriteLine("Navigation in progress while reading chat; retrying...");
            }

            await Actions.Wait500();
        }

        Assert.Fail($"Bot did not respond within {timeoutMs / 1000}s");
        return string.Empty;
    }

    protected async Task ClearChat()
    {
        var clearBtn = await Actions.FindElementById(GetChatSelector(ControlKeyClearBtn));
        if (clearBtn != null)
        {
            await Actions.ClickButtonById(GetChatSelector(ControlKeyClearBtn));
            await Actions.Wait500();
        }
    }

    /// <summary>
    /// Deterministic conversation reset that does not depend on the clear button being clickable:
    /// a page reload recreates the assistant-chat component, which generates a fresh client
    /// conversationId — the server then starts an empty conversation instead of resuming the
    /// previous one. The clear-button click afterwards is best-effort cosmetics only. Note that
    /// pending confirmation tokens live in backend memory per user and are NOT cleared by this;
    /// they expire via TTL (and are parameter-bound and same-turn-blocked since 2026-07-11).
    /// </summary>
    protected async Task ResetChatAsync()
    {
        await Actions.Reload();
        await Actions.Wait2000();
        await EnsureChatOpen();
        await ClearChat();
    }

    protected async Task ClearChatAndWait()
    {
        await Actions.ClickButtonById(GetChatSelector(ControlKeyClearBtn));
        await Actions.Wait1000();
        await WaitForChatInputEnabled();
    }

    protected async Task AssertSkillEnabled(string skillName)
    {
        var skill = await DbHelper.GetSkillInfoAsync(skillName);
        Assert.That(skill, Is.Not.Null, $"Skill '{skillName}' not found in agent_skills");
        Assert.That(skill!.IsEnabled, Is.True, $"Skill '{skillName}' is disabled in agent_skills");
        TestContext.Out.WriteLine($"Skill '{skillName}' verified: enabled, category={skill.Category}, type={skill.ExecutionType}");
    }

    private async Task<bool> WaitForInputEnabled(string inputSelector, int timeoutMs)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            var chatInput = await Actions.FindElementById(inputSelector, 500);
            if (chatInput != null)
            {
                var isDisabled = await chatInput.IsDisabledAsync();
                if (!isDisabled)
                    return true;
            }

            await Actions.Wait500();
        }

        return false;
    }
}
