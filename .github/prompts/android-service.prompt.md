---
mode: agent
description: Add or extend an Android foreground/background service for BLE monitoring or alarm delivery
---

## Context

The app has two Android services:

| Class | Purpose |
|-------|---------|
| `Platforms/Android/AndroidBackgroundService.cs` | General background monitoring; requests Doze-mode exemption |
| `Platforms/Android/BluetoothMonitoringService.cs` | Continuous BLE monitoring foreground service |

Both implement `IBackgroundService` (cross-platform interface in `Services/IBackgroundService.cs`) and register in `MauiProgram.cs`:

```csharp
#if ANDROID
builder.Services.AddSingleton<IBackgroundService, Platforms.Android.AndroidBackgroundService>();
#endif
```

## Foreground service template

```csharp
// Platforms/Android/MyMonitoringService.cs
[Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeConnectedDevice)]
public class MyMonitoringService : Service
{
    private const int NotificationId = 1001;
    private const string ChannelId = "my_monitoring_channel";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        StartForeground(NotificationId, BuildNotification());
        // Start your monitoring logic here
        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "My Monitoring", NotificationImportance.Low);
            var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;
            notificationManager.CreateNotificationChannel(channel);
        }
    }

    private Notification BuildNotification() =>
        new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Distance Alarm")
            .SetContentText("Monitoring active")
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetOngoing(true)
            .Build();
}
```

## Starting a foreground service (API-level safe)

```csharp
var intent = new Intent(context, typeof(MyMonitoringService));
if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
    context.StartForegroundService(intent);
else
    context.StartService(intent);
```

## Requesting Doze exemption

```csharp
if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
{
    var pm = (PowerManager)GetSystemService(PowerService)!;
    if (!pm.IsIgnoringBatteryOptimizations(PackageName))
    {
        var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations)
            .SetData(Android.Net.Uri.Parse($"package:{PackageName}"));
        StartActivity(intent);
    }
}
```

## AndroidManifest.xml entries required

```xml
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_CONNECTED_DEVICE" />
<uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS" />

<application>
    <service android:name=".MyMonitoringService"
             android:foregroundServiceType="connectedDevice"
             android:exported="false" />
</application>
```

## Rules

1. All foreground services must declare a `foregroundServiceType` in the manifest and in the `[Service]` attribute.
2. Use `StartForeground()` immediately in `OnStartCommand` to avoid ANR on Android 8+.
3. Always create a notification channel before calling `StartForeground` on Android 8+.
4. Request Doze-mode exemption (`REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`) for services that must run continuously.
5. Keep notification text short and use a low-importance channel to avoid disturbing the user.
6. Return `StartCommandResult.Sticky` so Android restarts the service if killed.
7. Clean up resources (BLE connections, timers) in `OnDestroy`.

## Task

${input:task:Describe the service you want to add or the change you need to make}
