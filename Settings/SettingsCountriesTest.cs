using Klacks.E2ETest.Constants;

namespace Klacks.E2ETest
{
    [TestFixture]
    [Order(26)]
    [Category("Input")]
    public class SettingsCountriesTest : LookupTableSettingsTestBase
    {
        protected override string EntityLabel => "Country";

        protected override string DbTableName => "countries";

        protected override string SectionId => SettingsCountriesIds.CountriesSection;

        protected override string HeaderId => SettingsCountriesIds.CountriesHeader;

        protected override string AddButtonId => SettingsCountriesIds.AddButton;

        protected override string NewRowAbbreviationId => SettingsCountriesIds.NewRowAbbreviation;

        protected override string NewRowNameDeId => SettingsCountriesIds.NewRowNameDe;

        protected override string NewRowPrefixId => SettingsCountriesIds.NewRowPrefix;

        protected override string RowAbbreviationPrefixId => SettingsCountriesIds.RowAbbreviationPrefix;

        protected override string RowDeletePrefixId => SettingsCountriesIds.RowDeletePrefix;

        protected override string TestAbbreviationValue => SettingsCountriesIds.TestAbbreviation;

        protected override string TestNameValue => SettingsCountriesIds.TestName;

        protected override string TestPrefixValue => SettingsCountriesIds.TestPrefix;
    }
}
