using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using EuroGen.Helpers;
using EuroGen.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EuroGen.Services;

public class DrawService
{
    public event Action? StatusChanged;

    private readonly ILogger<DrawService> _logger;

    public static string BaseUrl => "https://www.fdj.fr/jeux-de-tirage/euromillions-my-million/historique";
    public static string BaseGoogle => "https://www.google.com";
    public static string BaseDefaultDrawDownload => "https://www.sto.api.fdj.fr/anonymous/service-draw-info";

    private bool _asConnection = true;
    public bool AsConnection
    {
        get => _asConnection;
        set
        {
            _asConnection = value;
            UpdateStatus();
        }
    }
    private bool _isAvailableWebsite = true;
    public bool IsAvailableWebsite
    {
        get => _isAvailableWebsite;
        set
        {
            _isAvailableWebsite = value;
            UpdateStatus();
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            UpdateStatus();
        }
    }

    public IEnumerable<Draw>? Draws { get; set; }

    public DrawService(ILogger<DrawService> logger)
    {
        _logger = logger;
    }

    private static async Task<bool> IsInternetAvailable()
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

    private static async Task<bool> IsSiteAvailable(string url)
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

    private void UpdateStatus()
    {
        StatusChanged?.Invoke();
    }

    public async Task<IEnumerable<Draw>?> LoadDraws()
    {
        return await RetryUntilSuccessAsync(async () =>
        {
            AsConnection = await IsInternetAvailable();
            IsAvailableWebsite = await IsSiteAvailable(BaseUrl);

            var zipUrls = await GetEuromillionZipFiles();
            var csvFiles = await DownloadAndExtractZipFilesAsync(zipUrls);
            return await CsvFilesToObjectList<Draw>(csvFiles);

        }, retryDelayMilliseconds: 5000);
    }

    /// <summary>
    /// Read CSV files and create a list of the specified object
    /// </summary>
    /// <typeparam name="T">The type of the object to convert as list</typeparam>
    /// <param name="csvFiles">The uris to the CSV Files</param>
    /// <returns>An enumerable of object</returns>
    private async static Task<IEnumerable<T>> CsvFilesToObjectList<T>(IEnumerable<string> csvFiles)
    {
        var tasks = csvFiles.Select(CsvFileToObjectList<T>);
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r);
    }

    /// <summary>
    /// Read CSV file and create a list of the specified object
    /// </summary>
    /// <typeparam name="T">The type of the object to convert as list</typeparam>
    /// <param name="csvFile">The uri to the CSV File</param>
    /// <returns>An enumerable of object</returns>
    private async static Task<IEnumerable<T>> CsvFileToObjectList<T>(string csvFile)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ";",
        };

        using var reader = new StreamReader(csvFile);
        using var csv = new CsvReader(reader, config);

        return await csv.GetRecordsAsync<T>().ToListAsync();
    }

    /// <summary>
    /// Downloads all the files from the urls and extact them to a temp directory as a .csv
    /// </summary>
    /// <param name="urls">Links of the files</param>
    /// <returns>The paths to the CSV files</returns>
    private async static Task<IEnumerable<string>> DownloadAndExtractZipFilesAsync(IEnumerable<string> urls)
    {
        var tasks = urls.Select(async url =>
        {
            using HttpClient client = new();

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var zipFilePath = Path.GetTempFileName();
            await using (var zipFileStream = new FileStream(zipFilePath, FileMode.Create))
            {
                await response.Content.CopyToAsync(zipFileStream);
            }

            var extractPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);

            return Directory.GetFiles(extractPath, "*.csv");
        });

        var csvFilesArray = await Task.WhenAll(tasks);
        return csvFilesArray.SelectMany(files => files);
    }

    /// <summary>
    /// Get all the urls of the previous draws in the euromillions history page
    /// </summary>
    /// <returns>The urls of the zip file that contains the previous draws</returns>
    private static async Task<IEnumerable<string>> GetEuromillionZipFiles()
    {
        var web = new HtmlWeb();
        var document = await web.LoadFromWebAsync(BaseUrl);


        var nodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'grid-cols-1')]/a");
        if (nodes == null)
        {
            return [];
        }

        var a = nodes.Select(a => a.GetAttributeValue("href", string.Empty));

        var results = a.Where(href => !string.IsNullOrEmpty(href));
        return results;
    }

    private async Task<T?> RetryUntilSuccessAsync<T>(Func<Task<T>> action, int retryDelayMilliseconds = 5000, int maxRetries = -1)
    {
        int attempt = 0;
        while (maxRetries < 0 || attempt < maxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (maxRetries < 0 || attempt < maxRetries - 1)
            {
                _logger.LogWarning($"Tentative échouée : {ex.Message}. Nouvelle tentative dans {retryDelayMilliseconds / 1000} secondes.", ex);
                await Task.Delay(retryDelayMilliseconds);
            }

            attempt++;
        }

        _logger.LogError("Le nombre maximum de tentatives a été atteint.");
        return default;
    }
}