namespace Klacks.E2ETest.Constants;

public static class ContractIds
{
    public static readonly string AddContractButton = "add-contract-button";

    public static string GetContractSelectId(int index) => $"contract-{index}";

    public static string GetActiveCheckboxId(int index) => $"active-{index}";
}
