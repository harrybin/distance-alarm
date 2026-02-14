using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidNet = Android.Net;

namespace DistanceAlarm.Platforms.Android;

public class AndroidBackgroundService : DistanceAlarm.Services.IBackgroundService
{
    private bool _isMonitoring;

    public bool IsMonitoring => _isMonitoring;

    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring)
            return;

        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? Application.Context;

            // Request battery optimization exemption before starting
            await RequestBatteryOptimizationExemptionAsync();

            // Start the foreground service
            var serviceIntent = new Intent(context, typeof(BluetoothMonitoringService));
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(serviceIntent);
            }
            else
            {
                context.StartService(serviceIntent);
            }

            _isMonitoring = true;
            System.Diagnostics.Debug.WriteLine("Background monitoring service started");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start background service: {ex.Message}");
            throw;
        }
    }

    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
            return;

        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? Application.Context;
            var serviceIntent = new Intent(context, typeof(BluetoothMonitoringService));
            context.StopService(serviceIntent);

            _isMonitoring = false;
            System.Diagnostics.Debug.WriteLine("Background monitoring service stopped");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to stop background service: {ex.Message}");
        }
    }

    public async Task<bool> RequestBatteryOptimizationExemptionAsync()
    {
        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? Application.Context;
            var powerManager = context.GetSystemService(Context.PowerService) as PowerManager;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && powerManager != null)
            {
                var packageName = context.PackageName;
                
                // Check if already exempted
                if (!powerManager.IsIgnoringBatteryOptimizations(packageName))
                {
                    // Request exemption from battery optimization
                    var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(AndroidNet.Uri.Parse($"package:{packageName}"));
                    
                    if (Platform.CurrentActivity != null)
                    {
                        Platform.CurrentActivity.StartActivity(intent);
                        System.Diagnostics.Debug.WriteLine("Battery optimization exemption requested");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("App already exempted from battery optimization");
                    return true;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to request battery optimization exemption: {ex.Message}");
            return false;
        }
    }
}
