// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.WorkSchedule;

/// <summary>
/// Verifies that the schedule PDF export opens the generated document in a new browser tab
/// (blob: URL) instead of starting a file download. This covers the direct
/// openBlobInNewTab path used by SchedulePdfExportService.
/// </summary>
[TestFixture]
[Order(105)]
[Category("WorkSchedule")]
public class SchedulePdfExportTests : PlaywrightSetup
{
    private Listener? _listener;
    private readonly List<string> _consoleErrors = new();
    private readonly List<string> _downloads = new();
    private EventHandler<IConsoleMessage>? _consoleHandler;
    private EventHandler<IDownload>? _downloadHandler;

    private static string ScreenshotFolder
    {
        get
        {
            var projectRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
            var folder = Path.Combine(projectRoot, "Screenshots");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    [OneTimeSetUp]
    public async Task PrepareEnvironmentAsync()
    {
        await EnableExpertModeAndDismissOnboardingAsync();
        await Actions.SetLocalStorage(SchedulePdfExportIds.ViewModeStorageKey, SchedulePdfExportIds.TableViewModeValue);
    }

    [SetUp]
    public async Task SetupInternalAsync()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();

        _consoleErrors.Clear();
        _downloads.Clear();

        _consoleHandler = (_, msg) =>
        {
            if (msg.Type == "error")
            {
                _consoleErrors.Add(msg.Text);
            }
        };
        Page.Console += _consoleHandler;

        _downloadHandler = (_, download) => _downloads.Add(download.SuggestedFilename);
        Page.Download += _downloadHandler;

        await Actions.NavigateTo(BaseUrl + SchedulePdfExportIds.Route);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.ElementIsVisibleByCssSelector(SchedulePdfExportIds.PdfExportButtonSelector);
        await Actions.Wait2000();
    }

    [TearDown]
    public async Task CleanupAfterTestAsync()
    {
        if (_listener != null)
        {
            await _listener.WaitForResponseHandlingAsync();
            if (_listener.HasApiErrors())
            {
                TestContext.WriteLine(_listener.GetLastErrorMessage());
            }

            _listener.ResetErrors();
        }

        _listener = null;

        if (_consoleHandler != null)
        {
            Page.Console -= _consoleHandler;
            _consoleHandler = null;
        }

        if (_downloadHandler != null)
        {
            Page.Download -= _downloadHandler;
            _downloadHandler = null;
        }
    }

    [Test]
    public async Task SchedulePdfExportButton_OnClick_OpensPdfInNewTab()
    {
        // Arrange
        await Actions.TakeScreenshotAsync(Path.Combine(ScreenshotFolder, "schedule-pdf-01-page-loaded.png"));

        var button = await Actions.FindElementById(SchedulePdfExportIds.PdfExportButtonId);
        Assert.That(button, Is.Not.Null, "Schedule PDF export button not found on the schedule page");
        Assert.That(await Actions.IsDisabled(button), Is.False, "Schedule PDF export button is disabled");

        await Actions.ElementIsVisibleByCssSelector(SchedulePdfExportIds.RowHeaderRowSelector);

        // Act
        var popupUrl = await Actions.RunAndWaitForPopupUrlAsync(
            () => Actions.ClickButtonById(SchedulePdfExportIds.PdfExportButtonId),
            SchedulePdfExportIds.BlobUrlPrefix,
            SchedulePdfExportIds.PdfPopupTimeoutMs);

        // Assert
        await AssertPdfPopupAsync(popupUrl, "schedule-pdf");
    }

    private async Task AssertPdfPopupAsync(string? popupUrl, string screenshotPrefix)
    {
        var downloads = _downloads.Count == 0 ? "none" : string.Join(" | ", _downloads);
        var consoleErrors = _consoleErrors.Count == 0 ? "none" : string.Join(" | ", _consoleErrors);

        if (popupUrl == null)
        {
            await Actions.TakeScreenshotAsync(Path.Combine(ScreenshotFolder, $"{screenshotPrefix}-02-no-popup.png"));
            var toastText = await Actions.GetTextContentByCssSelector(SchedulePdfExportIds.ToastTextSelector);
            Assert.Fail(
                $"No new tab opened - the PDF was not shown in a new tab. Downloads triggered instead: {downloads}. " +
                $"Toast: '{toastText}'. Console errors: {consoleErrors}");
        }

        TestContext.Out.WriteLine($"Popup URL: {popupUrl}");
        TestContext.Out.WriteLine($"Downloads observed: {downloads}");
        await Actions.TakeScreenshotAsync(Path.Combine(ScreenshotFolder, $"{screenshotPrefix}-02-after-trigger.png"));

        Assert.That(popupUrl, Does.StartWith(SchedulePdfExportIds.BlobUrlPrefix),
            $"New tab did not open a blob: PDF preview - URL was '{popupUrl}'. Downloads: {downloads}");
        Assert.That(_listener!.HasApiErrors(), Is.False,
            $"API error while generating the schedule PDF: {_listener!.GetLastErrorMessage()}");
    }
}
