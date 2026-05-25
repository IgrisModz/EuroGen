namespace EuroGen;

public partial class App : Application
{
    const int width = 640;
    const int height = 680;

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
            Width = width,
            Height = height,
            MaximumHeight = 960,
            MaximumWidth = width,
            MinimumHeight = height,
            MinimumWidth = 420,
			X = (DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - width) / 2,
			Y = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density - height) / 2
		};

        return window;
    }
}
