using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EuroGen.Watcher;

public partial class InternetWatcher(string siteToWatch, TimeSpan interval) : IDisposable
{
    private static readonly string BaseGoogle = "https://www.google.com";
    private readonly string _siteToWatch = siteToWatch;
    private readonly TimeSpan _interval = interval;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    public bool InternetAvailable { get; private set; } = false;
    public bool SiteAvailable { get; private set; } = false;

    public async Task WatchInternetState(Func<Task> functionOnChange)
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            InternetAvailable = await IsInternetAvailable();
            SiteAvailable = await IsSiteAvailable(_siteToWatch);

            if (InternetAvailable && SiteAvailable)
            {
                await functionOnChange.Invoke();

                StopWatching();

                return;
            }

            await Task.Delay(_interval, _cts.Token);
        }
    }

    public void StopWatching()
    {
        _cts.Cancel();
    }

    public static async Task<bool> IsInternetAvailable()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync(BaseGoogle);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> IsSiteAvailable(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cts.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
