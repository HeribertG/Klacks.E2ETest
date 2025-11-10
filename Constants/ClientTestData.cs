namespace E2ETest.Constants;

public class ClientData
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Zip { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string GroupLevel1 { get; set; } = string.Empty;

    public string GroupLevel2 { get; set; } = string.Empty;

    public string GroupLevel3 { get; set; } = string.Empty;

    public int ContractTemplateIndex { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Birthday { get; set; } = string.Empty;

    public string MembershipEntryDate { get; set; } = string.Empty;

    public string NoteText { get; set; } = string.Empty;

    public string AvatarImagePath { get; set; } = string.Empty;
}

public static class ClientTestData
{
    public static readonly ClientData[] Clients = new[]
    {
        new ClientData
        {
            FirstName = "Heribert",
            LastName = "Gasparoli",
            Gender = "1",
            Street = "Kirchstrasse 52",
            Zip = "3097",
            City = "Liebefeld",
            State = "BE",
            Country = "CH",
            GroupLevel1 = "Deutschweiz Mitte",
            GroupLevel2 = "BE",
            GroupLevel3 = "Bern",
            ContractTemplateIndex = 2,
            PhoneNumber = "791021402",
            Email = "hgasparoli@hotmail.com",
            Birthday = "25.10.1959",
            MembershipEntryDate = "01.10.2025",
            NoteText = "Versuch",
            AvatarImagePath = "C:\\SourceCode\\Klacks.Ui\\src\\assets\\png\\avatar.png"
        },
        new ClientData
        {
            FirstName = "Marie-Anne",
            LastName = "Gasparoli",
            Gender = "0",
            Street = "Kirchstrasse 52",
            Zip = "3097",
            City = "Liebefeld",
            State = "BE",
            Country = "CH",
            GroupLevel1 = "Deutschweiz Mitte",
            GroupLevel2 = "BE",
            GroupLevel3 = "Bern",
            ContractTemplateIndex = 3,
            PhoneNumber = "795801759",
            Email = "hgasparoli@gmx.ch",
            Birthday = "31.7.1953",
            MembershipEntryDate = "01.10.2025",
            NoteText = "Versuch 2",
            AvatarImagePath = "C:\\SourceCode\\Klacks.Ui\\src\\assets\\png\\Marie-Anne Gasparoli.png"
        },
        new ClientData
        {
            FirstName = "Tommaso",
            LastName = "Gasparoli",
            Gender = "1",
            Street = "Kastanienstrasse 52",
            Zip = "3098",
            City = "Köniz",
            State = "BE",
            Country = "CH",
            GroupLevel1 = "Deutschweiz Mitte",
            GroupLevel2 = "BE",
            GroupLevel3 = "Bern",
            ContractTemplateIndex = 1,
            PhoneNumber = "796974617",
            Email = "t.gasparoli@gmx.ch",
            Birthday = "1.11.1991",
            MembershipEntryDate = "01.10.2025",
            NoteText = "Versuch 3",
            AvatarImagePath = "C:\\SourceCode\\Klacks.Ui\\src\\assets\\png\\Tommaso.png"
        }
    };
}
