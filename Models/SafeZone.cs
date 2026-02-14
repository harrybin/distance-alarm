using System.ComponentModel;

namespace DistanceAlarm.Models;

/// <summary>
/// Represents a geographical safe zone where distance alarms should not trigger
/// </summary>
public class SafeZone : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Home";
    private double _latitude;
    private double _longitude;
    private double _radiusMeters = 100; // Default 100 meters
    private bool _isEnabled = true;

    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public double Latitude
    {
        get => _latitude;
        set
        {
            if (Math.Abs(_latitude - value) > 0.0000001)
            {
                _latitude = value;
                OnPropertyChanged(nameof(Latitude));
            }
        }
    }

    public double Longitude
    {
        get => _longitude;
        set
        {
            if (Math.Abs(_longitude - value) > 0.0000001)
            {
                _longitude = value;
                OnPropertyChanged(nameof(Longitude));
            }
        }
    }

    public double RadiusMeters
    {
        get => _radiusMeters;
        set
        {
            if (Math.Abs(_radiusMeters - value) > 0.01)
            {
                _radiusMeters = value;
                OnPropertyChanged(nameof(RadiusMeters));
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
    }

    /// <summary>
    /// Checks if the given location is within this safe zone
    /// </summary>
    public bool IsLocationInZone(double latitude, double longitude)
    {
        if (!IsEnabled)
            return false;

        var distance = CalculateDistance(latitude, longitude, Latitude, Longitude);
        return distance <= RadiusMeters;
    }

    /// <summary>
    /// Calculate distance in meters between two GPS coordinates using Haversine formula
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadiusKm * c * 1000; // Convert to meters
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
