using Klacks.E2ETest.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Klacks.E2ETest.Helpers;

public class OAuth2TestSetup : PageTest
{
    public string BaseUrl { get; }

    public string ApiBaseUrl { get; }

    private Wrapper? _wrapper;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IPlaywright? _playwright;
    private readonly bool _isHeadless;
    private readonly IConfiguration _configuration;

    public OAuth2TestSetup()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<OAuth2TestSetup>(optional: true)
                .Build();

        _isHeadless = bool.Parse(_configuration["PlaywrightConfig:HeadLess"] ?? "true");
        BaseUrl = _configuration["PlaywrightConfig:BaseUrl"] ?? throw new InvalidOperationException("Can't read the base url.");
        ApiBaseUrl = _configuration["PlaywrightConfig:ApiBaseUrl"] ?? "http://localhost:5000/api/backend/";
    }

    public Wrapper Actions => _wrapper!;

    public new IPage Page => _page ?? throw new InvalidOperationException("Page is not initialized");

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await CreateBrowser();
        _wrapper = new Wrapper(Page);
        TestContext.Out.WriteLine("Browser started for OAuth2 tests");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await CleanupResources();
    }

    private async Task CleanupResources()
    {
        try
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }

            if (_context != null)
            {
                await _context.CloseAsync();
                await _context.DisposeAsync();
                _context = null;
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                await _browser.DisposeAsync();
                _browser = null;
            }

            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
            }

            _wrapper = null;
            TestContext.WriteLine("Browser cleanup completed");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Cleanup error: {ex.Message}");
        }
    }

    private async Task CreateBrowser()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = _isHeadless,
            SlowMo = 50
        };

        _browser = await _playwright.Chromium.LaunchAsync(launchOptions);
        var contextOptions = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = 1280,
                Height = 720
            },
            AcceptDownloads = true
        };

        _context = await _browser.NewContextAsync(contextOptions);
        _page = await _context.NewPageAsync();

        await _context.GrantPermissionsAsync(Array.Empty<string>());
    }
}
