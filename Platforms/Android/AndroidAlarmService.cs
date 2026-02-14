using Android.Content;
using Android.Media;
using AndroidX.Core.App;
using DistanceAlarm.Models;
using DistanceAlarm.Services;
using Microsoft.Maui.Controls;

namespace DistanceAlarm.Platforms.Android;

public class AndroidAlarmService : IAlarmService
{
    private bool _isAlarmActive;
    private const int NotificationId = 1001;
    private MediaPlayer? _mediaPlayer;
    private global::Android.OS.PowerManager.WakeLock? _screenWakeLock;

    public bool IsAlarmActive => _isAlarmActive;

    public async Task TriggerAlarmAsync(AlarmSettings settings)
    {
        if (_isAlarmActive)
            return;

        _isAlarmActive = true;

        // Wake up the screen first so user notices the alarm
        await WakeUpScreenAsync();

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
        
        // Stop sound playback
        try
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Release();
            _mediaPlayer = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping media player: {ex.Message}");
        }

        // Release screen wake lock
        ReleaseScreenWakeLock();

        // Cancel notification
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
            var context = Platform.CurrentActivity?.ApplicationContext ?? global::Android.App.Application.Context;
            
            // Stop any existing playback
            _mediaPlayer?.Stop();
            _mediaPlayer?.Release();

            // Create media player with default alarm sound
            _mediaPlayer = new MediaPlayer();
            
            // Use system alarm sound or notification sound
            var alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
            if (alarmUri == null)
            {
                alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
            }

            if (alarmUri != null)
            {
                _mediaPlayer.SetDataSource(context, alarmUri);
                _mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Alarm)
                    .SetContentType(AudioContentType.Sonification)
                    .Build());
                
                _mediaPlayer.Looping = true; // Loop until stopped
                _mediaPlayer.SetVolume((float)volume, (float)volume);
                
                _mediaPlayer.Prepare();
                _mediaPlayer.Start();
                
                System.Diagnostics.Debug.WriteLine("Alarm sound started");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sound playback error: {ex.Message}");
        }
    }

    private async Task WakeUpScreenAsync()
    {
        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? global::Android.App.Application.Context;
            var powerManager = context.GetSystemService(Context.PowerService) as global::Android.OS.PowerManager;
            
            if (powerManager != null)
            {
                // Acquire wake lock to turn on screen
                _screenWakeLock = powerManager.NewWakeLock(
                    global::Android.OS.WakeLockFlags.ScreenBright | 
                    global::Android.OS.WakeLockFlags.AcquireCausesWakeup |
                    global::Android.OS.WakeLockFlags.OnAfterRelease,
                    "DistanceAlarm::AlarmWakeLock");
                
                _screenWakeLock?.Acquire((long)TimeSpan.FromMinutes(1).TotalMilliseconds); // Auto-release after 1 minute
                System.Diagnostics.Debug.WriteLine("Screen wake lock acquired");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to wake screen: {ex.Message}");
        }
    }

    private void ReleaseScreenWakeLock()
    {
        try
        {
            if (_screenWakeLock?.IsHeld == true)
            {
                _screenWakeLock.Release();
                System.Diagnostics.Debug.WriteLine("Screen wake lock released");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error releasing screen wake lock: {ex.Message}");
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