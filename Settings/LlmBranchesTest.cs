using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.LlmChatIds;
using static Klacks.E2ETest.Constants.SettingsBranchIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(63)]
    public class LlmBranchesTest : PlaywrightSetup
    {
        private Listener _listener;
        private int _messageCountBefore;

        private static readonly List<string> CreatedBranchIds = new();

        private static readonly (string Name, string Address, string Phone, string Email) BranchZurich =
            ("Filiale Zürich", "Bahnhofstrasse 1, 8001 Zürich", "044 123 45 67", "zuerich@klacks-test.ch");

        private static readonly (string Name, string Address, string Phone, string Email) BranchLausanne =
            ("Filiale Lausanne", "Place de la Gare 1, 1003 Lausanne", "021 123 45 67", "lausanne@klacks-test.ch");

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();
        }

        [TearDown]
        public void TearDown()
        {
            if (_listener.HasApiErrors())
            {
                TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
            }
        }

        [Test]
        [Order(1)]
        public async Task Step1_OpenChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Open Chat Panel ===");

            // Act
            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();

            // Assert
            var chatInput = await Actions.FindElementById(ChatInput);
            Assert.That(chatInput, Is.Not.Null, "Chat input should be visible");

            TestContext.Out.WriteLine("Chat panel opened successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_VerifyPermissions()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Verify Branch Management Permissions ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Welche Rechte habe ich? Darf ich Filialen verwalten?");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with permissions info");
            Assert.That(response.Contains("Admin", StringComparison.OrdinalIgnoreCase), Is.True,
                $"User must have Admin rights. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Permissions verified successfully");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ValidateZurichAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Validate Zürich Address via Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Validiere die Adresse: Bahnhofstrasse 1, 8001 Zürich, Schweiz");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address validation result");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Zürich address validated successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_CreateZurichBranchViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Create Zürich Branch via LLM Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Erstelle eine neue Filiale: Name '{BranchZurich.Name}', Adresse '{BranchZurich.Address}', Telefon '{BranchZurich.Phone}', Email '{BranchZurich.Email}'");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var branchId = ExtractBranchIdFromResponse(response);
            if (!string.IsNullOrEmpty(branchId))
                CreatedBranchIds.Add(branchId);

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Zürich branch created via chat (ID: {branchId})");
        }

        [Test]
        [Order(5)]
        public async Task Step5_ValidateLausanneAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Validate Lausanne Address via Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Validiere die Adresse: Place de la Gare 1, 1003 Lausanne, Schweiz");
            var response = await WaitForBotResponse(_messageCountBefore, 90000);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with address validation result");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Lausanne address validated successfully");
        }

        [Test]
        [Order(6)]
        public async Task Step6_CreateLausanneBranchViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Create Lausanne Branch via LLM Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage($"Erstelle eine neue Filiale: Name '{BranchLausanne.Name}', Adresse '{BranchLausanne.Address}', Telefon '{BranchLausanne.Phone}', Email '{BranchLausanne.Email}'");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond");

            var branchId = ExtractBranchIdFromResponse(response);
            if (!string.IsNullOrEmpty(branchId))
                CreatedBranchIds.Add(branchId);

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Lausanne branch created via chat (ID: {branchId})");
        }

        [Test]
        [Order(7)]
        public async Task Step7_VerifyBothBranchesViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Verify Both Branches via LLM Chat ===");
            await EnsureChatOpen();

            // Act
            _messageCountBefore = await GetMessageCount();
            await SendChatMessage("Liste alle Filialen auf");
            var response = await WaitForBotResponse(_messageCountBefore);

            // Assert
            TestContext.Out.WriteLine($"Bot response: {response}");
            Assert.That(response, Is.Not.Empty, "Bot should respond with branch list");

            var hasZurich = response.Contains(BranchZurich.Name, StringComparison.OrdinalIgnoreCase)
                || response.Contains("Zürich", StringComparison.OrdinalIgnoreCase);
            var hasLausanne = response.Contains(BranchLausanne.Name, StringComparison.OrdinalIgnoreCase)
                || response.Contains("Lausanne", StringComparison.OrdinalIgnoreCase);

            TestContext.Out.WriteLine($"  Zürich found: {hasZurich}");
            TestContext.Out.WriteLine($"  Lausanne found: {hasLausanne}");

            Assert.That(hasZurich, Is.True, $"Response should contain Zürich branch. Got: {response}");
            Assert.That(hasLausanne, Is.True, $"Response should contain Lausanne branch. Got: {response}");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Both branches verified via chat");
        }

        [Test]
        [Order(8)]
        public async Task Step8_DeleteBothBranchesViaChat()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Delete Both Branches via LLM Chat ===");

            if (CreatedBranchIds.Count == 0)
            {
                TestContext.Out.WriteLine("No branches to delete - skipping");
                Assert.Inconclusive("No branches were created in previous steps");
                return;
            }

            await EnsureChatOpen();

            // Act
            foreach (var branchId in CreatedBranchIds.ToList())
            {
                _messageCountBefore = await GetMessageCount();
                await SendChatMessage($"Lösche die Filiale mit ID {branchId}");
                var response = await WaitForBotResponse(_messageCountBefore);

                TestContext.Out.WriteLine($"Delete response for {branchId}: {response}");
                Assert.That(response, Is.Not.Empty, $"Bot should respond for branch {branchId}");

                var hasConfirmation = response.Contains("gelöscht", StringComparison.OrdinalIgnoreCase)
                    || response.Contains("erfolgreich", StringComparison.OrdinalIgnoreCase)
                    || response.Contains("deleted", StringComparison.OrdinalIgnoreCase);
                Assert.That(hasConfirmation, Is.True,
                    $"Response should confirm deletion of branch {branchId}. Got: {response}");
            }

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"All {CreatedBranchIds.Count} test branches deleted via chat");
            CreatedBranchIds.Clear();
        }

        #region Helper Methods

        private static string? ExtractBranchIdFromResponse(string response)
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim().TrimStart('-', '*', ' ');

                if (trimmed.Contains("branchId", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("ID:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var id = parts[1].Trim().Trim('`', '\'', '"', '*', ' ');
                        if (!string.IsNullOrEmpty(id) && id.Contains('-'))
                            return id;
                    }
                }
            }

            return null;
        }

        private async Task EnsureChatOpen()
        {
            var chatInput = await Actions.FindElementById(ChatInput);
            if (chatInput == null)
            {
                await Actions.ClickButtonById(HeaderAssistantButton);
                await Actions.Wait1000();
            }

            await WaitForChatInputEnabled();
        }

        private async Task CloseChatIfOpen()
        {
            var aside = await Actions.QuerySelector("app-aside.visible");
            if (aside != null)
            {
                TestContext.Out.WriteLine("Closing chat aside panel");
                await Actions.ClickButtonById(HeaderAssistantButton);
                await Actions.Wait1000();

                var stillVisible = await Actions.QuerySelector("app-aside.visible");
                if (stillVisible != null)
                {
                    TestContext.Out.WriteLine("Aside still visible, retrying");
                    await Actions.ClickButtonById(HeaderAssistantButton);
                    await Actions.Wait1000();
                }
            }
        }

        private async Task WaitForChatInputEnabled()
        {
            var maxRetries = 3;

            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                var isEnabled = await WaitForInputEnabled(10000);
                if (isEnabled)
                    return;

                TestContext.Out.WriteLine($"Chat input disabled (attempt {attempt + 1}/{maxRetries}), refreshing page...");
                await Actions.Reload();
                await Actions.Wait2000();

                await Actions.ClickButtonById(HeaderAssistantButton);
                await Actions.Wait1000();
            }

            Assert.Fail("Chat input remained disabled after multiple refresh attempts");
        }

        private async Task<bool> WaitForInputEnabled(int timeoutMs)
        {
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                var chatInput = await Actions.FindElementById(ChatInput);
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

        private async Task SendChatMessage(string message)
        {
            TestContext.Out.WriteLine($"Sending message: {message}");
            await Actions.FillInputWithDispatch(ChatInput, message);
            await Actions.ClickButtonById(ChatSendBtn);
        }

        private async Task<int> GetMessageCount()
        {
            var messages = await Actions.QuerySelectorAll($"#{ChatMessages} .message-wrapper.assistant");
            return messages.Count;
        }

        private async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = 60000)
        {
            TestContext.Out.WriteLine("Waiting for bot response...");

            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);

            while (DateTime.UtcNow - startTime < timeout)
            {
                var typingIndicator = await Actions.QuerySelector($"#{ChatMessages} .typing-indicator");
                var currentMessages = await Actions.QuerySelectorAll($"#{ChatMessages} .message-wrapper.assistant");

                if (typingIndicator == null && currentMessages.Count > previousMessageCount)
                {
                    var lastMessage = currentMessages[currentMessages.Count - 1];
                    var messageText = await Actions.QueryChildSelector(lastMessage, ".message-text");
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

                await Actions.Wait500();
            }

            Assert.Fail($"Bot did not respond within {timeoutMs / 1000}s");
            return string.Empty;
        }

        #endregion
    }
}
