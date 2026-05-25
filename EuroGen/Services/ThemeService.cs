namespace EuroGen.Services;

public class ThemeService
{
    const string themeKey = "AppTheme";

    public event Action<bool>? ThemeChanged; // Modification ici pour accepter un booléen

    AppTheme appTheme;
    bool systemPreference;

    public ThemeService()
    {
        // Charger la préférence de thème au démarrage
        LoadThemePreference();
    }

    public AppTheme AppTheme
    {
        get => appTheme;
        set
        {
            if (appTheme != value)
            {
                appTheme = value;
                SaveThemePreference();
                UpdateTheme();
            }
        }
    }

    public bool IsDarkMode => appTheme == AppTheme.Dark ||
                              (appTheme == AppTheme.Unspecified && systemPreference);

    // Récupérer et sauvegarder les préférences de thème
    void SaveThemePreference()
    {
        Preferences.Set(themeKey, appTheme.ToString());
    }

    void LoadThemePreference()
    {
        if (Preferences.ContainsKey(themeKey))
        {
            var savedTheme = Preferences.Get(themeKey, AppTheme.Unspecified.ToString());
            appTheme = Enum.TryParse(savedTheme, out AppTheme mode) ? mode : AppTheme.Unspecified;
        }
        else
        {
            appTheme = AppTheme.Unspecified;
        }

        UpdateTheme();
    }

    void UpdateTheme()
    {
        var isDarkMode = IsDarkMode;
        // On passe un booléen pour indiquer si le thème est sombre ou non
        ThemeChanged?.Invoke(isDarkMode);

        // IMPORTANT: Pour le theme de l'application MAUI
        Application.Current!.UserAppTheme = appTheme;
    }

    // Définir la préférence système
    public void SetSystemPreference(bool isDark)
    {
        systemPreference = isDark;
        if (appTheme == AppTheme.Unspecified)
        {
            UpdateTheme();
        }
    }
}
