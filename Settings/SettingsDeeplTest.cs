using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsDeeplIds;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(75)]
    public class SettingsDeeplTest : PlaywrightSetup
    {
        private Listener _listener = null!;
        private static string _originalApiKey = string.Empty;

        private const string TestApiKey = "test-deepl-key-e2e";

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(Form);
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
        public async Task Step1_VerifyDeeplFormLoaded()
        {
            TestContext.Out.WriteLine("=== Step 1: Verify DeepL Form Loaded ===");

            var form = await Actions.FindElementById(Form);
            Assert.That(form, Is.Not.Null, "DeepL form should be visible");

            var header = await Actions.FindElementById(Header);
            Assert.That(header, Is.Not.Null, "DeepL header should be visible");

            var apiKeyInput = await Actions.FindElementById(ApiKey);
            Assert.That(apiKeyInput, Is.Not.Null, "DeepL API key input should be visible");

            var toggleBtn = await Actions.FindElementById(ApiKeyToggle);
            Assert.That(toggleBtn, Is.Not.Null, "DeepL API key toggle should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("DeepL form loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_ToggleApiKeyVisibility()
        {
            TestContext.Out.WriteLine("=== Step 2: Toggle API Key Visibility ===");

            var apiKeyInput = await Actions.FindElementById(ApiKey);
            Assert.That(apiKeyInput, Is.Not.Null, "API key input should exist");

            var initialType = await apiKeyInput!.GetAttributeAsync("type");
            TestContext.Out.WriteLine($"Initial input type: {initialType}");
            Assert.That(initialType, Is.EqualTo("password"), "API key input should initially be password type");

            await Actions.ClickElementById(ApiKeyToggle);
            await Actions.Wait500();

            var apiKeyInputAfterToggle = await Actions.FindElementById(ApiKey);
            var typeAfterToggle = await apiKeyInputAfterToggle!.GetAttributeAsync("type");
            TestContext.Out.WriteLine($"Type after toggle: {typeAfterToggle}");
            Assert.That(typeAfterToggle, Is.EqualTo("text"), "API key input should be text type after toggle");

            await Actions.ClickElementById(ApiKeyToggle);
            await Actions.Wait500();

            var apiKeyInputRestored = await Actions.FindElementById(ApiKey);
            var restoredType = await apiKeyInputRestored!.GetAttributeAsync("type");
            TestContext.Out.WriteLine($"Type after restore: {restoredType}");
            Assert.That(restoredType, Is.EqualTo("password"), "API key input should be password type again");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("API key visibility toggle works correctly");
        }

        [Test]
        [Order(3)]
        public async Task Step3_SetTestApiKeyAndVerifyPersistence()
        {
            TestContext.Out.WriteLine("=== Step 3: Set Test API Key and Verify Persistence ===");

            await Actions.ClickElementById(ApiKeyToggle);
            await Actions.Wait500();

            var apiKeyInput = await Actions.FindElementById(ApiKey);
            Assert.That(apiKeyInput, Is.Not.Null, "API key input should exist");
            _originalApiKey = await apiKeyInput!.EvaluateAsync<string>("el => el.value") ?? string.Empty;
            TestContext.Out.WriteLine($"Original API key length: {_originalApiKey.Length}");

            await Actions.FillInputById(ApiKey, TestApiKey);
            await Actions.Wait500();

            await Actions.PressKey(Keys.Tab);
            TestContext.Out.WriteLine("Set test API key and triggered blur");
            await Actions.Wait2000();

            await Actions.Reload();
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.ScrollIntoViewById(Form);
            await Actions.Wait500();

            await Actions.ClickElementById(ApiKeyToggle);
            await Actions.Wait500();

            var apiKeyInputAfterReload = await Actions.FindElementById(ApiKey);
            Assert.That(apiKeyInputAfterReload, Is.Not.Null, "API key input should exist after reload");
            var persistedValue = await apiKeyInputAfterReload!.EvaluateAsync<string>("el => el.value") ?? string.Empty;
            TestContext.Out.WriteLine($"Persisted API key value: {persistedValue}");

            Assert.That(persistedValue, Is.Not.Empty, "API key should have a persisted value after reload");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("API key persisted successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_RestoreOriginalApiKey()
        {
            TestContext.Out.WriteLine("=== Step 4: Restore Original API Key ===");

            await Actions.ClickElementById(ApiKeyToggle);
            await Actions.Wait500();

            await Actions.FillInputById(ApiKey, _originalApiKey);
            await Actions.Wait500();

            await Actions.PressKey(Keys.Tab);
            TestContext.Out.WriteLine("Restored original API key and triggered blur");
            await Actions.Wait2000();

            await Actions.ClickElementById(ApiKeyToggle);
            await Actions.Wait500();

            var apiKeyInput = await Actions.FindElementById(ApiKey);
            var restoredValue = await apiKeyInput!.EvaluateAsync<string>("el => el.value") ?? string.Empty;
            TestContext.Out.WriteLine($"Restored API key length: {restoredValue.Length}");

            Assert.That(restoredValue.Length, Is.EqualTo(_originalApiKey.Length),
                "Restored API key should have the same length as original");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Original API key restored successfully");
        }
    }
}
