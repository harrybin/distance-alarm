using Android.Content;
using AndroidX.Core.App;
using DistanceAlarm.Models;
using DistanceAlarm.Services;
using Microsoft.Maui.Controls;

namespace DistanceAlarm.Platforms.Android;

public class AndroidAlarmService : IAlarmService
{
    private bool _isAlarmActive;
    private const int NotificationId = 1001;

    public bool IsAlarmActive => _isAlarmActive;

    public async Task TriggerAlarmAsync(AlarmSettings settings)
    {
        if (_isAlarmActive)
            return;

        _isAlarmActive = true;

        var tasks = new List<Task>();

        if (settings.VibrationEnabled)
        {
            tasks.Add(VibrateAsync(settings.VibrationDuration));
        }

        if (settings.SoundEnabled)
        {
            tasks.Add(PlaySoundAsync(settings.AlarmSoundPath, settings.SoundVolume));
        }

        if (settings.NotificationEnabled)
        {
            tasks.Add(ShowNotificationAsync("Distance Alarm", "Connection to paired device lost!"));
        }

        await Task.WhenAll(tasks);
    }

    public async Task StopAlarmAsync()
    {
        _isAlarmActive = false;
        // Cancel any ongoing alarms
        var context = Platform.CurrentActivity?.ApplicationContext ?? global::Android.App.Application.Context;
        var notificationManager = NotificationManagerCompat.From(context);
        notificationManager.Cancel(NotificationId);
    }

    public async Task VibrateAsync(int durationMs)
    {
        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? global::Android.App.Application.Context;
            var vibrator = context?.GetSystemService(Context.VibratorService) as global::Android.OS.Vibrator;
            if (vibrator != null && vibrator.HasVibrator)
            {
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    var vibrationEffect = global::Android.OS.VibrationEffect.CreateOneShot(durationMs, global::Android.OS.VibrationEffect.DefaultAmplitude);
                    vibrator.Vibrate(vibrationEffect);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    vibrator.Vibrate(durationMs);
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
        }
    }

    public async Task PlaySoundAsync(string soundPath, double volume)
    {
        try
        {
            // Implementation for playing custom sounds
            // For now, use system notification sound
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sound playback error: {ex.Message}");
        }
    }

    public async Task ShowNotificationAsync(string title, string message)
    {
        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? global::Android.App.Application.Context;

            // Create notification channel for Android 8.0+
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var channelId = "distance_alarm_channel";
                var channelName = "Distance Alarm";
                var channelDescription = "Notifications for distance alarm events";

                var channel = new global::Android.App.NotificationChannel(channelId, channelName, global::Android.App.NotificationImportance.High)
                {
                    Description = channelDescription
                };

                var notificationManager = context.GetSystemService(Context.NotificationService) as global::Android.App.NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);

                var notification = new NotificationCompat.Builder(context, channelId)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert)
                    .SetPriority(NotificationCompat.PriorityHigh)
                    .SetDefaults(NotificationCompat.DefaultAll)
                    .SetAutoCancel(true)
                    .Build();

                var notificationManagerCompat = NotificationManagerCompat.From(context);
                notificationManagerCompat.Notify(NotificationId, notification);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}");
            // Fallback to simple alert
            await Application.Current?.MainPage?.DisplayAlert(title, message, "OK")!;
        }
    }
}