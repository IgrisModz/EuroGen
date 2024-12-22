using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Reflection;

namespace EuroGen.Services
{
    public class LocalizationService
    {
        private const string LanguageKey = "AppLanguage";
        private readonly IStringLocalizer _localizer;

        private string _language = "";

        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                SaveLanguagePreference(value);
                UpdateLanguage();
            }
        }

        public Action? LanguageChanged;

        public string this[string key] => _localizer[key];

        public LocalizationService(IStringLocalizerFactory factory)
        {
            var type = typeof(Resources.Strings);
            var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName!);
            _localizer = factory.Create(type.Name, assemblyName.Name!);
            LoadLanguagePreference();
        }

        public static void SaveLanguagePreference(string language)
        {
            Preferences.Set(LanguageKey, language);
            SetCulture(language);
        }

        // Charger la préférence de langue depuis les preferences
        private void LoadLanguagePreference()
        {
            if (Preferences.ContainsKey(LanguageKey))
            {
                var savedLanguage = Preferences.Get(LanguageKey, "en"); // Valeur par défaut "en"
                Language = savedLanguage;
            }
            else
            {
                string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                string language = currentCulture switch
                {
                    "ca" or "de" or "es" or "en" or "fr" or "ga" or "gv" or "it" or "lb" or "nl" or "pt" => currentCulture,
                    _ => "en",
                };
                Language = language;
            }

            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            LanguageChanged?.Invoke();
        }

        public static void SetCulture(string language)
        {
            var currentCulture = new CultureInfo(language);
            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = currentCulture;
        }

        public string GetString(string key)
        {
            return _localizer[key];
        }
    }
}
