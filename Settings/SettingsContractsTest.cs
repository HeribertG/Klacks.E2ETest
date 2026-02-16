using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsContractIds;

namespace Klacks.E2ETest;

[TestFixture]
[Order(71)]
public class SettingsContractsTest : PlaywrightSetup
{
    private Listener _listener = null!;

    private const string TestContractName = "E2E Test Contract";
    private const string TestGuaranteedHours = "160";
    private const string TestFulltime = "160";

    private static string _createdContractId = string.Empty;

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
    public async Task Step1_NavigateToSettingsAndVerifyContractsSection()
    {
        TestContext.Out.WriteLine("=== Step 1: Navigate to Settings and verify Contracts section ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(AddBtn);
        await Actions.Wait500();

        var addButton = await Actions.FindElementById(AddBtn);
        Assert.That(addButton, Is.Not.Null, "Contracts add button should be visible");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Contracts section loaded successfully");
    }

    [Test]
    [Order(2)]
    public async Task Step2_OpenContractModal()
    {
        TestContext.Out.WriteLine("=== Step 2: Open contract modal via add button ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(AddBtn);
        await Actions.Wait500();

        await Actions.ClickElementById(AddBtn);
        await Actions.Wait1000();

        var modalNameInput = await Actions.FindElementById(ModalName);
        Assert.That(modalNameInput, Is.Not.Null, "Contract modal should be open with name input visible");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Contract modal opened successfully");
    }

    [Test]
    [Order(3)]
    public async Task Step3_FillAndSaveContract()
    {
        TestContext.Out.WriteLine("=== Step 3: Fill contract form and save ===");

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        await Actions.ScrollIntoViewById(AddBtn);
        await Actions.Wait500();

        await Actions.ClickElementById(AddBtn);
        await Actions.Wait1000();

        var modalNameInput = await Actions.FindElementById(ModalName);
        Assert.That(modalNameInput, Is.Not.Null, "Contract modal should be open");

        await Actions.FillInputById(ModalName, TestContractName);
        await Actions.Wait300();

        await Actions.FillInputById(ModalGuaranteedHours, TestGuaranteedHours);
        await Actions.Wait300();

        await Actions.FillInputById(ModalFulltime, TestFulltime);
        await Actions.Wait300();

        await Actions.ClickButtonById(ModalSaveBtn);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Contract form filled and saved");
    }

    [Test]
    [Order(4)]
    public async Task Step4_VerifyContractInList()
    {
        TestContext.Out.WriteLine("=== Step 4: Verify new contract appears in list ===");

        await Actions.Wait1000();

        var contractId = await FindContractIdByName(TestContractName);
        Assert.That(contractId, Is.Not.Null.And.Not.Empty,
            $"Contract '{TestContractName}' should appear in the contracts list");

        _createdContractId = contractId!;
        TestContext.Out.WriteLine($"Contract found in list with ID: {_createdContractId}");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Contract verified in list successfully");
    }

    [Test]
    [Order(5)]
    public async Task Step5_DeleteCreatedContract()
    {
        TestContext.Out.WriteLine("=== Step 5: Delete the created contract ===");

        if (string.IsNullOrEmpty(_createdContractId))
        {
            var contractId = await FindContractIdByName(TestContractName);
            if (string.IsNullOrEmpty(contractId))
            {
                Assert.Inconclusive("No contract found to delete - contract may not have been created");
                return;
            }
            _createdContractId = contractId;
        }

        var deleteButtonId = RowDeletePrefix + _createdContractId;

        await Actions.ScrollIntoViewById(deleteButtonId);
        await Actions.Wait500();

        await Actions.ClickElementById(deleteButtonId);
        await Actions.Wait1000();

        var confirmButton = await Actions.FindElementById(ModalIds.DeleteConfirm);
        Assert.That(confirmButton, Is.Not.Null, "Delete confirmation modal should appear");

        await Actions.ClickButtonById(ModalIds.DeleteConfirm);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Contract '{TestContractName}' deletion initiated");
    }

    [Test]
    [Order(6)]
    public async Task Step6_VerifyContractDeleted()
    {
        TestContext.Out.WriteLine("=== Step 6: Verify contract has been deleted ===");

        await Actions.Wait1000();

        var contractId = await FindContractIdByName(TestContractName);
        Assert.That(contractId, Is.Null.Or.Empty,
            $"Contract '{TestContractName}' should no longer exist in the contracts list");

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Contract deletion verified successfully");
    }

    #region Helper Methods

    private async Task<string?> FindContractIdByName(string contractName)
    {
        var inputs = await Page.QuerySelectorAllAsync($"input[id^='{RowNamePrefix}']");
        foreach (var input in inputs)
        {
            var value = await input.InputValueAsync();
            if (value.Equals(contractName, StringComparison.OrdinalIgnoreCase))
            {
                var id = await input.GetAttributeAsync("id");
                if (!string.IsNullOrEmpty(id))
                {
                    return id.Replace(RowNamePrefix, string.Empty);
                }
            }
        }

        return null;
    }

    #endregion
}
