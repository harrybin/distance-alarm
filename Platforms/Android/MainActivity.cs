using Android.App;
using Android.Content.PM;
using Android.OS;

namespace DistanceAlarm;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainActivity OnCreate starting...");
            base.OnCreate(savedInstanceState);
            System.Diagnostics.Debug.WriteLine("MainActivity OnCreate completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainActivity OnCreate failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            throw; // Re-throw to see the actual error
        }
    }

    protected override void OnStart()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainActivity OnStart starting...");
            base.OnStart();
            System.Diagnostics.Debug.WriteLine("MainActivity OnStart completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainActivity OnStart failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    protected override void OnResume()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainActivity OnResume starting...");
            base.OnResume();
            System.Diagnostics.Debug.WriteLine("MainActivity OnResume completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainActivity OnResume failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }
}
