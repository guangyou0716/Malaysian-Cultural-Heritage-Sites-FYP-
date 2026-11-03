using Microsoft.Maui.Storage;

namespace PenangGoAppFresh.Services;

public static class AppSettings
{
    private const string ThemeKey = "penanggo_theme";
    private const string ReduceMotionKey = "penanggo_reduce_motion";

    public static string Theme
    {
        get => Preferences.Default.Get(ThemeKey, "system");
        set => Preferences.Default.Set(ThemeKey, value);
    }

    public static bool ReduceMotion
    {
        get => Preferences.Default.Get(ReduceMotionKey, false);
        set => Preferences.Default.Set(ReduceMotionKey, value);
    }
}