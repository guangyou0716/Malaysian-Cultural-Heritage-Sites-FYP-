using Android.App;
using Android.Content.PM;
using Android.OS;
using PenangGoAppFresh.Services;
using Android.Content;

namespace PenangGoAppFresh;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // Remove ARService and IARService references as AR is replaced by panorama viewer.
}
