using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;
using static Klacks.E2ETest.Constants.LlmChatIds;
using static Klacks.E2ETest.Constants.SettingsCalendarRulesIds;
using static Klacks.E2ETest.Constants.SettingsCountriesIds;
using static Klacks.E2ETest.Constants.SettingsStatesIds;

namespace Klacks.E2ETest;

[TestFixture]
[Order(68)]
public class LlmCalendarRulesJapanTest : PlaywrightSetup
{
    private Listener _listener = null!;
    private int _messageCountBefore;

    private static readonly List<string> CreatedRuleNames = new();
    private static readonly List<string> CreatedPrefectureCodes = new();
    private static bool _countryCreated;

    private const string CountryCode = "JP";
    private const string CountryName = "Japan";
    private const string CountryPrefix = "+81";

    private static readonly (string Code, string Name)[] Prefectures =
    {
        ("TKY", "Tokyo"),
        ("KYT", "Kyoto"),
        ("OKN", "Okinawa"),
    };

    private static readonly (string Name, string Rule, string SubRule, string Description)[] NationalHolidays =
    {
        ("Neujahr", "01/01", "SU+1", "Ganjitsu - Neujahrstag"),
        ("Tag der Staatsgruendung", "02/11", "SU+1", "Kenkoku Kinen no Hi"),
        ("Geburtstag des Kaisers", "02/23", "SU+1", "Tenno Tanjobi"),
        ("Showa-Tag", "04/29", "SU+1", "Showa no Hi - Golden Week"),
        ("Verfassungsgedenktag", "05/03", "SU+1", "Kenpo Kinenbi - Golden Week"),
        ("Tag des Gruens", "05/04", "SU+1", "Midori no Hi - Golden Week"),
        ("Kindertag", "05/05", "SU+1", "Kodomo no Hi - Golden Week"),
        ("Tag der Berge", "08/11", "SU+1", "Yama no Hi"),
        ("Tag der Kultur", "11/03", "SU+1", "Bunka no Hi"),
        ("Tag des Dankes fuer Arbeit", "11/23", "SU+1", "Kinro Kansha no Hi"),
    };

    private static readonly (string Name, string Rule, string SubRule, string Description)[] HappyMondayHolidays =
    {
        ("Tag der Volljaehrigkeit", "01/08+00+MO", "", "Seijin no Hi - 2. Montag im Januar"),
        ("Tag des Meeres", "07/15+00+MO", "", "Umi no Hi - 3. Montag im Juli"),
        ("Tag der Achtung vor dem Alter", "09/15+00+MO", "", "Keiro no Hi - 3. Montag im September"),
        ("Tag des Sports", "10/08+00+MO", "", "Supotsu no Hi - 2. Montag im Oktober"),
    };

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
    public async Task Step01_AskLlmAboutJapaneseHolidays()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 1: Ask LLM About Japanese Holidays ===");
        await Actions.ClickButtonById(HeaderAssistantButton);
        await Actions.Wait1000();

        // Act
        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Welche nationalen Feiertage gibt es in Japan? Nenne mir die wichtigsten mit Datum.");
        var response = await WaitForBotResponse(_messageCountBefore, 90000);

