namespace Klacks.E2ETest.Constants;

public static class QualificationsCardIds
{
    public const string Form = "client-qualifications-form";
    public const string AddButton = "add-qualification-button";
    public const string SelectPrefix = "qualificationId-";
    public const string LevelPrefix = "qualificationLevel-";
    public const string DeletePrefix = "delete-qualification-";

    public static string SelectId(int index) => SelectPrefix + index;

    public static string LevelId(int index) => LevelPrefix + index;

    public static string DeleteId(int index) => DeletePrefix + index;
}
