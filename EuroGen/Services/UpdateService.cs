using EuroGen.Watcher;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace EuroGen.Services
{
    public class UpdateService(HttpClient httpClient, ILogger<UpdateService> logger)
    {

        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<UpdateService> _logger = logger;

        public event Action<long, long>? ProgressChanged; // bytesDownloaded, totalBytes
        public event Action? DownloadCompleted;

        public string DestinationPath { get; set; } = string.Empty;

        public long ExistingLength { get; private set; }
        public long TotalLength { get; private set; }
        public double DownloadSpeedBytesPerSecond { get; private set; }
        
        internal string AppDirectory { get; } = Path.Combine(FileSystem.AppDataDirectory, "Update");

        private static string GitHubApiUrl => $"https://api.github.com/repos/IgrisModz/EuroGen/releases/latest";

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            if (!await InternetWatcher.IsInternetAvailable() || !await InternetWatcher.IsSiteAvailable(GitHubApiUrl))
            {
                _logger.LogWarning("Pas de connexion réseau.");
                return null;
            }

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("EuroGen");

            var response = await _httpClient.GetAsync(GitHubApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? string.Empty;
            var assets = doc.RootElement.GetProperty("assets");
            var readme = doc.RootElement.GetProperty("body").GetString() ?? string.Empty;

            var currentVersion = AppInfo.Current.VersionString;

            if (!IsNewVersion(currentVersion, tag))
                return null;

            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                var url = asset.GetProperty("browser_download_url").GetString();

                if ((OperatingSystem.IsAndroid() && name!.EndsWith(".apk")) ||
                    (OperatingSystem.IsWindows() && name!.EndsWith(".zip")))
                {
                    string sha256 = asset.TryGetProperty("label", out var label) ? label.GetString() ?? "" : "";
                    return new UpdateInfo
                    {
                        TagName = tag ?? "",
                        AssetUrl = url ?? "",
                        FileName = name ?? "",
                        ChangeLog = readme ?? "",
                        Sha256 = sha256
                    };
                }
            }

            return null;
        }

        public async Task<bool> DownloadAndVerifyAsync(UpdateInfo info, string destinationPath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(AppDirectory))
                {
                    Directory.CreateDirectory(AppDirectory);
                }

                if (File.Exists(destinationPath))
                {
                    ExistingLength = new FileInfo(destinationPath).Length;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, info.AssetUrl);
                if (ExistingLength > 0)
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(ExistingLength, null);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentRange?.Length ?? response.Content.Headers.ContentLength ?? -1;
                TotalLength = totalBytes;

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(destinationPath, FileMode.Append, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                long totalDownloaded = ExistingLength;
                int bytesRead;

                DateTime lastCheck = DateTime.Now;
                long lastBytes = totalDownloaded;

                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalDownloaded += bytesRead;

                    var now = DateTime.Now;
                    var secondsElapsed = (now - lastCheck).TotalSeconds;

                    if (secondsElapsed >= 1.0)
                    {
                        long bytesSinceLastCheck = totalDownloaded - lastBytes;
                        DownloadSpeedBytesPerSecond = bytesSinceLastCheck / secondsElapsed;

                        lastBytes = totalDownloaded;
                        lastCheck = now;
                    }

                    ProgressChanged?.Invoke(totalDownloaded, totalBytes);
                }

                fileStream.Close();
                DownloadSpeedBytesPerSecond = 0;
#if WINDOWS
                var extractFolder = Path.Combine(AppDirectory, Path.GetFileNameWithoutExtension(info.FileName));
                ZipFile.ExtractToDirectory(destinationPath, extractFolder);
#endif
                DownloadCompleted?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNewVersion(string currentVersion, string newVersion)
        {
            try
            {
                var v1 = new Version(currentVersion.TrimStart('v'));
                var v2 = new Version(newVersion.TrimStart('v'));
                return v2 > v1;
            }
            catch
            {
                return false;
            }
        }

        public async Task InitProgress(UpdateInfo info, CancellationToken cancellationToken = default)
        {
            DestinationPath = Path.Combine(AppDirectory, info.FileName);

            if (File.Exists(DestinationPath))
            {
                ExistingLength = new FileInfo(DestinationPath).Length;
            }

            var request = new HttpRequestMessage(HttpMethod.Head, info.AssetUrl);
            
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if(response.IsSuccessStatusCode)
            {
                TotalLength = response.Content.Headers.ContentRange?.Length ?? response.Content.Headers.ContentLength ?? -1;
            }
        }

        public async Task<bool> LaunchInstaller(UpdateInfo info)
        {
#if ANDROID
            try
            {
                if (info.FileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    var apkPath = Path.Combine(AppDirectory, info.FileName);
                    if (File.Exists(apkPath))
                    {
                        await Launcher.Default.OpenAsync(new OpenFileRequest
                        {
                            File = new ReadOnlyFile(apkPath)
                        });
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
#elif WINDOWS
            try
            {
                if (info.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var extractPath = Path.Combine(AppDirectory, Path.GetFileNameWithoutExtension(info.FileName));
                    if (Directory.Exists(extractPath))
                    {
                        // Lancer un exécutable dans le dossier
                        var exePath = Directory.GetFiles(extractPath, "*.msix").FirstOrDefault();
                        if (exePath != null)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true
                            });
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        public async Task<bool> RequestPermissionsAsync()
        {
#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
                _logger.LogWarning("Permission d'écriture refusée.");
            return status == PermissionStatus.Granted;
#else
            return true;
#endif
        }

        public bool CheckIfAlreadyDownloaded(UpdateInfo updateInfo)
        {
            var filePath = Path.Combine(AppDirectory, updateInfo.FileName);

            if (updateInfo.FileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            {
                return File.Exists(filePath);
            }

            if (updateInfo.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var extractFolder = Path.Combine(AppDirectory, Path.GetFileNameWithoutExtension(updateInfo.FileName));
                return Directory.Exists(extractFolder);
            }

            return false;
        }

        public async Task DeleteUpdates()
        {
            try
            {
                await RequestPermissionsAsync();
                if (Directory.Exists(AppDirectory))
                {
                    Directory.Delete(AppDirectory, recursive: true);
                    _logger.LogInformation("Mise à jour supprimée avec succès.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erreur : {Message}", ex.Message);
            }
        }
    }

    public class UpdateInfo
    {
        public string TagName { get; set; } = "";
        public string AssetUrl { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public string ChangeLog { get; set; } = "";
        public bool IsMandatory { get; set; } = true;
    }
}
