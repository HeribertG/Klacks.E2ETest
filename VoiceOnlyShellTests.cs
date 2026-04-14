// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest
{
    /// <summary>
    /// Smoke tests for the Klacksy voice-only shell: verify that switching the
    /// speech output mode between text and audio toggles the voice-shell and
    /// chat panels, and that the right-click context menu opens the transcript
    /// overlay.
    /// </summary>
    [TestFixture]
    [Order(95)]
    public class VoiceOnlyShellTests : PlaywrightSetup
    {
        private const string OutputModeSelectId = "outputMode";
        private const string OutputModeText = "text";
        private const string OutputModeAudio = "audio";

        private const string AssistantToggleButtonId = "header-assistant-button";
        private const string AsideCloseButtonId = "aside-close-btn";

        private const string VoiceShellSelector = "app-voice-shell";
        private const string VoiceShellInnerSelector = "app-voice-shell .voice-shell";
        private const string TranscriptOverlaySelector = "app-transcript-overlay";
        private const string AssistantChatSelector = "app-assistant-chat";

        private const string SpeechSettingsAnchorId = "settings-home-assistant-speech-row";

        private Listener _listener = null!;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await NavigateToSpeechSettingsAsync();
            await SetOutputModeAsync(OutputModeText);
            await Actions.Wait500();
        }

        [TearDown]
        public async Task TearDown()
        {
            await EnsureAsideClosedAsync();
            await SetOutputModeAsync(OutputModeText);

            if (_listener != null && _listener.HasApiErrors())
            {
                TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
            }
        }

        [Test]
        [Order(1)]
        public async Task Step1_SwitchingOutputModeToAudioShowsVoiceShell()
        {
            TestContext.Out.WriteLine("=== Step 1: Switching output mode to 'audio' shows voice shell ===");

            await SetOutputModeAsync(OutputModeAudio);
            await OpenAssistantAsideAsync();

            await Actions.ElementIsVisibleByCssSelector(VoiceShellSelector);
            await Actions.ElementIsHiddenByCssSelector(AssistantChatSelector);

            TestContext.Out.WriteLine("Voice shell is visible and assistant chat is hidden");
        }

        [Test]
        [Order(2)]
        public async Task Step2_RightClickOnIconOpensTranscriptOverlay()
        {
            TestContext.Out.WriteLine("=== Step 2: Right click on voice shell opens transcript overlay ===");

            await SetOutputModeAsync(OutputModeAudio);
            await OpenAssistantAsideAsync();
            await Actions.ElementIsVisibleByCssSelector(VoiceShellSelector);

            await Actions.RightClickByCssSelector(VoiceShellInnerSelector);

            await Actions.ElementIsVisibleByCssSelector(TranscriptOverlaySelector);

            TestContext.Out.WriteLine("Transcript overlay opened successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_SwitchingBackToTextRestoresAssistantChatPanel()
        {
            TestContext.Out.WriteLine("=== Step 3: Switching back to 'text' restores assistant chat panel ===");

            await SetOutputModeAsync(OutputModeAudio);
            await OpenAssistantAsideAsync();
            await Actions.ElementIsVisibleByCssSelector(VoiceShellSelector);

            await NavigateToSpeechSettingsAsync();
            await SetOutputModeAsync(OutputModeText);
            await OpenAssistantAsideAsync();

            await Actions.ElementIsVisibleByCssSelector(AssistantChatSelector);
            await Actions.ElementIsHiddenByCssSelector(VoiceShellSelector);

            TestContext.Out.WriteLine("Assistant chat is visible and voice shell is hidden");
        }

        private async Task NavigateToSpeechSettingsAsync()
        {
            await EnsureAsideClosedAsync();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            var anchor = await Actions.FindElementById(SpeechSettingsAnchorId);
            if (anchor != null)
            {
                await Actions.ScrollIntoViewById(SpeechSettingsAnchorId);
                await Actions.Wait500();
            }
        }

        private async Task SetOutputModeAsync(string value)
        {
            var select = await Actions.FindElementById(OutputModeSelectId);
            if (select == null)
            {
                await NavigateToSpeechSettingsAsync();
                select = await Actions.FindElementById(OutputModeSelectId);
            }

            if (select == null)
            {
                Assert.Fail($"Output mode select with id '{OutputModeSelectId}' not found");
                return;
            }

            var current = await Actions.ReadSelect(OutputModeSelectId);
            if (current == value)
            {
                return;
            }

            await Actions.SelectNativeOptionById(OutputModeSelectId, value);
            await Actions.Wait500();
        }

        private async Task OpenAssistantAsideAsync()
        {
            var closeButton = await Actions.FindElementById(AsideCloseButtonId);
            var isOpen = closeButton != null && await closeButton.IsVisibleAsync();

            if (!isOpen)
            {
                await Actions.ClickButtonById(AssistantToggleButtonId);
                await Actions.Wait500();
            }
        }

        private async Task EnsureAsideClosedAsync()
        {
            var closeButton = await Actions.FindElementById(AsideCloseButtonId);
            if (closeButton == null)
            {
                return;
            }

            if (await closeButton.IsVisibleAsync())
            {
                await Actions.ClickButtonById(AsideCloseButtonId);
                await Actions.Wait500();
            }
        }
    }
}
