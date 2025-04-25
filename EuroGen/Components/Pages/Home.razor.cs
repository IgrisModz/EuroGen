using EuroGen.Helpers;
using EuroGen.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace EuroGen.Components.Pages;

public partial class Home
{
    private string rotateClass = string.Empty;

    private List<Draw> _newDraws = [];

    private static int DrawLength => Preferences.Default.Get("DrawLength", 1);
    private int SelectedMinYear => Preferences.Default.Get("MinDate", Years[0]);
    private int SelectedMaxYear => Preferences.Default.Get("MaxDate", Years[^1]);
    private static CalculDrawType SelectedCalculType => (CalculDrawType)Preferences.Default.Get("DrawCalcul", (int)CalculDrawType.TotalDraw);

    private List<int> Years => DrawService.Years();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Localizer.LanguageChanged += () =>
            {
                StateHasChanged();
            };
        }
    }

    private async Task LoadDataAsync()
    {
        rotateClass = "rotate";
        _newDraws = LoadPreviouslyGenratedDraws();
        rotateClass = string.Empty;

        if (DrawService.Draws == null || !DrawService.Draws.Any())
        {
            await DrawService.LoadLocalDrawsAsync();
        }
    }

    private static List<Draw> LoadPreviouslyGenratedDraws()
    {
        string serializedDraws = Preferences.Default.Get("Draws", "");
        var draws = DeserializeDraws(serializedDraws) ?? [];

        for (int i = draws.Count; i < DrawLength; i++)
        {
            draws.Add(new Draw() { DrawDate = DateTime.Now });
        }

        return draws;
    }

    private static string SerializeDraws(List<Draw> draws)
    {
        // Sérialiser les tirages pour les enregistrer dans Preferences
        return System.Text.Json.JsonSerializer.Serialize(draws);
    }

    private static List<Draw>? DeserializeDraws(string serializedDraws)
    {
        // Désérialiser les tirages depuis Preferences
        return string.IsNullOrEmpty(serializedDraws) ? [] : System.Text.Json.JsonSerializer.Deserialize<List<Draw>>(serializedDraws);
    }

    private async Task GenerateDraw()
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

            _newDraws = emptyDraws;
            var totalDraw = SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalDrawByNumber;

            if (SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalNumber)
            {
                var numbersPercentages = await GetPercents(
                [
                    nameof(Draw.FirstNumber),
                    nameof(Draw.SecondNumber),
                    nameof(Draw.ThirdNumber),
                    nameof(Draw.FourthNumber),
                    nameof(Draw.FifthNumber)
                ], SelectedMinYear, SelectedMaxYear, totalDraw);

                var starsPercentages = await GetPercents([nameof(Draw.FirstStar), nameof(Draw.SecondStar)], SelectedMinYear, SelectedMaxYear, totalDraw);

                for (int i = 0; i < length; i++)
                {
                    var numbers = GetDrawNumber(numbersPercentages, 5);

                    var stars = GetDrawNumber(starsPercentages, 2);

                    _newDraws[i] = CreateDraw([.. numbers], [.. stars]);
                }
            }
            else
            {
                var number1Percentages = await GetPercents([nameof(Draw.FirstNumber)], SelectedMinYear, SelectedMaxYear, totalDraw);
                var number2Percentages = await GetPercents([nameof(Draw.SecondNumber)], SelectedMinYear, SelectedMaxYear, totalDraw);
                var number3Percentages = await GetPercents([nameof(Draw.ThirdNumber)], SelectedMinYear, SelectedMaxYear, totalDraw);
                var number4Percentages = await GetPercents([nameof(Draw.FourthNumber)], SelectedMinYear, SelectedMaxYear, totalDraw);
                var number5Percentages = await GetPercents([nameof(Draw.FifthNumber)], SelectedMinYear, SelectedMaxYear, totalDraw);

                var star1Percentages = await GetPercents([nameof(Draw.FirstStar)], SelectedMinYear, SelectedMaxYear, totalDraw);
                var star2Percentages = await GetPercents([nameof(Draw.SecondStar)], SelectedMinYear, SelectedMaxYear, totalDraw);


                for (int i = 0; i < length; i++)
                {
                    var numbers = GetDrawNumbers([number1Percentages, number2Percentages, number3Percentages, number4Percentages, number5Percentages]);

                    var stars = GetDrawNumbers([star1Percentages, star2Percentages]);

                    _newDraws[i] = CreateDraw([.. numbers], [.. stars]);
                }
            }

            await Task.Delay(2000);

            Preferences.Set($"Draws", SerializeDraws(_newDraws));
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

    private async Task GetBestDraw()
    {
        DrawService.IsLoading = true;
        rotateClass = "rotate";

        try
        {
            Logger.LogInformation("Début du meilleur tirage...");
            _newDraws = [new Draw()];
            var totalDraw = SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalDrawByNumber;

            if (SelectedCalculType is CalculDrawType.TotalDraw or CalculDrawType.TotalNumber)
            {
                var numbersPercentages = await GetPercents(
                [
                    nameof(Draw.FirstNumber),
                    nameof(Draw.SecondNumber),
                    nameof(Draw.ThirdNumber),
                    nameof(Draw.FourthNumber),
                    nameof(Draw.FifthNumber)
                ], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);

                var starsPercentages = await GetPercents([nameof(Draw.FirstStar), nameof(Draw.SecondStar)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);

                _newDraws[0] = CreateDraw([.. numbersPercentages.Keys], [.. starsPercentages.Keys]);
            }
            else
            {
                var number1Percentages = await GetPercents([nameof(Draw.FirstNumber)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);
                var number2Percentages = await GetPercents([nameof(Draw.SecondNumber)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);
                var number3Percentages = await GetPercents([nameof(Draw.ThirdNumber)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);
                var number4Percentages = await GetPercents([nameof(Draw.FourthNumber)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);
                var number5Percentages = await GetPercents([nameof(Draw.FifthNumber)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);

                var star1Percentages = await GetPercents([nameof(Draw.FirstStar)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);
                var star2Percentages = await GetPercents([nameof(Draw.SecondStar)], SelectedMinYear, SelectedMaxYear, totalDraw, OrderBy.ValueDescending);

                var firstNumber = number1Percentages.FirstOrDefault().Key;
                var secondNumber = number2Percentages.FirstOrDefault().Key;
                var thirdNumber = number3Percentages.FirstOrDefault().Key;
                var fourthNumber = number4Percentages.FirstOrDefault().Key;
                var fifthNumber = number5Percentages.FirstOrDefault().Key;
                var firstStar = star1Percentages.FirstOrDefault().Key;
                var secondStar = star2Percentages.FirstOrDefault().Key;

                _newDraws[0] = new Draw
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

            Preferences.Set($"Draws", SerializeDraws(_newDraws));
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

    private static Draw CreateDraw(List<int> numbers, List<int> stars)
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

    private async Task<IDictionary<int, double>> GetPercents(string[] propertyNames, int minYear, int maxYear, bool totalDraw = false, OrderBy order = OrderBy.None)
    {
        var filteredDraws = (DrawService.Draws ?? []).Where(d => d.DrawDate.Year >= minYear && d.DrawDate.Year <= maxYear).ToList();

        var values = filteredDraws.SelectMany(d => propertyNames.Select(d.GetPropertyValue)).ToList();

        var counts = await Task.Run(values.CalculateNumbers);

        var distinctDates = filteredDraws.Select(d => d.DrawDate).Distinct().Count();

        var percentages = await Task.Run(() => counts.CalculateNumbersPercent(totalDraw ? distinctDates : null));

        return percentages.ReorderBy(order);
    }

    private HashSet<int> GetDrawNumbers(IEnumerable<IDictionary<int, double>> probabilities)
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

    private HashSet<int> GetDrawNumber(IDictionary<int, double> probabilities, int returnLength)
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

    private static double GetSecureRandomDouble()
    {
        var byteArray = new byte[8];
        RandomNumberGenerator.Fill(byteArray);
        var randomNumber = BitConverter.ToUInt64(byteArray, 0);
        return randomNumber / (1.0 + ulong.MaxValue);
    }
}
