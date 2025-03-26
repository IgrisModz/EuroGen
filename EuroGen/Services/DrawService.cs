using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using EuroGen.Helpers;
using EuroGen.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EuroGen.Services;

public class DrawService(ILogger<DrawService> logger)
{
    public event Action? StatusChanged;

    private bool _asConnection = true;
    private bool _isAvailableWebsite = true;
    private bool _isLoading;

    private readonly ILogger<DrawService> _logger = logger;

    public static string BaseUrl => "https://www.fdj.fr/jeux-de-tirage/euromillions-my-million/historique";
    public static string BaseGoogle => "https://www.google.com";
    public static string BaseDefaultDrawDownload => "https://www.sto.api.fdj.fr/anonymous/service-draw-info";


    private static readonly Dictionary<string, string> ResourcesFiles = new()
    {
        { "1a2b3c4d-9876-4562-b3fc-2c963f66afa8", "euromillions.csv" },
        { "1a2b3c4d-9876-4562-b3fc-2c963f66afa9", "euromillions_2.csv" },
        { "1a2b3c4d-9876-4562-b3fc-2c963f66afb6", "euromillions_3.csv" },
        { "1a2b3c4d-9876-4562-b3fc-2c963f66afc6", "euromillions_4.csv" },
        { "1a2b3c4d-9876-4562-b3fc-2c963f66afd6", "euromillions_201902.csv" },
    };

    public bool AsConnection
    {
        get => _asConnection;
        set
        {
            _asConnection = value;
            UpdateStatus();
        }
    }

    public bool IsAvailableWebsite
    {
        get => _isAvailableWebsite;
        set
        {
            _isAvailableWebsite = value;
            UpdateStatus();
        }
    }

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
            var filteredUrls = zipUrls.Where(url => !ResourcesFiles.Keys.Any(fileGuid => url.EndsWith(fileGuid, StringComparison.OrdinalIgnoreCase)));

            var csvFiles = await DownloadAndExtractZipFilesAsync(filteredUrls);

            var localDraws = await LoadDrawsFromResources();
            var draws = localDraws?.Concat(await CsvFilesToObjectList<Draw>(csvFiles));

            return draws;

        }, retryDelayMilliseconds: 5000);
    }

    private async Task<IEnumerable<Draw>?> LoadDrawsFromResources()
    {

        try
        {

            var tasks = ResourcesFiles.Values
            .Select(async file =>
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync(file);
                return await ParseCsvStream<Draw>(stream);
            });

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des tirages depuis les resources.");
            return null;
        }
    }

    /// <summary>
    /// Read CSV files and create a list of the specified object
    /// </summary>
    /// <typeparam name="T">The type of the object to convert as list</typeparam>
    /// <param name="csvFiles">The uris to the CSV Files</param>
    /// <returns>An enumerable of object</returns>
    private async static Task<IEnumerable<T>> CsvFilesToObjectList<T>(IEnumerable<string> csvFiles)
    {
        var tasks = csvFiles.Select(ParseCsvFile<T>);
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r);
    }

    private static async Task<IEnumerable<T>> ParseCsvFile<T>(string csvFile)
    {
        await using var stream = File.OpenRead(csvFile);
        return await ParseCsvStream<T>(stream);
    }

    private static async Task<IEnumerable<T>> ParseCsvStream<T>(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ";",
        };

        using var reader = new StreamReader(stream);
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

            var zipFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
    /// <param name="retryDelayMilliseconds"></param>
    /// <param name="maxRetries"></param>
    /// <returns></returns>
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
                _logger.LogWarning(ex, "Tentative échouée : {Message}. Nouvelle tentative dans {RetryDelay} secondes.", ex.Message, retryDelayMilliseconds / 1000);
                await Task.Delay(retryDelayMilliseconds);
            }

            attempt++;
        }

        _logger.LogError("Le nombre maximum de tentatives a été atteint.");
        return default;
    }
}