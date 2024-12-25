using System.ComponentModel;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using EuroGen.Helpers;
using EuroGen.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace EuroGen.Services;

public class DrawService
{
    public Action StatusChanged;

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
    public bool _isAvailableWebsite = true;
    public bool IsAvailableWebsite
    {
        get => _isAvailableWebsite;
        set
        {
            _isAvailableWebsite = value;
            UpdateStatus();
        }
    }

    public bool _isLoading;
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
    private async Task<IEnumerable<T>> CsvFilesToObjectList<T>(IEnumerable<string> csvFiles)
    {
        var tasks = csvFiles.Select(csvFile => CsvFileToObjectList<T>(csvFile));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r);
    }

    /// <summary>
    /// Read CSV file and create a list of the specified object
    /// </summary>
    /// <typeparam name="T">The type of the object to convert as list</typeparam>
    /// <param name="csvFile">The uri to the CSV File</param>
    /// <returns>An enumerable of object</returns>
    private async Task<IEnumerable<T>> CsvFileToObjectList<T>(string csvFile)
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
    private async Task<IEnumerable<string>> DownloadAndExtractZipFilesAsync(IEnumerable<string> urls)
    {
        var tasks = urls.Select(async url =>
        {
            using HttpClient client = new();

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var zipFilePath = Path.GetRandomFileName();
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
    private async Task<IEnumerable<string>> GetEuromillionZipFiles()
    {
        var web = new HtmlWeb();
        var document = await web.LoadFromWebAsync(BaseUrl);

        var drawInfoUrlBase = GetDrawInfoUrlFromDocument(document) ?? BaseDefaultDrawDownload;

        var nodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'grid-cols-1')]/a");
        if (nodes == null)
        {
            return [];
        }

        var a = nodes.Select(a => a.GetAttributeValue("href", string.Empty).Replace("undefined", drawInfoUrlBase));

        var results = a.Where(href => !string.IsNullOrEmpty(href));
        return results;
    }

    private string? GetDrawInfoUrlFromDocument(HtmlDocument document)
    {
        // Sélectionner le script contenant la clé DrawInfoURL
        var scriptNode = document.DocumentNode.SelectSingleNode("//script[contains(text(), 'DrawInfoURL')]") ?? LogAndReturnDefault<HtmlNode>("Le script contenant 'DrawInfoURL' n'a pas été trouvé.");

        // Extraire le contenu du script
        var scriptContent = scriptNode.InnerText;

        // Rechercher la valeur de DrawInfoURL dans le script
        int jsonStartIndex = scriptContent.IndexOf('{');
        int jsonEndIndex = scriptContent.LastIndexOf('}');
        if (jsonStartIndex == -1 || jsonEndIndex == -1 || jsonEndIndex <= jsonStartIndex)
        {
            _logger.LogError("EUROMILLIONS SCRIPT: Impossible de trouver un JSON valide dans le script.");
        }

        string escapedJsonContent = scriptContent.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

        string jsonContent = Regex.Unescape(escapedJsonContent);

        // Analyse du JSON
        try
        {
            var jsonDocument = JsonDocument.Parse(jsonContent);
            if (jsonDocument.RootElement.TryGetProperty("state", out JsonElement stateElement) &&
            stateElement.TryGetProperty("queries", out JsonElement queriesElement) &&
            queriesElement[0].TryGetProperty("state", out JsonElement queryStateElement) &&
            queryStateElement.TryGetProperty("data", out JsonElement dataElement) &&
            dataElement.TryGetProperty("urls", out JsonElement urlsElement) &&
            urlsElement.TryGetProperty("DrawInfoURL", out JsonElement drawInfoUrlElement))
            {
                return drawInfoUrlElement.GetString() ?? LogAndReturnDefault<string>("La clé 'DrawInfoURL' existe mais est vide.");
            }
            else
            {
                _logger.LogWarning("La clé 'DrawInfoURL' n'a pas été trouvée dans le JSON.");
                return string.Empty;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Erreur lors de l'analyse du JSON : {ex.Message}", ex);
            return null;
        }
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

    private T? LogAndReturnDefault<T>(string? message)
    {
        _logger.LogWarning(message);
        return default;
    }
}