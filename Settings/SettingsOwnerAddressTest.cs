using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.SettingsOwnerAddressIds;
using static E2ETest.Constants.SettingsOwnerAddressTestData;

namespace E2ETest.Settings
{
    [TestFixture]
    public class SettingsOwnerAddressTest : PlaywrightSetup
    {
        private Listener _listener = null!;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            var ownerAddressForm = await Page.QuerySelectorAsync("#owner-address-form");
            if (ownerAddressForm != null)
            {
                await ownerAddressForm.ScrollIntoViewIfNeededAsync();
                await Actions.Wait500();
            }
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
        public async Task Step1_VerifyOwnerAddressPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Owner Address Page Loaded ===");

            // Assert
            var companyNameInput = await Actions.FindElementById(SettingOwnerAddressName);
            Assert.That(companyNameInput, Is.Not.Null, "Company name input should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Owner Address page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_SetOwnerAddressName()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Set Owner Address Name ===");

            // Act
            await Actions.FillInputAndEnterById(SettingOwnerAddressName, ExpectedOwnerName);
            await Actions.Wait500();

            // Assert
            var nameInput = await Actions.FindElementById(SettingOwnerAddressName);
            var currentValue = await nameInput!.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(ExpectedOwnerName), "Owner name should be set");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Owner name set to: {ExpectedOwnerName}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_SetOwnerAddressPhone()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Set Owner Address Phone ===");

            // Act
            await Actions.FillInputAndEnterById(SettingOwnerAddressTel, ExpectedPhone);
            await Actions.Wait500();

            // Assert
            var phoneInput = await Actions.FindElementById(SettingOwnerAddressTel);
            var currentValue = await phoneInput!.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(ExpectedPhone), "Phone should be set");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Phone set to: {ExpectedPhone}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_ClearOwnerAddressSupplement()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Clear Owner Address Supplement ===");

            // Act
            await Actions.FillInputAndEnterById(SettingOwnerAddressSupplement, ExpectedSupplement);
            await Actions.Wait500();

            // Assert
            var supplementInput = await Actions.FindElementById(SettingOwnerAddressSupplement);
            var currentValue = await supplementInput!.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(ExpectedSupplement), "Supplement should be empty");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Supplement cleared successfully");
        }

        [Test]
        [Order(5)]
        public async Task Step5_SetOwnerAddressEmail()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Set Owner Address Email ===");

            // Act
            await Actions.FillInputAndEnterById(SettingOwnerAddressEmail, ExpectedEmail);
            await Actions.Wait500();

            // Assert
            var emailInput = await Actions.FindElementById(SettingOwnerAddressEmail);
            var currentValue = await emailInput!.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(ExpectedEmail), "Email should be set");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Email set to: {ExpectedEmail}");
        }

        [Test]
        [Order(6)]
        public async Task Step6_SetOwnerAddressStreet()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Set Owner Address Street ===");

            // Act
            await Actions.FillInputAndEnterById(SettingOwnerAddressStreet, ExpectedStreet);
            await Actions.Wait500();

            // Assert
            var streetInput = await Actions.FindElementById(SettingOwnerAddressStreet);
            var currentValue = await streetInput!.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(ExpectedStreet), "Street should be set");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Street set to: {ExpectedStreet}");
        }

        [Test]
        [Order(7)]
        public async Task Step7_SetOwnerAddressZip()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Set Owner Address ZIP ===");

            // Act
            await Actions.FillInputAndEnterById(SettingOwnerAddressZip, ExpectedZip);
            await Actions.Wait500();

            // Assert
            var zipInput = await Actions.FindElementById(SettingOwnerAddressZip);
            var currentValue = await zipInput!.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(ExpectedZip), "ZIP should be set");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"ZIP set to: {ExpectedZip}");
        }

        [Test]
        [Order(8)]
        public async Task Step8_SetOwnerAddressCity()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Set Owner Address City ===");

            // Act
            await Actions.FillInputAndEnterById(SettingOwnerAddressCity, ExpectedCity);
            await Actions.Wait2000();

            // Assert
            var cityInput = await Actions.FindElementById(SettingOwnerAddressCity);
            var currentValue = await cityInput!.InputValueAsync();
            Assert.That(currentValue, Is.EqualTo(ExpectedCity), "City should be set");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"City set to: {ExpectedCity}");
            TestContext.Out.WriteLine("Waiting for data to be saved...");
        }

        [Test]
        [Order(9)]
        public async Task Step9_VerifyDataPersistence()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 9: Verify Data Persistence ===");

            // Act
            TestContext.Out.WriteLine("Navigating to Dashboard...");
            await Actions.ClickButtonById(MainNavIds.OpenDashboardId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            TestContext.Out.WriteLine("Navigating back to Settings...");
            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            var ownerAddressForm = await Page.QuerySelectorAsync("#owner-address-form");
            if (ownerAddressForm != null)
            {
                await ownerAddressForm.ScrollIntoViewIfNeededAsync();
                await Actions.Wait500();
            }

            // Assert
            TestContext.Out.WriteLine("Verifying all fields contain expected values...");

            var nameInput = await Actions.FindElementById(SettingOwnerAddressName);
            var nameValue = await nameInput!.InputValueAsync();
            Assert.That(nameValue, Is.EqualTo(ExpectedOwnerName), $"Owner name should be '{ExpectedOwnerName}'");
            TestContext.Out.WriteLine($"✓ Name: {nameValue}");

            var phoneInput = await Actions.FindElementById(SettingOwnerAddressTel);
            var phoneValue = await phoneInput!.InputValueAsync();
            Assert.That(phoneValue, Is.EqualTo(ExpectedPhone), $"Phone should be '{ExpectedPhone}'");
            TestContext.Out.WriteLine($"✓ Phone: {phoneValue}");

            var supplementInput = await Actions.FindElementById(SettingOwnerAddressSupplement);
            var supplementValue = await supplementInput!.InputValueAsync();
            Assert.That(supplementValue, Is.EqualTo(ExpectedSupplement), "Supplement should be empty");
            TestContext.Out.WriteLine($"✓ Supplement: (empty)");

            var emailInput = await Actions.FindElementById(SettingOwnerAddressEmail);
            var emailValue = await emailInput!.InputValueAsync();
            Assert.That(emailValue, Is.EqualTo(ExpectedEmail), $"Email should be '{ExpectedEmail}'");
            TestContext.Out.WriteLine($"✓ Email: {emailValue}");

            var streetInput = await Actions.FindElementById(SettingOwnerAddressStreet);
            var streetValue = await streetInput!.InputValueAsync();
            Assert.That(streetValue, Is.EqualTo(ExpectedStreet), $"Street should be '{ExpectedStreet}'");
            TestContext.Out.WriteLine($"✓ Street: {streetValue}");

            var zipInput = await Actions.FindElementById(SettingOwnerAddressZip);
            var zipValue = await zipInput!.InputValueAsync();
            Assert.That(zipValue, Is.EqualTo(ExpectedZip), $"ZIP should be '{ExpectedZip}'");
            TestContext.Out.WriteLine($"✓ ZIP: {zipValue}");

            var cityInput = await Actions.FindElementById(SettingOwnerAddressCity);
            var cityValue = await cityInput!.InputValueAsync();
            Assert.That(cityValue, Is.EqualTo(ExpectedCity), $"City should be '{ExpectedCity}'");
            TestContext.Out.WriteLine($"✓ City: {cityValue}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("All data persisted correctly!");
        }
    }
}
