namespace EuroGen.Services;

public class ThemeService
{
    private const string ThemeKey = "AppTheme";

    public event Action<bool>? ThemeChanged; // Modification ici pour accepter un booléen

    private AppTheme _appTheme;
    private bool _systemPreference;

    public ThemeService()
    {
        // Charger la préférence de thème au démarrage
        LoadThemePreference();
    }

    public AppTheme AppTheme
    {
        get => _appTheme;
        set
        {
            if (_appTheme != value)
            {
                _appTheme = value;
                SaveThemePreference();
                UpdateTheme();
            }
        }
    }

    public bool IsDarkMode => _appTheme == AppTheme.Dark ||
                              (_appTheme == AppTheme.Unspecified && _systemPreference);

    // Récupérer et sauvegarder les préférences de thème
    private void SaveThemePreference()
    {
        Preferences.Set(ThemeKey, _appTheme.ToString());
    }

    private void LoadThemePreference()
    {
        if (Preferences.ContainsKey(ThemeKey))
        {
            var savedTheme = Preferences.Get(ThemeKey, AppTheme.Unspecified.ToString());
            _appTheme = Enum.TryParse(savedTheme, out AppTheme mode) ? mode : AppTheme.Unspecified;
        }
        else
        {
            _appTheme = AppTheme.Unspecified;
        }

        UpdateTheme();
    }

    private void UpdateTheme()
    {
        var isDarkMode = IsDarkMode;
        // On passe un booléen pour indiquer si le thème est sombre ou non
        ThemeChanged?.Invoke(isDarkMode);

        // IMPORTANT: Pour le theme de l'application MAUI
        Application.Current!.UserAppTheme = _appTheme;
    }

    // Définir la préférence système
    public void SetSystemPreference(bool isDark)
    {
        _systemPreference = isDark;
        if (_appTheme == AppTheme.Unspecified)
        {
            UpdateTheme();
        }
    }
}
