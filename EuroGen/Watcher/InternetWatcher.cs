using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EuroGen.Watcher;

public partial class InternetWatcher(string siteToWatch, TimeSpan interval) : IDisposable
{
    static readonly string baseGoogle = "https://www.google.com";
    readonly string siteToWatch = siteToWatch;
    readonly TimeSpan interval = interval;
    readonly CancellationTokenSource cts = new();
    bool disposed = false;

    public bool InternetAvailable { get; private set; } = false;
    public bool SiteAvailable { get; private set; } = false;

    public async Task WatchInternetState(Func<Task> functionOnChange)
    {
        while (!cts.Token.IsCancellationRequested)
        {
            InternetAvailable = await IsInternetAvailable();
            SiteAvailable = await IsSiteAvailable(siteToWatch);

            if (InternetAvailable && SiteAvailable)
            {
                await functionOnChange.Invoke();

                StopWatching();

                return;
            }

            await Task.Delay(interval, cts.Token);
        }
    }

    public void StopWatching()
    {
        cts.Cancel();
    }

    public static async Task<bool> IsInternetAvailable()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync(baseGoogle);
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
        if (!disposed)
        {
            if (disposing)
            {
				cts.Cancel();
				cts.Dispose();
			}

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
