namespace EuroGen.Services
{
    public enum ThemeMode
    {
        System,
        Light,
        Dark
    }

    public class ThemeService
    {
        private const string ThemeKey = "AppTheme";

        public event Action<bool>? ThemeChanged; // Modification ici pour accepter un booléen

        private ThemeMode _themeMode;
        private bool _systemPreference;

        public ThemeService()
        {
            // Charger la préférence de thème au démarrage
            LoadThemePreference();
        }

        public ThemeMode ThemeMode
        {
            get => _themeMode;
            set
            {
                if (_themeMode != value)
                {
                    _themeMode = value;
                    SaveThemePreference();
                    UpdateTheme();
                }
            }
        }

        public bool IsDarkMode => _themeMode == ThemeMode.Dark ||
                                   (_themeMode == ThemeMode.System && _systemPreference);

        // Récupérer et sauvegarder les préférences de thème
        private void SaveThemePreference()
        {
            Preferences.Set(ThemeKey, _themeMode.ToString());
        }

        private void LoadThemePreference()
        {
            if (Preferences.ContainsKey(ThemeKey))
            {
                var savedTheme = Preferences.Get(ThemeKey, ThemeMode.System.ToString());
                _themeMode = Enum.TryParse(savedTheme, out ThemeMode mode) ? mode : ThemeMode.System;
            }
            else
            {
                _themeMode = ThemeMode.System;
            }

            UpdateTheme();
        }

        private void UpdateTheme()
        {
            // On passe un booléen pour indiquer si le thème est sombre ou non
            ThemeChanged?.Invoke(_themeMode == ThemeMode.Dark ||
                                 (_themeMode == ThemeMode.System && _systemPreference));
        }

        // Définir la préférence système
        public void SetSystemPreference(bool isDark)
        {
            _systemPreference = isDark;
            if (_themeMode == ThemeMode.System)
            {
                UpdateTheme();
            }
        }
    }
}
