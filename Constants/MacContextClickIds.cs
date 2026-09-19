// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.E2ETest.Constants;

/// <summary>
/// Selectors, routes, geometry and browser-side scripts for the emulated macOS Ctrl+Click
/// (Safari) context-click tests on the canvas based controls.
/// </summary>
public static class MacContextClickIds
{
    public const string BaseUrlEnvVar = "KLACKS_E2E_BASEURL";
    public const string DatabaseUrlEnvVar = "DATABASE_URL";
    public const string DefaultBaseUrl = "http://localhost:4200/";
    public const string DefaultConnectionString = "Host=localhost;Port=5434;Database=klacks_fresh;Username=postgres;Password=admin";

    public const string ApiRoutePattern = "https://localhost:5001/**";
    public const string OptionsMethod = "OPTIONS";
    public const string OriginHeader = "origin";
    public const string RequestHeadersHeader = "access-control-request-headers";
    public const string AllowOriginHeader = "access-control-allow-origin";
    public const string AllowCredentialsHeader = "access-control-allow-credentials";
    public const string AllowMethodsHeader = "access-control-allow-methods";
    public const string AllowHeadersHeader = "access-control-allow-headers";
    public const string VaryHeader = "vary";
    public const string TrueValue = "true";
    public const string AllMethods = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
    public const int PreflightStatus = 204;

    public const string LoginPath = "login";
    public const string ScheduleRoute = "workplace/schedule";
    public const string ContainerTemplateRoutePrefix = "workplace/container-template/";
    public const string AvailabilityRoute = "workplace/client-availability";
    public const string AbsenceRoute = "workplace/absence";

    public const string ScheduleCanvasSelector = "canvas[id^='template-canvas']";
    public const string TimeRulerCanvasSelector = "app-time-ruler canvas.main-canvas";
    public const string AbsenceCanvasSelector = "#absence-surface-canvas";
    public const string AbsenceContextMenuSelector = "#absence-surface-context-menu .menu-container";
    public const string AvailabilitySurfaceSelector = "#availability-surface-canvas";
    public const string AnyContextMenuSelector = "app-context-menu .menu-container[style*='display: block']";

    public const string AvailabilitySnapshotSql = "SELECT id, is_available, is_deleted FROM client_availability";
    public const string AvailabilityRowsOfDaySql = "SELECT id FROM client_availability WHERE client_id = @client AND date = @date";
    public const string AvailabilityResetRowSql = "UPDATE client_availability SET is_available = @isAvailable, is_deleted = @isDeleted WHERE id = @id";
    public const string AvailabilityDeleteRowSql = "DELETE FROM client_availability WHERE id = @id";
    public const int IsoDateLength = 10;
    public const string ClientParameter = "client";
    public const string DateParameter = "date";
    public const string IdParameter = "id";
    public const string IsAvailableParameter = "isAvailable";
    public const string IsDeletedParameter = "isDeleted";
    public const string AvailabilityBulkUrlPart = "ClientAvailabilities/Bulk";
    public const string HttpPostMethod = "POST";
    public const string AvailabilityIsAvailableJsonKey = "isAvailable";
    public const string AvailabilityItemsJsonKey = "items";

    public const string MousedownEvent = "mousedown";
    public const string MouseupEvent = "mouseup";
    public const string MousemoveEvent = "mousemove";
    public const string ContextMenuEvent = "contextmenu";

    public const string WResizeCursor = "w-resize";

    public const int PrimaryButton = 0;
    public const int SecondaryButton = 2;
    public const int PrimaryButtonsMask = 1;
    public const int NoButtonsMask = 0;

    public const string MacPlatform = "MacIntel";
    public const string MacUserAgentDataPlatform = "macOS";
    public const string MacUserAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15";

    public const string PlatformOverrideScript = @"
(() => {
  const define = (target, name, value) => {
    try { Object.defineProperty(target, name, { get: () => value, configurable: true }); } catch (e) { }
  };
  define(Navigator.prototype, 'platform', '" + MacPlatform + @"');
  define(Navigator.prototype, 'userAgent', '" + MacUserAgent + @"');
  if ('userAgentData' in navigator) {
    define(Navigator.prototype, 'userAgentData', { platform: '" + MacUserAgentDataPlatform + @"', mobile: false, brands: [] });
  }
})();";

