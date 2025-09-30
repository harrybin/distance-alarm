namespace DistanceAlarm.Services;

public interface IPermissionService
{
    Task<bool> RequestBluetoothPermissionsAsync();
    Task<bool> CheckBluetoothPermissionsAsync();
    Task<bool> RequestLocationPermissionsAsync();
    Task<bool> CheckLocationPermissionsAsync();
    Task<bool> RequestNotificationPermissionsAsync();
    Task<bool> CheckNotificationPermissionsAsync();
}