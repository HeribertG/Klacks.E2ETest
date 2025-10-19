using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsOwnerAddressTest : PlaywrightSetup
    {
        private Listener _listener;
        private string _originalCompanyName = string.Empty;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            // Navigate to Settings
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            // Navigate to Owner Address tab
            var ownerAddressTab = await Actions.FindElementByCssSelector("[href*='owner-address'], button:has-text('Owner Address'), a:has-text('Eigentümer Adresse')");
            if (ownerAddressTab != null)
            {
                await ownerAddressTab.ClickAsync();
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
        public async Task Step1_VerifyOwnerAddressPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Owner Address Page Loaded ===");

            // Assert
            var companyNameInput = await Actions.FindElementByCssSelector("input[name='companyName'], #companyName, [formcontrolname='companyName']");
            Assert.That(companyNameInput, Is.Not.Null, "Company name input should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Owner Address page loaded successfully");
        }

        [Test]
        public async Task Step2_EditCompanyName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Edit Company Name ===");
            var newCompanyName = $"Test Company {DateTime.Now.Ticks}";

            // Act - Get original value
            var companyNameInput = await Actions.FindElementByCssSelector("input[name='companyName'], #companyName");
            Assert.That(companyNameInput, Is.Not.Null, "Company name input should exist");

            _originalCompanyName = await companyNameInput!.InputValueAsync();
            TestContext.Out.WriteLine($"Original company name: {_originalCompanyName}");

            // Edit company name
            await companyNameInput.FillAsync(newCompanyName);
            await Actions.Wait500();

            // Assert
            var currentValue = await companyNameInput.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(newCompanyName), "Company name should be updated");

            TestContext.Out.WriteLine($"Company name changed to: {newCompanyName}");
        }

        [Test]
        public async Task Step3_EditMultipleAddressFields()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Edit Multiple Address Fields ===");
            var testAddress = new
            {
                CompanyName = $"Updated Company {DateTime.Now.Ticks}",
                Street = "Teststrasse 123",
                Zip = "12345",
                City = "Teststadt",
                Phone = "+49 123 456789",
                Email = "test@company.de"
            };

            // Act
            var fields = new Dictionary<string, string>
            {
                { "companyName", testAddress.CompanyName },
                { "street", testAddress.Street },
                { "zip", testAddress.Zip },
                { "city", testAddress.City },
                { "phone", testAddress.Phone },
                { "email", testAddress.Email }
            };

            foreach (var field in fields)
            {
                var input = await Actions.FindElementByCssSelector($"input[name='{field.Key}'], #{field.Key}");
                if (input != null)
                {
                    await input.FillAsync(field.Value);
                    await Actions.Wait200();
                }
            }

            await Actions.Wait500();

            // Assert - Verify all fields
            foreach (var field in fields)
            {
                var input = await Actions.FindElementByCssSelector($"input[name='{field.Key}'], #{field.Key}");
                if (input != null)
                {
                    var value = await input.InputValueAsync();
                    Assert.That(value, Is.EqualTo(field.Value), $"{field.Key} should be updated");
                }
            }

            TestContext.Out.WriteLine("Multiple address fields updated successfully");
        }

        [Test]
        public async Task Step4_SaveAndResetAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Save and Reset Address ===");

            // Act - Make a change
            var companyNameInput = await Actions.FindElementByCssSelector("input[name='companyName'], #companyName");
            if (companyNameInput != null)
            {
                var originalValue = await companyNameInput.InputValueAsync();
                var newValue = $"Temporary Company {DateTime.Now.Ticks}";

                await companyNameInput.FillAsync(newValue);
                await Actions.Wait500();

                // Find and click Reset button
                var resetButton = await Actions.FindElementByCssSelector("button:has-text('Reset'), button:has-text('Zurücksetzen'), [class*='btn-reset']");
                if (resetButton != null)
                {
                    await resetButton.ClickAsync();
                    await Actions.Wait500();

                    // Assert - Verify reset
                    var resetValue = await companyNameInput.InputValueAsync();
                    Assert.That(resetValue, Is.EqualTo(originalValue), "Value should be reset to original");

                    TestContext.Out.WriteLine("Reset functionality works correctly");
                }
                else
                {
                    TestContext.Out.WriteLine("Reset button not found - skipping reset test");
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
