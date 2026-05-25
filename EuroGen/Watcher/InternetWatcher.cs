namespace EuroGen.Watcher;

public partial class InternetWatcher(string siteToWatch, TimeSpan interval) : IDisposable
{
    readonly string siteToWatch = siteToWatch;
    readonly TimeSpan interval = interval;
    readonly CancellationTokenSource cts = new();
    bool disposed = false;
    TaskCompletionSource? tcsNetworkRestored;

    public static bool InternetAvailable => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    public bool SiteAvailable { get; private set; } = false;

    public async Task WatchInternetState(Func<Task> functionOnChange)
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (!InternetAvailable)
                {
                    SiteAvailable = false;
                    tcsNetworkRestored = new TaskCompletionSource();
                    using var reg = cts.Token.Register(() => tcsNetworkRestored.TrySetCanceled());
                    await tcsNetworkRestored.Task.ConfigureAwait(false);
                }

                if (cts.Token.IsCancellationRequested)
                {
                    break;
                }

                SiteAvailable = await IsSiteAvailable(siteToWatch);

                if (SiteAvailable)
                {
                    await functionOnChange.Invoke();
                    StopWatching();
                    return;
                }

                await Task.Delay(interval, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignorer l'exception d'annulation
        }
        finally
        {
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }
    }

    void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            tcsNetworkRestored?.TrySetResult();
        }
    }

    public void StopWatching()
    {
        cts.Cancel();
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
