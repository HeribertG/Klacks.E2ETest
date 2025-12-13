using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using static E2ETest.Constants.SettingsGridColorIds;

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

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(GridColorSection);
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
        public async Task Step1_VerifyGridColorPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Grid Color Page Loaded ===");

            // Assert
            var header = await Actions.FindElementById(GridColorHeader);
            Assert.That(header, Is.Not.Null, "Grid color header should be visible");

            var colorBox = await Actions.FindElementById(GridColorBox);
            Assert.That(colorBox, Is.Not.Null, "Grid color box should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Grid Color page loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_VerifyColorInputsExist()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Verify Color Inputs Exist ===");

            // Assert - Check that key color inputs exist
            var backgroundColorInput = await Actions.FindElementById($"{RowInputPrefix}{BackgroundColorKey}");
            var saturdayColorInput = await Actions.FindElementById($"{RowInputPrefix}{BackgroundColorSaturdayKey}");
            var sundayColorInput = await Actions.FindElementById($"{RowInputPrefix}{BackgroundColorSundayKey}");
            var holidayColorInput = await Actions.FindElementById($"{RowInputPrefix}{BackgroundColorHolidayKey}");

            Assert.That(backgroundColorInput, Is.Not.Null, "Background color input should exist");
            Assert.That(saturdayColorInput, Is.Not.Null, "Saturday color input should exist");
            Assert.That(sundayColorInput, Is.Not.Null, "Sunday color input should exist");
            Assert.That(holidayColorInput, Is.Not.Null, "Holiday color input should exist");

            TestContext.Out.WriteLine("All key color inputs exist");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_ChangeColorAndVerifyAutoSave()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Change Color and Verify AutoSave ===");
            var colorInputId = $"{RowInputPrefix}{BackgroundColorKey}";

            // Act - Get original color
            var colorInput = await Actions.FindElementById(colorInputId);
            Assert.That(colorInput, Is.Not.Null, "Color input should exist");

            var originalColor = await colorInput!.InputValueAsync();
            TestContext.Out.WriteLine($"Original color: {originalColor}");

            // Change to test color
            await colorInput.FillAsync(TestColor);
            await Actions.Wait500();

            // Trigger change event
            await Actions.ClickElementById(GridColorHeader);

            // Wait for autoSave (800ms timeout + buffer)
            TestContext.Out.WriteLine("Waiting for autoSave (1500ms)...");
            await Actions.Wait1500();

            // Re-find element after Angular re-render
            colorInput = await Actions.FindElementById(colorInputId);
            Assert.That(colorInput, Is.Not.Null, "Color input should still exist after save");

            // Verify change was applied
            var newColor = await colorInput!.InputValueAsync();
            Assert.That(newColor, Is.EqualTo(TestColor), "Color should be changed");
            TestContext.Out.WriteLine($"Color changed to: {newColor}");

            // Restore original color
            await colorInput.FillAsync(originalColor);
            await Actions.ClickElementById(GridColorHeader);
            await Actions.Wait1500();

            // Re-find element again after restore
            colorInput = await Actions.FindElementById(colorInputId);
            var restoredColor = await colorInput!.InputValueAsync();
            Assert.That(restoredColor, Is.EqualTo(originalColor), "Color should be restored");
            TestContext.Out.WriteLine($"Color restored to: {restoredColor}");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        [Order(4)]
        public async Task Step4_VerifyAllColorInputsHaveValues()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify All Color Inputs Have Values ===");

            // Act - Find all color inputs
            var colorInputs = await Page.QuerySelectorAllAsync($"input[type='color'][id^='{RowInputPrefix}']");
            Assert.That(colorInputs.Count, Is.GreaterThan(0), "At least one color input should exist");
            TestContext.Out.WriteLine($"Found {colorInputs.Count} color inputs");

            // Assert - All inputs should have valid color values
            foreach (var input in colorInputs)
            {
                var value = await input.InputValueAsync();
                var id = await input.GetAttributeAsync("id");
                Assert.That(value, Does.Match(@"^#[0-9A-Fa-f]{6}$"), $"Color input {id} should have valid hex color");
            }

            TestContext.Out.WriteLine("All color inputs have valid hex color values");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }
    }
}
