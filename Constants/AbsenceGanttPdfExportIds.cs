// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.E2ETest.Constants;

public static class AbsenceGanttPdfExportIds
{
    public const string Route = "workplace/absence";
    public const string HeaderPdfExportButtonId = "absence-gantt-pdf-export-button";
    public const string HeaderPdfExportButtonSelector = "#absence-gantt-pdf-export-button";
    public const string SurfaceCanvasSelector = "#absence-surface-canvas";
    public const string ListTabId = "absence-list-tab";
    public const string ListTabSelector = "#absence-list-tab";
    public const string MaskPdfExportId = "absence-mask-pdf-export";
    public const string MaskPdfExportSelector = "#absence-mask-pdf-export";
    public const string GridTableSelector = "#absence-grid-table";
    public const string ToastTextSelector = "span.toast-text";
    public const string BlobUrlPrefix = "blob:";
    public const int PdfPopupTimeoutMs = 120000;
    public const int MaxRowProbeCount = 12;
    public const float RowClickX = 40;
    public const float FirstRowClickY = 62;
    public const float RowHeight = 45;
}
