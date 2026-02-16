using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsSchedulingDefaultsIds;

namespace Klacks.E2ETest;

[TestFixture]
[Order(72)]
public class SettingsSchedulingDefaultsTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private static string _originalDefaultWorkingHours = string.Empty;
    private static string _originalMaximumHours = string.Empty;
    private static string _originalMaxWorkDays = string.Empty;

    private const string TestDefaultWorkingHours = "9";
    private const string TestMaximumHours = "210";
    private const string TestMaxWorkDays = "6";

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(Container);
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
    public async Task Step1_VerifySchedulingDefaultsLoaded()
    {
        TestContext.Out.WriteLine("=== Step 1: Verify Scheduling Defaults Loaded ===");

        var header = await Actions.FindElementById(Header);
        Assert.That(header, Is.Not.Null, "Scheduling defaults header should be visible");

        var form = await Actions.FindElementById(Form);
        Assert.That(form, Is.Not.Null, "Scheduling defaults form should be visible");

        var defaultWorkingHoursInput = await Actions.FindElementById(DefaultWorkingHours);
        Assert.That(defaultWorkingHoursInput, Is.Not.Null, "Default working hours input should be visible");

        var maxWorkDaysInput = await Actions.FindElementById(MaxWorkDays);
        Assert.That(maxWorkDaysInput, Is.Not.Null, "Max work days input should be visible");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Scheduling Defaults section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_ReadAndStoreOriginalValues()
    {
        TestContext.Out.WriteLine("=== Step 2: Read and Store Original Values ===");

        _originalDefaultWorkingHours = await Actions.ReadInput(DefaultWorkingHours);
        TestContext.Out.WriteLine($"Original DefaultWorkingHours: {_originalDefaultWorkingHours}");

        _originalMaximumHours = await Actions.ReadInput(MaximumHours);
        TestContext.Out.WriteLine($"Original MaximumHours: {_originalMaximumHours}");

        _originalMaxWorkDays = await Actions.ReadInput(MaxWorkDays);
        TestContext.Out.WriteLine($"Original MaxWorkDays: {_originalMaxWorkDays}");

        Assert.That(_originalDefaultWorkingHours, Is.Not.Empty, "DefaultWorkingHours should have a value");
        Assert.That(_originalMaximumHours, Is.Not.Empty, "MaximumHours should have a value");
        Assert.That(_originalMaxWorkDays, Is.Not.Empty, "MaxWorkDays should have a value");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Original values stored successfully");
    }

    [Test]
    [Order(3)]
    public async Task Step3_ChangeValuesAndSave()
    {
        TestContext.Out.WriteLine("=== Step 3: Change Values and Save ===");

        await Actions.FillInputById(DefaultWorkingHours, TestDefaultWorkingHours);
        await Actions.PressKey(Keys.Tab);
        await Actions.Wait500();

        await Actions.FillInputById(MaximumHours, TestMaximumHours);
        await Actions.PressKey(Keys.Tab);
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(MaxWorkDays);
        await Actions.Wait500();

        await Actions.FillInputById(MaxWorkDays, TestMaxWorkDays);
        await Actions.PressKey(Keys.Tab);
        await Actions.Wait500();

        var dwh = await Actions.ReadInput(DefaultWorkingHours);
        var mh = await Actions.ReadInput(MaximumHours);
        var mwd = await Actions.ReadInput(MaxWorkDays);
        TestContext.Out.WriteLine($"UI values - DWH: {dwh}, MH: {mh}, MWD: {mwd}");

        Assert.That(dwh, Is.EqualTo(TestDefaultWorkingHours), "DefaultWorkingHours UI should show new value");
        Assert.That(mh, Is.EqualTo(TestMaximumHours), "MaximumHours UI should show new value");
        Assert.That(mwd, Is.EqualTo(TestMaxWorkDays), "MaxWorkDays UI should show new value");

        await Actions.SaveSettingViaApi("defaultWorkingHours", TestDefaultWorkingHours);
        await Actions.SaveSettingViaApi("maximumHours", TestMaximumHours);
        await Actions.SaveSettingViaApi("SCHEDULING_MAX_WORK_DAYS", TestMaxWorkDays);
        TestContext.Out.WriteLine("Settings saved via API");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Values changed and saved successfully");
    }

    [Test]
    [Order(4)]
    public async Task Step4_VerifyPersistenceViaApi()
    {
        TestContext.Out.WriteLine("=== Step 4: Verify Persistence via API ===");

        var dwhValue = await Actions.ReadSettingViaApi("defaultWorkingHours");
        TestContext.Out.WriteLine($"API DefaultWorkingHours: {dwhValue}");
        Assert.That(dwhValue, Is.EqualTo(TestDefaultWorkingHours),
            $"DefaultWorkingHours should be '{TestDefaultWorkingHours}' in DB");

        var mhValue = await Actions.ReadSettingViaApi("maximumHours");
        TestContext.Out.WriteLine($"API MaximumHours: {mhValue}");
        Assert.That(mhValue, Is.EqualTo(TestMaximumHours),
            $"MaximumHours should be '{TestMaximumHours}' in DB");

        var mwdValue = await Actions.ReadSettingViaApi("SCHEDULING_MAX_WORK_DAYS");
        TestContext.Out.WriteLine($"API MaxWorkDays: {mwdValue}");
        Assert.That(mwdValue, Is.EqualTo(TestMaxWorkDays),
            $"MaxWorkDays should be '{TestMaxWorkDays}' in DB");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("All values persisted correctly in DB");
    }

    [Test]
    [Order(5)]
    public async Task Step5_RestoreOriginalValues()
    {
        TestContext.Out.WriteLine("=== Step 5: Restore Original Values ===");

        if (string.IsNullOrEmpty(_originalDefaultWorkingHours) ||
            string.IsNullOrEmpty(_originalMaximumHours) ||
            string.IsNullOrEmpty(_originalMaxWorkDays))
        {
            TestContext.Out.WriteLine("Original values not available - skipping restore");
            Assert.Inconclusive("Original values were not stored in Step 2");
            return;
        }

        await Actions.SaveSettingViaApi("defaultWorkingHours", _originalDefaultWorkingHours);
        await Actions.SaveSettingViaApi("maximumHours", _originalMaximumHours);
        await Actions.SaveSettingViaApi("SCHEDULING_MAX_WORK_DAYS", _originalMaxWorkDays);
        TestContext.Out.WriteLine("Original values restored via API");

        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(Container);
        await Actions.Wait1000();

        var restoredDefaultWorkingHours = await Actions.ReadInput(DefaultWorkingHours);
        var restoredMaximumHours = await Actions.ReadInput(MaximumHours);

        await Actions.ScrollIntoViewById(MaxWorkDays);
        await Actions.Wait500();
        var restoredMaxWorkDays = await Actions.ReadInput(MaxWorkDays);

        TestContext.Out.WriteLine($"Restored values - DWH: {restoredDefaultWorkingHours}, MH: {restoredMaximumHours}, MWD: {restoredMaxWorkDays}");

        Assert.That(restoredDefaultWorkingHours, Is.EqualTo(_originalDefaultWorkingHours),
            "DefaultWorkingHours should be restored");
        Assert.That(restoredMaximumHours, Is.EqualTo(_originalMaximumHours),
            "MaximumHours should be restored");
        Assert.That(restoredMaxWorkDays, Is.EqualTo(_originalMaxWorkDays),
            "MaxWorkDays should be restored");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("All original values restored successfully");
    }
}
