namespace Klacks.E2ETest.Constants;

public static class SettingsIdentityProviderIds
{
    public const string Section = "settings-identity-providers";
    public const string Card = "identity-provider-card";
    public const string Header = "identity-provider-header";
    public const string Rows = "identity-provider-rows";
    public const string AddBtn = "identity-provider-add-btn";

    public const string HeaderContainer = "identity-provider-header-container";
    public const string HeaderName = "identity-provider-header-name";

    public const string ModalHeader = "identity-provider-modal-header";
    public const string ModalCloseBtn = "identity-provider-modal-close-btn";
    public const string ModalBody = "identity-provider-modal-body";

    public const string ModalInputName = "providerName";
    public const string ModalInputType = "type";

    public const string ModalTestBtn = "identity-provider-modal-test-btn";
    public const string ModalSyncBtn = "identity-provider-modal-sync-btn";

    public const string ModalTabGeneral = "identity-provider-modal-tab-general-link";
    public const string ModalTabConnection = "identity-provider-modal-tab-connection-link";
    public const string ModalTabOAuth = "identity-provider-modal-tab-oauth-link";

    public const string ModalGeneralContent = "identity-provider-modal-general-content";
    public const string ModalConnectionContent = "identity-provider-modal-connection-content";
    public const string ModalOAuthContent = "identity-provider-modal-oauth-content";

    public const string InputIsEnabled = "isEnabled";
    public const string InputUseForAuth = "useForAuth";
    public const string InputUseForImport = "useForImport";
    public const string InputSortOrder = "sortOrder";

    public const string InputHost = "host";
    public const string InputPort = "port";
    public const string InputUseSsl = "useSsl";
    public const string InputBaseDn = "baseDn";
    public const string InputBindDn = "bindDn";
    public const string InputBindPassword = "bindPassword";
    public const string InputUserFilter = "userFilter";

    public const string InputClientId = "clientId";
    public const string InputClientSecret = "clientSecret";
    public const string InputTenantId = "tenantId";
    public const string InputAuthorizationUrl = "authorizationUrl";
    public const string InputTokenUrl = "tokenUrl";
    public const string InputUserInfoUrl = "userInfoUrl";
    public const string InputScopes = "scopes";

    public const string ModalCancelBtn = "identity-provider-modal-cancel-btn";
    public const string ModalSaveBtn = "identity-provider-modal-save-btn";
    public const string ModalSaveCloseBtn = "identity-provider-modal-save-close-btn";
    public const string ModalFooter = "identity-provider-modal-footer";

    public static string GetRowContainerId(string providerId) => $"identity-provider-row-container-{providerId}";
    public static string GetRowNameId(string providerId) => $"identity-provider-row-name-{providerId}";
    public static string GetRowDeleteId(string providerId) => $"identity-provider-row-delete-{providerId}";
}

public record LdapTestServer(
    string Name,
    string Host,
    int Port,
    string BaseDn,
    string BindDn,
    string BindPassword,
    string UserFilter,
    string ExpectedUser
);

public static class SettingsIdentityProviderTestData
{
    public static readonly LdapTestServer Forumsys = new(
        Name: "Forumsys",
        Host: "ldap.forumsys.com",
        Port: 389,
        BaseDn: "dc=example,dc=com",
        BindDn: "cn=read-only-admin,dc=example,dc=com",
        BindPassword: "password",
        UserFilter: "(objectClass=person)",
        ExpectedUser: "Einstein"
    );

    public static readonly LdapTestServer Zflexldap = new(
        Name: "Zflexldap",
        Host: "zflexldap.com",
        Port: 389,
        BaseDn: "dc=zflexsoftware,dc=com",
        BindDn: "cn=ro_admin,ou=sysadmins,dc=zflexsoftware,dc=com",
        BindPassword: "zflexpass",
        UserFilter: "(objectClass=person)",
        ExpectedUser: "Guest"
    );

    public static readonly LdapTestServer[] AllServers = { Forumsys, Zflexldap };

    public const string ForumsysHost = "ldap.forumsys.com";
    public const int ForumsysPort = 389;
    public const string ForumsysBaseDn = "dc=example,dc=com";
    public const string ForumsysBindDn = "cn=read-only-admin,dc=example,dc=com";
    public const string ForumsysBindPassword = "password";
    public const string ForumsysUserFilter = "(objectClass=person)";
}
