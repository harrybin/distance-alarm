using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistanceAlarm.Models;
using DistanceAlarm.Services;

namespace DistanceAlarm.ViewModels;

/// <summary>
/// Minimal ViewModel for the Wear OS watch app.
/// The watch only monitors BLE connection status and triggers a vibration alarm
/// when the connection is lost. All configuration is managed via the companion
/// phone app and stored in shared Preferences.
/// </summary>
public partial class WearOsViewModel : ObservableObject
{
    private readonly IBluetoothService _bluetoothService;
    private readonly IAlarmService _alarmService;
    private readonly IBackgroundService? _backgroundService;

    [ObservableProperty]
    private string _statusMessage = "Initializing...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;

    [ObservableProperty]
    private int _currentRssi = 0;

    [ObservableProperty]
    private bool _isAlarmActive = false;

    [ObservableProperty]
    private string _connectedDeviceName = string.Empty;

    /// <summary>Derived property: true when BLE connection is established.</summary>
    public bool IsConnected => ConnectionStatus == ConnectionStatus.Connected;

    public WearOsViewModel(IBluetoothService bluetoothService, IAlarmService alarmService,
        IBackgroundService? backgroundService = null)
    {
        _bluetoothService = bluetoothService;
        _alarmService = alarmService;
        _backgroundService = backgroundService;

        _bluetoothService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _bluetoothService.ConnectionLost += OnConnectionLost;
        _bluetoothService.RssiUpdated += OnRssiUpdated;

        _ = Task.Run(InitializeAsync);
    }

    private async Task InitializeAsync()
    {
        try
        {
            if (!await _bluetoothService.IsBluetoothEnabledAsync())
            {
                StatusMessage = "Bluetooth off";
                return;
            }

            if (!await _bluetoothService.RequestBluetoothPermissionsAsync())
            {
                StatusMessage = "BT permission denied";
                return;
            }

            // Auto-start background monitoring so the watch keeps working with screen off
            if (_backgroundService != null)
            {
                try
                {
                    await _backgroundService.StartMonitoringAsync();
                    System.Diagnostics.Debug.WriteLine("WearOS: background monitoring started");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WearOS: background monitoring failed: {ex.Message}");
                }
            }

            StatusMessage = "Waiting for connection";

            // Resume pinging any already-connected device
            var connected = await _bluetoothService.GetPairedDevicesAsync();
            if (connected.Any())
            {
                ConnectedDeviceName = connected.First().DisplayName;
                var pingInterval = Preferences.Get("PingInterval", 10);
                await _bluetoothService.StartPingingAsync(pingInterval);
                StatusMessage = $"Monitoring {ConnectedDeviceName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Init error";
            System.Diagnostics.Debug.WriteLine($"WearOsViewModel InitializeAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopAlarmAsync()
    {
        try
        {
            await _alarmService.StopAlarmAsync();
            IsAlarmActive = false;
            StatusMessage = IsConnected ? $"Monitoring {ConnectedDeviceName}" : "Waiting for connection";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WearOsViewModel StopAlarm error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestAlarmAsync()
    {
        try
        {
            var settings = LoadSettingsFromPreferences();
            await _alarmService.TriggerAlarmAsync(settings);
            IsAlarmActive = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WearOsViewModel TestAlarm error: {ex.Message}");
        }
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionState state)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            ConnectionStatus = state.Status;
            StatusMessage = state.StatusMessage;
            ConnectedDeviceName = state.ConnectedDevice?.DisplayName ?? string.Empty;
        });
    }

    private async void OnConnectionLost(object? sender, EventArgs e)
    {
        try
        {
            if (Application.Current?.Dispatcher is { } dispatcher)
            {
                await dispatcher.DispatchAsync(async () =>
                {
                    StatusMessage = "Connection lost!";
                    IsAlarmActive = true;
                    var settings = LoadSettingsFromPreferences();
                    await _alarmService.TriggerAlarmAsync(settings);
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WearOsViewModel OnConnectionLost error: {ex.Message}");
        }
    }

    private void OnRssiUpdated(object? sender, int rssi)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            CurrentRssi = rssi;
        });
    }

    /// <summary>
    /// Loads alarm settings from device Preferences.
    /// On Wear OS the companion phone app writes these values;
    /// sensible defaults are used if not yet configured.
    /// </summary>
    private static AlarmSettings LoadSettingsFromPreferences()
    {
        return new AlarmSettings
        {
            PingInterval = Preferences.Get("PingInterval", 10),
            VibrationEnabled = Preferences.Get("VibrationEnabled", true),
            VibrationDuration = Preferences.Get("VibrationDuration", 1000),
            // Sound disabled on watch by default (watch speakers are limited)
            SoundEnabled = Preferences.Get("SoundEnabled", false),
            NotificationEnabled = Preferences.Get("NotificationEnabled", true),
        };
    }
}
