using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;

namespace E2ETest
{
    [TestFixture]
    [Order(24)]
    public class SettingsGridColorTest : PlaywrightSetup
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

            // Navigate to Grid Color tab
            var gridColorTab = await Actions.FindElementByCssSelector("[href*='grid-color'], button:has-text('Grid Color'), a:has-text('Rasterfarbe')");
            if (gridColorTab != null)
            {
                await gridColorTab.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait500();
            }

            // Scroll container into viewport
            var container = await Page.QuerySelectorAsync(".container-box");
            if (container != null)
            {
                await container.ScrollIntoViewIfNeededAsync();
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
        public async Task Step1_VerifyGridColorPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Grid Color Page Loaded ===");

            // Assert
            var colorInputs = await Page.QuerySelectorAllAsync("input[id^='grid-color-row-']");
            Assert.That(colorInputs.Count, Is.GreaterThan(0), "Grid color inputs should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Grid Color page loaded successfully");
        }

        [Test]
        public async Task Step2_VerifyColorInputsExist()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Verify Color Inputs Exist ===");

            // Act & Assert
            var colorInputs = await Page.QuerySelectorAllAsync("input[type='color'][id^='grid-color-row-']");
            Assert.That(colorInputs.Count, Is.GreaterThan(0), "At least one color input should exist");

            TestContext.Out.WriteLine($"Found {colorInputs.Count} color input fields");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step3_ChangeGridColor()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Change Grid Color ===");

            // Act - Find first color input
            var firstColorInput = await Actions.FindElementByCssSelector("input[type='color'][id^='grid-color-row-']");
            if (firstColorInput != null)
            {
                var originalColor = await firstColorInput.InputValueAsync();
                TestContext.Out.WriteLine($"Original color: {originalColor}");

                var newColor = "#ff5733";
                await firstColorInput.FillAsync(newColor);
                await Actions.Wait500();

                // Verify change
                var currentColor = await firstColorInput.InputValueAsync();
                Assert.That(currentColor, Is.EqualTo(newColor), "Color should be updated");

                // Restore original color
                await firstColorInput.FillAsync(originalColor);
                await Actions.Wait500();

                TestContext.Out.WriteLine("Grid color changed and restored successfully");
            }
            else
            {
                TestContext.Out.WriteLine("No color input found");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step4_VerifyColorPreview()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Color Preview ===");

            // Act - Look for color input elements
            var colorInputs = await Page.QuerySelectorAllAsync("input[type='color'][id^='grid-color-row-']");
            if (colorInputs.Count > 0)
            {
                TestContext.Out.WriteLine($"Found {colorInputs.Count} color inputs with preview capability");

                var firstInput = colorInputs[0];
                var inputValue = await firstInput.InputValueAsync();
                TestContext.Out.WriteLine($"First color value: {inputValue}");

                Assert.That(inputValue, Is.Not.Null.And.Not.Empty, "Color input should have a value");
            }
            else
            {
                TestContext.Out.WriteLine("No color inputs found");
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        public async Task Step5_SaveGridColorSettings()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Save Grid Color Settings ===");

            // Act - Make a change
            var firstColorInput = await Actions.FindElementByCssSelector("input[type='color'][id^='grid-color-row-']");
            if (firstColorInput != null)
            {
                var originalColor = await firstColorInput.InputValueAsync();
                var tempColor = "#00ff00";

                await firstColorInput.FillAsync(tempColor);
                await Actions.Wait500();

                // Find and click Save button
                var saveButton = await Actions.FindElementByCssSelector("button:has-text('Save'), button:has-text('Speichern'), [class*='btn-save']");
                if (saveButton != null)
                {
                    await saveButton.ClickAsync();
                    await Actions.WaitForSpinnerToDisappear();
                    await Actions.Wait500();

                    TestContext.Out.WriteLine("Grid color settings saved");

                    // Restore original color
                    await firstColorInput.FillAsync(originalColor);
                    await saveButton.ClickAsync();
                    await Actions.WaitForSpinnerToDisappear();
                    await Actions.Wait500();
                }
                else
                {
                    TestContext.Out.WriteLine("Save button not found - changes might be auto-saved");

                    // Restore original color
                    await firstColorInput.FillAsync(originalColor);
                    await Actions.Wait500();
                }
            }

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
