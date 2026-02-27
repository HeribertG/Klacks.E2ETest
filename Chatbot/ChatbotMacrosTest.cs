// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(54)]
    public class ChatbotMacrosTest : ChatbotTestBase
    {
        private const string CssMacroRowName = "input[id^='macro-row-name-']";

        private const int CreateTimeoutMs = 120000;
        private const int WaitDomTimeoutMs = 30000;
        private const int WaitRemovedTimeoutMs = 20000;
        private const int MaxRetries = 3;

        private static bool _macroCreated;
        private static readonly string MacroName = $"TestMacro_{DateTime.UtcNow:yyyyMMddHHmmss}";

        private const string MacroScript =
            "import weekday\n" +
            "import sorate\n\n" +
            "IF weekday >= 6 THEN\n" +
            "  OUTPUT 1, sorate\n" +
            "ELSE\n" +
            "  OUTPUT 1, 0\n" +
            "END IF";

        private int _messageCountBefore;

        [Test, Order(1)]
        public async Task Step1_OpenChat()
        {
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            await Actions.ClickButtonById(GetChatSelector(ControlKeyToggleBtn));
            await Actions.Wait1000();

            var chatInput = await Actions.FindElementById(GetChatSelector(ControlKeyInput));
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test, Order(2)]
        public async Task Step2_CreateMacroWithScriptViaChat()
        {
            TestContext.Out.WriteLine($"=== Step 2: Create Macro '{MacroName}' with Script via LLM Chat (UI) ===");

            var response = await CreateMacroWithRetry(MacroName);
            _macroCreated = true;

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(
                response.Contains("Syntax OK", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Script", StringComparison.OrdinalIgnoreCase)
                || response.Contains("erstellt", StringComparison.OrdinalIgnoreCase)
                || response.Contains("created", StringComparison.OrdinalIgnoreCase)
                || response.Contains(MacroName, StringComparison.OrdinalIgnoreCase)
                || response.Contains("Macro", StringComparison.OrdinalIgnoreCase),
                Is.True, $"Response should confirm script was created. Got: {response}");

            TestContext.Out.WriteLine($"Macro created with script via UI: {MacroName}");
        }

        [Test, Order(3)]
        public async Task Step3_ListMacrosViaChat()
        {
            TestContext.Out.WriteLine("=== Step 3: List Macros via LLM Chat (UI) ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle Macros auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            var macroExists = await MacroExistsInDom(MacroName);
            TestContext.Out.WriteLine($"  {MacroName}: {(macroExists ? "FOUND in DOM" : "NOT FOUND in DOM")}");

            Assert.That(macroExists, Is.True, $"Macro '{MacroName}' should be visible in Settings DOM");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Macro verified in DOM");
        }

        [Test, Order(4)]
        public async Task Step4_DeleteMacroViaChat()
        {
            TestContext.Out.WriteLine("=== Step 4: Delete Macro via LLM Chat (UI) ===");

            if (!_macroCreated)
            {
                TestContext.Out.WriteLine("No macros to delete - skipping");
                Assert.Inconclusive("No macros were created in previous steps");
                return;
            }

            await DeleteMacroWithRetry(MacroName);
            await Actions.Wait2000();

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Test macro '{MacroName}' deleted via UI");
            _macroCreated = false;
        }

        [Test, Order(5)]
        public async Task Step5_VerifyMacroDeletedViaChat()
        {
            TestContext.Out.WriteLine("=== Step 5: Verify Macro Deleted ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle Macros auf");
            await WaitForBotResponse(_messageCountBefore, 90000);
            await Actions.Wait2000();

            var macroExists = await MacroExistsInDom(MacroName);
            TestContext.Out.WriteLine($"  {MacroName}: {(macroExists ? "STILL EXISTS" : "DELETED")}");

            Assert.That(macroExists, Is.False, $"Macro '{MacroName}' should no longer exist in DOM");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Test macro confirmed deleted");
        }

        private async Task<string> CreateMacroWithRetry(string name)
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                TestContext.Out.WriteLine($"Create macro attempt {attempt}/{MaxRetries}: {name}");
                await EnsureChatOpen();
                await ClearChatAndWait();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Erstelle ein neues Macro mit dem Namen '{name}' und genau folgendem Script:\n{MacroScript}");
                var response = await WaitForBotResponse(_messageCountBefore, CreateTimeoutMs);
                TestContext.Out.WriteLine($"Bot response ({response.Length} chars): {response[..Math.Min(300, response.Length)]}");

                if (await WaitForMacroInDom(name))
                {
                    Assert.That(TestListener.HasApiErrors(), Is.False,
                        $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");
                    return response;
                }

                TestContext.Out.WriteLine($"Macro not found in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Macro '{name}' was not created after {MaxRetries} attempts");
            return string.Empty;
        }

        private async Task DeleteMacroWithRetry(string macroName)
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                TestContext.Out.WriteLine($"Delete macro attempt {attempt}/{MaxRetries}: {macroName}");
                await EnsureChatOpen();
                await ClearChatAndWait();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage($"Lösche das Macro '{macroName}'");
                var response = await WaitForBotResponse(_messageCountBefore, 90000);
                TestContext.Out.WriteLine($"Delete response: {response[..Math.Min(200, response.Length)]}");

                var removed = await WaitForMacroRemovedFromDom(macroName);
                if (removed)
                {
                    TestContext.Out.WriteLine($"Macro '{macroName}' confirmed removed from DOM");
                    return;
                }

                TestContext.Out.WriteLine($"Macro '{macroName}' still in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Macro '{macroName}' was not deleted after {MaxRetries} attempts");
        }

        private async Task<bool> WaitForMacroInDom(string macroName)
        {
            TestContext.Out.WriteLine($"Waiting for macro '{macroName}' to appear in DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < WaitDomTimeoutMs)
            {
                if (await MacroExistsInDom(macroName))
                {
                    TestContext.Out.WriteLine($"Macro '{macroName}' found in DOM");
                    return true;
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"Macro '{macroName}' NOT found in DOM after {WaitDomTimeoutMs / 1000}s");
            return false;
        }

        private async Task<bool> MacroExistsInDom(string macroName)
        {
            var inputs = await Actions.QuerySelectorAll(CssMacroRowName);
            foreach (var input in inputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(macroName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private async Task<bool> WaitForMacroRemovedFromDom(string macroName)
        {
            TestContext.Out.WriteLine($"Waiting for macro '{macroName}' to be removed from DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < WaitRemovedTimeoutMs)
            {
                if (!await MacroExistsInDom(macroName))
                {
                    TestContext.Out.WriteLine($"Macro '{macroName}' removed from DOM");
                    return true;
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"Macro '{macroName}' still in DOM after {WaitRemovedTimeoutMs / 1000}s");
            return false;
        }
    }
}
