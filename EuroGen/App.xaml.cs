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
            Width = Width,
            Height = Height,
            MaximumHeight = 960,
            MaximumWidth = Width,
            MinimumHeight = Height,
            MinimumWidth = 420
        };

        return window;
    }
}
