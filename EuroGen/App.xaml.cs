namespace EuroGen;

public partial class App : Application
{
    public const int Width = 620;
    public const int Height = 680;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage())
        {
            Title = "EuroGen",
			IsMaximizable = false,
            Width = Width,
            Height = Height,
            MaximumHeight = 960,
            MaximumWidth = Width,
            MinimumHeight = Height,
            MinimumWidth = 420,
			X = (DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density) / 2,
			Y = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density) / 2
		};

        return window;
    }
}
