using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsSchedulingRuleIds;

namespace Klacks.E2ETest;

[TestFixture]
[Order(73)]
public class SettingsSchedulingRulesTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private static string? _createdRuleId;

    private const string TestRuleName = "E2E Test Rule";
    private const string TestMaxWorkDays = "5";
    private const string TestMinRestDays = "2";
    private const string TestMaxDailyHours = "10";

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(AddBtn);
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

    private async Task<string?> FindRuleIdByName(string ruleName)
    {
        var nameElements = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
        foreach (var element in nameElements)
        {
            var value = await element.InputValueAsync();
            if (value != null && value.Contains(ruleName))
            {
                var id = await element.GetAttributeAsync("id");
                if (id != null)
                {
                    return id.Replace(RowNamePrefix, "");
                }
            }
        }
        return null;
    }

    [Test]
    [Order(1)]
    public async Task Step1_VerifySchedulingRulesLoaded()
    {
        TestContext.Out.WriteLine("=== Step 1: Verify Scheduling Rules Section Loaded ===");

        var leftoverId = await FindRuleIdByName(TestRuleName);
        while (leftoverId != null)
        {
            TestContext.Out.WriteLine($"Cleaning up leftover rule: {leftoverId}");
            var deleteButtonId = $"{RowDeletePrefix}{leftoverId}";
            await Actions.ScrollIntoViewById(deleteButtonId);
            await Actions.Wait500();
            await Actions.ClickElementById(deleteButtonId);
            await Actions.Wait1000();
            var confirmBtn = await Actions.FindElementById(ModalIds.DeleteConfirm);
            if (confirmBtn != null)
            {
                await confirmBtn.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait2000();
            }
            leftoverId = await FindRuleIdByName(TestRuleName);
        }

        var addButton = await Actions.FindElementById(AddBtn);
        Assert.That(addButton, Is.Not.Null, "Add rule button should be visible");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Scheduling Rules section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateNewSchedulingRule()
    {
        TestContext.Out.WriteLine("=== Step 2: Create New Scheduling Rule ===");

        await Actions.ClickButtonById(AddBtn);
        await Actions.Wait1000();

        var modalSaveBtn = await Actions.FindElementById(ModalSaveBtn);
        Assert.That(modalSaveBtn, Is.Not.Null, "Modal should be open");

        await Actions.ClearInputById(ModalName);
        await Actions.TypeIntoInputById(ModalName, TestRuleName);

        await Actions.ClearInputById(ModalMaxWorkDays);
        await Actions.TypeIntoInputById(ModalMaxWorkDays, TestMaxWorkDays);

        await Actions.ClearInputById(ModalMinRestDays);
        await Actions.TypeIntoInputById(ModalMinRestDays, TestMinRestDays);

        await Actions.ClearInputById(ModalMaxDailyHours);
        await Actions.TypeIntoInputById(ModalMaxDailyHours, TestMaxDailyHours);

        await Actions.ClickButtonById(ModalSaveBtn);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"API error after create: {_listener.GetLastErrorMessage()}");

        await Actions.ScrollIntoViewById(AddBtn);
        await Actions.Wait1000();

        _createdRuleId = await FindRuleIdByName(TestRuleName);
        TestContext.Out.WriteLine($"Created rule ID: {_createdRuleId}");

        Assert.That(_createdRuleId, Is.Not.Null, "Created rule should be found in the list");

        TestContext.Out.WriteLine($"Scheduling rule '{TestRuleName}' created successfully");
    }

    [Test]
    [Order(3)]
    public async Task Step3_VerifyCreatedRuleInList()
    {
        TestContext.Out.WriteLine("=== Step 3: Verify Created Rule in List ===");

        if (string.IsNullOrEmpty(_createdRuleId))
        {
            TestContext.Out.WriteLine("No rule was created in Step 2 - skipping");
            Assert.Inconclusive("No rule was created in previous step");
            return;
        }

        var ruleNameElement = await Actions.FindElementById($"{RowNamePrefix}{_createdRuleId}");
        Assert.That(ruleNameElement, Is.Not.Null, "Rule name element should be visible in the list");

        if (ruleNameElement != null)
        {
            var nameValue = await ruleNameElement.InputValueAsync();
            TestContext.Out.WriteLine($"Rule name in list: {nameValue}");
            Assert.That(nameValue, Does.Contain(TestRuleName),
                "Rule name should match the created rule");
        }

        var deleteButton = await Actions.FindElementById($"{RowDeletePrefix}{_createdRuleId}");
        Assert.That(deleteButton, Is.Not.Null, "Delete button should be visible for the created rule");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Created rule verified in list");
    }

    [Test]
    [Order(4)]
    public async Task Step4_EditCreatedRule()
    {
        TestContext.Out.WriteLine("=== Step 4: Edit Created Rule ===");

        if (string.IsNullOrEmpty(_createdRuleId))
        {
            TestContext.Out.WriteLine("No rule was created - skipping");
            Assert.Inconclusive("No rule was created in previous step");
            return;
        }

        var ruleNameElement = await Actions.FindElementById($"{RowNamePrefix}{_createdRuleId}");
        Assert.That(ruleNameElement, Is.Not.Null, "Rule name element should exist for clicking");

        await ruleNameElement!.ClickAsync();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked on rule row to open edit modal");

        var modalSaveBtn = await Actions.FindElementById(ModalSaveBtn);
        Assert.That(modalSaveBtn, Is.Not.Null, "Edit modal should be open");

        var nameValue = await Actions.ReadInput(ModalName);
        TestContext.Out.WriteLine($"Modal name value: {nameValue}");
        Assert.That(nameValue, Does.Contain(TestRuleName),
            "Modal should show the correct rule name");

        await Actions.ClickElementById(ModalCancelBtn);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Closed edit modal via Cancel");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Edit modal opened and verified successfully");
    }

    [Test]
    [Order(5)]
    public async Task Step5_DeleteCreatedRule()
    {
        TestContext.Out.WriteLine("=== Step 5: Delete Created Rule ===");

        if (string.IsNullOrEmpty(_createdRuleId))
        {
            TestContext.Out.WriteLine("No rule was created - skipping delete");
            Assert.Inconclusive("No rule was created in previous step");
            return;
        }

        var deleteButtonId = $"{RowDeletePrefix}{_createdRuleId}";
        TestContext.Out.WriteLine($"Clicking delete button: {deleteButtonId}");

        await Actions.ScrollIntoViewById(deleteButtonId);
        await Actions.Wait500();

        await Actions.ClickElementById(deleteButtonId);
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked delete button");

        var confirmBtn = await Actions.FindElementById(ModalIds.DeleteConfirm);
        if (confirmBtn == null)
        {
            TestContext.Out.WriteLine("Delete confirmation button not found");
            Assert.Fail("Delete confirmation modal did not appear");
            return;
        }

        await confirmBtn.ClickAsync();
        TestContext.Out.WriteLine("Confirmed deletion");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        var deletedRule = await FindRuleIdByName(TestRuleName);
        Assert.That(deletedRule, Is.Null, "Deleted rule should no longer appear in the list");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        _createdRuleId = null;
        TestContext.Out.WriteLine("Scheduling rule deleted successfully");
    }

    [Test]
    [Order(6)]
    public async Task Step6_VerifyDeletionPersisted()
    {
        TestContext.Out.WriteLine("=== Step 6: Verify Deletion Persisted After Reload ===");

        TestContext.Out.WriteLine("Reloading page...");
        await Actions.Reload();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(AddBtn);
        await Actions.Wait1000();

        var ruleAfterReload = await FindRuleIdByName(TestRuleName);
        Assert.That(ruleAfterReload, Is.Null,
            "Deleted rule should not exist after page reload");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Deletion persisted correctly after reload");
    }
}
