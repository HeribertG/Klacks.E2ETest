using Microsoft.Playwright;

namespace E2ETest.Wrappers;

/// <summary>
/// Monitors and handles API responses in Playwright tests, providing error detection and timeout management.
/// This class listens for HTTP responses, tracks error states, and manages asynchronous response handling.
/// </summary>
/// <remarks>
/// The Listener class provides functionality to:
/// - Monitor HTTP responses for error status codes (4xx and 5xx)
/// - Maintain error state and error messages
/// - Handle response timeouts
/// - Reset error states between test runs
/// 
/// Usage example:
/// var listener = new Listener(page);
/// listener.RecognizeApiErrors();
/// await listener.WaitForResponseHandlingAsync();
/// if (listener.HasApiErrors()) {
///     var errorMessage = listener.GetLastErrorMessage();
/// }
/// </remarks>
public sealed class Listener
{
    private readonly IPage _page;
    private bool _hasApiError = false;
    private string _lastErrorMessage = string.Empty;
    private TaskCompletionSource<bool> _responseHandled;

    public Listener(IPage page)
    {
        _page = page;
        _responseHandled = new TaskCompletionSource<bool>();
    }

    public void RecognizeApiErrors()
    {
        _page.Response += async (_, response) =>
        {
            try
            {
                if ((response.Status >= 400 && response.Status < 500) || (response.Status >= 500 && response.Status < 600))
                {
                    _hasApiError = true;
                    _lastErrorMessage = $"API Error detected: {response.Status} for {response.Url}: {await response.TextAsync()}";
                }
            }
            catch (Exception ex)
            {
                _hasApiError = true;
                _lastErrorMessage = $"Error processing response: {ex.Message}";
            }
            finally
            {
                _responseHandled.TrySetResult(true);
            }
        };
    }

    /// <summary>
    /// Waits for the completion of API response handling with a timeout mechanism.
    /// If no response is received within 5 seconds, the wait operation is terminated.
    /// The overall timeout is set to 30 seconds to prevent indefinite hanging.
    /// </summary>
    /// <returns>A task representing the asynchronous wait operation.</returns>
    /// <remarks>
    /// The method uses a cancellation token to enforce timeouts and ensures that
    /// the response handling task is always completed, either naturally or through timeout.
    /// If an error occurs during the wait, it is logged before being re-thrown.
    /// </remarks>
    /// <exception cref="Exception">Thrown when an error occurs during response handling.</exception>
    public async Task WaitForResponseHandlingAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var completedTask = await Task.WhenAny(
                _responseHandled.Task,
                Task.Delay(5000, cts.Token)
            );

            if (completedTask != _responseHandled.Task)
            {
                _responseHandled.TrySetResult(true);
                TestContext.WriteLine("Response handling timed out after 5 seconds");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Error waiting for response handling: {ex.Message}");
            _responseHandled.TrySetResult(true);
            throw;
        }
    }

    public bool HasApiErrors()
    {
        return _hasApiError;
    }

    public string GetLastErrorMessage()
    {
        return _lastErrorMessage;
    }

    public void ResetErrors()
    {
        _hasApiError = false;
        _lastErrorMessage = string.Empty;
        _responseHandled = new TaskCompletionSource<bool>();
    }
}