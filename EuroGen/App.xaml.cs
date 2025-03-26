namespace EuroGen;

public partial class App : Application
{
    public const int Width = 1200;
    public const int Height = 724;

    public App()
    {
        InitializeComponent();

        MainPage = new MainPage();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = "EuroGen";
        window.Width = Width;
        window.Height = Height;
        window.MaximumHeight = 960;
        window.MaximumWidth = 1200;
        window.MinimumHeight = 724;
        window.MinimumWidth = 500;

        return window;
    }
}
