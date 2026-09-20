// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.E2ETest.Constants;

/// <summary>
/// Selectors, routes, geometry and browser-side scripts for the tablet long-press context-menu tests
/// on the canvas based grids. Chromium runs the real gesture recognition through CDP touch input,
/// WebKit only the synthetic pointer sequence, which is exactly the iPadOS path where no native
/// contextmenu ever arrives.
/// </summary>
public static class TouchIds
{
    public const string BaseUrlEnvVar = "KLACKS_E2E_BASEURL";
    public const string DefaultBaseUrl = "http://localhost:4200/";

    public const string ChromiumEngine = "chromium";
    public const string WebkitEngine = "webkit";

    public const string LoginPath = "login";
    public const string ScheduleRoute = "workplace/schedule";
    public const string AbsenceRoute = "workplace/absence";

    public const string ScheduleCanvasSelector = "canvas[id^='template-canvas']";
    public const string AbsenceCanvasSelector = "#absence-surface-canvas";
    public const string AnyContextMenuSelector = "app-context-menu .menu-container[style*='display: block']";

    public const string PointerDownEvent = "pointerdown";
    public const string PointerUpEvent = "pointerup";
    public const string TouchPointerType = "touch";

    public const int PrimaryButton = 0;
    public const int NoButtonsMask = 0;
    public const int TouchPointerId = 1;

    public const string TouchStartType = "touchStart";
    public const string TouchEndType = "touchEnd";
    public const string DispatchTouchEventCommand = "Input.dispatchTouchEvent";
    public const string CdpTypeKey = "type";
    public const string CdpTouchPointsKey = "touchPoints";
    public const string CdpXKey = "x";
    public const string CdpYKey = "y";

    public const string DispatchPointerEventScript = @"
(el, a) => {
  const rect = el.getBoundingClientRect();
  el.dispatchEvent(new PointerEvent(a.eventType, {
    bubbles: true,
    cancelable: true,
    composed: true,
    view: window,
    button: a.button,
    buttons: a.buttons,
    clientX: rect.left + a.x,
    clientY: rect.top + a.y,
    pointerId: a.pointerId,
    pointerType: a.pointerType,
    isPrimary: true
  }));
}";

    public const string ContextMenuTrustProbeScript = @"
(() => {
  window.__klacksContextMenuLog = [];
  document.addEventListener('contextmenu', (e) => {
    window.__klacksContextMenuLog.push((e.isTrusted ? 'native' : 'synthetic') + '@' + Math.round(e.clientX) + ',' + Math.round(e.clientY));
  }, true);
})();";

    public const string ReadContextMenuLogScript = "() => (window.__klacksContextMenuLog || []).join(' | ')";

    public const string ReadMenuPositionScript = @"
(el) => {
  const rect = el.getBoundingClientRect();
  return Math.round(rect.left) + ',' + Math.round(rect.top);
}";

    public const float ScheduleProbeX = 260;
    public const float ScheduleProbeY = 110;
    public const float AbsenceProbeX = 260;
    public const float AbsenceProbeY = 110;

    public const int LongPressHoldMs = 900;
    public const int SyntheticHoldMs = 700;
    public const int DoubleOpenObservationMs = 800;
    public const int ContextMenuTimeoutMs = 4000;
    public const int MenuPollMs = 100;
    public const int GridLoadSettleMs = 2500;
    public const int UiSettleMs = 400;
    public const int LoginTimeoutMs = 15000;

    public const int ViewportWidth = 1280;
    public const int ViewportHeight = 800;
    public const string LocaleCode = "de-CH";

    public const string EvidenceFilePrefix = "touch-longpress-failure-";
}
