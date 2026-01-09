using E2ETest.Constants;
using E2ETest.Helpers;
using E2ETest.Wrappers;
using Microsoft.Playwright;

namespace E2ETest.Login;

[TestFixture]
public class OAuth2SsoTest : OAuth2TestSetup
{
    private Listener? _listener;

    [SetUp]
    public void SetupInternal()
    {
        _listener = new Listener(Page);
        _listener.RecognizeApiErrors();
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
            _listener?.ResetErrors();
        }
        _listener = null;
    }

    [Test]
    [Order(1)]
    public async Task VerifyOAuth2ProvidersDisplayed()
    {
        // Arrange
        await Page.GotoAsync(BaseUrl + "login");
        await Actions.Wait1000();

        // Act
        var ssoButton = await Actions.FindElementByCssSelector(".oauth2-btn, [class*='oauth2']");

        // Assert
        Assert.That(ssoButton, Is.Not.Null, "No OAuth2/SSO button found on login page");
        TestContext.Out.WriteLine("OAuth2 SSO button found on login page");
    }

    [Test]
    [Order(2)]
    public async Task VerifyOAuth2AuthorizeRedirect()
    {
        // Arrange
        await Page.GotoAsync(BaseUrl + "login");
        await Actions.Wait1000();

        var ssoButton = await Actions.FindElementByCssSelector(".oauth2-btn");
        if (ssoButton == null)
        {
            Assert.Ignore("No OAuth2 provider configured - skipping test");
            return;
        }

        // Act
        var responseTask = Page.WaitForResponseAsync(r => r.Url.Contains("OAuth2/authorize"));
        await ssoButton.ClickAsync();

        // Assert
        var response = await responseTask;
        Assert.That(response.Status, Is.EqualTo(200), "OAuth2 authorize endpoint should return 200");
        TestContext.Out.WriteLine("OAuth2 authorize endpoint returned successfully");
    }

    [Test]
    [Order(3)]
    public async Task VerifyOAuth2CallbackWithTestCode()
    {
        // Arrange
        var providerId = await GetFirstOAuth2ProviderId();
        if (providerId == null)
        {
            Assert.Ignore("No OAuth2 provider configured - skipping test");
            return;
        }

        var state = $"{providerId}_{Guid.NewGuid():N}";
        var redirectUri = BaseUrl + "oauth2/callback";

        // Act - First navigate to any page to set localStorage
        await Page.GotoAsync(BaseUrl + "login");
        await Actions.Wait500();

        await Page.EvaluateAsync($@"
            localStorage.setItem('oauth2_state', '{state}');
            localStorage.setItem('oauth2_redirect_uri', '{redirectUri}');
        ");

        var callbackUrl = $"{BaseUrl}oauth2/callback?code={OAuth2Ids.E2ETestCode}&state={state}";
        await Page.GotoAsync(callbackUrl);
        await Actions.Wait2000();

        // Assert
        var currentUrl = Page.Url;
        Assert.That(currentUrl, Does.Contain("workplace").Or.Contain("dashboard"),
            $"Should redirect to workplace after OAuth2 login, but URL is: {currentUrl}");

        TestContext.Out.WriteLine("OAuth2 callback with test code succeeded - user logged in");
    }

    [Test]
    [Order(4)]
    public async Task VerifyOAuth2ErrorHandling()
    {
        // Arrange - First navigate to set localStorage
        await Page.GotoAsync(BaseUrl + "login");
        await Actions.Wait500();

        await Page.EvaluateAsync(@"
            localStorage.setItem('oauth2_state', 'test-state');
        ");

        // Act
        var callbackUrl = $"{BaseUrl}oauth2/callback?error=access_denied&error_description=User%20cancelled%20login";
        await Page.GotoAsync(callbackUrl);
        await Actions.Wait1000();

        // Assert
        var errorElement = await Actions.FindElementByCssSelector(".alert-danger, [class*='error']");
        Assert.That(errorElement, Is.Not.Null, "Error message should be displayed when OAuth2 fails");

        var backButton = await Actions.FindElementByCssSelector("button");
        Assert.That(backButton, Is.Not.Null, "Back to login button should be present");

        TestContext.Out.WriteLine("OAuth2 error handling works correctly");
    }

    private async Task<string?> GetFirstOAuth2ProviderId()
    {
        try
        {
            var response = await Page.APIRequest.GetAsync($"{ApiBaseUrl}OAuth2/providers");
            if (!response.Ok)
            {
                return null;
            }

            var json = await response.JsonAsync();
            if (json?.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var first = json?.EnumerateArray().FirstOrDefault();
                if (first.HasValue && first.Value.TryGetProperty("id", out var id))
                {
                    return id.GetString();
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
