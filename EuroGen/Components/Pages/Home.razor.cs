using EuroGen.Extensions;
using EuroGen.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace EuroGen.Components.Pages;

public partial class Home : IAsyncDisposable
{
    static readonly IReadOnlyList<IReadOnlyList<Func<Draw, int>>> numberGroups =
    [
        [Draw.NumberSelectors[0]],
        [Draw.NumberSelectors[1]],
        [Draw.NumberSelectors[2]],
        [Draw.NumberSelectors[3]],
        [Draw.NumberSelectors[4]],
    ];

    static readonly IReadOnlyList<IReadOnlyList<Func<Draw, int>>> starGroups =
    [
        [Draw.StarSelectors[0]],
        [Draw.StarSelectors[1]],
    ];

    string rotateClass = string.Empty;
    Action? languageChangedHandler;

    List<Draw> newDraws = [];

    static int DrawLength => Preferences.Default.Get("DrawLength", 1);
    int SelectedMinYear => Preferences.Default.Get("MinDate", Years[0]);
    int SelectedMaxYear => Preferences.Default.Get("MaxDate", Years[^1]);
    static CalculDrawType SelectedCalculType => (CalculDrawType)Preferences.Default.Get("DrawCalcul", (int)CalculDrawType.TotalDraw);

    List<int> Years => DrawService.Years();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            languageChangedHandler = () => _ = InvokeAsync(StateHasChanged);
            Localizer.LanguageChanged += languageChangedHandler;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (languageChangedHandler is not null)
        {
            Localizer.LanguageChanged -= languageChangedHandler;
            languageChangedHandler = null;
        }

		GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    async Task LoadDataAsync()
    {
        rotateClass = "rotate";
        newDraws = LoadPreviouslyGenratedDraws();
        rotateClass = string.Empty;

        if (DrawService.Draws == null || !DrawService.Draws.Any())
        {
            await DrawService.LoadLocalDrawsAsync();
		}
    }

    static List<Draw> LoadPreviouslyGenratedDraws()
    {
        string serializedDraws = Preferences.Default.Get("Draws", "");
        var draws = DeserializeDraws(serializedDraws) ?? [];

        for (int i = draws.Count; i < DrawLength; i++)
        {
            draws.Add(new Draw() { DrawDate = DateTime.Now });
        }

        return draws;
    }

    static string SerializeDraws(List<Draw> draws)
    {
        // Sérialiser les tirages pour les enregistrer dans Preferences
        return System.Text.Json.JsonSerializer.Serialize(draws);
    }

    static List<Draw>? DeserializeDraws(string serializedDraws)
    {
        // Désérialiser les tirages depuis Preferences
        return string.IsNullOrEmpty(serializedDraws) ? [] : System.Text.Json.JsonSerializer.Deserialize<List<Draw>>(serializedDraws);
    }

    async Task GenerateDraw()
    {
        DrawService.IsLoading = true;
        rotateClass = "rotate";

        try
        {
            Logger.LogInformation("Début du tirage...");
            var length = DrawLength;

            var emptyDraws = new List<Draw>();
            for (int i = 0; i < length; i++)
            {
                emptyDraws.Add(new Draw());
            }

            newDraws = emptyDraws;
            var totalDraw = SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalDrawByNumber;

            if (SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalNumber)
            {
                var numbersPercentages = await GetPercents(Draw.NumberSelectors, SelectedMinYear, SelectedMaxYear, totalDraw);

                var starsPercentages = await GetPercents(Draw.StarSelectors, SelectedMinYear, SelectedMaxYear, totalDraw);

                for (int i = 0; i < length; i++)
                {
                    var numbers = GetDrawNumber(numbersPercentages, 5);

                    var stars = GetDrawNumber(starsPercentages, 2);

                    newDraws[i] = CreateDraw([.. numbers], [.. stars]);
                }
            }
            else
            {
                var numberPercentages = await GetGroupedPercents(numberGroups, SelectedMinYear, SelectedMaxYear, totalDraw);
                var starPercentages = await GetGroupedPercents(starGroups, SelectedMinYear, SelectedMaxYear, totalDraw);

                for (int i = 0; i < length; i++)
                {
                    var numbers = GetDrawNumbers(numberPercentages);

                    var stars = GetDrawNumbers(starPercentages);

                    newDraws[i] = CreateDraw([.. numbers], [.. stars]);
                }
            }

            await Task.Delay(2000);

            Preferences.Set($"Draws", SerializeDraws(newDraws));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erreur lors du chargement.");
        }
        finally
        {
            DrawService.IsLoading = false;
            rotateClass = string.Empty;
            Preferences.Set("LastMethodUsed", "GetDraw");
        }
    }