        // Assert
        TestContext.Out.WriteLine($"Bot response: {response[..Math.Min(500, response.Length)]}");
        Assert.That(response, Is.Not.Empty, "Bot should respond with Japanese holidays");
        Assert.That(
            response.Contains("Neujahr", StringComparison.OrdinalIgnoreCase)
            || response.Contains("New Year", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Ganjitsu", StringComparison.OrdinalIgnoreCase)
            || response.Contains("1. Januar", StringComparison.OrdinalIgnoreCase)
            || response.Contains("January", StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Response should mention New Year's Day. Got: {response[..Math.Min(300, response.Length)]}");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("LLM provided Japanese holiday information");
    }

    [Test]
    [Order(2)]
    public async Task Step02_AskLlmAboutHappyMondaySystem()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 2: Ask LLM About Happy Monday System ===");
        await EnsureChatOpen();

        await Actions.ClickButtonById(ChatClearBtn);
        await Actions.Wait1000();
        await WaitForChatInputEnabled();

        // Act
        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Erklaere mir das Happy Monday System in Japan. Welche Feiertage werden auf Montag verschoben?");
        var response = await WaitForBotResponse(_messageCountBefore, 90000);

        // Assert
        TestContext.Out.WriteLine($"Bot response: {response[..Math.Min(500, response.Length)]}");
        Assert.That(response, Is.Not.Empty, "Bot should explain Happy Monday system");
        Assert.That(
            response.Contains("Montag", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Monday", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Happy", StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Response should mention Monday shifting. Got: {response[..Math.Min(300, response.Length)]}");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("LLM explained Happy Monday system");
    }

    [Test]
    [Order(3)]
    public async Task Step03_AskLlmAboutJapanesePrefectures()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 3: Ask LLM About Japanese Prefectures ===");
        await EnsureChatOpen();

        await Actions.ClickButtonById(ChatClearBtn);
        await Actions.Wait1000();
        await WaitForChatInputEnabled();

        // Act
        _messageCountBefore = await GetMessageCount();
        await SendChatMessage("Wie viele Praefekturen hat Japan? Nenne mir Tokyo, Kyoto und Okinawa mit ihren Besonderheiten.");
        var response = await WaitForBotResponse(_messageCountBefore, 90000);

        // Assert
        TestContext.Out.WriteLine($"Bot response: {response[..Math.Min(500, response.Length)]}");
        Assert.That(response, Is.Not.Empty, "Bot should respond with prefecture info");
        Assert.That(
            response.Contains("47", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Tokyo", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Kyoto", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Okinawa", StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Response should mention 47 prefectures or key cities. Got: {response[..Math.Min(300, response.Length)]}");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("LLM provided prefecture information");
    }

    [Test]
    [Order(4)]
    public async Task Step04_CreateJapanAsCountry()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 4: Create Japan as Country ===");
        await CloseChatIfOpen();

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(SettingsCountriesIds.CountriesSection);
        await Actions.Wait500();

        var existingJp = await Actions.FindInputIdByValue(SettingsCountriesIds.RowAbbreviationPrefix, CountryCode);
        if (existingJp != null)
        {
            TestContext.Out.WriteLine("Japan (JP) already exists - skipping creation");
            _countryCreated = true;
            return;
        }

        // Act
        await Actions.ClickButtonById(SettingsCountriesIds.AddButton);
        await Actions.Wait500();

        await Actions.FillInputById(SettingsCountriesIds.NewRowAbbreviation, CountryCode);
        await Actions.Wait500();

        await Actions.FillInputById(SettingsCountriesIds.NewRowNameDe, CountryName);
        await Actions.Wait500();

        await Actions.FillInputById(SettingsCountriesIds.NewRowPrefix, CountryPrefix);
        await Actions.Wait500();

        await Actions.ClickElementById(SettingsCountriesIds.CountriesHeader);
        TestContext.Out.WriteLine("Waiting for autoSave (3000ms)...");
        await Actions.Wait3000();

        // Assert
        var savedRowId = await Actions.FindInputIdByValue(SettingsCountriesIds.RowAbbreviationPrefix, CountryCode);
        Assert.That(savedRowId, Is.Not.Null, $"Country '{CountryCode}' should be saved");

        var countryId = savedRowId!.Replace(SettingsCountriesIds.RowAbbreviationPrefix, "");
        Assert.That(countryId, Is.Not.Empty.And.Not.EqualTo("undefined"), "Country should have a real GUID");

        _countryCreated = true;
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Japan created with ID: {countryId}");
    }

    [Test]
    [Order(5)]
    public async Task Step05_CreateJapanesePrefectures()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 5: Create Japanese Prefectures ===");

        if (!_countryCreated)
        {
            Assert.Inconclusive("Country was not created in previous step");
            return;
        }

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(SettingsStatesIds.StatesSection);
        await Actions.Wait500();

        // Act
        foreach (var (code, name) in Prefectures)
        {
            TestContext.Out.WriteLine($"Creating prefecture: {name} ({code})...");

            var existing = await Actions.FindInputIdByValue(SettingsStatesIds.RowAbbreviationPrefix, code);
            if (existing != null)
            {
                TestContext.Out.WriteLine($"  {name} ({code}) already exists - skipping");
                CreatedPrefectureCodes.Add(code);
                continue;
            }

            await Actions.ClickButtonById(SettingsStatesIds.AddButton);
            await Actions.Wait500();

            await Actions.FillInputById(SettingsStatesIds.NewRowAbbreviation, code);
            await Actions.Wait500();

            await Actions.FillInputById(SettingsStatesIds.NewRowNameDe, name);
            await Actions.Wait500();

            await Actions.FillInputById(SettingsStatesIds.NewRowPrefix, CountryCode);
            await Actions.Wait500();

            await Actions.ClickElementById(SettingsStatesIds.StatesHeader);
            TestContext.Out.WriteLine($"  Waiting for autoSave (3000ms)...");
            await Actions.Wait3000();

            var savedRowId = await Actions.FindInputIdByValue(SettingsStatesIds.RowAbbreviationPrefix, code);
            Assert.That(savedRowId, Is.Not.Null, $"Prefecture '{code}' should be saved");

            CreatedPrefectureCodes.Add(code);
            TestContext.Out.WriteLine($"  {name} ({code}) created successfully");
        }

        // Assert
        Assert.That(CreatedPrefectureCodes.Count, Is.EqualTo(Prefectures.Length),
            "All prefectures should be created");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"All {Prefectures.Length} prefectures created");
    }

