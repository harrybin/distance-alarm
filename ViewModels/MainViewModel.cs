using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistanceAlarm.Models;
using DistanceAlarm.Services;
using System.Collections.ObjectModel;

namespace DistanceAlarm.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBluetoothService _bluetoothService;
    private readonly IAlarmService _alarmService;

    [ObservableProperty]
    private ConnectionState _connectionState;

    [ObservableProperty]
    private AlarmSettings _settings;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = "Not connected";

    public ObservableCollection<BleDevice> DiscoveredDevices { get; } = new();

    public MainViewModel(IBluetoothService bluetoothService, IAlarmService alarmService)
    {
        _bluetoothService = bluetoothService;
        _alarmService = alarmService;
        _connectionState = new ConnectionState();
        _settings = new AlarmSettings();

        // Subscribe to service events
        _bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
        _bluetoothService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _bluetoothService.ConnectionLost += OnConnectionLost;
    }

    [RelayCommand]
    private async Task StartScanAsync()
    {
        try
        {
            if (!await _bluetoothService.IsBluetoothEnabledAsync())
            {
                StatusMessage = "Bluetooth is not enabled";
                return;
            }

            IsScanning = true;
            DiscoveredDevices.Clear();
            StatusMessage = "Scanning for devices...";

            await _bluetoothService.StartScanningAsync();

            // Auto-stop scanning after 30 seconds
            _ = Task.Delay(30000).ContinueWith(async _ => await StopScanAsync());
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task StopScanAsync()
    {
        try
        {
            await _bluetoothService.StopScanningAsync();
            IsScanning = false;
            StatusMessage = DiscoveredDevices.Any() ? $"Found {DiscoveredDevices.Count} devices" : "No devices found";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Stop scan failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ConnectToDeviceAsync(BleDevice device)
    {
        try
        {
            if (IsScanning)
                await StopScanAsync();

            StatusMessage = $"Connecting to {device.DisplayName}...";

            var success = await _bluetoothService.ConnectToDeviceAsync(device);

            if (success)
            {
                StatusMessage = $"Connected to {device.DisplayName}";
                await _bluetoothService.StartPingingAsync(Settings.PingInterval);
            }
            else
            {
                StatusMessage = "Connection failed";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            await _bluetoothService.DisconnectAsync();
            StatusMessage = "Disconnected";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Disconnect error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestAlarmAsync()
    {
        try
        {
            await _alarmService.TriggerAlarmAsync(Settings);
            StatusMessage = "Test alarm triggered";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Test alarm failed: {ex.Message}";
        }
    }

    private void OnDeviceDiscovered(object? sender, BleDevice device)
    {
        // Update on UI thread
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.Id == device.Id);
            if (existingDevice != null)
            {
                // Update existing device with all properties that may have changed
                existingDevice.Name = device.Name;
                existingDevice.RssiValue = device.RssiValue;
                existingDevice.LastSeen = device.LastSeen;
                existingDevice.Device = device.Device;
            }
            else
            {
                // Add new device
                DiscoveredDevices.Add(device);
            }
        });
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionState state)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            ConnectionState = state;
            StatusMessage = state.StatusMessage;
        });
    }

    private async void OnConnectionLost(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.Dispatch(async () =>
        {
            StatusMessage = "Connection lost - triggering alarm";
            await _alarmService.TriggerAlarmAsync(Settings);
        });
    }
}