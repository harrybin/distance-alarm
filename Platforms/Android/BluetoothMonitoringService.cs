using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using DistanceAlarm.Services;

namespace DistanceAlarm.Platforms.Android;

/// <summary>
/// Foreground service to maintain BLE connection monitoring in the background
/// This ensures the app continues to monitor the connection even when the user
/// locks the screen or switches to another app
/// </summary>
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class BluetoothMonitoringService : Service
{
    private const int NotificationId = 9001;
    private const string ChannelId = "bluetooth_monitoring_channel";
    private PowerManager.WakeLock? _wakeLock;
    private IBluetoothService? _bluetoothService;
    private bool _isRunning = false;

    public override IBinder? OnBind(Intent? intent)
    {
        return null; // This is a started service, not a bound service
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (_isRunning)
            return StartCommandResult.Sticky;

        _isRunning = true;

        // Create notification channel for Android 8.0+
        CreateNotificationChannel();

        // Create foreground notification
        var notification = CreateNotification();
        StartForeground(NotificationId, notification);

        // Acquire wake lock to keep CPU running for BLE monitoring
        AcquireWakeLock();

        System.Diagnostics.Debug.WriteLine("BluetoothMonitoringService started");

        return StartCommandResult.Sticky; // Restart service if killed by system
    }

    public override void OnDestroy()
    {
        _isRunning = false;
        ReleaseWakeLock();
        System.Diagnostics.Debug.WriteLine("BluetoothMonitoringService stopped");
        base.OnDestroy();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ChannelId,
                "Bluetooth Monitoring",
                NotificationImportance.Low)
            {
                Description = "Monitors Bluetooth connection to prevent device loss"
            };

            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    private Notification CreateNotification()
    {
        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Distance Alarm Active")
            .SetContentText("Monitoring connection to paired device")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetPriority(NotificationCompat.PriorityLow)
            .SetOngoing(true) // Cannot be dismissed by user
            .SetCategory(NotificationCompat.CategoryService);

        return builder.Build();
    }

    private void AcquireWakeLock()
    {
        try
        {
            var powerManager = GetSystemService(PowerService) as PowerManager;
            if (powerManager != null)
            {
                _wakeLock = powerManager.NewWakeLock(
                    WakeLockFlags.Partial,
                    "DistanceAlarm::BluetoothMonitoringWakeLock");
                
                _wakeLock?.Acquire();
                System.Diagnostics.Debug.WriteLine("Wake lock acquired");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to acquire wake lock: {ex.Message}");
        }
    }

    private void ReleaseWakeLock()
    {
        try
        {
            if (_wakeLock?.IsHeld == true)
            {
                _wakeLock.Release();
                System.Diagnostics.Debug.WriteLine("Wake lock released");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to release wake lock: {ex.Message}");
        }
    }
}
