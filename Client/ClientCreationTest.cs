using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Microsoft.Playwright;

namespace E2ETest.Client;

[TestFixture]
public class ClientCreationTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private List<string> _createdClientIds = new();

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        await Page.GotoAsync($"{BaseUrl}workplace/client");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Errors detected: {_listener.GetLastErrorMessage()}");
        }
    }

    [Test]
    [Order(1)]
    public async Task Step1_NavigateToClientPage()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Navigate to Client Page ===");

        // Act
        var currentUrl = Actions.ReadCurrentUrl();

        // Assert
        Assert.That(currentUrl, Does.Contain("workplace/client"), "Should navigate to client page");
        Assert.That(_listener.HasApiErrors(), Is.False, "No API errors should occur during navigation");

        TestContext.Out.WriteLine($"Successfully navigated to client page: {currentUrl}");
    }

    [Test]
    [Order(2)]
    public async Task Step2_CreateMultipleClients()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Create Multiple Clients ===");

        // Act & Assert
        for (int i = 0; i < ClientTestData.Clients.Length; i++)
        {
            var clientData = ClientTestData.Clients[i];
            TestContext.Out.WriteLine($"\n--- Creating Client {i + 1}/{ClientTestData.Clients.Length}: {clientData.FirstName} {clientData.LastName} ---");

            await CreateClient(clientData);

            // Navigate back to client page for next client
            if (i < ClientTestData.Clients.Length - 1)
            {
                await Page.GotoAsync($"{BaseUrl}workplace/client");
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
            }
        }

        TestContext.Out.WriteLine($"\n=== All {ClientTestData.Clients.Length} clients created successfully ===");
    }

    private async Task CreateClient(ClientData clientData)
    {
        // Click New Client
        await Actions.ClickButtonById(ClientIds.NewClientButton);
        await Actions.Wait1000();
        await Actions.WaitForSpinnerToDisappear();
        TestContext.Out.WriteLine("Clicked 'New Client' button");

        var currentUrl = Actions.ReadCurrentUrl();
        Assert.That(currentUrl, Does.Contain("workplace/edit-address"), "Should navigate to edit-address page");
        TestContext.Out.WriteLine($"Navigated to: {currentUrl}");

        // Fill First Name
        await Actions.FillInputById(ClientIds.InputFirstName, clientData.FirstName);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled first name: {clientData.FirstName}");

        // Fill Last Name
        await Actions.FillInputById(ClientIds.InputLastName, clientData.LastName);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled last name: {clientData.LastName}");

        // Select Gender - Male (value="1")
        await Actions.SelectNativeOptionById(ClientIds.InputGender, clientData.Gender);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Selected gender: Male");

        // Fill Address
        TestContext.Out.WriteLine("Filling address fields...");

        // Fill Address - Street
        await Actions.FillInputById(ClientIds.InputStreet, clientData.Street);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled street: {clientData.Street}");

        // Fill ZIP
        await Actions.FillInputById(ClientIds.InputZip, clientData.Zip);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled ZIP: {clientData.Zip}");

        // Fill City
        await Actions.FillInputById(ClientIds.InputCity, clientData.City);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled city: {clientData.City}");

        // Select Country first - CH (Switzerland) - makes State visible
        await Actions.SelectNativeOptionById(ClientIds.InputCountry, clientData.Country);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Selected country: {clientData.Country}");

        // Select State - BE (Bern)
        await Actions.SelectNativeOptionById(ClientIds.InputState, clientData.State);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Selected state: {clientData.State}");

        // Add Phone Number
        TestContext.Out.WriteLine("Adding phone number...");
        await Actions.FillInputById("phoneValue-0", clientData.PhoneNumber);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled phone: {clientData.PhoneNumber}");

        // Add Email
        TestContext.Out.WriteLine("Adding email...");
        await Actions.FillInputById("emailValue-0", clientData.Email);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled email: {clientData.Email}");

        // Add Birthday
        TestContext.Out.WriteLine("Adding birthday...");
        await Actions.FillInputById("profile-birthday", clientData.Birthday);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled birthday: {clientData.Birthday}");

        // Add Contract
        TestContext.Out.WriteLine("Adding contract to client...");
        await Actions.ScrollIntoViewById(ContractIds.AddContractButton);
        await Actions.Wait500();

        await Actions.ClickButtonById(ContractIds.AddContractButton);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked 'Add Contract' button");

        await Actions.SelectNativeOptionByIndex(ContractIds.GetContractSelectId(0), clientData.ContractTemplateIndex);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Selected contract template at index {clientData.ContractTemplateIndex}");

        await Actions.ClickCheckBoxById(ContractIds.GetActiveCheckboxId(0));
        await Actions.Wait500();
        TestContext.Out.WriteLine("Activated contract");

        // Add Group
        TestContext.Out.WriteLine("Adding group to client...");
        await Actions.ScrollIntoViewById(GroupIds.AddGroupButton);
        await Actions.Wait500();

        await Actions.ClickButtonById(GroupIds.AddGroupButton);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Clicked 'Add Group' button");

        await Actions.ClickButtonById(GroupIds.DropdownToggle);
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Opened group dropdown");

        // Expand Deutschweiz Mitte
        await Actions.ExpandGroupNodeByName(clientData.GroupLevel1);
        TestContext.Out.WriteLine($"Expanded {clientData.GroupLevel1}");

        // Expand BE
        await Actions.ExpandGroupNodeByName(clientData.GroupLevel2);
        TestContext.Out.WriteLine($"Expanded {clientData.GroupLevel2}");

        // Select Bern
        await Actions.SelectGroupByName(clientData.GroupLevel3);
        TestContext.Out.WriteLine($"Selected {clientData.GroupLevel3}");

        // Add Membership Entry Date
        TestContext.Out.WriteLine("Adding membership entry date...");
        await Actions.ScrollIntoViewById("membership-entry-date");
        await Actions.Wait500();
        await Actions.FillInputById("membership-entry-date", clientData.MembershipEntryDate);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled membership entry date: {clientData.MembershipEntryDate}");

        // Add Note
        TestContext.Out.WriteLine("Adding note...");
        await Actions.ScrollIntoViewById("note-0");
        await Actions.Wait500();
        await Actions.ClickChildElementById("note-0", ".first-line");
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Expanded note by clicking on collapsed note");

        await Actions.FillRichTextEditorById("note-editable-0", clientData.NoteText);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled note: {clientData.NoteText}");

        // Upload Client Image
        TestContext.Out.WriteLine("Uploading client image...");
        await Actions.ScrollIntoViewById("upload-client-image-area");
        await Actions.Wait500();
        await Actions.UploadFileById("clientImage", clientData.AvatarImagePath);
        await Actions.Wait1000();
        TestContext.Out.WriteLine($"Uploaded image: {clientData.AvatarImagePath}");

        // Save Client with all data
        await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
        await Actions.Wait500();
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine("Clicked Save button");

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur during client creation. Error: {_listener.GetLastErrorMessage()}");

        var finalUrl = Actions.ReadCurrentUrl();
        _createdClientIds.Add(finalUrl.Split('/').Last());
        TestContext.Out.WriteLine($"=== Client created successfully: {clientData.FirstName} {clientData.LastName} ===");
        TestContext.Out.WriteLine($"Current URL: {finalUrl}");
    }
}
