using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Microsoft.Playwright;

namespace Klacks.E2ETest;

[TestFixture]
public class DetailedEditAddressTest : PlaywrightSetup
{
    [Test]
    public async Task EditAddress_NewClient_ShouldNotFreeze()
    {
        // Arrange
        Page.Console += (_, msg) => Console.WriteLine($"[BROWSER] {msg.Type}: {msg.Text}");

        // Act - Navigate to new client page
        await Page.GotoAsync(BaseUrl + "workplace/edit-address",
            new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

        // Assert - Wait for specific elements (fails if page freezes)
        await Page.WaitForSelectorAsync("#gender", new() { Timeout = 10000 });
        await Page.WaitForSelectorAsync("#firstname", new() { Timeout = 5000 });
        await Page.WaitForSelectorAsync("#profile-name", new() { Timeout = 5000 });

        // Try to interact with elements (only works if page is responsive)
        var genderSelect = Page.Locator("#gender");
        await genderSelect.ClickAsync(new() { Timeout = 5000 });

        var firstnameInput = Page.Locator("#firstname");
        await firstnameInput.FillAsync("TestVorname", new() { Timeout = 5000 });

        var nameInput = Page.Locator("#profile-name");
        await nameInput.FillAsync("TestName", new() { Timeout = 5000 });

        // Verify values were actually set
        var firstname = await firstnameInput.InputValueAsync(new() { Timeout = 5000 });
        var name = await nameInput.InputValueAsync(new() { Timeout = 5000 });

        Assert.That(firstname, Is.EqualTo("TestVorname"), "Firstname field should be editable");
        Assert.That(name, Is.EqualTo("TestName"), "Name field should be editable");
    }

    [Test]
    public async Task EditAddress_ExistingClient_ShouldNotFreeze()
    {
        // Arrange
        Page.Console += (_, msg) => Console.WriteLine($"[BROWSER] {msg.Type}: {msg.Text}");

        // Act - First go to client list and click on edit button of first client
        await Page.GotoAsync(BaseUrl + "workplace/client",
            new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

        // Wait for client list to load and click edit button on first row
        await Page.WaitForSelectorAsync("#client-edit-button-0", new() { Timeout = 10000 });
        await Page.Locator("#client-edit-button-0").ClickAsync(new() { Timeout = 5000 });

        // Wait for edit page to load
        await Page.WaitForSelectorAsync("#gender", new() { Timeout = 10000 });
        await Page.WaitForSelectorAsync("#firstname", new() { Timeout = 5000 });

        // Assert - Try to interact - click on gender select
        var genderSelect = Page.Locator("#gender");
        await genderSelect.ClickAsync(new() { Timeout = 5000 });

        // Verify firstname has a value (existing client should have data)
        var firstnameInput = Page.Locator("#firstname");
        var firstname = await firstnameInput.InputValueAsync(new() { Timeout = 5000 });

        Assert.That(firstname, Is.Not.Empty, "Existing client should have a firstname");
    }

    [Test]
    public async Task EditAddress_MembershipDatePicker_ShouldWork()
    {
        // Arrange
        Page.Console += (_, msg) => Console.WriteLine($"[BROWSER] {msg.Type}: {msg.Text}");

        // Act - Navigate to new client
        await Page.GotoAsync(BaseUrl + "workplace/edit-address",
            new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

        // Assert - Wait for membership date picker (this was the problematic element)
        await Page.WaitForSelectorAsync("#membership-entry-date", new() { Timeout = 10000 });

        // Try to interact with the date picker
        var datePicker = Page.Locator("#membership-entry-date");
        await datePicker.ClickAsync(new() { Timeout = 5000 });

        // The page should still be responsive after clicking the date picker
        var firstnameInput = Page.Locator("#firstname");
        await firstnameInput.FillAsync("AfterDatePickerTest", new() { Timeout = 5000 });

        var value = await firstnameInput.InputValueAsync(new() { Timeout = 5000 });
        Assert.That(value, Is.EqualTo("AfterDatePickerTest"), "Page should remain responsive after date picker interaction");
    }
}
