using EuroGen.Components.Pages;
using MudBlazor;

namespace EuroGen.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    MudThemeProvider? mudThemeProvider;
    Action<bool>? themeChangedHandler;
    Action? languageChangedHandler;
    Action? drawStatusChangedHandler;

    readonly MudTheme mudTheme = new()
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

    bool IsActive(string href) =>
        Navigation.Uri.TrimEnd('/') == Navigation.ToAbsoluteUri(href).AbsoluteUri.TrimEnd('/');

    Task<IDialogReference> OpenSettings()
    {
        return DialogService.ShowAsync<Settings>(Localizer["Settings"]);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (mudThemeProvider is not null)
            {
                var systemPreference = await mudThemeProvider.GetSystemDarkModeAsync();
                ThemeService.SetSystemPreference(systemPreference);

                await mudThemeProvider.WatchSystemDarkModeAsync(newValue =>
                {
                    ThemeService.SetSystemPreference(newValue);
                    StateHasChanged();
                    return Task.CompletedTask;
                });
            }

            var updateInfo = await UpdateService.CheckForUpdatesAsync();
            if (updateInfo is not null)
            {
                var parameters = new DialogParameters
                {
                    { nameof(UpdateDialog.UpdateInfo), updateInfo }
                };

                var options = new DialogOptions
                {
                    CloseOnEscapeKey = !updateInfo.IsMandatory,
                    CloseButton = false,
                    BackdropClick = !updateInfo.IsMandatory,
                    FullScreen = true,
                    MaxWidth = MaxWidth.ExtraLarge
                };

                await DialogService.ShowAsync<UpdateDialog>(Localizer["UpdateAvailable"], parameters, options);
            }
            else
            {
                await UpdateService.DeleteUpdates();
            }

                themeChangedHandler = isDarkMode => { _ = InvokeAsync(StateHasChanged); };
                ThemeService.ThemeChanged += themeChangedHandler;

            languageChangedHandler = () => _ = InvokeAsync(StateHasChanged);
            Localizer.LanguageChanged += languageChangedHandler;

            drawStatusChangedHandler = () => _ = InvokeAsync(StateHasChanged);
            DrawService.StatusChanged += drawStatusChangedHandler;

            StateHasChanged();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (themeChangedHandler is not null)
        {
            ThemeService.ThemeChanged -= themeChangedHandler;
            themeChangedHandler = null;
        }

        if (languageChangedHandler is not null)
        {
            Localizer.LanguageChanged -= languageChangedHandler;
            languageChangedHandler = null;
        }

        if (drawStatusChangedHandler is not null)
        {
            DrawService.StatusChanged -= drawStatusChangedHandler;
            drawStatusChangedHandler = null;
        }

		GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
