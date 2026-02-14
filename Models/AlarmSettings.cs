using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DistanceAlarm.Models;

public class AlarmSettings : INotifyPropertyChanged
{
    private int _pingInterval = 10; // seconds - increased from 5 for battery life
    private bool _vibrationEnabled = true;
    private bool _soundEnabled = true;
    private bool _notificationEnabled = true;
    private int _vibrationDuration = 1000; // milliseconds
    private double _soundVolume = 0.8;
    private string _alarmSoundPath = "";
    private int _rssiThreshold = -80; // dBm - trigger alarm when signal weaker than this
    private int _failedPingThreshold = 2; // Number of failed pings before triggering alarm (reduced from 3)
    private bool _enableSafeZones = true;
    private bool _enableAutoReconnect = true;
    private int _reconnectMaxAttempts = 5;
    private int _reconnectInitialDelaySeconds = 2;

    public int PingInterval
    {
        get => _pingInterval;
        set
        {
            if (_pingInterval != value)
            {
                _pingInterval = value;
                OnPropertyChanged(nameof(PingInterval));
            }
        }
    }

    public bool VibrationEnabled
    {
        get => _vibrationEnabled;
        set
        {
            if (_vibrationEnabled != value)
            {
                _vibrationEnabled = value;
                OnPropertyChanged(nameof(VibrationEnabled));
            }
        }
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            if (_soundEnabled != value)
            {
                _soundEnabled = value;
                OnPropertyChanged(nameof(SoundEnabled));
            }
        }
    }

    public bool NotificationEnabled
    {
        get => _notificationEnabled;
        set
        {
            if (_notificationEnabled != value)
            {
                _notificationEnabled = value;
                OnPropertyChanged(nameof(NotificationEnabled));
            }
        }
    }

    public int VibrationDuration
    {
        get => _vibrationDuration;
        set
        {
            if (_vibrationDuration != value)
            {
                _vibrationDuration = value;
                OnPropertyChanged(nameof(VibrationDuration));
            }
        }
    }

    public double SoundVolume
    {
        get => _soundVolume;
        set
        {
            if (Math.Abs(_soundVolume - value) > 0.01)
            {
                _soundVolume = value;
                OnPropertyChanged(nameof(SoundVolume));
            }
        }
    }

    public string AlarmSoundPath
    {
        get => _alarmSoundPath;
        set
        {
            if (_alarmSoundPath != value)
            {
                _alarmSoundPath = value;
                OnPropertyChanged(nameof(AlarmSoundPath));
            }
        }
    }

    public int RssiThreshold
    {
        get => _rssiThreshold;
        set
        {
            if (_rssiThreshold != value)
            {
                _rssiThreshold = value;
                OnPropertyChanged(nameof(RssiThreshold));
            }
        }
    }

    public int FailedPingThreshold
    {
        get => _failedPingThreshold;
        set
        {
            if (_failedPingThreshold != value)
            {
                _failedPingThreshold = value;
                OnPropertyChanged(nameof(FailedPingThreshold));
            }
        }
    }

    public bool EnableSafeZones
    {
        get => _enableSafeZones;
        set
        {
            if (_enableSafeZones != value)
            {
                _enableSafeZones = value;
                OnPropertyChanged(nameof(EnableSafeZones));
            }
        }
    }

    public bool EnableAutoReconnect
    {
        get => _enableAutoReconnect;
        set
        {
            if (_enableAutoReconnect != value)
            {
                _enableAutoReconnect = value;
                OnPropertyChanged(nameof(EnableAutoReconnect));
            }
        }
    }

    public int ReconnectMaxAttempts
    {
        get => _reconnectMaxAttempts;
        set
        {
            if (_reconnectMaxAttempts != value)
            {
                _reconnectMaxAttempts = value;
                OnPropertyChanged(nameof(ReconnectMaxAttempts));
            }
        }
    }

    public int ReconnectInitialDelaySeconds
    {
        get => _reconnectInitialDelaySeconds;
        set
        {
            if (_reconnectInitialDelaySeconds != value)
            {
                _reconnectInitialDelaySeconds = value;
                OnPropertyChanged(nameof(ReconnectInitialDelaySeconds));
            }
        }
    }

    public ObservableCollection<SafeZone> SafeZones { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}