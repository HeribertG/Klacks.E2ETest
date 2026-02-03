using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsEmailIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(27)]
    public class SettingsEmailTest : PlaywrightSetup
    {
        private Listener _listener;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(EmailSection);
            await Actions.Wait500();
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
        public async Task Step1_VerifyEmailSettingsPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Email Settings Page Loaded ===");

            // Assert
            var smtpServerInput = await Actions.FindElementById(OutgoingServer);
            Assert.That(smtpServerInput, Is.Not.Null, "SMTP server input should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Email Settings page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_VerifyAllEmailConfigFieldsExist()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Verify All Email Config Fields Exist ===");

            // Assert - Check all email config fields exist
            var smtpServer = await Actions.FindElementById(OutgoingServer);
            var smtpPort = await Actions.FindElementById(OutgoingServerPort);
            var smtpTimeout = await Actions.FindElementById(OutgoingServerTimeout);
            var enabledSsl = await Actions.FindElementById(EnabledSSL);
            var authType = await Actions.FindElementById(AuthenticationType);
            var smtpUsername = await Actions.FindElementById(SmtpAuthUser);
            var smtpPassword = await Actions.FindElementById(SmtpAuthKey);

            Assert.That(smtpServer, Is.Not.Null, "SMTP server field should exist");
            Assert.That(smtpPort, Is.Not.Null, "SMTP port field should exist");
            Assert.That(smtpTimeout, Is.Not.Null, "SMTP timeout field should exist");
            Assert.That(enabledSsl, Is.Not.Null, "Enabled SSL field should exist");
            Assert.That(authType, Is.Not.Null, "Authentication type field should exist");
            Assert.That(smtpUsername, Is.Not.Null, "SMTP username field should exist");
            Assert.That(smtpPassword, Is.Not.Null, "SMTP password field should exist");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("All email configuration fields are present");
        }

        [Test]
        [Order(3)]
        public async Task Step3_TogglePasswordVisibility()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Toggle Password Visibility ===");

            // Act - Get initial password field type
            var passwordInput = await Actions.FindElementById(SmtpAuthKey);
            Assert.That(passwordInput, Is.Not.Null, "Password field should exist");

            var initialType = await passwordInput!.GetAttributeAsync("type");
            TestContext.Out.WriteLine($"Initial password field type: {initialType}");
            Assert.That(initialType, Is.EqualTo("password"), "Password field should initially be hidden");

            // Click toggle button
            await Actions.ClickElementById(PasswordToggle);
            await Actions.Wait500();

            // Assert - Password should now be visible (re-fetch element after DOM update)
            var passwordInputAfterToggle = await Actions.FindElementById(SmtpAuthKey);
            var newType = await passwordInputAfterToggle!.GetAttributeAsync("type");
            Assert.That(newType, Is.EqualTo("text"), "Password field should be visible after toggle");
            TestContext.Out.WriteLine($"Password visibility toggled to: {newType}");

            // Toggle back
            await Actions.ClickElementById(PasswordToggle);
            await Actions.Wait500();

            var passwordInputRestored = await Actions.FindElementById(SmtpAuthKey);
            var restoredType = await passwordInputRestored!.GetAttributeAsync("type");
            Assert.That(restoredType, Is.EqualTo("password"), "Password field should be hidden again");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Password visibility toggle works correctly");
        }

        [Test]
        [Order(4)]
        public async Task Step4_SendTestEmailAndVerifySuccess()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Send Test Email and Verify Success ===");

            // Act - Click test email button
            var testButton = await Actions.FindElementById(TestButton);
            Assert.That(testButton, Is.Not.Null, "Test email button should exist");

            await Actions.ClickElementById(TestButton);
            TestContext.Out.WriteLine("Test email button clicked, waiting for result...");

            // Wait for toast to appear (success or error)
            await Actions.Wait3000();

            // Assert - Check for success toast
            var successToast = await Page.QuerySelectorAsync("ngb-toast.bg-success");
            var errorToast = await Page.QuerySelectorAsync("ngb-toast.bg-danger");

            if (successToast != null)
            {
                var toastText = await successToast.TextContentAsync();
                TestContext.Out.WriteLine($"Success toast appeared: {toastText}");
                Assert.Pass("Test email sent successfully");
            }
            else if (errorToast != null)
            {
                var toastText = await errorToast.TextContentAsync();
                TestContext.Out.WriteLine($"Error toast appeared: {toastText}");

                if (toastText != null && toastText.Contains("Missing required fields"))
                {
                    Assert.Inconclusive("Test email skipped - email configuration is incomplete");
                }
                else
                {
                    Assert.Fail($"Test email failed: {toastText}");
                }
            }
            else
            {
                TestContext.Out.WriteLine("No toast appeared - checking if still loading");

                // Check if button is still in loading state
                var isDisabled = await testButton.IsDisabledAsync();
                if (isDisabled)
                {
                    TestContext.Out.WriteLine("Button still disabled - waiting longer...");
                    await Actions.Wait3000();

                    successToast = await Page.QuerySelectorAsync("ngb-toast.bg-success");
                    errorToast = await Page.QuerySelectorAsync("ngb-toast.bg-danger");

                    if (successToast != null)
                    {
                        TestContext.Out.WriteLine("Success toast appeared after longer wait");
                        Assert.Pass("Test email sent successfully");
                    }
                    else if (errorToast != null)
                    {
                        var toastText = await errorToast.TextContentAsync();
                        Assert.Fail($"Test email failed: {toastText}");
                    }
                }

                Assert.Fail("No response received from test email");
            }
        }
    }
}
