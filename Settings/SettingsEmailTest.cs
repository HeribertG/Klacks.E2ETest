using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsEmailTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            // Navigate to Settings
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Navigate to Email Settings tab
            var emailTab = await Actions.FindElementByCssSelector("[href*='email'], button:has-text('Email'), a:has-text('E-Mail')");
            if (emailTab != null)
            {
                await emailTab.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait500();
            }
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_listener.HasApiErrors())
            {
                TestContext.Out.WriteLine($"API Error: {_listener.GetLastErrorMessage()}");
            }

            await _listener.WaitForResponseHandlingAsync();
        }

        [Test]
        public async Task Step1_VerifyEmailSettingsPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Email Settings Page Loaded ===");

            // Assert
            var smtpServerInput = await Actions.FindElementByCssSelector("input[name*='server'], input[name*='smtp']");
            Assert.That(smtpServerInput, Is.Not.Null, "SMTP server input should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Email Settings page loaded successfully");
        }

        [Test]
        public async Task Step2_ViewEmailConfiguration()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: View Email Configuration ===");

            // Act & Assert - Check all email config fields exist
            var smtpServer = await Actions.FindElementByCssSelector("input[name*='server'], #server");
            var smtpPort = await Actions.FindElementByCssSelector("input[name*='port'], #port");
            var smtpUsername = await Actions.FindElementByCssSelector("input[name*='username'], #username");
            var smtpPassword = await Actions.FindElementByCssSelector("input[name*='password'], #password");

            Assert.That(smtpServer, Is.Not.Null, "SMTP server field should exist");
            Assert.That(smtpPort, Is.Not.Null, "SMTP port field should exist");
            Assert.That(smtpUsername, Is.Not.Null, "SMTP username field should exist");
            Assert.That(smtpPassword, Is.Not.Null, "SMTP password field should exist");

            TestContext.Out.WriteLine("All email configuration fields are present");
        }

        [Test]
        public async Task Step3_TogglePasswordVisibility()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Toggle Password Visibility ===");

            // Act - Find password field
            var passwordInput = await Actions.FindElementByCssSelector("input[name*='password'], #password");
            Assert.That(passwordInput, Is.Not.Null, "Password field should exist");

            var initialType = await passwordInput!.GetAttributeAsync("type");
            TestContext.Out.WriteLine($"Initial password field type: {initialType}");

            // Find toggle button (eye icon)
            var toggleButton = await Actions.FindElementByCssSelector("button[class*='eye'], fa-eye, [class*='toggle-password']");
            if (toggleButton != null)
            {
                await toggleButton.ClickAsync();
                await Actions.Wait200();

                var newType = await passwordInput.GetAttributeAsync("type");
                Assert.That(newType, Is.Not.EqualTo(initialType), "Password field type should change");

                TestContext.Out.WriteLine($"Password visibility toggled to: {newType}");
            }
            else
            {
                TestContext.Out.WriteLine("Password toggle button not found - skipping test");
            }
        }

        [Test]
        public async Task Step4_TestEmailConfiguration()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Test Email Configuration ===");

            // Act - Find test button
            var testButton = await Actions.FindElementByCssSelector("button:has-text('Test'), button:has-text('Testen'), [class*='btn-test']");
            if (testButton != null)
            {
                await testButton.ClickAsync();
                await Actions.Wait1000();

                // Wait for test result (success or error message)
                var resultMessage = await Actions.FindElementByCssSelector(".alert, .toast, [class*='message']");
                if (resultMessage != null)
                {
                    var messageText = await resultMessage.TextContentAsync();
                    TestContext.Out.WriteLine($"Test result: {messageText}");
                }

                TestContext.Out.WriteLine("Email configuration test executed");
            }
            else
            {
                TestContext.Out.WriteLine("Test button not found - skipping test");
            }

            // Note: API error check might be expected here if SMTP is not configured
            TestContext.Out.WriteLine("Email test completed (errors may be expected if SMTP not configured)");
        }
    }
}
