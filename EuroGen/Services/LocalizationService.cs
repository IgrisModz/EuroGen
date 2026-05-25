using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Reflection;

namespace EuroGen.Services;

public class LocalizationService
{
    const string languageKey = "AppLanguage";
    readonly IStringLocalizer localizer;

    string language = "";

    public string Language
    {
        get => language;
        set
        {
            language = value;
            SaveLanguagePreference(value);
            UpdateLanguage();
        }
    }

    public Action? LanguageChanged;

    public string this[string key] => localizer[key];

    public LocalizationService(IStringLocalizerFactory factory)
    {
        var type = typeof(Resources.Strings);
        var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName!);
        localizer = factory.Create(type.Name, assemblyName.Name!);
        LoadLanguagePreference();
    }

    public static void SaveLanguagePreference(string language)
    {
        Preferences.Set(languageKey, language);
        SetCulture(language);
    }

    // Charger la préférence de langue depuis les preferences
    void LoadLanguagePreference()
    {
        if (Preferences.ContainsKey(languageKey))
        {
            var savedLanguage = Preferences.Get(languageKey, "en"); // Valeur par défaut "en"
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

    void UpdateLanguage()
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
        return localizer[key];
    }
}
