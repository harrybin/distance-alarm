using DistanceAlarm.Models;

namespace DistanceAlarm.Services;

public interface ILocationService
{
    Task<bool> RequestLocationPermissionsAsync();
    Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync();
    Task<bool> IsInAnySafeZoneAsync(IEnumerable<SafeZone> safeZones);
    Task<SafeZone?> GetCurrentSafeZoneAsync(IEnumerable<SafeZone> safeZones);
}
