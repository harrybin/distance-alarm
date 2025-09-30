using Plugin.BLE.Abstractions.Contracts;
using System.ComponentModel;

namespace DistanceAlarm.Models;

public class BleDevice : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _macAddress = string.Empty;
    private int _rssiValue;
    private bool _isConnected;
    private DateTime _lastSeen;
    private IDevice? _device;

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
                OnPropertyChanged(nameof(DisplayName));
            }
        } 
    }

    public string MacAddress 
    { 
        get => _macAddress; 
        set 
        { 
            if (_macAddress != value)
            {
                _macAddress = value;
                OnPropertyChanged(nameof(MacAddress));
                OnPropertyChanged(nameof(DisplayName));
            }
        } 
    }

    public int RssiValue 
    { 
        get => _rssiValue; 
        set 
        { 
            if (_rssiValue != value)
            {
                _rssiValue = value;
                OnPropertyChanged(nameof(RssiValue));
                OnPropertyChanged(nameof(SignalStrength));
            }
        } 
    }

    public bool IsConnected 
    { 
        get => _isConnected; 
        set 
        { 
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        } 
    }

    public DateTime LastSeen 
    { 
        get => _lastSeen; 
        set 
        { 
            if (_lastSeen != value)
            {
                _lastSeen = value;
                OnPropertyChanged(nameof(LastSeen));
            }
        } 
    }

    public IDevice? Device 
    { 
        get => _device; 
        set 
        { 
            if (_device != value)
            {
                _device = value;
                OnPropertyChanged(nameof(Device));
            }
        } 
    }

    public string DisplayName => !string.IsNullOrWhiteSpace(Name) && Name != "Unknown" ? Name : 
                                !string.IsNullOrWhiteSpace(MacAddress) ? $"Device ({MacAddress})" : 
                                $"Unknown Device";
    
    public string SignalStrength => $"{RssiValue} dBm";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}

public class ConnectionState
{
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
    public BleDevice? ConnectedDevice { get; set; }
    public DateTime LastPingTime { get; set; }
    public bool IsAlarmActive { get; set; }
    public string StatusMessage { get; set; } = "Not connected";
    public int FailedPingCount { get; set; }
}