    public const string DispatchMouseEventScript = @"
(el, a) => {
  const rect = el.getBoundingClientRect();
  const init = {
    bubbles: true,
    cancelable: true,
    composed: true,
    view: window,
    button: a.button,
    buttons: a.buttons,
    ctrlKey: a.ctrlKey,
    clientX: rect.left + a.x,
    clientY: rect.top + a.y
  };
  el.dispatchEvent(new MouseEvent(a.eventType, init));
}";

    public const string ReadBodyCursorScript = "() => document.body.style.cursor";

    public const string ReadPlatformScript = "() => navigator.platform";

    public const string CanvasRegionSignatureScript = @"
(c, a) => {
  const ctx = c.getContext('2d');
  const box = c.getBoundingClientRect();
  const sx = c.width / box.width;
  const sy = c.height / box.height;
  const data = ctx.getImageData(
    Math.floor(a.x * sx), Math.floor(a.y * sy),
    Math.max(1, Math.floor(a.width * sx)), Math.max(1, Math.floor(a.height * sy))).data;
  let hash = 2166136261;
  for (let i = 0; i < data.length; i++) {
    hash ^= data[i];
    hash = Math.imul(hash, 16777619) >>> 0;
  }
  return hash.toString(16);
}";

    public const string FindUniformRunScript = @"
(c, a) => {
  const ctx = c.getContext('2d');
  const box = c.getBoundingClientRect();
  const sx = c.width / box.width;
  const sy = c.height / box.height;
  const key = (d, i) => d[i] + ',' + d[i + 1] + ',' + d[i + 2];
  for (let y = a.fromY; y < Math.min(a.toY, box.height); y += a.stepY) {
    const row = ctx.getImageData(0, Math.floor(y * sy), c.width, 1).data;
    const counts = new Map();
    for (let i = 0; i < row.length; i += 4) {
      const k = key(row, i);
      counts.set(k, (counts.get(k) || 0) + 1);
    }
    let background = null;
    let best = 0;
    for (const [k, n] of counts) { if (n > best) { best = n; background = k; } }
    if (a.edgeBackground) { background = key(row, 0); }
    let runStart = 0;
    for (let i = 4; i <= row.length; i += 4) {
      const same = i < row.length && key(row, i) === key(row, i - 4);
      if (same) continue;
      const runLength = (i / 4 - runStart) / sx;
      if (runLength >= a.minRun && key(row, runStart * 4) !== background && row[runStart * 4 + 3] === 255) {
        return [(runStart + (i / 4 - runStart) / 2) / sx, y];
      }
      runStart = i / 4;
    }
  }
  return null;
}";

    public const float ScheduleProbeX = 260;
    public const float ScheduleProbeY = 110;

    public const float AvailabilityCellX = 19;
    public const float AvailabilityCellY = 71;
    public const float AvailabilityNeighbourCellX = 57;
    public const float AvailabilityRegionWidth = 200;
    public const float AvailabilityRegionHeight = 96;
    public const float AvailabilityRegionTop = 55;

    public const float BarScanFromY = 45;
    public const float BarScanToY = 700;
    public const float BarScanStepY = 3;
    public const float BarMinRunPx = 60;
    public const float BarDragDistancePx = 120;
    public const float BarPositionTolerancePx = 2;

    public const float RulerScanFromY = 60;
    public const float RulerScanToY = 700;
    public const float RulerScanStepY = 3;
    public const float RulerMinRunPx = 100;
    public const float RulerEmptyAreaX = 2;
    public const float RulerHoverStepPx = 30;
    public const float RulerHoverX = 30;
    public const float RulerSecondShiftScanOffsetY = 100;
    public const float RulerInsideShiftOffsetY = 10;
    public const float RulerHoverToY = 400;

    public const int DebounceSettleMs = 2500;
    public const int UiSettleMs = 400;
    public const int GridLoadSettleMs = 2500;
    public const int BarStableTimeoutMs = 8000;
    public const int BarStablePollMs = 300;
    public const int HttpErrorStatusFrom = 400;
    public const int PageEventLimit = 60;
    public const string EvidenceFilePrefix = "macclick-failure-";
    public const int LoginTimeoutMs = 15000;
    public const int ContextMenuTimeoutMs = 3000;
    public const int ViewportWidth = 1280;
    public const int ViewportHeight = 720;
    public const string LocaleCode = "de-CH";
}
