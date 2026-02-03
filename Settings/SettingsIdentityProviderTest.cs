using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using static Klacks.E2ETest.Constants.SettingsIdentityProviderIds;
using static Klacks.E2ETest.Constants.SettingsIdentityProviderTestData;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(28)]
    public class SettingsIdentityProviderTest : PlaywrightSetup
    {
        private Listener _listener = null!;
        private static string? _createdProviderId;
        private static string? _createdProviderName;
        private static string? _ldapProviderId;
        private static string? _ldapProviderName;
        private static string? _zflexProviderId;
        private static string? _zflexProviderName;

        [SetUp]
        public async Task Setup()
        {
            _listener = new Listener(Page);
            _listener.RecognizeApiErrors();

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();

            await Actions.ScrollIntoViewById(Section);
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
        public async Task Step1_VerifyIdentityProviderPageLoaded()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 1: Verify Identity Provider Section Loaded ===");

            // Assert
            var header = await Actions.FindElementById(Header);
            Assert.That(header, Is.Not.Null, "Identity provider header should be visible");

            var addButton = await Actions.FindElementById(AddBtn);
            Assert.That(addButton, Is.Not.Null, "Add provider button should be visible");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Identity Provider section loaded successfully");
        }

        [Test]
        [Order(2)]
        public async Task Step2_CreateNewProvider()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 2: Create New Identity Provider ===");
            var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
            _createdProviderName = $"TestProvider{timestamp}";
            TestContext.Out.WriteLine($"Creating provider: {_createdProviderName}");

            // Act
            var addButton = await Actions.FindElementById(AddBtn);
            Assert.That(addButton, Is.Not.Null, "Add button should exist");

            await addButton!.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Clicked Add Provider button");

            var modalHeader = await Actions.FindElementById(ModalHeader);
            Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
            TestContext.Out.WriteLine("Modal opened successfully");

            TestContext.Out.WriteLine($"Filling provider name: {_createdProviderName}");
            await Actions.ClearInputById(ModalInputName);
            await Actions.TypeIntoInputById(ModalInputName, _createdProviderName);
            await Actions.Wait500();

            var saveCloseButton = await Actions.FindElementById(ModalSaveCloseBtn);
            Assert.That(saveCloseButton, Is.Not.Null, "Save & Close button should exist");

            var isButtonEnabled = await saveCloseButton!.IsEnabledAsync();
            TestContext.Out.WriteLine($"Save & Close button enabled: {isButtonEnabled}");
            Assert.That(isButtonEnabled, Is.True, "Save & Close button should be enabled");

            await Actions.ClickElementById(ModalSaveCloseBtn);
            TestContext.Out.WriteLine("Clicked Save & Close button");
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            // Find the created provider's ID
            var rowInputs = await Page.QuerySelectorAllAsync($"input[id^='identity-provider-row-name-']");
            TestContext.Out.WriteLine($"Found {rowInputs.Count} provider rows");

            foreach (var input in rowInputs)
            {
                var value = await input.InputValueAsync();
                var inputId = await input.GetAttributeAsync("id");
                TestContext.Out.WriteLine($"  Provider: '{value}' (id: {inputId})");

                if (value == _createdProviderName)
                {
                    _createdProviderId = inputId?.Replace("identity-provider-row-name-", "");
                    TestContext.Out.WriteLine($"  -> MATCH! Created provider ID: {_createdProviderId}");
                    break;
                }
            }

            // Assert
            Assert.That(_createdProviderId, Is.Not.Null, "Created provider should be found in the list");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Provider created successfully: {_createdProviderName}");
        }

        [Test]
        [Order(3)]
        public async Task Step3_OpenAndCloseProviderModal()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 3: Open and Close Provider Modal ===");

            if (string.IsNullOrEmpty(_createdProviderId))
            {
                TestContext.Out.WriteLine("No provider was created in Step2 - skipping");
                Assert.Inconclusive("No provider was created in previous step");
                return;
            }

            // Act
            var providerNameId = GetRowNameId(_createdProviderId);
            var providerRow = await Actions.FindElementById(providerNameId);
            Assert.That(providerRow, Is.Not.Null, $"Provider row for {_createdProviderId} should exist");

            await providerRow!.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Clicked on provider row to open modal");

            var modalHeader = await Actions.FindElementById(ModalHeader);
            Assert.That(modalHeader, Is.Not.Null, "Modal should be open");
            TestContext.Out.WriteLine("Modal opened successfully");

            var generalTab = await Actions.FindElementById(ModalTabGeneral);
            Assert.That(generalTab, Is.Not.Null, "General tab should be visible");
            TestContext.Out.WriteLine("General tab is visible");

            await Actions.ClickElementById(ModalCancelBtn);
            await Actions.Wait500();
            TestContext.Out.WriteLine("Clicked Cancel to close modal");

            // Assert
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Provider modal opened and closed successfully");
        }

        [Test]
        [Order(4)]
        public async Task Step4_VerifyProviderRowsExist()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 4: Verify Provider Rows Exist ===");

            // Act
            var providerRows = await Page.QuerySelectorAllAsync($"input[id^='identity-provider-row-name-']");

            // Assert
            Assert.That(providerRows.Count, Is.GreaterThan(0), "At least one provider should exist");
            TestContext.Out.WriteLine($"Found {providerRows.Count} providers in the list");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");
        }

        [Test]
        [Order(5)]
        public async Task Step5_DeleteCreatedProvider()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 5: Delete Created Provider ===");

            if (string.IsNullOrEmpty(_createdProviderId))
            {
                TestContext.Out.WriteLine("No provider was created - skipping delete");
                Assert.Inconclusive("No provider was created in previous step");
                return;
            }

            // Act
            var deleteButtonId = GetRowDeleteId(_createdProviderId);
            var deleteButton = await Actions.FindElementById(deleteButtonId);
            Assert.That(deleteButton, Is.Not.Null, $"Delete button for provider {_createdProviderId} should exist");

            await deleteButton!.ClickAsync();
            await Actions.Wait500();
            TestContext.Out.WriteLine("Clicked delete button");

            await Actions.ClickElementById("modal-delete-confirm");
            await Actions.Wait2000();
            TestContext.Out.WriteLine("Confirmed deletion");

            // Assert
            var deletedProvider = await Page.QuerySelectorAsync($"#identity-provider-row-name-{_createdProviderId}");
            Assert.That(deletedProvider, Is.Null, "Deleted provider should no longer exist in the list");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Provider {_createdProviderName} deleted successfully");
            _createdProviderId = null;
            _createdProviderName = null;
        }

        [Test]
        [Order(6)]
        public async Task Step6_CreateLdapProviderWithConnectionData()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 6: Create LDAP Provider with Forumsys Data ===");
            var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
            _ldapProviderName = $"ForumsysLDAP{timestamp}";
            TestContext.Out.WriteLine($"Creating LDAP provider: {_ldapProviderName}");

            // Act
            var addButton = await Actions.FindElementById(AddBtn);
            Assert.That(addButton, Is.Not.Null, "Add button should exist");

            await addButton!.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Clicked Add Provider button");

            var modalHeader = await Actions.FindElementById(ModalHeader);
            Assert.That(modalHeader, Is.Not.Null, "Modal should be open");

            await Actions.ClearInputById(ModalInputName);
            await Actions.TypeIntoInputById(ModalInputName, _ldapProviderName);
            TestContext.Out.WriteLine($"Set provider name: {_ldapProviderName}");
            TestContext.Out.WriteLine("LDAP type is already the default");

            await Actions.ClickElementById(ModalSaveBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Saved provider to get ID");

            await Actions.ClickElementById(ModalTabConnection);
            await Actions.Wait500();
            TestContext.Out.WriteLine("Switched to Connection tab");

            await Actions.ClearInputById(InputHost);
            await Actions.TypeIntoInputById(InputHost, ForumsysHost);
            TestContext.Out.WriteLine($"Set host: {ForumsysHost}");

            await Actions.ClearInputById(InputPort);
            await Actions.TypeIntoInputById(InputPort, ForumsysPort.ToString());
            TestContext.Out.WriteLine($"Set port: {ForumsysPort}");

            await Actions.ClearInputById(InputBaseDn);
            await Actions.TypeIntoInputById(InputBaseDn, ForumsysBaseDn);
            TestContext.Out.WriteLine($"Set base DN: {ForumsysBaseDn}");

            await Actions.ClearInputById(InputBindDn);
            await Actions.TypeIntoInputById(InputBindDn, ForumsysBindDn);
            TestContext.Out.WriteLine($"Set bind DN: {ForumsysBindDn}");

            await Actions.ClearInputById(InputBindPassword);
            await Actions.TypeIntoInputById(InputBindPassword, ForumsysBindPassword);
            TestContext.Out.WriteLine("Set bind password");

            await Actions.ClearInputById(InputUserFilter);
            await Actions.TypeIntoInputById(InputUserFilter, ForumsysUserFilter);
            TestContext.Out.WriteLine($"Set user filter: {ForumsysUserFilter}");

            await Actions.ClickElementById(ModalSaveCloseBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();
            TestContext.Out.WriteLine("Saved and closed modal");

            var rowInputs = await Page.QuerySelectorAllAsync($"input[id^='identity-provider-row-name-']");
            foreach (var input in rowInputs)
            {
                var value = await input.InputValueAsync();
                var inputId = await input.GetAttributeAsync("id");
                if (value == _ldapProviderName)
                {
                    _ldapProviderId = inputId?.Replace("identity-provider-row-name-", "");
                    TestContext.Out.WriteLine($"Found LDAP provider ID: {_ldapProviderId}");
                    break;
                }
            }

            // Assert
            Assert.That(_ldapProviderId, Is.Not.Null, "LDAP provider should be created");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"LDAP Provider created successfully: {_ldapProviderName}");
        }

        [Test]
        [Order(7)]
        public async Task Step7_TestLdapConnection()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 7: Test LDAP Connection to Forumsys ===");

            if (string.IsNullOrEmpty(_ldapProviderId))
            {
                TestContext.Out.WriteLine("No LDAP provider was created in Step6 - skipping");
                Assert.Inconclusive("No LDAP provider was created in previous step");
                return;
            }

            // Act
            var providerNameId = GetRowNameId(_ldapProviderId);
            var providerRow = await Actions.FindElementById(providerNameId);
            Assert.That(providerRow, Is.Not.Null, "LDAP provider row should exist");

            await providerRow!.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Opened LDAP provider modal");

            await Actions.ClickElementById(ModalTabConnection);
            await Actions.Wait500();
            TestContext.Out.WriteLine("Switched to Connection tab");

            var testBtn = await Actions.FindElementById(ModalTestBtn);
            Assert.That(testBtn, Is.Not.Null, "Test Connection button should be visible");

            await testBtn!.ClickAsync();
            TestContext.Out.WriteLine("Clicked Test Connection button");

            await Actions.Wait3500();
            await Actions.Wait3500();

            var alertSuccess = await Page.QuerySelectorAsync(".alert-success");
            var alertDanger = await Page.QuerySelectorAsync(".alert-danger");

            if (alertSuccess != null)
            {
                var successText = await alertSuccess.InnerTextAsync();
                TestContext.Out.WriteLine($"Connection SUCCESS: {successText}");
            }
            else if (alertDanger != null)
            {
                var errorText = await alertDanger.InnerTextAsync();
                TestContext.Out.WriteLine($"Connection FAILED: {errorText}");
            }

            await Actions.ClickElementById(ModalCancelBtn);
            await Actions.Wait500();
            TestContext.Out.WriteLine("Closed modal");

            // Assert
            Assert.That(alertSuccess, Is.Not.Null, "LDAP connection test must succeed");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("LDAP connection test completed successfully");
        }

        [Test]
        [Order(8)]
        public async Task Step8_EnableImportAndSyncClients()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 8: Enable Import and Sync Clients from Forumsys ===");

            if (string.IsNullOrEmpty(_ldapProviderId))
            {
                TestContext.Out.WriteLine("No LDAP provider was created - skipping");
                Assert.Inconclusive("No LDAP provider was created in previous step");
                return;
            }

            // Act
            var providerNameId = GetRowNameId(_ldapProviderId);
            var providerRow = await Actions.FindElementById(providerNameId);
            Assert.That(providerRow, Is.Not.Null, "LDAP provider row should exist");

            await providerRow!.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Opened LDAP provider modal");

            var useForImportCheckbox = await Actions.FindElementById(InputUseForImport);
            Assert.That(useForImportCheckbox, Is.Not.Null, "Use for Import checkbox should exist");

            var isChecked = await useForImportCheckbox!.IsCheckedAsync();
            if (!isChecked)
            {
                await useForImportCheckbox.ClickAsync();
                TestContext.Out.WriteLine("Enabled 'Use for Import' option");
            }
            else
            {
                TestContext.Out.WriteLine("'Use for Import' was already enabled");
            }

            await Actions.ClickElementById(ModalSaveBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Saved provider with import enabled");

            await Actions.ClickElementById(ModalTabConnection);
            await Actions.Wait500();
            TestContext.Out.WriteLine("Switched to Connection tab");

            var syncBtn = await Actions.FindElementById(ModalSyncBtn);
            Assert.That(syncBtn, Is.Not.Null, "Sync Clients button should be visible");

            await syncBtn!.ClickAsync();
            TestContext.Out.WriteLine("Clicked Sync Clients button");

            await Actions.Wait3500();
            await Actions.Wait3500();
            await Actions.Wait3500();

            var alertSuccess = await Page.QuerySelectorAsync(".alert-success");
            var alertDanger = await Page.QuerySelectorAsync(".alert-danger");

            if (alertSuccess != null)
            {
                var successText = await alertSuccess.InnerTextAsync();
                TestContext.Out.WriteLine($"Sync SUCCESS: {successText}");
            }
            else if (alertDanger != null)
            {
                var errorText = await alertDanger.InnerTextAsync();
                TestContext.Out.WriteLine($"Sync FAILED: {errorText}");
            }

            await Actions.ClickElementById(ModalCancelBtn);
            await Actions.Wait500();
            TestContext.Out.WriteLine("Closed modal");

            // Assert
            Assert.That(alertSuccess != null || alertDanger != null, Is.True,
                "Sync should show a result (success or failure)");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Client sync completed");
        }

        [Test]
        [Order(9)]
        public async Task Step9_VerifyImportedClientsInAllAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 9: Verify Imported Clients in All-Address ===");

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();
            TestContext.Out.WriteLine("Navigated to All-Address view");

            var resetBtn = await Actions.FindElementById(ClientFilterIds.ResetAddressButtonId);
            if (resetBtn != null && await resetBtn.IsEnabledAsync())
            {
                await resetBtn.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait1000();
                TestContext.Out.WriteLine("Reset filters");
            }

            var allRowsBefore = await Page.QuerySelectorAllAsync(ClientFilterIds.ClientRowSelector);
            TestContext.Out.WriteLine($"Initial rows after reset: {allRowsBefore.Count}");

            var searchInput = await Actions.FindElementById(ClientFilterIds.SearchInputId);
            if (searchInput == null)
            {
                TestContext.Out.WriteLine("ERROR: Search input not found!");
                var pageContent = await Page.ContentAsync();
                TestContext.Out.WriteLine($"Page URL: {Page.Url}");
            }
            else
            {
                TestContext.Out.WriteLine("Search input found");
            }

            await Actions.ClearInputById(ClientFilterIds.SearchInputId);
            await Actions.TypeIntoInputById(ClientFilterIds.SearchInputId, "Einstein");
            await Actions.Wait500();

            var inputValue = await searchInput!.InputValueAsync();
            TestContext.Out.WriteLine($"Search input value after typing: '{inputValue}'");

            var searchBtn = await Actions.FindElementById(ClientFilterIds.SearchButtonId);
            if (searchBtn == null)
            {
                TestContext.Out.WriteLine("ERROR: Search button not found!");
            }
            else
            {
                TestContext.Out.WriteLine("Search button found");
                await searchBtn.ClickAsync();
            }

            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();
            TestContext.Out.WriteLine("Searched for 'Einstein'");

            var clientRows = await Page.QuerySelectorAllAsync(ClientFilterIds.ClientRowSelector);
            TestContext.Out.WriteLine($"Found {clientRows.Count} client rows after search");

            var foundEinstein = false;
            foreach (var row in clientRows)
            {
                var rowText = await row.InnerTextAsync();
                TestContext.Out.WriteLine($"  Row: {rowText}");
                if (rowText.Contains("Einstein", StringComparison.OrdinalIgnoreCase))
                {
                    foundEinstein = true;
                    TestContext.Out.WriteLine("  -> Found Einstein!");
                }
            }

            // Assert
            Assert.That(foundEinstein, Is.True, "Einstein should be found in All-Address after LDAP sync");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Imported clients verified in All-Address");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            await Actions.ScrollIntoViewById(Section);
            await Actions.Wait500();
            TestContext.Out.WriteLine("Navigated back to Settings");
        }

        [Test]
        [Order(10)]
        public async Task Step10_DeleteLdapProvider()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 10: Delete LDAP Provider ===");

            if (string.IsNullOrEmpty(_ldapProviderId))
            {
                TestContext.Out.WriteLine("No LDAP provider was created - skipping delete");
                Assert.Inconclusive("No LDAP provider was created in previous step");
                return;
            }

            // Act
            var deleteButtonId = GetRowDeleteId(_ldapProviderId);
            var deleteButton = await Actions.FindElementById(deleteButtonId);
            Assert.That(deleteButton, Is.Not.Null, $"Delete button for LDAP provider should exist");

            await deleteButton!.ClickAsync();
            await Actions.Wait500();
            TestContext.Out.WriteLine("Clicked delete button");

            await Actions.ClickElementById("modal-delete-confirm");
            await Actions.Wait2000();
            TestContext.Out.WriteLine("Confirmed deletion");

            // Assert
            var deletedProvider = await Page.QuerySelectorAsync($"#identity-provider-row-name-{_ldapProviderId}");
            Assert.That(deletedProvider, Is.Null, "Deleted LDAP provider should no longer exist");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"LDAP Provider {_ldapProviderName} deleted successfully");
            _ldapProviderId = null;
            _ldapProviderName = null;
        }

        [Test]
        [Order(11)]
        [Ignore("Zflexldap server is offline")]
        public async Task Step11_CreateZflexLdapProvider()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 11: Create Zflexldap Provider ===");
            var server = Zflexldap;
            var timestamp = DateTime.Now.Ticks.ToString().Substring(10, 6);
            _zflexProviderName = $"{server.Name}LDAP{timestamp}";
            TestContext.Out.WriteLine($"Creating LDAP provider: {_zflexProviderName}");

            // Act
            var addButton = await Actions.FindElementById(AddBtn);
            Assert.That(addButton, Is.Not.Null, "Add button should exist");

            await addButton!.ClickAsync();
            await Actions.Wait1000();

            await Actions.ClearInputById(ModalInputName);
            await Actions.TypeIntoInputById(ModalInputName, _zflexProviderName);
            TestContext.Out.WriteLine($"Set provider name: {_zflexProviderName}");

            await Actions.ClickElementById(ModalSaveBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.ClickElementById(ModalTabConnection);
            await Actions.Wait500();

            await Actions.ClearInputById(InputHost);
            await Actions.TypeIntoInputById(InputHost, server.Host);
            TestContext.Out.WriteLine($"Set host: {server.Host}");

            await Actions.ClearInputById(InputPort);
            await Actions.TypeIntoInputById(InputPort, server.Port.ToString());
            TestContext.Out.WriteLine($"Set port: {server.Port}");

            await Actions.ClearInputById(InputBaseDn);
            await Actions.TypeIntoInputById(InputBaseDn, server.BaseDn);
            TestContext.Out.WriteLine($"Set base DN: {server.BaseDn}");

            await Actions.ClearInputById(InputBindDn);
            await Actions.TypeIntoInputById(InputBindDn, server.BindDn);
            TestContext.Out.WriteLine($"Set bind DN: {server.BindDn}");

            await Actions.ClearInputById(InputBindPassword);
            await Actions.TypeIntoInputById(InputBindPassword, server.BindPassword);

            await Actions.ClearInputById(InputUserFilter);
            await Actions.TypeIntoInputById(InputUserFilter, server.UserFilter);

            await Actions.ClickElementById(ModalSaveCloseBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait2000();

            var rowInputs = await Page.QuerySelectorAllAsync($"input[id^='identity-provider-row-name-']");
            foreach (var input in rowInputs)
            {
                var value = await input.InputValueAsync();
                var inputId = await input.GetAttributeAsync("id");
                if (value == _zflexProviderName)
                {
                    _zflexProviderId = inputId?.Replace("identity-provider-row-name-", "");
                    TestContext.Out.WriteLine($"Found Zflex provider ID: {_zflexProviderId}");
                    break;
                }
            }

            // Assert
            Assert.That(_zflexProviderId, Is.Not.Null, "Zflex LDAP provider should be created");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Zflex LDAP Provider created successfully: {_zflexProviderName}");
        }

        [Test]
        [Order(12)]
        [Ignore("Zflexldap server is offline")]
        public async Task Step12_TestZflexLdapConnection()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 12: Test LDAP Connection to Zflexldap ===");

            if (string.IsNullOrEmpty(_zflexProviderId))
            {
                TestContext.Out.WriteLine("No Zflex provider was created - skipping");
                Assert.Inconclusive("No Zflex provider was created in previous step");
                return;
            }

            // Act
            var providerNameId = GetRowNameId(_zflexProviderId);
            var providerRow = await Actions.FindElementById(providerNameId);
            Assert.That(providerRow, Is.Not.Null, "Zflex provider row should exist");

            await providerRow!.ClickAsync();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Opened Zflex provider modal");

            await Actions.ClickElementById(ModalTabConnection);
            await Actions.Wait500();

            var testBtn = await Actions.FindElementById(ModalTestBtn);
            Assert.That(testBtn, Is.Not.Null, "Test Connection button should be visible");

            await testBtn!.ClickAsync();
            TestContext.Out.WriteLine("Clicked Test Connection button");

            await Actions.Wait3500();
            await Actions.Wait3500();

            var alertSuccess = await Page.QuerySelectorAsync(".alert-success");
            var alertDanger = await Page.QuerySelectorAsync(".alert-danger");

            if (alertSuccess != null)
            {
                var successText = await alertSuccess.InnerTextAsync();
                TestContext.Out.WriteLine($"Connection SUCCESS: {successText}");
            }
            else if (alertDanger != null)
            {
                var errorText = await alertDanger.InnerTextAsync();
                TestContext.Out.WriteLine($"Connection FAILED: {errorText}");
            }

            await Actions.ClickElementById(ModalCancelBtn);
            await Actions.Wait500();

            // Assert
            Assert.That(alertSuccess != null || alertDanger != null, Is.True,
                "Test connection should show a result");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Zflex LDAP connection test completed");
        }

        [Test]
        [Order(13)]
        [Ignore("Zflexldap server is offline")]
        public async Task Step13_SyncClientsFromZflexLdap()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 13: Sync Clients from Zflexldap ===");

            if (string.IsNullOrEmpty(_zflexProviderId))
            {
                TestContext.Out.WriteLine("No Zflex provider was created - skipping");
                Assert.Inconclusive("No Zflex provider was created in previous step");
                return;
            }

            // Act
            var providerNameId = GetRowNameId(_zflexProviderId);
            var providerRow = await Actions.FindElementById(providerNameId);
            await providerRow!.ClickAsync();
            await Actions.Wait1000();

            var useForImportCheckbox = await Actions.FindElementById(InputUseForImport);
            var isChecked = await useForImportCheckbox!.IsCheckedAsync();
            if (!isChecked)
            {
                await useForImportCheckbox.ClickAsync();
                TestContext.Out.WriteLine("Enabled 'Use for Import' option");
            }

            await Actions.ClickElementById(ModalSaveBtn);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();

            await Actions.ClickElementById(ModalTabConnection);
            await Actions.Wait500();

            var syncBtn = await Actions.FindElementById(ModalSyncBtn);
            await syncBtn!.ClickAsync();
            TestContext.Out.WriteLine("Clicked Sync Clients button");

            await Actions.Wait3500();
            await Actions.Wait3500();
            await Actions.Wait3500();

            var alertSuccess = await Page.QuerySelectorAsync(".alert-success");
            var alertDanger = await Page.QuerySelectorAsync(".alert-danger");

            if (alertSuccess != null)
            {
                var successText = await alertSuccess.InnerTextAsync();
                TestContext.Out.WriteLine($"Sync SUCCESS: {successText}");
            }
            else if (alertDanger != null)
            {
                var errorText = await alertDanger.InnerTextAsync();
                TestContext.Out.WriteLine($"Sync FAILED: {errorText}");
            }

            await Actions.ClickElementById(ModalCancelBtn);
            await Actions.Wait500();

            // Assert
            Assert.That(alertSuccess != null || alertDanger != null, Is.True,
                "Sync should show a result");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Zflex client sync completed");
        }

        [Test]
        [Order(14)]
        [Ignore("Zflexldap server is offline")]
        public async Task Step14_VerifyZflexClientsInAllAddress()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 14: Verify Zflex Clients in All-Address ===");
            var expectedUser = Zflexldap.ExpectedUser;

            // Act
            await Actions.ClickButtonById(MainNavIds.OpenEmployeesId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
            TestContext.Out.WriteLine("Navigated to All-Address view");

            await Actions.ClearInputById(ClientFilterIds.SearchInputId);
            await Actions.TypeIntoInputById(ClientFilterIds.SearchInputId, expectedUser);
            await Actions.Wait500();

            await Actions.ClickElementById(ClientFilterIds.SearchButtonId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait1000();
            TestContext.Out.WriteLine($"Searched for '{expectedUser}'");

            var clientRows = await Page.QuerySelectorAllAsync(ClientFilterIds.ClientRowSelector);
            TestContext.Out.WriteLine($"Found {clientRows.Count} client rows");

            var foundUser = false;
            foreach (var row in clientRows)
            {
                var rowText = await row.InnerTextAsync();
                TestContext.Out.WriteLine($"  Row: {rowText}");
                if (rowText.Contains(expectedUser, StringComparison.OrdinalIgnoreCase))
                {
                    foundUser = true;
                    TestContext.Out.WriteLine($"  -> Found {expectedUser}!");
                }
            }

            // Assert
            Assert.That(foundUser, Is.True, $"{expectedUser} should be found in All-Address after Zflex sync");
            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine("Zflex imported clients verified");

            await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
            await Actions.WaitForSpinnerToDisappear();
            await Actions.Wait500();
            await Actions.ScrollIntoViewById(Section);
            await Actions.Wait500();
        }

        [Test]
        [Order(15)]
        [Ignore("Zflexldap server is offline")]
        public async Task Step15_DeleteZflexLdapProvider()
        {
            // Arrange
            TestContext.Out.WriteLine("=== Step 15: Delete Zflex LDAP Provider ===");

            if (string.IsNullOrEmpty(_zflexProviderId))
            {
                TestContext.Out.WriteLine("No Zflex provider was created - skipping delete");
                Assert.Inconclusive("No Zflex provider was created in previous step");
                return;
            }

            // Act
            var deleteButtonId = GetRowDeleteId(_zflexProviderId);
            var deleteButton = await Actions.FindElementById(deleteButtonId);
            Assert.That(deleteButton, Is.Not.Null, "Delete button for Zflex provider should exist");

            await deleteButton!.ClickAsync();
            await Actions.Wait500();
            TestContext.Out.WriteLine("Clicked delete button");

            await Actions.ClickElementById("modal-delete-confirm");
            await Actions.Wait2000();
            TestContext.Out.WriteLine("Confirmed deletion");

            // Assert
            var deletedProvider = await Page.QuerySelectorAsync($"#identity-provider-row-name-{_zflexProviderId}");
            Assert.That(deletedProvider, Is.Null, "Deleted Zflex provider should no longer exist");

            Assert.That(_listener.HasApiErrors(), Is.False,
                $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

            TestContext.Out.WriteLine($"Zflex LDAP Provider {_zflexProviderName} deleted successfully");
            _zflexProviderId = null;
            _zflexProviderName = null;
        }
    }
}
