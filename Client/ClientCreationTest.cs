using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Microsoft.Playwright;

namespace E2ETest;

[TestFixture]
[Order(10)]
public class ClientCreationTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private List<string> _createdClientIds = new();

    [SetUp]
    public async Task Setup()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        // Navigate directly to client list
        await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        // Wait for the client table rows to appear - this ensures data is loaded
        await WaitForClientTableData();
    }

    private async Task WaitForClientTableData()
    {
        const int maxAttempts = 60;
        const int delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Look for table rows with specific ID pattern: client-row-0, client-row-1, etc.
            var tableRows = await Actions.GetElementsBySelector(ClientIds.TableRowSelector);
            if (tableRows.Count > 0)
            {
                TestContext.Out.WriteLine($"Client table loaded after {i * delayMs}ms ({tableRows.Count} rows)");
                return;
            }

            await Task.Delay(delayMs);
        }

        TestContext.Out.WriteLine($"WARNING: Client table not loaded after {maxAttempts * delayMs}ms - continuing anyway");
    }

    [TearDown]
    public void TearDown()
    {
        if (_listener.HasApiErrors())
        {
            TestContext.Out.WriteLine($"API Errors detected: {_listener.GetLastErrorMessage()}");
        }
    }

    [Test]
    [Order(1)]
    public void Step1_NavigateToClientPage()
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
                await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
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

        // Wait for Countries to be loaded in the dropdown
        await WaitForCountriesToLoad();
        TestContext.Out.WriteLine("Countries loaded in dropdown");

        // Fill First Name
        await Actions.ScrollIntoViewById(ClientIds.InputFirstName);
        await Actions.FillInputById(ClientIds.InputFirstName, clientData.FirstName);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled first name: {clientData.FirstName}");

        // Fill Last Name
        await Actions.ScrollIntoViewById(ClientIds.InputLastName);
        await Actions.FillInputById(ClientIds.InputLastName, clientData.LastName);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled last name: {clientData.LastName}");

        // Select Gender
        await Actions.ScrollIntoViewById(ClientIds.InputGender);
        await Actions.SelectNativeOptionById(ClientIds.InputGender, clientData.Gender);
        await Actions.Wait500();
        TestContext.Out.WriteLine("Selected gender: Male");

        // Fill Address
        TestContext.Out.WriteLine("Filling address fields...");

        // Fill Address - Street
        await Actions.ScrollIntoViewById(ClientIds.InputStreet);
        await Actions.FillInputById(ClientIds.InputStreet, clientData.Street);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled street: {clientData.Street}");

        // Fill ZIP - this will auto-fill City via API
        await Actions.ScrollIntoViewById(ClientIds.InputZip);
        await Actions.FillInputById(ClientIds.InputZip, clientData.Zip);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine($"Filled ZIP: {clientData.Zip}");

        // Check if City has correct value, clear and fill if needed
        await Actions.ScrollIntoViewById(ClientIds.InputCity);
        var currentCityValue = await Actions.ReadInput(ClientIds.InputCity);
        if (currentCityValue != clientData.City)
        {
            if (!string.IsNullOrWhiteSpace(currentCityValue))
            {
                await Actions.ClearInputById(ClientIds.InputCity);
                TestContext.Out.WriteLine($"Cleared city field (was: {currentCityValue})");
            }
            await Actions.FillInputById(ClientIds.InputCity, clientData.City);
            await Actions.Wait500();
            TestContext.Out.WriteLine($"Filled city: {clientData.City}");
        }
        else
        {
            TestContext.Out.WriteLine($"City already correct: {currentCityValue}");
        }

        // Select Country first - CH (Switzerland) - makes State visible
        await Actions.ScrollIntoViewById(ClientIds.InputCountry);
        await Actions.SelectNativeOptionById(ClientIds.InputCountry, clientData.Country);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
        TestContext.Out.WriteLine($"Selected country: {clientData.Country}");

        // Select State - BE (Bern) - wait for states to load after country selection
        await Actions.ScrollIntoViewById(ClientIds.InputState);
        await Actions.Wait500();
        await Actions.SelectNativeOptionById(ClientIds.InputState, clientData.State);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Selected state: {clientData.State}");

        // Add Phone Number
        TestContext.Out.WriteLine("Adding phone number...");
        await Actions.ScrollIntoViewById("phoneValue-0");
        await Actions.FillInputById("phoneValue-0", clientData.PhoneNumber);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled phone: {clientData.PhoneNumber}");

        // Add Email
        TestContext.Out.WriteLine("Adding email...");
        await Actions.ScrollIntoViewById("emailValue-0");
        await Actions.FillInputById("emailValue-0", clientData.Email);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled email: {clientData.Email}");

        // Add Birthday
        TestContext.Out.WriteLine("Adding birthday...");
        await Actions.ScrollIntoViewById("profile-birthday");
        await Actions.FillInputById("profile-birthday", clientData.Birthday);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled birthday: {clientData.Birthday}");

        // === MEMBERSHIP SECTION - must come immediately after address-persona ===
        // Set Client Type (only if valid: 0=Employee, 1=ExternEmp, 2=Customer)
        if (clientData.ClientType == 0 || clientData.ClientType == 1 || clientData.ClientType == 2)
        {
            TestContext.Out.WriteLine("Setting client type...");
            await Actions.ScrollIntoViewById("client-type");
            await Actions.Wait500();
            await Actions.SelectNativeOptionById("client-type", clientData.ClientType.ToString());
            await Actions.Wait500();
            var clientTypeName = clientData.ClientType == 0 ? "Employee" : clientData.ClientType == 1 ? "ExternEmp" : "Customer";
            TestContext.Out.WriteLine($"Selected client type: {clientData.ClientType} ({clientTypeName})");
        }
        else
        {
            TestContext.Out.WriteLine($"Skipping client type selection (ClientType={clientData.ClientType} is not a valid option in UI)");
        }

        // Add Membership Entry Date
        TestContext.Out.WriteLine("Adding membership entry date...");
        await Actions.ScrollIntoViewById("membership-entry-date");
        await Actions.Wait500();
        await Actions.FillInputById("membership-entry-date", clientData.MembershipEntryDate);
        await Actions.Wait500();
        TestContext.Out.WriteLine($"Filled membership entry date: {clientData.MembershipEntryDate}");

        // === CONTRACT SECTION ===
        // Add Contract (only if ClientType is not 3)
        if (clientData.ClientType != 3 && clientData.ContractTemplateIndex >= 0)
        {
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
        }
        else
        {
            TestContext.Out.WriteLine($"Skipping contract addition (ClientType={clientData.ClientType}, ContractTemplateIndex={clientData.ContractTemplateIndex})");
        }

        // === GROUP SECTION ===
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

        // Check if Save button is enabled before saving
        await Actions.ScrollIntoViewById(SaveBarIds.SaveButton);
        await Actions.Wait500();

        var saveButton = await Actions.FindElementById(SaveBarIds.SaveButton);
        var isDisabled = await saveButton.IsDisabledAsync();

        if (isDisabled)
        {
            TestContext.Out.WriteLine("ERROR: Save button is DISABLED! A field is not correctly filled.");
            Assert.Fail("Save button is disabled - form validation failed");
        }

        TestContext.Out.WriteLine("Save button is enabled, proceeding with save...");
        await Actions.ClickButtonById(SaveBarIds.SaveButton);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();
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

    private async Task WaitForCountriesToLoad()
    {
        const int maxAttempts = 20;
        const int delayMs = 500;

        for (int i = 0; i < maxAttempts; i++)
        {
            var countrySelect = await Page.QuerySelectorAsync($"#{ClientIds.InputCountry}");
            if (countrySelect != null)
            {
                var options = await countrySelect.QuerySelectorAllAsync("option");
                if (options.Count > 1)
                {
                    TestContext.Out.WriteLine($"Countries loaded after {i * delayMs}ms ({options.Count} options)");
                    return;
                }
            }

            await Task.Delay(delayMs);
        }

        TestContext.Out.WriteLine($"WARNING: Countries not loaded after {maxAttempts * delayMs}ms");
    }
}
