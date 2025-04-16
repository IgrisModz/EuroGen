using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using System.Runtime.Versioning;

namespace EuroGen;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            SetStatusBarColor();

            Application.Current!.RequestedThemeChanged += (sender, args) =>
            {
                SetStatusBarColor();
            };
        }
    }

    [SupportedOSPlatform("android21.0")]
    [SupportedOSPlatform("ios11.0")]
    public void SetStatusBarColor()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(21) || OperatingSystem.IsIOSVersionAtLeast(11, 0))
        {

            var lightColor = (Color?)Application.Current?.Resources["LightAssetColor"];
            var darkColor = (Color?)Application.Current?.Resources["DarkAssetColor"];

            var currentTheme = Application.Current?.RequestedTheme;
            var isDarkMode = currentTheme == AppTheme.Dark;

            CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(isDarkMode ? darkColor : lightColor);
        }
    }
}
