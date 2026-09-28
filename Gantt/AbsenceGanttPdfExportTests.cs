// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.E2ETest.Constants;
using Klacks.E2ETest.Helpers;
using Klacks.E2ETest.Wrappers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.Gantt;

/// <summary>
/// Verifies the two PDF exports of the absence gantt page. The header export uses the direct
/// openBlobInNewTab helper, the mask export goes through ReportService.openPdfPreview. Both must
/// show the document in a new browser tab (blob: URL) instead of starting a file download.
/// </summary>
[TestFixture]
[Order(34)]
[Category("Gantt")]
public class AbsenceGanttPdfExportTests : PlaywrightSetup
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

        await Actions.NavigateTo(BaseUrl + AbsenceGanttPdfExportIds.Route);
        await Actions.WaitForSpinnerToDisappear();
        await Actions.ElementIsVisibleByCssSelector(AbsenceGanttPdfExportIds.SurfaceCanvasSelector);
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
    [Order(1)]
    public async Task HeaderPdfExportButton_OnClick_OpensPdfInNewTab()
    {
        // Arrange
        await Actions.TakeScreenshotAsync(Path.Combine(ScreenshotFolder, "absence-gantt-header-pdf-01-page-loaded.png"));
        await Actions.ElementIsVisibleByCssSelector(AbsenceGanttPdfExportIds.HeaderPdfExportButtonSelector);

        // Act
        var popupUrl = await Actions.RunAndWaitForPopupUrlAsync(
            () => Actions.ClickButtonById(AbsenceGanttPdfExportIds.HeaderPdfExportButtonId),
            AbsenceGanttPdfExportIds.BlobUrlPrefix,
            AbsenceGanttPdfExportIds.PdfPopupTimeoutMs);

        // Assert
        await AssertPdfPopupAsync(popupUrl, "absence-gantt-header-pdf");
    }

    [Test]
    [Order(2)]
    public async Task MaskPdfExport_OnSelectedClientWithAbsences_OpensPdfInNewTab()
    {
        // Arrange
        var rowFound = await SelectFirstRowWithAbsencesAsync();
        if (!rowFound)
        {
            Assert.Ignore(
                "No client row with absences found on the absence gantt page - the mask PDF export " +
                "is only rendered for a selected client that has at least one absence entry.");
        }

        await Actions.TakeScreenshotAsync(Path.Combine(ScreenshotFolder, "absence-gantt-mask-pdf-01-list-tab.png"));

        // Act
        var popupUrl = await Actions.RunAndWaitForPopupUrlAsync(
            () => Actions.ClickButtonById(AbsenceGanttPdfExportIds.MaskPdfExportId),
            AbsenceGanttPdfExportIds.BlobUrlPrefix,
            AbsenceGanttPdfExportIds.PdfPopupTimeoutMs);

        // Assert
        await AssertPdfPopupAsync(popupUrl, "absence-gantt-mask-pdf");
    }

    /// <summary>
    /// Clicks the gantt canvas row by row until a client is selected whose list tab exposes the
    /// PDF export icon. The icon is only shown for a client with at least one absence entry.
    /// </summary>
    /// <returns>True when such a row was selected and the list tab is active</returns>
    private async Task<bool> SelectFirstRowWithAbsencesAsync()
    {
        for (var rowIndex = 0; rowIndex < AbsenceGanttPdfExportIds.MaxRowProbeCount; rowIndex++)
        {
            var y = AbsenceGanttPdfExportIds.FirstRowClickY + (rowIndex * AbsenceGanttPdfExportIds.RowHeight);
            await Actions.ClickByCssSelectorAtPosition(
                AbsenceGanttPdfExportIds.SurfaceCanvasSelector,
                AbsenceGanttPdfExportIds.RowClickX,
                y);
            await Actions.Wait1000();

            if (!await Actions.IsElementVisibleById(AbsenceGanttPdfExportIds.ListTabId))
            {
                continue;
            }

            await Actions.ClickButtonById(AbsenceGanttPdfExportIds.ListTabId);
            await Actions.Wait1000();

            if (await Actions.IsElementVisibleById(AbsenceGanttPdfExportIds.MaskPdfExportId))
            {
                TestContext.Out.WriteLine($"Selected gantt row {rowIndex} - mask PDF export is available");
                return true;
            }
        }

        return false;
    }

    private async Task AssertPdfPopupAsync(string? popupUrl, string screenshotPrefix)
    {
        var downloads = _downloads.Count == 0 ? "none" : string.Join(" | ", _downloads);
        var consoleErrors = _consoleErrors.Count == 0 ? "none" : string.Join(" | ", _consoleErrors);

        if (popupUrl == null)
        {
            await Actions.TakeScreenshotAsync(Path.Combine(ScreenshotFolder, $"{screenshotPrefix}-02-no-popup.png"));
            var toastText = await Actions.GetTextContentByCssSelector(AbsenceGanttPdfExportIds.ToastTextSelector);
            Assert.Fail(
                $"No new tab opened - the PDF was not shown in a new tab. Downloads triggered instead: {downloads}. " +
                $"Toast: '{toastText}'. Console errors: {consoleErrors}");
        }

        TestContext.Out.WriteLine($"Popup URL: {popupUrl}");
        TestContext.Out.WriteLine($"Downloads observed: {downloads}");
        await Actions.TakeScreenshotAsync(Path.Combine(ScreenshotFolder, $"{screenshotPrefix}-02-after-trigger.png"));

        Assert.That(popupUrl, Does.StartWith(AbsenceGanttPdfExportIds.BlobUrlPrefix),
            $"New tab did not open a blob: PDF preview - URL was '{popupUrl}'. Downloads: {downloads}");
        Assert.That(_listener!.HasApiErrors(), Is.False,
            $"API error while generating the absence PDF: {_listener!.GetLastErrorMessage()}");
    }
}
