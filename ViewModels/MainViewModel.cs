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
    private readonly ILocationService? _locationService;
    private readonly IBackgroundService? _backgroundService;

    [ObservableProperty]
    private ConnectionState _connectionState;

    [ObservableProperty]
    private AlarmSettings _settings;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = "Not connected";

    [ObservableProperty]
    private int _currentRssi = 0;

    [ObservableProperty]
    private bool _isInSafeZone = false;

    [ObservableProperty]
    private bool _isMonitoringEnabled = false;

    public ObservableCollection<BleDevice> DiscoveredDevices { get; } = new();

    public MainViewModel(IBluetoothService bluetoothService, IAlarmService alarmService, 
        ILocationService? locationService = null, IBackgroundService? backgroundService = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel constructor starting...");

            _bluetoothService = bluetoothService;
            _alarmService = alarmService;
            _locationService = locationService;
            _backgroundService = backgroundService;
            _connectionState = new ConnectionState();
            _settings = new AlarmSettings();

            // Add default safe zone (home) - user MUST configure location before enabling
            _settings.SafeZones.Add(new SafeZone 
            { 
                Name = "Home",
                Latitude = 0,  // Invalid default - must be set by user
                Longitude = 0,
                RadiusMeters = 100,
                IsEnabled = false // Disabled by default - requires location setup
            });

            // Subscribe to service events with error handling
            try
            {
                _bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
                _bluetoothService.ConnectionStatusChanged += OnConnectionStatusChanged;
                _bluetoothService.ConnectionLost += OnConnectionLost;
                _bluetoothService.RssiUpdated += OnRssiUpdated;
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
                
                // Set failed ping threshold from settings using configuration interface
                if (_bluetoothService is IBluetoothServiceConfiguration configService)
                {
                    configService.SetFailedPingThreshold(Settings.FailedPingThreshold);
                }
                
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
            // Check if we're in a safe zone before triggering alarm
            if (await ShouldTriggerAlarmAsync())
            {
                StatusMessage = "Connection lost - triggering alarm";
                await _alarmService.TriggerAlarmAsync(Settings);

                // Attempt automatic reconnection if enabled
                if (Settings.EnableAutoReconnect)
                {
                    StatusMessage = "Connection lost - attempting to reconnect...";
                    _ = Task.Run(async () =>
                    {
                        var reconnected = await _bluetoothService.AttemptReconnectAsync(
                            Settings.ReconnectMaxAttempts,
                            Settings.ReconnectInitialDelaySeconds);

                        if (reconnected)
                        {
                            await _alarmService.StopAlarmAsync();
                            StatusMessage = "Reconnected successfully";
                        }
                        else
                        {
                            StatusMessage = "Reconnection failed - device out of range";
                        }
                    });
                }
            }
            else
            {
                StatusMessage = "Connection lost but in safe zone - no alarm";
                System.Diagnostics.Debug.WriteLine("Device disconnected but currently in safe zone");
            }
        });
    }

    private void OnRssiUpdated(object? sender, int rssi)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            CurrentRssi = rssi;
            
            // Check if signal is too weak (approaching disconnection)
            if (rssi < Settings.RssiThreshold && ConnectionState.Status == ConnectionStatus.Connected)
            {
                StatusMessage = $"Weak signal: {rssi} dBm - device may be out of range";
                System.Diagnostics.Debug.WriteLine($"RSSI below threshold: {rssi} < {Settings.RssiThreshold}");
            }
        });
    }

    /// <summary>
    /// Determines if alarm should be triggered based on safe zones and settings
    /// </summary>
    private async Task<bool> ShouldTriggerAlarmAsync()
    {
        // If safe zones are disabled, always trigger alarm
        if (!Settings.EnableSafeZones || _locationService == null)
            return true;

        try
        {
            // Check if currently in any safe zone
            var inSafeZone = await _locationService.IsInAnySafeZoneAsync(Settings.SafeZones);
            IsInSafeZone = inSafeZone;
            
            // Don't trigger alarm if in safe zone
            return !inSafeZone;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking safe zones: {ex.Message}");
            // On error, trigger alarm to be safe
            return true;
        }
    }

    [RelayCommand]
    private async Task ToggleMonitoringAsync()
    {
        try
        {
            if (_backgroundService == null)
            {
                StatusMessage = "Background monitoring not available on this platform";
                return;
            }

            if (IsMonitoringEnabled)
            {
                await _backgroundService.StopMonitoringAsync();
                IsMonitoringEnabled = false;
                StatusMessage = "Background monitoring stopped";
            }
            else
            {
                // Request battery optimization exemption
                await _backgroundService.RequestBatteryOptimizationExemptionAsync();
                
                await _backgroundService.StartMonitoringAsync();
                IsMonitoringEnabled = true;
                StatusMessage = "Background monitoring started";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to toggle monitoring: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ToggleMonitoring error: {ex}");
        }
    }

    [RelayCommand]
    private async Task SetCurrentLocationAsSafeZoneAsync()
    {
        try
        {
            if (_locationService == null)
            {
                StatusMessage = "Location service not available";
                return;
            }

            var location = await _locationService.GetCurrentLocationAsync();
            if (location.HasValue)
            {
                var homeZone = Settings.SafeZones.FirstOrDefault(z => z.Name == "Home");
                if (homeZone != null)
                {
                    homeZone.Latitude = location.Value.Latitude;
                    homeZone.Longitude = location.Value.Longitude;
                    homeZone.IsEnabled = true;
                    StatusMessage = $"Home location set: {location.Value.Latitude:F6}, {location.Value.Longitude:F6}";
                }
            }
            else
            {
                StatusMessage = "Failed to get current location";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to set location: {ex.Message}";
        }
    }
}