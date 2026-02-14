namespace DistanceAlarm.Services;

public interface IBackgroundService
{
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    bool IsMonitoring { get; }
    Task<bool> RequestBatteryOptimizationExemptionAsync();
}
