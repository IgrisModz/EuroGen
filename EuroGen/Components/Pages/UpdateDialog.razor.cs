using EuroGen.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EuroGen.Components.Pages;

public partial class UpdateDialog : IDisposable
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public UpdateInfo? UpdateInfo { get; set; }

    double progressPercentage = 0;
    string downloadedSize = "0 MB";
    string totalSize = "0 MB";
    string downloadSpeed = "0 MB/s";
    bool isDownloading = false;
    bool isPaused = false;
    CancellationTokenSource? cts;
    bool isDownloaded = false;
    string timeRemaining = "0s";
	bool disposedValue;

	bool IsMandatory => UpdateInfo is null || UpdateInfo.IsMandatory;

    protected override async Task OnInitializedAsync()
    {
        if (UpdateInfo is null)
        {
            MudDialog.Cancel();
            return;
        }

        UpdateService.DestinationPath = Path.Combine(UpdateService.AppDirectory, UpdateInfo!.FileName);

        UpdateService.ProgressChanged += ReportProgress;
        UpdateService.DownloadCompleted += () =>
        {
            isDownloaded = true;
            ReportProgress(0, 0);
            InvokeAsync(StateHasChanged);
        };

        cts = new CancellationTokenSource();
        await UpdateService.InitProgress(UpdateInfo, cts.Token);
        ReportProgress(UpdateService.ExistingLength, UpdateService.TotalLength);

        if (UpdateService.CheckIfAlreadyDownloaded(UpdateInfo))
        {
            isDownloaded = true;
        }
    }

    async Task StartUpdate()
    {
        try
        {
            await UpdateService.RequestPermissionsAsync();

            isDownloading = true;
            isPaused = false;
            cts = new CancellationTokenSource();
            UpdateService.DestinationPath = Path.Combine(UpdateService.AppDirectory, UpdateInfo!.FileName);

            var result = await UpdateService.DownloadAndVerifyAsync(UpdateInfo, UpdateService.DestinationPath, cts.Token);

            if (!result)
            {
                await ShowError(Localizer["DownloadError"]);
            }
        }
        catch (Exception ex)
        {
            await ShowError($"{Localizer["Error"]}: {ex.Message}");
        }
        finally
        {
            isDownloading = false;
            cts?.Dispose();
            cts = null;
        }
    }

    void Pause()
    {
        cts?.Cancel();
        isPaused = true;
    }


    async Task Resume()
    {
        await StartUpdate();
    }

    void Cancel()
    {
        cts?.Cancel();
        MudDialog.Cancel();
    }

    async Task Install()
    {
        await UpdateService.LaunchInstaller(UpdateInfo!);
    }

    void ReportProgress(long current, long total)
    {
        progressPercentage = total > 0 ? current * 100.0 / total : 0;
        downloadedSize = FormatBytes(current);
        totalSize = FormatBytes(total);
        downloadSpeed = $"{FormatBytes(UpdateService.DownloadSpeedBytesPerSecond)}/s";
        double estimatedTime = double.IsNaN(UpdateService.DownloadSpeedBytesPerSecond) || UpdateService.DownloadSpeedBytesPerSecond <= 0
    ? -1
    : (total - current) / UpdateService.DownloadSpeedBytesPerSecond;
        timeRemaining = estimatedTime < 0 ? $"0{Localizer["Second"]}" : FormatTimeRemaining(estimatedTime);
        InvokeAsync(StateHasChanged);
    }

    string FormatBytes(double bytes)
    {
        const double terabyte = 1024.0 * 1024.0 * 1024.0 * 1024.0;
        const double gigabyte = 1024.0 * 1024.0 * 1024.0;
        const double megabyte = 1024.0 * 1024.0;
        const double kilobyte = 1024.0;

        if (bytes >= terabyte)
		{
			return $"{bytes / terabyte:F2} {Localizer["Terabyte"]}";
		}

		if (bytes >= gigabyte)
		{
			return $"{bytes / gigabyte:F2} {Localizer["Gigabyte"]}";
		}

		if (bytes >= megabyte)
		{
			return $"{bytes / megabyte:F2} {Localizer["Megabyte"]}";
		}

		return bytes >= kilobyte ? $"{bytes / kilobyte:F2} {Localizer["Kilobyte"]}" : $"{bytes:F0} {Localizer["Byte"]}";
	}

	string FormatTimeRemaining(double seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
		if (time.TotalHours >= 1)
		{
			return $"{(int)time.TotalHours}{Localizer["Hour"]} {time.Minutes}{Localizer["Minute"]}";
		}

		return time.TotalMinutes >= 1
			? $"{(int)time.TotalMinutes}{Localizer["Minute"]} {time.Seconds}{Localizer["Second"]}"
			: $"{time.Seconds}{Localizer["Second"]}";
	}

	static Task ShowError(string message)
    {
        Console.WriteLine(message);
        return Task.CompletedTask; // TODO: improve with MudDialog
    }
    static Typo ConvertTypo(Typo typo)
    {
        return typo switch
        {
            Typo.h1 => Typo.h4,
            Typo.h3 => Typo.h5,
            _ => typo
        };
    }

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				if (cts != null)
				{
					cts.Cancel();
					cts.Dispose();
					cts = null;
				}
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
