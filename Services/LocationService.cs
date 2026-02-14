using DistanceAlarm.Models;

namespace DistanceAlarm.Services;

public class LocationService : ILocationService
{
    public async Task<bool> RequestLocationPermissionsAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                // Also request background location if available
                #if ANDROID
                if (DeviceInfo.Version.Major >= 10)
                {
                    // Request background location for safe zones to work when app is backgrounded
                    var backgroundStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
                    return backgroundStatus == PermissionStatus.Granted;
                }
                #endif
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Location permission request failed: {ex.Message}");
            return false;
        }
    }

    public async Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(request);
            
            if (location != null)
            {
                return (location.Latitude, location.Longitude);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get location: {ex.Message}");
        }
        
        return null;
    }

    public async Task<bool> IsInAnySafeZoneAsync(IEnumerable<SafeZone> safeZones)
    {
        var currentZone = await GetCurrentSafeZoneAsync(safeZones);
        return currentZone != null;
    }

    public async Task<SafeZone?> GetCurrentSafeZoneAsync(IEnumerable<SafeZone> safeZones)
    {
        try
        {
            var location = await GetCurrentLocationAsync();
            if (!location.HasValue)
                return null;

            foreach (var zone in safeZones.Where(z => z.IsEnabled))
            {
                if (zone.IsLocationInZone(location.Value.Latitude, location.Value.Longitude))
                {
                    System.Diagnostics.Debug.WriteLine($"Currently in safe zone: {zone.Name}");
                    return zone;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking safe zones: {ex.Message}");
        }

        return null;
    }
}
