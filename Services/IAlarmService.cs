using DistanceAlarm.Models;

namespace DistanceAlarm.Services;

public interface IAlarmService
{
    Task TriggerAlarmAsync(AlarmSettings settings);
    Task StopAlarmAsync();
    Task VibrateAsync(int durationMs);
    Task PlaySoundAsync(string soundPath, double volume);
    Task ShowNotificationAsync(string title, string message);
    bool IsAlarmActive { get; }
}