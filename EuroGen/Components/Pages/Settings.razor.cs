using MudBlazor;

namespace EuroGen.Components.Pages;

public partial class Settings
{
    private List<int> Years => DrawService.Years();

    private List<int> _minYears = [];

    private List<int> _maxYears = [];

    private int SelectedMinYear
    {
        get => Preferences.Default.Get("MinDate", Years[0]);
        set => Preferences.Default.Set("MinDate", value);
    }

    private int SelectedMaxYear
    {
        get => Preferences.Default.Get("MaxDate", Years[^1]);
        set => Preferences.Default.Set("MaxDate", value);
    }

    private static int SelectedDrawLength
    {
        get => Preferences.Default.Get("DrawLength", 1);
        set => Preferences.Default.Set("DrawLength", value);
    }

    private static CalculDrawType SelectedCalculDrawType
    {
        get => (CalculDrawType)Preferences.Default.Get("DrawCalcul", (int)CalculDrawType.TotalDraw);
        set => Preferences.Default.Set("DrawCalcul", (int)value);
    }

    private static CalculStatsType SelectedCalculStatsType
    {
        get => (CalculStatsType)Preferences.Default.Get("StatsCalcul", (int)CalculStatsType.TotalDraw);
        set => Preferences.Default.Set("StatsCalcul", (int)value);
    }


    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        DrawService.IsLoading = true;

        if (DrawService.Draws == null || !DrawService.Draws.Any())
        {
            await DrawService.LoadLocalDrawsAsync();
        }

        UpdateYearOptions();

        DrawService.IsLoading = false;
    }

    private void OnThemeChanged(AppTheme mode)
    {
        ThemeService.AppTheme = mode;

        string theme = mode switch
        {
            AppTheme.Unspecified => Localizer["SystemPreference"],
            AppTheme.Dark => Localizer["Dark"],
            AppTheme.Light => Localizer["Light"],
            _ => Localizer["SystemPreference"]
        };

        Snackbar.Add($"{Localizer["ThemeChanged"]}: {theme}", Severity.Info);
    }

    private void OnLanguageChanged(string newLanguage)
    {
        Localizer.Language = newLanguage;

        string languageName = newLanguage switch
        {
            "ca" => "Català",
            "de" => "Deutsch",
            "en" => "English",
            "es" => "Español",
            "fr" => "Français",
            "ga" => "Gaeilge",
            "gv" => "Gaelg",
            "it" => "Italiano",
            "lb" => "Lëtzebuergesch",
            "nl" => "Nederlands",
            "pt" => "Português",
            _ => newLanguage
        };

        Snackbar.Add($"{Localizer["LanguageChanged"]}: {languageName}", Severity.Success);
    }

    private void OnSelectedMinYearChanged(int value)
    {
        SelectedMinYear = value;

        UpdateYearOptions();

        if (SelectedMaxYear < value)
        {
            SelectedMaxYear = _maxYears[0];
        }
    }

    private void OnSelectedMaxYearChanged(int value)
    {
        SelectedMaxYear = value;

        UpdateYearOptions();

        if (SelectedMinYear > value)
        {
            SelectedMinYear = _minYears[^1];
        }
    }

    private void UpdateYearOptions()
    {
        // Filtrer les options disponibles pour les deux sélections
        _minYears = [.. Years.Where(year => year <= SelectedMaxYear)];
        _maxYears = [.. Years.Where(year => year >= SelectedMinYear)];
    }

    private async Task ResetPreferences()
    {
        var parameters = new DialogParameters<Dialog>
        {
            {x => x.ContentText, Localizer["SureReset"] },
            {x => x.OkButtonText, Localizer["Reset"] },
            {x => x.CancelButtonText, Localizer["Cancel"] },
            {x => x.Color, MudBlazor.Color.Error },
        };

        var dialog = await DialogService.ShowAsync<Dialog>(Localizer["ResetPreferences"], parameters);
        var result = await dialog.Result;

        if (!result!.Canceled)
        {
            var language = Localizer.Language;
            var theme = ThemeService.AppTheme;

            Preferences.Default.Clear();

            Localizer.Language = language;
            ThemeService.AppTheme = theme;

            Snackbar.Add(Localizer["PreferencesSuccessfullyReset"], Severity.Warning);
        }

    }
}
