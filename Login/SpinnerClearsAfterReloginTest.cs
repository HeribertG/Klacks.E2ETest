using Klacks.E2ETest.Helpers;

namespace Klacks.E2ETest.Login;

/// <summary>
/// Reproduces the reported bug: after Klacks sits idle until the access AND
/// refresh token are invalid, the next navigation fires a burst of parallel
/// requests that all 401. The first triggers a (failing) token refresh; the
/// others used to hang forever, leaking the global loading spinner so the
/// dashboard spinner kept spinning after re-login. This test forces that exact
/// situation and asserts the spinner clears (and the app is usable) after the
/// re-login. Requires a running app (frontend 4200 + backend 5001).
/// </summary>
[TestFixture]
[Order(43)]
[Ignore("Live spinner/re-login flow: requires the running app (frontend 4200 + backend 5001). Run on demand, not in fresh-DB CI.")]
public class SpinnerClearsAfterReloginTest : PlaywrightSetup
{
    private const string SpinnerSelector = ".lds-ripple";

    [Test]
    public async Task ReLogin_AfterTokenInvalidation_SpinnerClearsAndDashboardLoads()
    {
        TestContext.Out.WriteLine("=== Settle on the dashboard ===");
        await Actions.NavigateTo($"{BaseUrl}workplace/dashboard");
        await Actions.WaitForSpinnerToDisappear();
        await Actions.Wait1000();

        TestContext.Out.WriteLine("=== Invalidate access + refresh token (idle expiry) ===");
        await InvalidateTokens();

        TestContext.Out.WriteLine("=== Reload dashboard -> parallel 401 storm -> refresh fails -> login ===");
        await Actions.NavigateTo($"{BaseUrl}workplace/dashboard");
        await Actions.WaitUntilUrlContains("login");

        TestContext.Out.WriteLine("=== Re-login ===");
        await Login();
        await Actions.WaitUntilUrlNotContaining("login");

        TestContext.Out.WriteLine("=== Assert: the global spinner must stop (bug = spins forever) ===");
        var cleared = await WaitForSpinnerGone(40, 500);
        Assert.That(cleared, Is.True,
            "After re-login the global loading spinner must disappear; it must not keep spinning forever.");

        TestContext.Out.WriteLine("=== Assert: the app is usable again (back on a workplace page) ===");
        await Actions.WaitUntilUrlContains("workplace");
        Assert.That(Actions.ReadCurrentUrl(), Does.Contain("workplace"),
            "After re-login the user should be back on a usable workplace page.");
    }

    private async Task<bool> WaitForSpinnerGone(int attempts, int delayMs)
    {
        for (var i = 0; i < attempts; i++)
        {
            var spinner = await Actions.QuerySelector(SpinnerSelector);
            if (spinner == null)
            {
                return true;
            }
            await Task.Delay(delayMs);
        }
        return await Actions.QuerySelector(SpinnerSelector) == null;
    }

    private async Task InvalidateTokens()
    {
        await Page.EvaluateAsync(@"() => {
            localStorage.setItem('JWT_TOKEN', 'invalid-expired-token');
            localStorage.setItem('JWT_REFRESH', 'invalid-expired-token');
        }");
    }
}
