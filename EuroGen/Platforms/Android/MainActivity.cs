using Android.App;
using Android.Content.PM;
using Android.OS;
using MApp = Microsoft.Maui.Controls;

namespace EuroGen
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetNavigationBarColor();

            MApp.Application.Current!.RequestedThemeChanged += (sender, args) =>
            {
                SetNavigationBarColor();
            };
        }

        private void SetNavigationBarColor()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                var lightColor = (Color?)MApp.Application.Current?.Resources["PageBackgroundColor"];
                var darkColor = (Color?)MApp.Application.Current?.Resources["PageBackgroundColor2"];

                var currentTheme = MApp.Application.Current?.RequestedTheme;
                var color = currentTheme == AppTheme.Dark ? darkColor : lightColor;

                Window?.SetNavigationBarColor(Android.Graphics.Color.ParseColor(color?.ToHex()!));
            }
        }
    }
}
