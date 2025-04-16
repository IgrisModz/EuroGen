using EuroGen.Components.Pages;
using MudBlazor;

namespace EuroGen.Components.Layout;

public partial class MainLayout
{
    private MudThemeProvider? _mudThemeProvider;

    private readonly MudTheme _mudTheme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Background = MudBlazor.Colors.Gray.Lighten5,
            AppbarBackground = MudBlazor.Colors.Gray.Lighten5,
            DrawerBackground = MudBlazor.Colors.Gray.Lighten5,
            AppbarText = MudBlazor.Colors.Shades.Black,
            Surface = MudBlazor.Colors.Gray.Lighten5,
            Primary = "#0D1F6F"
        },
        PaletteDark = new PaletteDark()
        {
            Background = MudBlazor.Colors.Gray.Darken4,
            AppbarBackground = MudBlazor.Colors.Gray.Darken4,
            DrawerBackground = MudBlazor.Colors.Gray.Darken4,
            AppbarText = MudBlazor.Colors.Gray.Default,
            Surface = "#252525",
            Primary = MudBlazor.Colors.Blue.Accent4
        },
    };

    private bool IsActive(string href) =>
        Navigation.Uri.TrimEnd('/') == Navigation.ToAbsoluteUri(href).AbsoluteUri.TrimEnd('/');

    private Task<IDialogReference> OpenSettings()
    {
        return DialogService.ShowAsync<Settings>(Localizer["Settings"]);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (_mudThemeProvider != null)
            {
                var systemPreference = await _mudThemeProvider.GetSystemPreference();
                ThemeService.SetSystemPreference(systemPreference);

                await _mudThemeProvider.WatchSystemPreference(newValue =>
                {
                    ThemeService.SetSystemPreference(newValue);
                    StateHasChanged();
                    return Task.CompletedTask;
                });
            }

            ThemeService.ThemeChanged += (isDarkMode) =>
            {
                // Re-render pour appliquer le nouveau thème
                StateHasChanged();
            };

            Localizer.LanguageChanged += () =>
            {
                StateHasChanged();
            };

            DrawService.StatusChanged += () =>
            {
                StateHasChanged();
            };

            StateHasChanged();
        }
    }
}
