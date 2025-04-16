using EuroGen.Helpers;
using EuroGen.Models;
using Microsoft.Maui.Storage;

namespace EuroGen.Components.Pages;

public partial class Stats
{
    public enum SearchType
    {
        Number,
        Star
    }

    private IEnumerable<Models.Stats> _stats = [];

    private SearchType _selectedSearchType;

    private int SelectedMinYear => Preferences.Default.Get("MinDate", Years[0]);
    private int SelectedMaxYear => Preferences.Default.Get("MaxDate", Years[^1]);
    private static CalculStatsType SelectedCalculType => (CalculStatsType)Preferences.Default.Get("StatsCalcul", (int)CalculStatsType.TotalDraw);

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
        DrawService.IsLoading = true;

        if (DrawService.Draws == null || !DrawService.Draws.Any())
        {
            await DrawService.LoadLocalDrawsAsync();
        }

        await Refresh(_selectedSearchType);

        DrawService.IsLoading = false;
    }

    private async Task OnSelectedSearchTypeChanged(SearchType searchType)
    {
        _selectedSearchType = searchType;
        await Refresh(searchType);
    }

    private async Task Refresh(SearchType searchType)
    {
        if (searchType == SearchType.Number)
            await GetNumbers();
        else
            await GetStars();
    }

    private async Task GetNumbers()
    {
        var totalDraw = Stats.SelectedCalculType == CalculStatsType.TotalDraw;
        _stats = await GetStats(DrawService.Draws!,
        [
            nameof(Draw.FirstNumber), nameof(Draw.SecondNumber), nameof(Draw.ThirdNumber), nameof(Draw.FourthNumber), nameof(Draw.FifthNumber)
        ], SelectedMinYear, SelectedMaxYear, totalDraw);
    }

    private async Task GetStars()
    {
        var totalDraw = Stats.SelectedCalculType == CalculStatsType.TotalDraw;
        _stats = await GetStats(DrawService.Draws!, [nameof(Draw.FirstStar), nameof(Draw.SecondStar)], SelectedMinYear, SelectedMaxYear, totalDraw);
    }

    private static async Task<IEnumerable<Models.Stats>> GetStats(IEnumerable<Draw> draws, string[] propertyNames, int minYear, int maxYear, bool totalDraw = false)
    {
        List<Models.Stats> stats = [];

        var filteredDraws = draws.Where(d => d.DrawDate.Year >= minYear && d.DrawDate.Year <= maxYear).ToList();

        var valuesAndDates = filteredDraws.SelectMany(d => propertyNames.Select(d.GetPropertyValueAndDate)).ToList();

        var counts = await Task.Run(() => valuesAndDates.Select(vd => vd.Value).CalculateNumbers());

        var distinctDates = filteredDraws.Select(o => o.DrawDate).Distinct().Count();

        var percentages = await Task.Run(() => counts.CalculateNumbersPercent(totalDraw ? distinctDates : null));

        var lastDates = await Task.Run(valuesAndDates.CalculateLastDates);

        foreach (var kvp in counts.OrderBy(n => n.Key))
        {
            stats.Add(new Models.Stats
            {
                Id = kvp.Key - 1,
                Number = kvp.Key,
                NumberOfOutput = kvp.Value,
                PercentOfOutput = $"{percentages[kvp.Key]:F2}%",
                LastRelease = lastDates[kvp.Key].ToShortDateString(),
            });
        }

        return stats;
    }
}
