using EuroGen.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EuroGen.Components.Pages;

public partial class UpdateDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public UpdateInfo? UpdateInfo { get; set; }

    private double _progressPercentage = 0;
    private string _downloadedSize = "0 MB";
    private string _totalSize = "0 MB";
    private string _downloadSpeed = "0 MB/s";
    private bool _isDownloading = false;
    private bool _isPaused = false;
    private CancellationTokenSource? _cts;
    private bool _isDownloaded = false;
    private string _timeRemaining = "0s";
    private bool IsMandatory => UpdateInfo is null || UpdateInfo.IsMandatory;

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
            _isDownloaded = true;
            ReportProgress(0, 0);
            InvokeAsync(StateHasChanged);
        };

        _cts = new CancellationTokenSource();
        await UpdateService.InitProgress(UpdateInfo, _cts.Token);
        ReportProgress(UpdateService.ExistingLength, UpdateService.TotalLength);

        if (UpdateService.CheckIfAlreadyDownloaded(UpdateInfo))
        {
            _isDownloaded = true;
        }
    }

    private async Task StartUpdate()
    {
        try
        {
            await UpdateService.RequestPermissionsAsync();

            _isDownloading = true;
            _isPaused = false;
            _cts = new CancellationTokenSource();
            UpdateService.DestinationPath = Path.Combine(UpdateService.AppDirectory, UpdateInfo!.FileName);

            var result = await UpdateService.DownloadAndVerifyAsync(UpdateInfo, UpdateService.DestinationPath, _cts.Token);

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
            _isDownloading = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Pause()
    {
        _cts?.Cancel();
        _isPaused = true;
    }


    private async Task Resume()
    {
        await StartUpdate();
    }

    private void Cancel()
    {
        _cts?.Cancel();
        MudDialog.Cancel();
    }

    private async Task Install()
    {
        await UpdateService.LaunchInstaller(UpdateInfo!);
    }

    private void ReportProgress(long current, long total)
    {
        _progressPercentage = total > 0 ? current * 100.0 / total : 0;
        _downloadedSize = FormatBytes(current);
        _totalSize = FormatBytes(total);
        _downloadSpeed = $"{FormatBytes(UpdateService.DownloadSpeedBytesPerSecond)}/s";
        double estimatedTime = double.IsNaN(UpdateService.DownloadSpeedBytesPerSecond) || UpdateService.DownloadSpeedBytesPerSecond <= 0
    ? -1
    : (total - current) / UpdateService.DownloadSpeedBytesPerSecond;
        _timeRemaining = estimatedTime < 0 ? $"0{Localizer["Second"]}" : FormatTimeRemaining(estimatedTime);
        InvokeAsync(StateHasChanged);
    }

    private string FormatBytes(double bytes)
    {
        const double terabyte = 1024.0 * 1024.0 * 1024.0 * 1024.0;
        const double gigabyte = 1024.0 * 1024.0 * 1024.0;
        const double megabyte = 1024.0 * 1024.0;
        const double kilobyte = 1024.0;

        if (bytes >= terabyte)
            return $"{bytes / terabyte:F2} {Localizer["Terabyte"]}";
        if (bytes >= gigabyte)
            return $"{bytes / gigabyte:F2} {Localizer["Gigabyte"]}";
        if (bytes >= megabyte)
            return $"{bytes / megabyte:F2} {Localizer["Megabyte"]}";
        if (bytes >= kilobyte)
            return $"{bytes / kilobyte:F2} {Localizer["Kilobyte"]}";
        return $"{bytes:F0} {Localizer["Byte"]}";
    }

    private string FormatTimeRemaining(double seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}{Localizer["Hour"]} {time.Minutes}{Localizer["Minute"]}";
        if (time.TotalMinutes >= 1)
            return $"{(int)time.TotalMinutes}{Localizer["Minute"]} {time.Seconds}{Localizer["Second"]}";
        return $"{time.Seconds}{Localizer["Second"]}";
    }

    private Task ShowError(string message)
    {
        Console.WriteLine(message);
        return Task.CompletedTask; // à améliorer avec MudDialog
    }
    private Typo ConvertTypo(Typo typo)
    {
        return typo switch
        {
            Typo.h1 => Typo.h4,
            Typo.h3 => Typo.h5,
            _ => typo
        };
    }
}
