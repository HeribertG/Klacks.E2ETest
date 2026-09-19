// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.Playwright;

namespace Klacks.E2ETest.Helpers;

/// <summary>
/// Records the bodies of outgoing HTTP requests whose URL contains a given fragment.
/// </summary>
/// <param name="page">The page whose requests are observed</param>
/// <param name="urlFragment">URL fragment a request must contain to be recorded</param>
/// <param name="method">HTTP method a request must use to be recorded</param>
public sealed class RequestRecorder : IDisposable
{
    private readonly IPage _page;
    private readonly string _urlFragment;
    private readonly string _method;
    private readonly List<string> _bodies = new();
    private readonly object _gate = new();

    public RequestRecorder(IPage page, string urlFragment, string method)
    {
        _page = page;
        _urlFragment = urlFragment;
        _method = method;
        _page.Request += OnRequest;
    }

    public IReadOnlyList<string> Bodies
    {
        get
        {
            lock (_gate)
            {
                return _bodies.ToList();
            }
        }
    }

    public int Count => Bodies.Count;

    public void Clear()
    {
        lock (_gate)
        {
            _bodies.Clear();
        }
    }

    public void Dispose()
    {
        _page.Request -= OnRequest;
    }

    private void OnRequest(object? sender, IRequest request)
    {
        if (!request.Url.Contains(_urlFragment) || !string.Equals(request.Method, _method, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lock (_gate)
        {
            _bodies.Add(request.PostData ?? string.Empty);
        }
    }
}
