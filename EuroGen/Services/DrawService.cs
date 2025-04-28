using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using EuroGen.Data;
using EuroGen.Helpers;
using EuroGen.Models;
using EuroGen.Watcher;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace EuroGen.Services;

public class DrawService(ILogger<DrawService> logger, AppDbContext dbContext)
{
    public event Action? StatusChanged;

    private bool _asConnection = true;
    private bool _isAvailableWebsite = true;
    private bool _isFirstLoading;
    private bool _isLoading;

    private readonly ILogger<DrawService> _logger = logger;
    private readonly AppDbContext _dbContext = dbContext;

    public static string BaseUrl => "https://www.fdj.fr/jeux-de-tirage/euromillions-my-million/historique";
    public static string BaseDefaultDrawDownload => "https://www.sto.api.fdj.fr/anonymous/service-draw-info";

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

    public bool IsFirstLoading
    {
        get => _isFirstLoading;
        set
        {
            _isFirstLoading = value;
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

    public List<int> Years()
    {
        return Draws?.Select(d => d.DrawDate.Year).Distinct().Order().ToList() ?? [];
    }

    private void UpdateStatus()
    {
        StatusChanged?.Invoke();
    }

    public async Task LoadLocalDrawsAsync()
    {
        Draws = await _dbContext.Draws.ToListAsync() ?? [];

        var internetWatcher = new InternetWatcher(BaseUrl, TimeSpan.FromSeconds(5));
        await internetWatcher.WatchInternetState(async () =>
        {
            if (!Draws.Any())
            {
                IsFirstLoading = true;
            }
            var draws = await LoadDraws();
            if (draws != null)
            {
                var existingDraws = await _dbContext.Draws
                                    .Where(d => draws.Select(draw => draw.DrawDate).Contains(d.DrawDate))
                                    .ToListAsync();
                var newDraws = draws.Where(d => !existingDraws.Any(ed => ed.DrawDate == d.DrawDate)).ToList();
                if (newDraws.Count > 0)
                {
                    await _dbContext.Draws.AddRangeAsync(newDraws);
                }
                await _dbContext.SaveChangesAsync();
                Draws = Draws.Union(draws);
            }
            IsFirstLoading = false;
        });
    }

    public async Task<IEnumerable<Draw>?> LoadDraws()
    {
        return await RetryUntilSuccessAsync(async () =>
        {
            var zipUrls = await GetEuromillionZipFiles();
            var csvFiles = await DownloadAndExtractZipFilesAsync(zipUrls);
            var draws = await CsvFilesToObjectList<Draw>(csvFiles);
            return draws;

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


        var nodes = document.DocumentNode.SelectNodes("//a[contains(@download, 'euromillions')]");
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
    public List<int> GetYears()
    {
        return Draws?.Select(d => d.DrawDate.Year).Distinct().Order().ToList() ?? [];
    }
}