using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsWorkSettingIds;

namespace Klacks.E2ETest;

[TestFixture]
[Order(70)]
public class SettingsWorkSettingTest : PlaywrightSetup
{
    private Listener _listener = null!;

    private static string _originalVacationDays = string.Empty;
    private static string _originalProbationPeriod = string.Empty;
    private static string _originalNoticePeriod = string.Empty;
    private static string _originalPaymentInterval = string.Empty;
    private static string _originalNightRate = string.Empty;
    private static string _originalHolidayRate = string.Empty;
    private static string _originalSaRate = string.Empty;
    private static string _originalSoRate = string.Empty;
    private static string _originalDayVisibleBefore = string.Empty;
    private static string _originalDayVisibleAfter = string.Empty;

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
    public async Task Step1_NavigateToSettingsAndVerifyWorkSettingLoaded()
    {
        TestContext.Out.WriteLine("=== Step 1: Navigate to Settings and verify WorkSetting loaded ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(Container);
        await Actions.Wait500();

        var header = await Actions.FindElementById(Header);
        Assert.That(header, Is.Not.Null, "WorkSetting header should be visible");

        var form = await Actions.FindElementById(Form);
        Assert.That(form, Is.Not.Null, "WorkSetting form should be visible");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("WorkSetting section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_ReadAllCurrentValues()
    {
        TestContext.Out.WriteLine("=== Step 2: Read all current values ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        await Actions.ScrollIntoViewById(Container);
        await Actions.Wait500();

        _originalVacationDays = await Actions.ReadInput(VacationDaysPerYear);
        _originalProbationPeriod = await Actions.ReadInput(ProbationPeriod);
        _originalNoticePeriod = await Actions.ReadInput(NoticePeriod);

        var paymentSelect = await Actions.FindElementById(PaymentInterval);
        Assert.That(paymentSelect, Is.Not.Null, "PaymentInterval select should exist");
        _originalPaymentInterval = await paymentSelect!.EvaluateAsync<string>("el => el.value") ?? "0";

        await Actions.ScrollIntoViewById(NightRate);
        await Actions.Wait300();

        _originalNightRate = await Actions.ReadInput(NightRate);
        _originalHolidayRate = await Actions.ReadInput(HolidayRate);

        await Actions.ScrollIntoViewById(SaRate);
        await Actions.Wait300();

        _originalSaRate = await Actions.ReadInput(SaRate);
        _originalSoRate = await Actions.ReadInput(SoRate);

        await Actions.ScrollIntoViewById(DayVisibleBefore);
        await Actions.Wait300();

        _originalDayVisibleBefore = await Actions.ReadInput(DayVisibleBefore);
        _originalDayVisibleAfter = await Actions.ReadInput(DayVisibleAfter);

        TestContext.Out.WriteLine($"VacationDays: {_originalVacationDays}");
        TestContext.Out.WriteLine($"ProbationPeriod: {_originalProbationPeriod}");
        TestContext.Out.WriteLine($"NoticePeriod: {_originalNoticePeriod}");
        TestContext.Out.WriteLine($"PaymentInterval: {_originalPaymentInterval}");
        TestContext.Out.WriteLine($"NightRate: {_originalNightRate}");
        TestContext.Out.WriteLine($"HolidayRate: {_originalHolidayRate}");
        TestContext.Out.WriteLine($"SaRate: {_originalSaRate}");
        TestContext.Out.WriteLine($"SoRate: {_originalSoRate}");
        TestContext.Out.WriteLine($"DayVisibleBefore: {_originalDayVisibleBefore}");
        TestContext.Out.WriteLine($"DayVisibleAfter: {_originalDayVisibleAfter}");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("All current values read successfully");
    }

    [Test]
    [Order(3)]
    public async Task Step3_ChangeVacationDays()
    {
        TestContext.Out.WriteLine("=== Step 3: Change vacationDaysPerYear to 25 ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        await Actions.ScrollIntoViewById(VacationDaysPerYear);
        await Actions.Wait500();

        await Actions.FillInputById(VacationDaysPerYear, "25");
        await Actions.PressKey(Keys.Tab);
        await Actions.Wait2000();

        var currentValue = await Actions.ReadInput(VacationDaysPerYear);
        Assert.That(currentValue, Is.EqualTo("25"), "VacationDaysPerYear should be 25");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("VacationDaysPerYear changed to 25");
    }

    [Test]
    [Order(4)]
    public async Task Step4_ChangePaymentInterval()
    {
        TestContext.Out.WriteLine("=== Step 4: Change paymentInterval to monthly (2) ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        await Actions.ScrollIntoViewById(PaymentInterval);
        await Actions.Wait500();

        await Actions.SelectNativeOptionById(PaymentInterval, "2");
        await Actions.Wait1000();

        var paymentSelect = await Actions.FindElementById(PaymentInterval);
        Assert.That(paymentSelect, Is.Not.Null, "PaymentInterval select should exist");
        var currentValue = await paymentSelect!.EvaluateAsync<string>("el => el.value") ?? string.Empty;
        Assert.That(currentValue, Is.EqualTo("2"), "PaymentInterval should be 2 (monthly)");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("PaymentInterval changed to monthly (2)");
    }

    [Test]
    [Order(5)]
    public async Task Step5_ChangeNightRate()
    {
        TestContext.Out.WriteLine("=== Step 5: Change nightRate to 50 ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        await Actions.ScrollIntoViewById(NightRate);
        await Actions.Wait500();

        await Actions.FillInputById(NightRate, "50");
        await Actions.PressKey(Keys.Tab);
        await Actions.Wait2000();

        var currentValue = await Actions.ReadInput(NightRate);
        Assert.That(currentValue, Is.EqualTo("50"), "NightRate should be 50");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("NightRate changed to 50");
    }

    [Test]
    [Order(6)]
    public async Task Step6_ReloadAndVerifyPersistedValues()
    {
        TestContext.Out.WriteLine("=== Step 6: Reload page and verify persisted values ===");

        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(VacationDaysPerYear);
        await Actions.Wait500();

        var vacationDays = await Actions.ReadInput(VacationDaysPerYear);
        Assert.That(vacationDays, Is.EqualTo("25"),
            $"VacationDaysPerYear should be persisted as 25, but was '{vacationDays}'");

        await Actions.ScrollIntoViewById(PaymentInterval);
        await Actions.Wait300();

        var paymentSelect = await Actions.FindElementById(PaymentInterval);
        Assert.That(paymentSelect, Is.Not.Null, "PaymentInterval select should exist");
        var paymentValue = await paymentSelect!.EvaluateAsync<string>("el => el.value") ?? string.Empty;
        Assert.That(paymentValue, Is.EqualTo("2"),
            $"PaymentInterval should be persisted as 2, but was '{paymentValue}'");

        await Actions.ScrollIntoViewById(NightRate);
        await Actions.Wait300();

        var nightRate = await Actions.ReadInput(NightRate);
        Assert.That(nightRate, Is.EqualTo("50"),
            $"NightRate should be persisted as 50, but was '{nightRate}'");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("All values persisted correctly after reload");
    }

    [Test]
    [Order(7)]
    public async Task Step7_RestoreOriginalValues()
    {
        TestContext.Out.WriteLine("=== Step 7: Restore original values ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(VacationDaysPerYear);
        await Actions.Wait500();

        if (!string.IsNullOrEmpty(_originalVacationDays))
        {
            await Actions.FillInputById(VacationDaysPerYear, _originalVacationDays);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        if (!string.IsNullOrEmpty(_originalProbationPeriod))
        {
            await Actions.FillInputById(ProbationPeriod, _originalProbationPeriod);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        await Actions.ScrollIntoViewById(NoticePeriod);
        await Actions.Wait300();

        if (!string.IsNullOrEmpty(_originalNoticePeriod))
        {
            await Actions.FillInputById(NoticePeriod, _originalNoticePeriod);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        if (!string.IsNullOrEmpty(_originalPaymentInterval))
        {
            await Actions.SelectNativeOptionById(PaymentInterval, _originalPaymentInterval);
            await Actions.Wait500();
        }

        await Actions.ScrollIntoViewById(NightRate);
        await Actions.Wait300();

        if (!string.IsNullOrEmpty(_originalNightRate))
        {
            await Actions.FillInputById(NightRate, _originalNightRate);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        if (!string.IsNullOrEmpty(_originalHolidayRate))
        {
            await Actions.FillInputById(HolidayRate, _originalHolidayRate);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        await Actions.ScrollIntoViewById(SaRate);
        await Actions.Wait300();

        if (!string.IsNullOrEmpty(_originalSaRate))
        {
            await Actions.FillInputById(SaRate, _originalSaRate);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        if (!string.IsNullOrEmpty(_originalSoRate))
        {
            await Actions.FillInputById(SoRate, _originalSoRate);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        await Actions.ScrollIntoViewById(DayVisibleBefore);
        await Actions.Wait300();

        if (!string.IsNullOrEmpty(_originalDayVisibleBefore))
        {
            await Actions.FillInputById(DayVisibleBefore, _originalDayVisibleBefore);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        if (!string.IsNullOrEmpty(_originalDayVisibleAfter))
        {
            await Actions.FillInputById(DayVisibleAfter, _originalDayVisibleAfter);
            await Actions.PressKey(Keys.Tab);
            await Actions.Wait500();
        }

        await Actions.Wait1000();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("All original values restored successfully");
    }

    [Test]
    [Order(8)]
    public async Task Step8_VerifyRestoredValues()
    {
        TestContext.Out.WriteLine("=== Step 8: Verify restored values after reload ===");

        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(VacationDaysPerYear);
        await Actions.Wait500();

        var vacationDays = await Actions.ReadInput(VacationDaysPerYear);
        Assert.That(vacationDays, Is.EqualTo(_originalVacationDays),
            $"VacationDaysPerYear should be restored to '{_originalVacationDays}', but was '{vacationDays}'");

        await Actions.ScrollIntoViewById(NightRate);
        await Actions.Wait300();

        var nightRate = await Actions.ReadInput(NightRate);
        Assert.That(nightRate, Is.EqualTo(_originalNightRate),
            $"NightRate should be restored to '{_originalNightRate}', but was '{nightRate}'");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("All restored values verified successfully");
    }
}
