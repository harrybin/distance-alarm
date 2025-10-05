using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistanceAlarm.Models;
using DistanceAlarm.Services;
using System.Collections.ObjectModel;

namespace DistanceAlarm.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBluetoothService _bluetoothService = null!;
    private readonly IAlarmService _alarmService = null!;

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
        try
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel constructor starting...");

            _bluetoothService = bluetoothService;
            _alarmService = alarmService;
            _connectionState = new ConnectionState();
            _settings = new AlarmSettings();

            // Subscribe to service events with error handling
            try
            {
                _bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
                _bluetoothService.ConnectionStatusChanged += OnConnectionStatusChanged;
                _bluetoothService.ConnectionLost += OnConnectionLost;
                System.Diagnostics.Debug.WriteLine("MainViewModel event subscriptions completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainViewModel event subscription failed: {ex.Message}");
                // Continue without throwing - events are not critical for basic functionality
            }

            System.Diagnostics.Debug.WriteLine("MainViewModel constructor completed successfully");
            
            // Load connected devices automatically on startup
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000); // Small delay to allow services to initialize
                    await LoadConnectedDevicesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-load connected devices failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel constructor failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

            // Initialize with safe defaults to prevent total failure
            _connectionState = new ConnectionState();
            _settings = new AlarmSettings();
            StatusMessage = "Initialization error - some features may not work";
        }
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

            // Request permissions before scanning
            if (!await _bluetoothService.RequestBluetoothPermissionsAsync())
            {
                StatusMessage = "Bluetooth permissions denied. Please grant location permissions in app settings.";
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
            System.Diagnostics.Debug.WriteLine($"StartScanAsync error: {ex}");
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

    [RelayCommand]
    private async Task LoadConnectedDevicesAsync()
    {
        try
        {
            var connectedDevices = await _bluetoothService.GetPairedDevicesAsync();
            
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                foreach (var device in connectedDevices)
                {
                    var existingDevice = DiscoveredDevices.FirstOrDefault(d => d.Id == device.Id);
                    if (existingDevice == null)
                    {
                        DiscoveredDevices.Add(device);
                    }
                    else
                    {
                        // Update existing device as connected
                        existingDevice.IsConnected = true;
                        existingDevice.LastSeen = device.LastSeen;
                    }
                }
                
                if (connectedDevices.Any())
                {
                    StatusMessage = $"Loaded {connectedDevices.Count} connected device(s)";
                    
                    // If a connection was recognized, start pinging automatically
                    var connectionState = _bluetoothService.GetConnectionState();
                    if (connectionState.Status == ConnectionStatus.Connected && connectionState.ConnectedDevice != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _bluetoothService.StartPingingAsync(Settings.PingInterval);
                                System.Diagnostics.Debug.WriteLine("Started pinging for existing connection");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to start pinging for existing connection: {ex.Message}");
                            }
                        });
                    }
                }
                else
                {
                    StatusMessage = "No connected devices found";
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load connected devices: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"LoadConnectedDevicesAsync error: {ex}");
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