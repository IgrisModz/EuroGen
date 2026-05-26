namespace EuroGen;

public partial class App : Application
{
    const double windowWidth = 700;
    const double windowHeight = 680;
	const double maxWindowHeight = 960;
	const double minWindowWidth = 420;

	public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
		var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
		var density = displayInfo.Density;

		// Calculate the screen width and height in device-independent units (DIPs)
		var screenWidth = displayInfo.Width / density;
		var screenHeight = displayInfo.Height / density;

		// Calculate the position to center the window on the screen
		var posX = (screenWidth - windowWidth) / 2;
		var posY = (screenHeight - windowHeight) / 2;

		var window = new Window(new MainPage())
        {
            Title = "EuroGen",
			IsMaximizable = false,

            Width = windowWidth,
            Height = windowHeight,

            MaximumHeight = maxWindowHeight,
            MaximumWidth = windowWidth,

            MinimumHeight = windowHeight,
            MinimumWidth = minWindowWidth,

			X = posX,
			Y = posY
		};

        return window;
    }
}