    [Test]
    [Order(6)]
    public async Task Step06_CreateFixedDateHolidaysForTokyo()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 6: Create Fixed Date National Holidays for Tokyo ===");

        if (CreatedPrefectureCodes.Count == 0)
        {
            Assert.Inconclusive("No prefectures were created");
            return;
        }

        await NavigateToCalendarRules();

        var tokyoCode = "TKY";
        var selectedHolidays = NationalHolidays.Take(5).ToArray();

        // Act
        foreach (var (name, rule, subRule, description) in selectedHolidays)
        {
            var ruleName = $"JP {name}";
            TestContext.Out.WriteLine($"Creating: {ruleName} ({rule}) for {tokyoCode}...");

            await CreateCalendarRule(ruleName, rule, subRule, CountryCode, tokyoCode, description, true, true);
            CreatedRuleNames.Add(ruleName);

            TestContext.Out.WriteLine($"  {ruleName} created");
        }

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Created {selectedHolidays.Length} fixed-date holidays for Tokyo");
    }

    [Test]
    [Order(7)]
    public async Task Step07_CreateHappyMondayHolidaysForTokyo()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 7: Create Happy Monday Holidays for Tokyo ===");

        if (CreatedPrefectureCodes.Count == 0)
        {
            Assert.Inconclusive("No prefectures were created");
            return;
        }

        await NavigateToCalendarRules();

        var tokyoCode = "TKY";

        // Act
        foreach (var (name, rule, subRule, description) in HappyMondayHolidays)
        {
            var ruleName = $"JP {name}";
            TestContext.Out.WriteLine($"Creating Happy Monday: {ruleName} ({rule}) for {tokyoCode}...");

            await CreateCalendarRule(ruleName, rule, subRule, CountryCode, tokyoCode, description, true, true);
            CreatedRuleNames.Add(ruleName);

            TestContext.Out.WriteLine($"  {ruleName} created");
        }

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Created {HappyMondayHolidays.Length} Happy Monday holidays for Tokyo");
    }

    [Test]
    [Order(8)]
    public async Task Step08_CreateHolidaysForKyotoAndOkinawa()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 8: Create Key Holidays for Kyoto and Okinawa ===");

        if (CreatedPrefectureCodes.Count < 3)
        {
            Assert.Inconclusive("Not all prefectures were created");
            return;
        }

        await NavigateToCalendarRules();

        var otherPrefectures = new[] { "KYT", "OKN" };
        var keyHolidays = new[]
        {
            ("JP Neujahr", "01/01", "SU+1", "Ganjitsu - Neujahrstag"),
            ("JP Kindertag", "05/05", "SU+1", "Kodomo no Hi - Golden Week"),
            ("JP Tag des Meeres", "07/15+00+MO", "", "Umi no Hi - Happy Monday"),
        };

        // Act
        foreach (var prefCode in otherPrefectures)
        {
            TestContext.Out.WriteLine($"Creating holidays for {prefCode}...");

            foreach (var (name, rule, subRule, description) in keyHolidays)
            {
                var ruleName = $"{name} ({prefCode})";
                TestContext.Out.WriteLine($"  Creating: {ruleName}...");

                await CreateCalendarRule(ruleName, rule, subRule, CountryCode, prefCode, description, true, true);
                CreatedRuleNames.Add(ruleName);
            }

            TestContext.Out.WriteLine($"  {keyHolidays.Length} holidays created for {prefCode}");
        }

        // Assert
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine($"Created holidays for Kyoto and Okinawa");
    }

    [Test]
    [Order(9)]
    public async Task Step09_VerifyTokyoHolidaysViaFilter()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 9: Verify Tokyo Holidays via Filter ===");

        await NavigateToCalendarRules();

        // Act
        await SelectSpecificStateInFilter(CountryCode, "TKY");
        await Actions.Wait1000();

        var neujahr = await FindRuleWithPagination("JP Neujahr");
        var volljaehrigkeit = await FindRuleWithPagination("JP Tag der Volljaehrigkeit");
        var kindertag = await FindRuleWithPagination("JP Kindertag");

        // Assert
        TestContext.Out.WriteLine($"  Neujahr: {(neujahr.HasValue ? "FOUND" : "NOT FOUND")}");
        TestContext.Out.WriteLine($"  Volljaehrigkeit: {(volljaehrigkeit.HasValue ? "FOUND" : "NOT FOUND")}");
        TestContext.Out.WriteLine($"  Kindertag: {(kindertag.HasValue ? "FOUND" : "NOT FOUND")}");

        Assert.That(neujahr.HasValue, Is.True, "Neujahr rule should exist for Tokyo");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Tokyo holidays verified successfully");
    }

    [Test]
    [Order(10)]
    public async Task Step10_AskLlmToSummarizeCreatedRules()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 10: Ask LLM to Summarize ===");

        await Actions.ClickButtonById(HeaderAssistantButton);
        await Actions.Wait1000();
        await EnsureChatOpen();

        await Actions.ClickButtonById(ChatClearBtn);
        await Actions.Wait1000();
        await WaitForChatInputEnabled();

        // Act
        _messageCountBefore = await GetMessageCount();
        await SendChatMessage(
            $"Wir haben gerade japanische Feiertage als Kalenderregeln erstellt. " +
            $"Kannst du mir die Bedeutung des 'Kodomo no Hi' (Kindertag am 5. Mai) erklaeren? " +
            $"Und warum ist die 'Golden Week' in Japan so wichtig?");
        var response = await WaitForBotResponse(_messageCountBefore, 90000);

        // Assert
        TestContext.Out.WriteLine($"Bot response: {response[..Math.Min(500, response.Length)]}");
        Assert.That(response, Is.Not.Empty, "Bot should explain Kodomo no Hi");
        Assert.That(
            response.Contains("Kind", StringComparison.OrdinalIgnoreCase)
            || response.Contains("child", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Golden", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Mai", StringComparison.OrdinalIgnoreCase)
            || response.Contains("May", StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Response should mention Children's Day or Golden Week. Got: {response[..Math.Min(300, response.Length)]}");
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("LLM provided holiday explanation");
    }

    [Test]
    [Order(11)]
    public async Task Step11_CleanupCalendarRules()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 11: Cleanup Calendar Rules ===");

        if (CreatedRuleNames.Count == 0)
        {
            TestContext.Out.WriteLine("No rules to clean up");
            Assert.Pass("No rules were created");
            return;
        }

        await CloseChatIfOpen();
        await NavigateToCalendarRules();
        await SelectAllStatesInFilter();
        await Actions.Wait1000();

        // Act
        var deletedCount = 0;
        foreach (var ruleName in CreatedRuleNames.ToList())
        {
            TestContext.Out.WriteLine($"Deleting rule: {ruleName}...");
            var ruleIndex = await FindRuleWithPagination(ruleName);

            if (!ruleIndex.HasValue)
            {
                TestContext.Out.WriteLine($"  Rule '{ruleName}' not found - may already be deleted");
                continue;
            }

            var deleteBtn = await Actions.FindElementById(GetDeleteBtnId(ruleIndex.Value));
            if (deleteBtn == null)
            {
                TestContext.Out.WriteLine($"  Delete button not found for index {ruleIndex.Value}");
                continue;
            }

            await deleteBtn.ClickAsync();
            await Actions.Wait1000();

            var confirmBtn = await WaitForDeleteConfirm();
            if (confirmBtn != null)
            {
                await confirmBtn.ClickAsync();
                await Actions.WaitForSpinnerToDisappear();
                await Actions.Wait2000();
                deletedCount++;
                TestContext.Out.WriteLine($"  Deleted: {ruleName}");
            }
            else
            {
                TestContext.Out.WriteLine($"  WARNING: Delete confirm button not found for {ruleName}");
            }
        }

        // Assert
        TestContext.Out.WriteLine($"Deleted {deletedCount}/{CreatedRuleNames.Count} calendar rules");
        CreatedRuleNames.Clear();

        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Calendar rules cleanup completed");
    }

    [Test]
    [Order(12)]
    public async Task Step12_CleanupPrefectures()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 12: Cleanup Prefectures ===");

        if (CreatedPrefectureCodes.Count == 0)
        {
            TestContext.Out.WriteLine("No prefectures to clean up");
            Assert.Pass("No prefectures were created");
            return;
        }

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(SettingsStatesIds.StatesSection);
        await Actions.Wait500();

        // Act
        foreach (var code in CreatedPrefectureCodes.ToList())
        {
            TestContext.Out.WriteLine($"Deleting prefecture: {code}...");
            var savedRowId = await Actions.FindInputIdByValue(SettingsStatesIds.RowAbbreviationPrefix, code);
            if (savedRowId == null)
            {
                TestContext.Out.WriteLine($"  Prefecture {code} not found - already deleted");
                continue;
            }

            var stateId = savedRowId.Replace(SettingsStatesIds.RowAbbreviationPrefix, "");
            var deleteButtonId = $"{SettingsStatesIds.RowDeletePrefix}{stateId}";

            await Actions.ClickElementById(deleteButtonId);
            await Actions.Wait500();
            await Actions.ClickElementById(ModalIds.DeleteConfirm);
            await Actions.Wait3000();

            TestContext.Out.WriteLine($"  Deleted: {code}");
        }

        // Assert
        CreatedPrefectureCodes.Clear();
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Prefectures cleanup completed");
    }

    [Test]
    [Order(13)]
    public async Task Step13_CleanupCountry()
    {
        // Arrange
        TestContext.Out.WriteLine("=== Step 13: Cleanup Japan Country ===");

        if (!_countryCreated)
        {
            TestContext.Out.WriteLine("Country was not created - skipping");
            Assert.Pass("Country was not created");
            return;
        }

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(SettingsCountriesIds.CountriesSection);
        await Actions.Wait500();

        // Act
        var savedRowId = await Actions.FindInputIdByValue(SettingsCountriesIds.RowAbbreviationPrefix, CountryCode);
        if (savedRowId == null)
        {
            TestContext.Out.WriteLine("Japan country not found - already deleted");
            Assert.Pass("Country already deleted");
            return;
        }

        var countryId = savedRowId.Replace(SettingsCountriesIds.RowAbbreviationPrefix, "");
        var deleteButtonId = $"{SettingsCountriesIds.RowDeletePrefix}{countryId}";

        await Actions.ClickElementById(deleteButtonId);
        await Actions.Wait500();
        await Actions.ClickElementById(ModalIds.DeleteConfirm);
        await Actions.Wait3000();

        // Assert
        var deletedRow = await Actions.FindInputIdByValue(SettingsCountriesIds.RowAbbreviationPrefix, CountryCode);
        Assert.That(deletedRow, Is.Null, "Japan country should be deleted");

        _countryCreated = false;
        Assert.That(_listener.HasApiErrors(), Is.False,
            $"No API errors should occur. Error: {_listener.GetLastErrorMessage()}");

        TestContext.Out.WriteLine("Japan country deleted successfully");
    }

    #region Calendar Rule Helpers

    private async Task NavigateToCalendarRules()
    {
        var addBtn = await Actions.FindElementById(AddBtn);
        if (addBtn != null)
            return;

        await Actions.ClickButtonById(MainNavIds.OpenSettingsId);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait500();

        await Actions.ScrollIntoViewById(Section);
        await Actions.Wait500();
    }

    private async Task CreateCalendarRule(
        string name, string rule, string subRule,
        string country, string state, string description,
        bool isMandatory, bool isPaid)
    {
        var addButton = await Actions.FindElementById(AddBtn);
        Assert.That(addButton, Is.Not.Null, "Add button should exist");

        await addButton!.ClickAsync();
        await Actions.Wait1000();

        var modalHeader = await Actions.FindElementById(ModalHeader);
        Assert.That(modalHeader, Is.Not.Null, "Modal should be open");

        await Actions.ClearInputById(ModalInputName);
        await Actions.TypeIntoInputById(ModalInputName, name);

        await Actions.ClearInputById(ModalInputRule);
        await Actions.TypeIntoInputById(ModalInputRule, rule);

        if (!string.IsNullOrEmpty(subRule))
        {
            await Actions.ClearInputById(ModalInputSubRule);
            await Actions.TypeIntoInputById(ModalInputSubRule, subRule);
        }

        var countrySelect = await Actions.FindElementById(ModalInputCountry);
        if (countrySelect != null)
        {
            await countrySelect.SelectOptionAsync(new SelectOptionValue { Value = country });
            await Actions.Wait1000();
        }

        var stateSelect = await Actions.FindElementById(ModalInputState);
        if (stateSelect != null)
        {
            await stateSelect.SelectOptionAsync(new SelectOptionValue { Value = state });
            await Actions.Wait500();
        }

        if (isMandatory)
        {
            var mandatoryCheckbox = await Actions.FindElementById(ModalInputIsMandatory);
            if (mandatoryCheckbox != null)
            {
                var isChecked = await mandatoryCheckbox.IsCheckedAsync();
                if (!isChecked) await mandatoryCheckbox.ClickAsync();
            }
        }

        if (isPaid)
        {
            var paidCheckbox = await Actions.FindElementById(ModalInputIsPaid);
            if (paidCheckbox != null)
            {
                var isChecked = await paidCheckbox.IsCheckedAsync();
                if (!isChecked) await paidCheckbox.ClickAsync();
            }
        }

        if (!string.IsNullOrEmpty(description))
        {
            await Actions.ClearInputById(ModalInputDescription);
            await Actions.TypeIntoInputById(ModalInputDescription, description);
        }

        var resultLabel = await Actions.FindElementById(ModalResult);
        if (resultLabel != null)
        {
            var resultText = await resultLabel.InnerTextAsync();
            TestContext.Out.WriteLine($"    Calculated date: {resultText}");
        }

        var addModalBtn = await Actions.FindElementById(ModalAddBtn);
        Assert.That(addModalBtn, Is.Not.Null, "Modal Add button should exist");

        var isEnabled = await addModalBtn!.IsEnabledAsync();
        Assert.That(isEnabled, Is.True, $"Add button should be enabled for rule '{name}'");

        await addModalBtn.ClickAsync();
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait2000();
    }

    private async Task SelectAllStatesInFilter()
    {
        var dropdownBtn = await Actions.FindElementById(DropdownFormButton);
        if (dropdownBtn == null) return;

        await dropdownBtn.ClickAsync();
        await Actions.Wait500();

        var selectAllBtn = await Actions.FindElementById(DropdownSelectAllBtn);
        if (selectAllBtn != null)
        {
            await selectAllBtn.ClickAsync();
            await Actions.Wait500();
        }

        var closeBtn = await Actions.FindElementById(DropdownCloseBtn);
        if (closeBtn != null)
        {
            await closeBtn.ClickAsync();
            await Actions.Wait1000();
        }
        else
        {
            await dropdownBtn.ClickAsync();
            await Actions.Wait1000();
        }
    }

    private async Task SelectSpecificStateInFilter(string country, string state)
    {
        var dropdownBtn = await Actions.FindElementById(DropdownFormButton);
        if (dropdownBtn == null) return;

        await dropdownBtn.ClickAsync();
        await Actions.Wait500();

        var deselectAllBtn = await Actions.FindElementById(DropdownDeselectAllBtn);
        if (deselectAllBtn != null)
        {
            await deselectAllBtn.ClickAsync();
            await Actions.Wait500();
        }

        var checkboxId = GetDropdownStateCheckboxId(country, state);
        var stateCheckbox = await Actions.FindElementById(checkboxId);
        if (stateCheckbox != null)
        {
            var isChecked = await stateCheckbox.IsCheckedAsync();
            if (!isChecked)
            {
                await stateCheckbox.ClickAsync();
                await Actions.Wait500();
            }
        }

        var closeBtn = await Actions.FindElementById(DropdownCloseBtn);
        if (closeBtn != null)
        {
            await closeBtn.ClickAsync();
            await Actions.Wait1000();
        }
        else
        {
            await dropdownBtn.ClickAsync();
            await Actions.Wait1000();
        }
    }

    private async Task<int?> FindRuleInTableByName(string ruleName)
    {
        var rows = await Page.QuerySelectorAllAsync(RowSelector);
        foreach (var row in rows)
        {
            var rowId = await row.GetAttributeAsync("id");
            if (string.IsNullOrEmpty(rowId)) continue;

            var parts = rowId.Split('-');
            if (parts.Length < 4 || !int.TryParse(parts[^1], out int index))
                continue;

            var cellName = await Actions.FindElementById(GetCellNameId(index));
            if (cellName != null)
            {
                var nameText = await cellName.InnerTextAsync();
                if (nameText.Contains(ruleName))
                    return index;
            }
        }
        return null;
    }

    private async Task<int?> FindRuleWithPagination(string ruleName, int maxPages = 10)
    {
        var firstPageBtn = await Page.QuerySelectorAsync(
            $"#{Pagination} .page-item:not(.disabled) .page-link[aria-label='First']");
        if (firstPageBtn != null)
        {
            await firstPageBtn.ClickAsync();
            await Actions.Wait1000();
        }

        for (int page = 0; page < maxPages; page++)
        {
            var ruleIndex = await FindRuleInTableByName(ruleName);
            if (ruleIndex.HasValue)
                return ruleIndex;

            var nextPageBtn = await Page.QuerySelectorAsync(
                $"#{Pagination} .page-item:not(.disabled) .page-link[aria-label='Next']");
            if (nextPageBtn == null)
                break;

            await nextPageBtn.ClickAsync();
            await Actions.Wait1000();
        }
        return null;
    }

    private async Task<IElementHandle?> WaitForDeleteConfirm(int timeoutMs = 10000)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            var confirmBtn = await Actions.FindElementById("modal-delete-confirm");
            if (confirmBtn != null)
                return confirmBtn;
            await Actions.Wait500();
        }
        return null;
    }

    #endregion

    #region Chat Helper Methods

    private async Task EnsureChatOpen()
    {
        var chatInput = await Actions.FindElementById(ChatInput);
        if (chatInput == null)
        {
            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();
        }

        await WaitForChatInputEnabled();
    }

    private async Task CloseChatIfOpen()
    {
        var chatInput = await Actions.FindElementById(ChatInput);
        if (chatInput != null)
        {
            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait500();
        }
    }

    private async Task WaitForChatInputEnabled()
    {
        var maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var isEnabled = await WaitForInputEnabled(15000);
            if (isEnabled)
                return;

            TestContext.Out.WriteLine($"Chat input disabled (attempt {attempt + 1}/{maxRetries}), refreshing page...");
            await Actions.Reload();
            await Actions.Wait2000();

            await Actions.ClickButtonById(HeaderAssistantButton);
            await Actions.Wait1000();
        }

        Assert.Fail("Chat input remained disabled after multiple refresh attempts");
    }

    private async Task<bool> WaitForInputEnabled(int timeoutMs)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            var chatInput = await Actions.FindElementById(ChatInput);
            if (chatInput != null)
            {
                var isDisabled = await chatInput.IsDisabledAsync();
                if (!isDisabled)
                    return true;
            }

            await Actions.Wait500();
        }

        return false;
    }

    private async Task SendChatMessage(string message)
    {
        TestContext.Out.WriteLine($"Sending message: {message}");
        await Actions.FillInputWithDispatch(ChatInput, message);
        await Actions.ClickButtonById(ChatSendBtn);
    }

    private async Task<int> GetMessageCount()
    {
        var messages = await Actions.QuerySelectorAll($"#{ChatMessages} .message-wrapper.assistant");
        return messages.Count;
    }

    private async Task<string> WaitForBotResponse(int previousMessageCount, int timeoutMs = 60000)
    {
        TestContext.Out.WriteLine("Waiting for bot response...");

        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);

        while (DateTime.UtcNow - startTime < timeout)
        {
            var typingIndicator = await Actions.QuerySelector($"#{ChatMessages} .typing-indicator");
            var currentMessages = await Actions.QuerySelectorAll($"#{ChatMessages} .message-wrapper.assistant");

            if (typingIndicator == null && currentMessages.Count > previousMessageCount)
            {
                var lastMessage = currentMessages[currentMessages.Count - 1];
                var messageText = await Actions.QueryChildSelector(lastMessage, ".message-text");
                if (messageText != null)
                {
                    var text = await Actions.GetElementText(messageText);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        TestContext.Out.WriteLine($"Bot responded after {(DateTime.UtcNow - startTime).TotalSeconds:F1}s");
                        return text.Trim();
                    }
                }
            }

            await Actions.Wait500();
        }

        Assert.Fail($"Bot did not respond within {timeoutMs / 1000}s");
        return string.Empty;
    }

    #endregion
}
