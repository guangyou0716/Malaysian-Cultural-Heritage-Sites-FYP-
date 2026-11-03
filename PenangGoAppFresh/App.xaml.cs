namespace PenangGoAppFresh;

public partial class App : Application
{
    private static bool _isInitialized = false;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (_isInitialized)
        {
            return null;
        }

        _isInitialized = true;

        var window = new Window(new MainPage())
        {
            Title = "PenangGo - Malaysian Cultural Heritage",
            Width = 1200,
            Height = 800
        };

#if WINDOWS
        // Center the window on startup
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        var screenWidth = displayInfo.Width;
        var screenHeight = displayInfo.Height;
        window.X = (screenWidth - window.Width) / 2;
        window.Y = (screenHeight - window.Height) / 2;
#endif

        return window;
    }
}