    async Task GetBestDraw()
    {
        DrawService.IsLoading = true;
        rotateClass = "rotate";

        try
        {
            Logger.LogInformation("Début du meilleur tirage...");
            newDraws = [new Draw()];
            var totalDraw = SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalDrawByNumber;

            if (SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalNumber)
            {
                var numbersPercentages = await GetPercents(Draw.NumberSelectors, SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);

                var starsPercentages = await GetPercents(Draw.StarSelectors, SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);

                newDraws[0] = CreateDraw([.. numbersPercentages.Keys], [.. starsPercentages.Keys]);
            }
            else
            {
                var numberPercentages = await GetGroupedPercents(numberGroups, SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);
                var starPercentages = await GetGroupedPercents(starGroups, SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);

                var firstNumber = numberPercentages[0].FirstOrDefault().Key;
                var secondNumber = numberPercentages[1].FirstOrDefault().Key;
                var thirdNumber = numberPercentages[2].FirstOrDefault().Key;
                var fourthNumber = numberPercentages[3].FirstOrDefault().Key;
                var fifthNumber = numberPercentages[4].FirstOrDefault().Key;
                var firstStar = starPercentages[0].FirstOrDefault().Key;
                var secondStar = starPercentages[1].FirstOrDefault().Key;

                newDraws[0] = new Draw
                {
                    FirstNumber = firstNumber,
                    SecondNumber = secondNumber,
                    ThirdNumber = thirdNumber,
                    FourthNumber = fourthNumber,
                    FifthNumber = fifthNumber,
                    FirstStar = firstStar,
                    SecondStar = secondStar,
                };

            }

            await Task.Delay(2000);

            Preferences.Set($"Draws", SerializeDraws(newDraws));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erreur lors du chargement.");
        }
        finally
        {
            DrawService.IsLoading = false;
            rotateClass = string.Empty;
            Preferences.Set("LastMethodUsed", "GetBestDraw");
        }
    }

    static Draw CreateDraw(List<int> numbers, List<int> stars)
    {
        return new Draw
        {
            FirstNumber = numbers.ElementAtOrDefault(0),
            SecondNumber = numbers.ElementAtOrDefault(1),
            ThirdNumber = numbers.ElementAtOrDefault(2),
            FourthNumber = numbers.ElementAtOrDefault(3),
            FifthNumber = numbers.ElementAtOrDefault(4),
            FirstStar = stars.ElementAtOrDefault(0),
            SecondStar = stars.ElementAtOrDefault(1),
        };
    }

    async Task<IDictionary<int, double>[]> GetGroupedPercents(IReadOnlyList<IReadOnlyList<Func<Draw, int>>> groups, int minYear, int maxYear, bool totalDraw = false, OrderBy order = OrderBy.None)
    {
        var tasks = groups.Select(group => GetPercents(group, minYear, maxYear, totalDraw, order));
        return await Task.WhenAll(tasks);
    }

    async Task<IDictionary<int, double>> GetPercents(IEnumerable<Func<Draw, int>> selectors, int minYear, int maxYear, bool totalDraw = false, OrderBy order = OrderBy.None)
    {
        var filteredDraws = (DrawService.Draws ?? []).Where(d => d.DrawDate.Year >= minYear && d.DrawDate.Year <= maxYear).ToList();

        var values = filteredDraws.SelectMany(draw => draw.GetValues(selectors)).ToList();

        var counts = await Task.Run(values.CalculateNumbers);

        var distinctDates = filteredDraws.Select(d => d.DrawDate).Distinct().Count();

        var percentages = await Task.Run(() => counts.CalculateNumbersPercent(totalDraw ? distinctDates : null));

        return percentages.ReorderBy(order);
    }

    HashSet<int> GetDrawNumbers(IEnumerable<IDictionary<int, double>> probabilities)
    {
        var uniqueValues = new HashSet<int>();
        var i = 0;
        var exist = new List<int>();
        var enumerable = probabilities as IDictionary<int, double>[] ?? [.. probabilities];
        while (uniqueValues.Count < enumerable.Length)
        {
            var count = uniqueValues.Count;
            if (i > enumerable.Length)
            {
                i = 0;
            }

            if (!exist.Contains(i))
            {
                uniqueValues.Add(GetDrawNumber(enumerable[i], 1).First());
            }

            if (uniqueValues.Count > count)
            {
                exist.Add(i);
            }

            i++;
        }

        return uniqueValues;
    }

    HashSet<int> GetDrawNumber(IDictionary<int, double> probabilities, int returnLength)
    {
        if (probabilities == null || probabilities.Count == 0)
        {
            Logger.LogError("Le dictionnaire de probabilités ne peut pas être vide.");
            return [];
        }

        if (returnLength > probabilities.Count)
        {
            Logger.LogError("Le nombre de tirages demandés dépasse le nombre de valeurs uniques possibles.");
            return [];
        }

        var total = probabilities.Values.Sum();
        var normalized = probabilities
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new KeyValuePair<int, double>(kvp.Key, kvp.Value / total))
            .ToList();

        var cumulativeList = new List<(int number, double threshold)>();
        double cumulative = 0;
        foreach (var kvp in normalized)
        {
            cumulative += kvp.Value;
            cumulativeList.Add((kvp.Key, cumulative));
        }

        var result = new HashSet<int>();
        while (result.Count < returnLength)
        {
            var random = GetSecureRandomDouble();
            foreach (var (number, threshold) in cumulativeList)
            {
                if (random < threshold)
                {
                    result.Add(number);
                    break;
                }
            }
        }

        return result;
    }

    static double GetSecureRandomDouble()
    {
        var byteArray = new byte[8];
        RandomNumberGenerator.Fill(byteArray);
        var randomNumber = BitConverter.ToUInt64(byteArray, 0);
        return randomNumber / (1.0 + ulong.MaxValue);
    }
}
