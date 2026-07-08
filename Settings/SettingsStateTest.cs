using Klacks.E2ETest.Constants;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(25)]
    [Category("Input")]
    public class SettingsStateTest : LookupTableSettingsTestBase
    {
        protected override string EntityLabel => "State";

        protected override string DbTableName => "state";

        protected override string SectionId => SettingsStatesIds.StatesSection;

        protected override string HeaderId => SettingsStatesIds.StatesHeader;

        protected override string AddButtonId => SettingsStatesIds.AddButton;

        protected override string NewRowAbbreviationId => SettingsStatesIds.NewRowAbbreviation;

        protected override string NewRowNameDeId => SettingsStatesIds.NewRowNameDe;

        protected override string NewRowPrefixId => SettingsStatesIds.NewRowPrefix;

        protected override string RowAbbreviationPrefixId => SettingsStatesIds.RowAbbreviationPrefix;

        protected override string RowDeletePrefixId => SettingsStatesIds.RowDeletePrefix;

        protected override string TestAbbreviationValue => SettingsStatesIds.TestAbbreviation;

        protected override string TestNameValue => SettingsStatesIds.TestName;

        protected override string TestPrefixValue => SettingsStatesIds.TestPrefix;
    }
}
