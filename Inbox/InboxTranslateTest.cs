// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;

namespace Klacks.E2ETest.Inbox;

[TestFixture]
[Order(95)]
public class InboxTranslateTest : PlaywrightSetup
{
    private Listener? _listener;

    [SetUp]
    public void SetupInternal()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
    }

    [Test]
    [Order(1)]
    public async Task Translate_Email_ShowsTranslationAndTogglesBackToOriginal()
    {
        await Actions.NavigateTo(BaseUrl + "workplace/inbox");
        await Actions.WaitUntilUrlContains("inbox");

        var inboxFolder = await Actions.FindElementByCssSelector(".folder-item:has-text('Posteingang'), .folder-item:has-text('Inbox')");
        Assert.That(inboxFolder, Is.Not.Null, "Could not find the Inbox/Posteingang folder entry");
        await inboxFolder!.ClickAsync();

        var emailRow = await Actions.FindElementByCssSelector(".email-item:has-text('Your login request to Groq')");
        Assert.That(emailRow, Is.Not.Null, "Could not find the 'Your login request to Groq' email in the inbox list");
        await emailRow!.ClickAsync();

        var subjectElement = await Actions.FindElementByCssSelector(".detail-subject");
        Assert.That(subjectElement, Is.Not.Null, "Detail subject element not found after selecting email");
        var originalSubject = await subjectElement!.TextContentAsync();

        var translateButton = await Actions.FindElementByCssSelector("button[aria-label='Translate'], button[aria-label='Übersetzen']");
        Assert.That(translateButton, Is.Not.Null, "Icon-only translate button not found via aria-label");
        await translateButton!.ClickAsync();

        var toggleAfterTranslate = await Actions.FindElementByCssSelector("button:has-text('Show original'), button:has-text('Original anzeigen')");
        Assert.That(toggleAfterTranslate, Is.Not.Null, "'Show original' toggle should appear once translation succeeds");

        await Actions.ElementIsHiddenByCssSelector(".alert-warning");
        Assert.That(_listener!.HasApiErrors(), Is.False, $"API error during translation: {_listener!.GetLastErrorMessage()}");

        await Actions.ClickButtonByText("Show original", "Original anzeigen");

        var toggleAfterRevert = await Actions.FindElementByCssSelector("button:has-text('Show translation'), button:has-text('Übersetzung anzeigen')");
        Assert.That(toggleAfterRevert, Is.Not.Null, "'Show translation' toggle should reappear after switching back to original");

        var revertedSubjectElement = await Actions.FindElementByCssSelector(".detail-subject");
        var revertedSubject = await revertedSubjectElement!.TextContentAsync();

        Assert.That(revertedSubject, Is.EqualTo(originalSubject), "Toggling back should restore the original subject");
    }
}
