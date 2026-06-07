using System.Diagnostics;
using System.Text.RegularExpressions;
using Klacks.E2ETest.Helpers;
using Microsoft.Playwright;

namespace Klacks.E2ETest.WorkSchedule;

/// <summary>
/// Verifies that on a cold Schedule open the work-schedule (Works/Schedule) and
/// shift-schedule (Shifts/Schedule) requests fire in parallel rather than the
/// shift request only starting after the work grid has finished chunk-loading.
/// Captures the full request/response timeline so the cancel-restart pattern of
/// the old sequential behaviour (a second startRow:0 shift load firing late) is
/// visible. The fix collapses the shift load to a single early request.
/// </summary>
[TestFixture]
[Order(102)]
public class ScheduleParallelLoadTest : PlaywrightSetup
{
    private const string WorkScheduleMarker = "Works/Schedule";
    private const string ShiftScheduleMarker = "Shifts/Schedule";

    private sealed record Hit(long Ms, int? StartRow, string Kind);

    [Test]
    public async Task ColdOpen_WorkAndShift_FireInParallel()
    {
        var clock = Stopwatch.StartNew();
        var hits = new List<Hit>();
        var startRowRegex = new Regex("\"startRow\"\\s*:\\s*(\\d+)", RegexOptions.Compiled);
        IResponse? firstShiftResponse = null;

        void OnRequest(object? _, IRequest request)
        {
            var kind = ClassifyRequest(request.Url);
            if (kind is null)
            {
                return;
            }

            int? startRow = null;
            var body = request.PostData;
            if (!string.IsNullOrEmpty(body))
            {
                var match = startRowRegex.Match(body);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed))
                {
                    startRow = parsed;
                }
            }

            lock (hits)
            {
                hits.Add(new Hit(clock.ElapsedMilliseconds, startRow, $"{kind}-REQ"));
            }
        }

        void OnResponse(object? _, IResponse response)
        {
            var kind = ClassifyRequest(response.Url);
            if (kind is null)
            {
                return;
            }

            if (kind == "SHIFT" && firstShiftResponse is null && response.Status == 200)
            {
                firstShiftResponse = response;
            }

            lock (hits)
            {
                hits.Add(new Hit(clock.ElapsedMilliseconds, null, $"{kind}-RESP({response.Status})"));
            }
        }

        Page.Request += OnRequest;
        Page.Response += OnResponse;

        try
        {
            await Page.GotoAsync(BaseUrl + "workplace/schedule?testMode");

            await WaitUntilAsync(
                () =>
                {
                    lock (hits)
                    {
                        return hits.Any(h => h.Kind.StartsWith("WORK-RESP"))
                            && hits.Any(h => h.Kind.StartsWith("SHIFT-RESP"));
                    }
                },
                timeoutMs: 25000);

            // Let any late (cancel-restart) shift reload surface before asserting.
            await Task.Delay(2500);
        }
        finally
        {
            Page.Request -= OnRequest;
            Page.Response -= OnResponse;
        }

        List<Hit> ordered;
        lock (hits)
        {
            ordered = hits.OrderBy(h => h.Ms).ToList();
        }

        TestContext.Out.WriteLine("=== Cold Schedule open — network timeline (ms from navigation) ===");
        foreach (var hit in ordered)
        {
            var startRowText = hit.StartRow.HasValue ? $" startRow={hit.StartRow}" : string.Empty;
            TestContext.Out.WriteLine($"  {hit.Ms,6} ms  {hit.Kind}{startRowText}");
        }

        var firstWorkReq = ordered.FirstOrDefault(h => h.Kind == "WORK-REQ");
        var firstShiftReq = ordered.FirstOrDefault(h => h.Kind == "SHIFT-REQ");
        var firstWorkResp = ordered.FirstOrDefault(h => h.Kind.StartsWith("WORK-RESP"));
        var firstShiftResp = ordered.FirstOrDefault(h => h.Kind.StartsWith("SHIFT-RESP"));
        var initialShiftLoads = ordered.Count(h => h.Kind == "SHIFT-REQ" && h.StartRow == 0);

        Assert.That(firstWorkReq, Is.Not.Null, "Works/Schedule request never fired");
        Assert.That(firstShiftReq, Is.Not.Null, "Shifts/Schedule request never fired — initial shift load is missing");

        var requestGap = Math.Abs(firstWorkReq!.Ms - firstShiftReq!.Ms);
        TestContext.Out.WriteLine($"--- first WORK request : {firstWorkReq.Ms} ms");
        TestContext.Out.WriteLine($"--- first SHIFT request: {firstShiftReq.Ms} ms");
        TestContext.Out.WriteLine($"--- first WORK response: {firstWorkResp?.Ms}");
        TestContext.Out.WriteLine($"--- first SHIFT response: {firstShiftResp?.Ms}");
        TestContext.Out.WriteLine($"--- request-start gap (parallel if small): {requestGap} ms");
        TestContext.Out.WriteLine($"--- initial (startRow=0) SHIFT loads: {initialShiftLoads}  (1 = single load, >1 = cancel-restart / holiday refetch)");

        Assert.That(requestGap, Is.LessThan(1500),
            $"Shift request started {requestGap} ms after the work request — not loading in parallel.");

        Assert.That(firstShiftResponse, Is.Not.Null, "no successful shift response captured");
        var payload = await firstShiftResponse!.JsonAsync();
        Assert.That(payload, Is.Not.Null, "shift response had no JSON body");
        var shiftCount = payload!.Value.GetProperty("shifts").GetArrayLength();
        var totalCount = payload.Value.GetProperty("totalCount").GetInt32();
        TestContext.Out.WriteLine($"--- first shift payload: shifts={shiftCount}, totalCount={totalCount}");
        Assert.That(totalCount, Is.GreaterThan(0), "shift totalCount should be > 0 (repository count path)");
        Assert.That(shiftCount, Is.GreaterThan(0), "first shift chunk should contain shifts (repository pagination path)");
    }

    private static string? ClassifyRequest(string url)
    {
        if (url.Contains(WorkScheduleMarker))
        {
            return "WORK";
        }
        if (url.Contains(ShiftScheduleMarker))
        {
            return "SHIFT";
        }
        return null;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }
            await Task.Delay(100);
        }
    }
}
