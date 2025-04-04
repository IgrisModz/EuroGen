using CommunityToolkit.Maui.Behaviors;

namespace EuroGen;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

#if ANDROID || IOS
        SetStatusBarColor();

        Application.Current!.RequestedThemeChanged += (sender, args) =>
        {
            SetStatusBarColor();
        };
#endif
    }

    public void SetStatusBarColor()
    {
#if ANDROID || IOS
#if IOS
        if (!UIKit.UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
        {
            return;
        }
#elif ANDROID
        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
        {
            return;
        }
#endif
        var existingBehavior = Behaviors.OfType<StatusBarBehavior>().FirstOrDefault();
        if (existingBehavior != null)
        {
            Behaviors.Remove(existingBehavior);
        }

        var newBehavior = new StatusBarBehavior
        {
            StatusBarColor = Application.Current!.RequestedTheme == AppTheme.Dark
                ? (Color)Application.Current.Resources["DarkAssetColor"]
                : (Color)Application.Current.Resources["LightAssetColor"]
        };

        Behaviors.Add(newBehavior);
#endif
    }
}
