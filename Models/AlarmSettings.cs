using System.ComponentModel;

namespace DistanceAlarm.Models;

public class AlarmSettings : INotifyPropertyChanged
{
    private int _pingInterval = 5; // seconds
    private bool _vibrationEnabled = true;
    private bool _soundEnabled = true;
    private bool _notificationEnabled = true;
    private int _vibrationDuration = 1000; // milliseconds
    private double _soundVolume = 0.8;
    private string _alarmSoundPath = "";

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}