using E2ETest.Constants;
using E2ETest.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using NUnit.Framework.Interfaces;

namespace E2ETest.Helpers;

public class PlaywrightSetup : PageTest
{
    public string BaseUrl { get; }

    public string UserName { get; }

    public string Password { get; }

    public string CurrentDate { get; }

    public string TimeStamp { get; }

    private Wrapper? _wrapper;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IPlaywright? _playwright;
    private readonly bool _isHeadless;
    private readonly IConfiguration _configuration;
    private readonly bool _recordVideo;
    private readonly bool _recordAllTests;
    private readonly bool _windowsMaximized = false;

    public PlaywrightSetup()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<PlaywrightSetup>(optional: true)
                .Build();

        UserName = _configuration["user"] ?? throw new InvalidOperationException("The user must not be zero or empty.");
        Password = _configuration["password"] ?? throw new InvalidOperationException("The password must not be zero or empty.");
        _recordVideo = bool.Parse(_configuration["PlaywrightConfig:RecordVideo"] ?? "false");
        _recordAllTests = bool.Parse(_configuration["PlaywrightConfig:RecordAllTests"] ?? "false");
        _isHeadless = bool.Parse(_configuration["PlaywrightConfig:HeadLess"] ?? "true");

        BaseUrl = _configuration["PlaywrightConfig:BaseUrl"] ?? throw new InvalidOperationException("Can't read the base url.");

        CurrentDate = DateTime.Now.ToString("dd.MM.yyyy");
        TimeStamp = DateTime.Now.Ticks.ToString();
    }

    public Wrapper Actions
    {
        get { return _wrapper!; }
    }

    /// <summary>
    /// Sets Browser/Context/Page and performs a one-time login before all tests.
    /// </summary>
    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await CreateBrowser();

        _wrapper = new Wrapper(Page);


        await Page.GotoAsync(BaseUrl + "login");


        TestContext.Out.WriteLine("Browser started");

        await Login();

        TestContext.Out.WriteLine("Logging successfully");
    }

    /// <summary>
    /// Saves recorded video in case of failure and then cleans up all resources.
    /// </summary>
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        try
        {
            await SaveAndProcessVideo();
        }
        finally
        {
            await CleanupResources();
        }
    }

    public new IPage Page => _page ?? throw new InvalidOperationException("Page is not initialized");

    public async Task Login()
    {
        TestContext.Out.WriteLine("start to login");

        await Actions.Wait500();
        var inputEmail = await Actions.FindElementById(LogInIds.InputEmailId);
        if (inputEmail != null)
        {
            await Actions.FillInputById(LogInIds.InputEmailId, UserName);
            await Actions.WaitForSpinnerToDisappear();
        }
        else
        {
            Assert.Fail("input email not found");
        }

        var inputPassword = await Actions.FindElementById(LogInIds.InputPasswordId);
        if (inputPassword != null)
        {
            await Actions.FillInputById(LogInIds.InputPasswordId, Password);
            await Actions.WaitForSpinnerToDisappear();
        }
        else
        {
            Assert.Fail("input password not found");
        }

        var ButtonSumit = await Actions.FindElementById(LogInIds.ButtonSumitId);
        if (ButtonSumit != null)
        {
            await Actions.ClickButtonById(LogInIds.ButtonSumitId);
            await Actions.WaitForSpinnerToDisappear();
        }
    }

    /// <summary>
    /// Copies the WebM video to the project directory and appends it if tests fail.
    /// </summary>
    private async Task SaveAndProcessVideo()
    {
        if (!_recordVideo || (!_recordAllTests && TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed))
        {
            return;
        }

        if (_page == null)
        {
            return;
        }

        if (_page!.Video == null)
        {
            return;
        }

        string? videoPath = null;
        string? targetPath = null;

        try
        {
            videoPath = await _page!.Video!.PathAsync();
            if (videoPath != null)
            {
                var testName = TestContext.CurrentContext.Test.Name;
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var status = TestContext.CurrentContext.Result.Outcome.Status.ToString();
                var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\.."));
                var videoFolder = Path.Combine(projectRoot, "Videos");
                Directory.CreateDirectory(videoFolder);
                targetPath = Path.Combine(videoFolder, $"{testName}_{status}_{timestamp}.webm");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Error getting video path: {ex.Message}");
            return;
        }

        if (videoPath != null && targetPath != null && File.Exists(videoPath))
        {
            for (int retry = 0; retry < 5; retry++)
            {
                try
                {
                    await Task.Delay(5000);

                    using (var sourceStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var destinationStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }

                    TestContext.AddTestAttachment(targetPath);

                    break;
                }
                catch (IOException) when (retry < 4)
                {
                    await Task.Delay(5000);
                    TestContext.WriteLine($"Retry {retry + 1} of 5 copying video file");
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Error copying video file: {ex.Message}");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Safely closes and disposes the Playwright page, context, and browser.
    /// </summary>
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

    /// <summary>
    /// Creates and configures the Playwright browser, context, and page, including video options.
    /// </summary>
    private async Task CreateBrowser()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = _isHeadless,
            SlowMo = 50,
            Args = _windowsMaximized ? new[] { "--start-fullscreen" } : null
        };

        _browser = await _playwright.Chromium.LaunchAsync(launchOptions);
        var contextOptions = new BrowserNewContextOptions();
        contextOptions.ViewportSize = new ViewportSize
        {
            Width = 1280,   // Statt 1920
            Height = 720    // Statt 1080
        };

        if (_recordVideo)
        {
            var videoDir = Path.Combine(Directory.GetCurrentDirectory(), "Videos");
            if (!Directory.Exists(videoDir))
            {
                Directory.CreateDirectory(videoDir);
                TestContext.Out.WriteLine($"Video directory created: {videoDir}");
            }

            TestContext.Out.WriteLine($"Using video directory: {videoDir}");
            TestContext.Out.WriteLine($"RecordVideo setting: {_recordVideo}");
            TestContext.Out.WriteLine($"RecordAllTests setting: {_recordAllTests}");

            contextOptions.RecordVideoDir = videoDir;
            contextOptions.RecordVideoSize = _windowsMaximized ? null : new RecordVideoSize
            {
                Width = 1280,   // Statt 1920
                Height = 720    // Statt 1080
            };
        }

        contextOptions.AcceptDownloads = true;

        _context = await _browser.NewContextAsync(contextOptions);
        _page = await _context.NewPageAsync();
    }
}
