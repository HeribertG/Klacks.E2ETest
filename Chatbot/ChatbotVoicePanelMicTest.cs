// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Reproduces the panel-mode microphone flow (outputMode=both): clicking the mic button
 * in the aside chat must start listening, transcribe the fake-microphone audio and put
 * the text into the chat input. Requires KLACKS_E2E_FAKE_AUDIO_WAV pointing to a WAV
 * with speech followed by silence (Chromium fake audio capture).
 */

using System.Collections.Concurrent;
using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(90)]
    [Category("KlacksyVoice")]
    public class ChatbotVoicePanelMicTest : ChatbotTestBase
    {
        private const string OutputModeSetting = "ASSISTANT_OUTPUT_MODE";
        private const string OutputModeBoth = "both";
        private const string VoiceButtonId = "assistant-chat-voice-btn";
        private const string ChatInputId = "assistant-chat-input";
        private const string ListeningClass = "listening";
        private const int ListeningTimeoutMs = 8000;
        private const int TranscriptTimeoutMs = 40000;
        private const int PollIntervalMs = 250;

        private readonly ConcurrentQueue<string> _consoleLines = new();

        [OneTimeSetUp]
        public async Task VoicePanelOneTimeSetUp()
        {
            var wavPath = Environment.GetEnvironmentVariable("KLACKS_E2E_FAKE_AUDIO_WAV");
            Assert.That(wavPath, Is.Not.Null.And.Not.Empty,
                "KLACKS_E2E_FAKE_AUDIO_WAV must point to a WAV file for the fake microphone");
            Assert.That(File.Exists(wavPath), Is.True, $"Fake audio WAV not found: {wavPath}");

            await DbHelper.ExecuteSqlAsync(
                $"UPDATE settings SET value = '{OutputModeBoth}' WHERE type = '{OutputModeSetting}'");

            var wavBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(wavPath!));
            await Page.AddInitScriptAsync($$"""
                (() => {
                  const wavBase64 = '{{wavBase64}}';
                  navigator.mediaDevices.getUserMedia = async (constraints) => {
                    if (!constraints || !constraints.audio) {
                      throw new DOMException('Only audio is faked in this test', 'NotFoundError');
                    }
                    const ctx = new AudioContext({ sampleRate: 16000 });
                    const bytes = Uint8Array.from(atob(wavBase64), (c) => c.charCodeAt(0));
                    const buffer = await ctx.decodeAudioData(bytes.buffer);
                    const source = ctx.createBufferSource();
                    source.buffer = buffer;
                    source.loop = true;
                    const destination = ctx.createMediaStreamDestination();
                    source.connect(destination);
                    source.start();
                    return destination.stream;
                  };
                })();
                """);

            await Actions.Reload();
            await Actions.Wait1000();

            Page.Console += (_, msg) => _consoleLines.Enqueue(msg.Text);
        }

        [Test, Order(1)]
        public async Task MicButton_InPanelMode_TranscribesSpokenQuestionIntoInput()
        {
            TestContext.Out.WriteLine("=== Panel-mode mic reproduction (outputMode=both) ===");

            await EnsureChatOpen();
            var voiceButton = await Actions.FindElementById(VoiceButtonId);
            Assert.That(voiceButton, Is.Not.Null, "Voice button must be visible in panel mode");

            await Actions.ClickButtonById(VoiceButtonId);

            var listeningSeen = false;
            var classTransitions = new List<string>();
            var lastClass = string.Empty;
            var listeningDeadline = DateTime.UtcNow.AddMilliseconds(ListeningTimeoutMs);
            while (DateTime.UtcNow < listeningDeadline)
            {
                var cls = await ReadButtonClassAsync();
                if (cls != lastClass)
                {
                    classTransitions.Add(cls);
                    lastClass = cls;
                }
                if (cls.Contains(ListeningClass))
                {
                    listeningSeen = true;
                    break;
                }
                await Task.Delay(PollIntervalMs);
            }

            TestContext.Out.WriteLine($"Button class transitions after click: {string.Join(" | ", classTransitions)}");

            var transcript = string.Empty;
            if (listeningSeen)
            {
                var transcriptDeadline = DateTime.UtcNow.AddMilliseconds(TranscriptTimeoutMs);
                while (DateTime.UtcNow < transcriptDeadline)
                {
                    var cls = await ReadButtonClassAsync();
                    if (cls != lastClass)
                    {
                        classTransitions.Add(cls);
                        lastClass = cls;
                    }

                    transcript = await ReadChatInputValueAsync();
                    if (string.IsNullOrWhiteSpace(transcript))
                    {
                        transcript = ReadTranscriptFromConsole();
                    }
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        break;
                    }
                    await Task.Delay(PollIntervalMs);
                }
            }

            TestContext.Out.WriteLine($"All class transitions: {string.Join(" | ", classTransitions)}");
            TestContext.Out.WriteLine($"Chat input transcript: '{transcript}'");
            DumpVoiceConsoleLines();

            Assert.Multiple(() =>
            {
                Assert.That(listeningSeen, Is.True, "Mic click must switch the voice button into the listening state");
                Assert.That(transcript, Is.Not.Empty, "Spoken fake-microphone audio must arrive as text in the chat input");
            });
        }

        private async Task<string> ReadButtonClassAsync()
        {
            var button = await Actions.FindElementById(VoiceButtonId, 1000);
            if (button == null)
            {
                return "<button not found>";
            }
            return await button.GetAttributeAsync("class") ?? string.Empty;
        }

        private async Task<string> ReadChatInputValueAsync()
        {
            var input = await Actions.FindElementById(ChatInputId, 1000);
            if (input == null)
            {
                return string.Empty;
            }
            return await input.InputValueAsync();
        }

        private string ReadTranscriptFromConsole()
        {
            const string marker = "blob STT result: ";
            foreach (var line in _consoleLines)
            {
                var idx = line.IndexOf(marker, StringComparison.Ordinal);
                if (idx < 0)
                {
                    continue;
                }
                var text = line[(idx + marker.Length)..].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
            return string.Empty;
        }

        private void DumpVoiceConsoleLines()
        {
            TestContext.Out.WriteLine("--- Browser console ([VS] voice pipeline) ---");
            foreach (var line in _consoleLines)
            {
                if (line.Contains("[VS]"))
                {
                    TestContext.Out.WriteLine(line);
                }
            }
        }
    }
}
