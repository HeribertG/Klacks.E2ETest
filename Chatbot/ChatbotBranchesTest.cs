// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Chatbot.Helpers;

namespace Klacks.E2ETest.Chatbot
{
    [TestFixture]
    [Order(53)]
    public class ChatbotBranchesTest : ChatbotTestBase
    {
        private const string PageKeySettingsBranches = "settings-branches";
        private const string ControlKeyRowName = "row-name";

        private const string SkillValidateAddress = "validate_address";

        private const int CreateTimeoutMs = 120000;
        private const int WaitDomTimeoutMs = 30000;
        private const int WaitRemovedTimeoutMs = 20000;
        private const int MaxRetries = 3;

        private static readonly List<string> CreatedBranchNames = new();

        private static readonly (string Name, string Address, string Phone, string Email) BranchZurich =
            ("Filiale Zürich", "Bahnhofstrasse 1, 8001 Zürich", "044 123 45 67", "zuerich@klacks-test.ch");

        private static readonly (string Name, string Address, string Phone, string Email) BranchLausanne =
            ("Filiale Lausanne", "Place de la Gare 1, 1003 Lausanne", "021 123 45 67", "lausanne@klacks-test.ch");

        private int _messageCountBefore;
        private string _branchRowSelector = string.Empty;

        [OneTimeSetUp]
        public async Task LoadBranchSelectors()
        {
            var branchSelectors = await DbHelper.GetUiControlSelectorsAsync(PageKeySettingsBranches);
            Assert.That(branchSelectors, Is.Not.Empty, "Branch selectors must be loaded from ui_controls");

            Assert.That(branchSelectors.ContainsKey(ControlKeyRowName), Is.True,
                $"Selector '{ControlKeyRowName}' not found in ui_controls for page '{PageKeySettingsBranches}'");
            _branchRowSelector = branchSelectors[ControlKeyRowName];
        }

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
        public async Task Step2_VerifyPermissions()
        {
            TestContext.Out.WriteLine("=== Step 2: Verify Branch Management Permissions ===");
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Bin ich ein Administrator? Welche Berechtigungen habe ich?");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(
                response.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Berechtigung", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Recht", StringComparison.OrdinalIgnoreCase),
                Is.True,
                $"User must have Admin rights. Got: {response}");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Permissions verified successfully");
        }

        [Test, Order(3)]
        public async Task Step3_ValidateZurichAddress()
        {
            TestContext.Out.WriteLine("=== Step 3: Validate Zürich Address via Chat ===");
            await AssertSkillEnabled(SkillValidateAddress);
            await EnsureChatOpen();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Validiere die Adresse: Bahnhofstrasse 1, 8001 Zürich, Schweiz");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address validation result");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Zürich address validated successfully");
        }

        [Test, Order(4)]
        public async Task Step4_CreateZurichBranchViaChat()
        {
            TestContext.Out.WriteLine("=== Step 4: Create Zürich Branch via LLM Chat (UI) ===");
            var branch = BranchZurich;

            await CreateBranchWithRetry(branch.Name, branch.Address, branch.Phone, branch.Email);
            CreatedBranchNames.Add(branch.Name);

            TestContext.Out.WriteLine($"Zürich branch created via UI: {branch.Name}");
        }

        [Test, Order(5)]
        public async Task Step5_ValidateLausanneAddress()
        {
            TestContext.Out.WriteLine("=== Step 5: Validate Lausanne Address via Chat ===");
            await EnsureChatOpen();
            await ClearChatAndWait();

            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Validiere die Adresse: Place de la Gare 1, 1003 Lausanne, Schweiz");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address validation result");
            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Lausanne address validated successfully");
        }

        [Test, Order(6)]
        public async Task Step6_CreateLausanneBranchViaChat()
        {
            TestContext.Out.WriteLine("=== Step 6: Create Lausanne Branch via LLM Chat (UI) ===");
            var branch = BranchLausanne;

            await CreateBranchWithRetry(branch.Name, branch.Address, branch.Phone, branch.Email);
            CreatedBranchNames.Add(branch.Name);

            TestContext.Out.WriteLine($"Lausanne branch created via UI: {branch.Name}");
        }

        [Test, Order(7)]
        public async Task Step7_VerifyBothBranchesViaChat()
        {
            TestContext.Out.WriteLine("=== Step 7: Verify Both Branches in DOM ===");

            var zurichExists = await BranchExistsInDom(BranchZurich.Name);
            var lausanneExists = await BranchExistsInDom(BranchLausanne.Name);

            TestContext.Out.WriteLine($"  Zürich: {(zurichExists ? "FOUND in DOM" : "NOT FOUND in DOM")}");
            TestContext.Out.WriteLine($"  Lausanne: {(lausanneExists ? "FOUND in DOM" : "NOT FOUND in DOM")}");

            Assert.That(zurichExists && lausanneExists, Is.True, "Both test branches should be visible in Settings DOM");

            TestContext.Out.WriteLine("Both branches verified in DOM");
        }

        [Test, Order(8)]
        public async Task Step8_DeleteBothBranchesViaChat()
        {
            TestContext.Out.WriteLine("=== Step 8: Delete Both Branches via LLM Chat (UI) ===");

            if (CreatedBranchNames.Count == 0)
            {
                TestContext.Out.WriteLine("No branches to delete - skipping");
                Assert.Inconclusive("No branches were created in previous steps");
                return;
            }

            foreach (var branchName in CreatedBranchNames.ToList())
            {
                await DeleteBranchWithRetry(branchName);
                await Actions.Wait2000();
            }

            Assert.That(TestListener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {TestListener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"All {CreatedBranchNames.Count} test branches deleted via UI");
            CreatedBranchNames.Clear();
        }

        [Test, Order(9)]
        public async Task Step9_VerifyBranchesDeletedViaChat()
        {
            TestContext.Out.WriteLine("=== Step 9: Verify Branches Deleted ===");
            await Actions.Wait2000();

            var zurichExists = await BranchExistsInDom(BranchZurich.Name);
            var lausanneExists = await BranchExistsInDom(BranchLausanne.Name);

            TestContext.Out.WriteLine($"  Zürich: {(zurichExists ? "STILL EXISTS" : "DELETED")}");
            TestContext.Out.WriteLine($"  Lausanne: {(lausanneExists ? "STILL EXISTS" : "DELETED")}");

            Assert.That(zurichExists, Is.False, $"Branch '{BranchZurich.Name}' should no longer exist in DOM");
            Assert.That(lausanneExists, Is.False, $"Branch '{BranchLausanne.Name}' should no longer exist in DOM");

            TestContext.Out.WriteLine("All test branches confirmed deleted");
        }

        private async Task CreateBranchWithRetry(string name, string address, string phone, string email)
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                TestContext.Out.WriteLine($"Create branch attempt {attempt}/{MaxRetries}: {name}");
                await EnsureChatOpen();
                await ClearChatAndWait();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage(
                    $"Erstelle eine neue Filiale mit dem Namen '{name}', " +
                    $"Adresse '{address}', Telefon '{phone}', Email '{email}'");
                var response = await WaitForBotResponse(_messageCountBefore, CreateTimeoutMs);
                TestContext.Out.WriteLine($"Bot response ({response.Length} chars): {response[..Math.Min(200, response.Length)]}");

                var found = await WaitForBranchInDom(name);
                if (found)
                {
                    if (TestListener.HasApiErrors() && TestListener.GetLastErrorMessage().Contains("already exists"))
                    {
                        TestContext.Out.WriteLine("Ignoring 'already exists' error since branch was created successfully");
                    }

                    return;
                }

                TestContext.Out.WriteLine($"Branch not found in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Branch '{name}' was not created after {MaxRetries} attempts");
        }

        private async Task DeleteBranchWithRetry(string branchName)
        {
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                TestContext.Out.WriteLine($"Delete branch attempt {attempt}/{MaxRetries}: {branchName}");
                await EnsureChatOpen();
                await ClearChatAndWait();

                _messageCountBefore = await GetMessageCount();
                await SendChatMessage($"Lösche die Filiale '{branchName}'");
                var response = await WaitForBotResponse(_messageCountBefore, 90000);
                TestContext.Out.WriteLine($"Delete response: {response[..Math.Min(200, response.Length)]}");

                var removed = await WaitForBranchRemovedFromDom(branchName);
                if (removed)
                {
                    TestContext.Out.WriteLine($"Branch '{branchName}' confirmed removed from DOM");
                    return;
                }

                TestContext.Out.WriteLine($"Branch '{branchName}' still in DOM after attempt {attempt}, will retry...");
                await Actions.Wait2000();
            }

            Assert.Fail($"Branch '{branchName}' was not deleted after {MaxRetries} attempts");
        }

        private async Task<bool> WaitForBranchInDom(string branchName)
        {
            TestContext.Out.WriteLine($"Waiting for branch '{branchName}' to appear in DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < WaitDomTimeoutMs)
            {
                if (await BranchExistsInDom(branchName))
                {
                    TestContext.Out.WriteLine($"Branch '{branchName}' found in DOM");
                    return true;
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"Branch '{branchName}' NOT found in DOM after {WaitDomTimeoutMs / 1000}s");
            return false;
        }

        private async Task<bool> BranchExistsInDom(string branchName)
        {
            var inputs = await Actions.QuerySelectorAll(_branchRowSelector);
            foreach (var input in inputs)
            {
                var value = await input.InputValueAsync();
                if (value.Contains(branchName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private async Task<bool> WaitForBranchRemovedFromDom(string branchName)
        {
            TestContext.Out.WriteLine($"Waiting for branch '{branchName}' to be removed from DOM...");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < WaitRemovedTimeoutMs)
            {
                if (!await BranchExistsInDom(branchName))
                {
                    TestContext.Out.WriteLine($"Branch '{branchName}' removed from DOM");
                    return true;
                }

                await Actions.Wait500();
            }

            TestContext.Out.WriteLine($"Branch '{branchName}' still in DOM after {WaitRemovedTimeoutMs / 1000}s");
            return false;
        }
    }
}
