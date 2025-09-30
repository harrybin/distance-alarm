using DistanceAlarm.Models;
using Microsoft.Maui.Controls;

namespace DistanceAlarm.Services;

public class AlarmService : IAlarmService
{
    private bool _isAlarmActive;

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
        // Implementation to stop all active alarms
    }

    public async Task VibrateAsync(int durationMs)
    {
        try
        {
            // Platform-specific vibration will be handled by AndroidAlarmService
            // This is a fallback implementation
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            // Log error - vibration might not be supported
            System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
        }
    }

    public async Task PlaySoundAsync(string soundPath, double volume)
    {
        try
        {
            // Platform-specific sound implementation will be added
            // For now, just a placeholder
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
            // Use the platform-specific notification system instead of UI alerts
            // For now, just log the notification - this should be implemented in AndroidAlarmService
            System.Diagnostics.Debug.WriteLine($"NOTIFICATION: {title} - {message}");

            // Avoid accessing Application.Current.MainPage which can cause crashes on Wear OS
            // This will be handled by platform-specific implementations
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}");
        }
    }
}