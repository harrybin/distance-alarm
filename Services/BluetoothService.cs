using DistanceAlarm.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
#if ANDROID
using DistanceAlarm.Platforms.Android;
#endif

namespace DistanceAlarm.Services;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This service is designed for Android platform only")]
public class BluetoothService : IBluetoothService, IBluetoothServiceConfiguration, IDisposable
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private readonly ConnectionState _connectionState;
    private IDevice? _connectedDevice;
    private Timer? _pingTimer;
    private CancellationTokenSource? _scanCancellationToken;
    private int _reconnectAttempts = 0;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _isReconnecting = false;
    private bool _disposed = false;
    private int _failedPingThreshold = 2; // Default threshold
#if ANDROID
    private EnhancedBleScanner? _enhancedScanner; // Android-specific enhanced scanner for WearOS discovery
#endif

    public event EventHandler<BleDevice>? DeviceDiscovered;
    public event EventHandler<ConnectionState>? ConnectionStatusChanged;
    public event EventHandler? ConnectionLost;
    public event EventHandler<int>? RssiUpdated; // New event for RSSI monitoring

    public BluetoothService()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = _ble.Adapter;
        _connectionState = new ConnectionState();

        // Subscribe to adapter events
        _adapter.DeviceAdvertised += OnDeviceAdvertised;
        _adapter.DeviceConnected += OnDeviceConnected;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;

#if ANDROID
        // Initialize enhanced BLE scanner for better WearOS device discovery
        try
        {
            _enhancedScanner = new EnhancedBleScanner();
            _enhancedScanner.DeviceDiscovered += OnEnhancedScannerDeviceDiscovered;
            System.Diagnostics.Debug.WriteLine("EnhancedBleScanner initialized for WearOS support");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner initialization failed: {ex.Message}");
            _enhancedScanner = null;
        }
#endif
    }

    public async Task<bool> IsBluetoothEnabledAsync()
    {
        return _ble.State == BluetoothState.On;
    }

    public async Task<bool> RequestBluetoothPermissionsAsync()
    {
        try
        {
#if ANDROID
            // Android 12+ (API 31+) requires BLUETOOTH_SCAN and BLUETOOTH_CONNECT at runtime.
            // Location permission is not required for BLE on Android 12+ when using these permissions.
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.S)
            {
                var btStatus = await Permissions.RequestAsync<Platforms.Android.BluetoothPermissions>();
                if (btStatus != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("Bluetooth permissions denied on Android 12+");
                    return false;
                }
                return true;
            }
#endif

            // Android < 12: location permission is required for BLE scanning
            var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                System.Diagnostics.Debug.WriteLine("Location permission denied - BLE scanning may not work");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission request failed: {ex.Message}");
            return false;
        }
    }

    public async Task StartScanningAsync()
    {
        if (_ble.State != BluetoothState.On)
        {
            throw new InvalidOperationException("Bluetooth is not enabled");
        }

#if ANDROID
        // Start Plugin.BLE scan to obtain IDevice objects required for ConnectToDeviceAsync
        _scanCancellationToken = new CancellationTokenSource();
        var pluginBleScanTask = _adapter.StartScanningForDevicesAsync(cancellationToken: _scanCancellationToken.Token);

        // Also run the enhanced scanner for better WearOS device discovery
        if (_enhancedScanner != null)
        {
            try
            {
                _enhancedScanner.StartScan();
                System.Diagnostics.Debug.WriteLine("Using EnhancedBleScanner alongside Plugin.BLE for WearOS device discovery");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner failed to start: {ex.Message}");
            }
        }

        try
        {
            await pluginBleScanTask;
        }
        catch (OperationCanceledException)
        {
            // Normal scan stop via cancellation token — not an error
            System.Diagnostics.Debug.WriteLine("Plugin.BLE scan stopped (cancellation)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Plugin.BLE scan error: {ex.Message}");
        }
#else
        _scanCancellationToken = new CancellationTokenSource();
        try
        {
            await _adapter.StartScanningForDevicesAsync(cancellationToken: _scanCancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal scan stop via cancellation token — not an error
        }
#endif
    }

    public async Task StopScanningAsync()
    {
#if ANDROID
        // Stop enhanced scanner on Android
        if (_enhancedScanner != null && _enhancedScanner.IsScanning)
        {
            _enhancedScanner.StopScan();
        }
#endif
        
        _scanCancellationToken?.Cancel();
        await _adapter.StopScanningForDevicesAsync();
    }

    public async Task<bool> ConnectToDeviceAsync(BleDevice device)
    {
        try
        {
            if (device.Device == null)
                return false;

            UpdateConnectionStatus(ConnectionStatus.Connecting, "Connecting to device...");

            await _adapter.ConnectToDeviceAsync(device.Device);
            _connectedDevice = device.Device;

            UpdateConnectionStatus(ConnectionStatus.Connected, $"Connected to {device.DisplayName}");
            _connectionState.ConnectedDevice = device;

            // Persist the device ID so the phone can auto-reconnect on next startup
            Preferences.Default.Set(AppConstants.SavedDeviceIdKey,   device.Device.Id.ToString());
            Preferences.Default.Set(AppConstants.SavedDeviceNameKey, device.DisplayName);

            System.Diagnostics.Debug.WriteLine(
                $"BluetoothService: Saved paired device – {device.DisplayName} ({device.Device.Id})");

            return true;
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus(ConnectionStatus.Failed, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Connects directly to the previously-paired watch using the device ID that was
    /// persisted to <see cref="Preferences"/> after the first manual pairing.
    /// </summary>
    public async Task<bool> ConnectToSavedDeviceAsync()
    {
        var savedId = Preferences.Default.Get(AppConstants.SavedDeviceIdKey, string.Empty);
        if (string.IsNullOrEmpty(savedId))
        {
            System.Diagnostics.Debug.WriteLine(
                "BluetoothService.ConnectToSavedDeviceAsync: no saved device ID");
            return false;
        }

        if (!Guid.TryParse(savedId, out var deviceGuid))
        {
            System.Diagnostics.Debug.WriteLine(
                $"BluetoothService.ConnectToSavedDeviceAsync: saved ID '{savedId}' is not a valid GUID");
            return false;
        }

        try
        {
            var savedName = Preferences.Default.Get(AppConstants.SavedDeviceNameKey, "Watch");
            UpdateConnectionStatus(ConnectionStatus.Connecting,
                $"Connecting to {savedName}...");

            var rawDevice = await _adapter.ConnectToKnownDeviceAsync(
                deviceGuid,
                new Plugin.BLE.Abstractions.ConnectParameters(false, false));

            if (rawDevice == null)
            {
                UpdateConnectionStatus(ConnectionStatus.Disconnected, "Auto-connect failed");
                return false;
            }

            _connectedDevice = rawDevice;

            var bleDevice = new BleDevice
            {
                Id         = rawDevice.Id.ToString(),
                Name       = !string.IsNullOrWhiteSpace(rawDevice.Name) ? rawDevice.Name : savedName,
                MacAddress = rawDevice.Id.ToString(),
                RssiValue  = rawDevice.Rssi,
                Device     = rawDevice,
                LastSeen   = DateTime.Now,
                IsConnected = true
            };

            _connectionState.ConnectedDevice = bleDevice;
            UpdateConnectionStatus(ConnectionStatus.Connected,
                $"Connected to {bleDevice.DisplayName}");

            System.Diagnostics.Debug.WriteLine(
                $"BluetoothService: Auto-connected to saved device – {bleDevice.DisplayName}");

            return true;
        }
        catch (OperationCanceledException)
        {
            UpdateConnectionStatus(ConnectionStatus.Disconnected, "Auto-connect cancelled");
            return false;
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus(ConnectionStatus.Disconnected,
                $"Auto-connect failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(
                $"BluetoothService.ConnectToSavedDeviceAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Pushes the current alarm settings to the connected watch by writing to the
    /// Settings GATT characteristic.  No-op if the watch GATT service is not found.
    /// Retries service discovery up to 5 times with incremental back-off instead of
    /// a fixed sleep, to avoid relying on an arbitrary delay after connection.
    /// </summary>
    public async Task PushSettingsToDeviceAsync(AlarmSettings settings)
    {
        if (_connectedDevice == null)
            return;

        try
        {
            // Poll for the service with exponential back-off: Plugin.BLE triggers GATT
            // service discovery when GetServiceAsync is called; the first call may return
            // null if discovery has not completed yet.
            Plugin.BLE.Abstractions.Contracts.IService? service = null;
            for (int attempt = 0; attempt < 5 && service == null; attempt++)
            {
                if (attempt > 0)
                    await Task.Delay(500 * attempt);

                service = await _connectedDevice.GetServiceAsync(
                    Guid.Parse(AppConstants.WatchServiceUuid));
            }

            if (service == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "BluetoothService.PushSettingsToDeviceAsync: watch service not found after retries");
                return;
            }

            var characteristic = await service.GetCharacteristicAsync(
                Guid.Parse(AppConstants.SettingsCharacteristicUuid));

            if (characteristic == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "BluetoothService.PushSettingsToDeviceAsync: settings characteristic not found");
                return;
            }

            // Serialize settings as UTF-8 JSON
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                pingInterval        = settings.PingInterval,
                vibrationEnabled    = settings.VibrationEnabled,
                vibrationDuration   = settings.VibrationDuration,
                soundEnabled        = settings.SoundEnabled,
                rssiThreshold       = settings.RssiThreshold,
                failedPingThreshold = settings.FailedPingThreshold
            });

            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await characteristic.WriteAsync(bytes);

            System.Diagnostics.Debug.WriteLine(
                $"BluetoothService: Settings pushed to watch – {json}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"BluetoothService.PushSettingsToDeviceAsync error: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connectedDevice != null)
        {
            await StopPingingAsync();
            await _adapter.DisconnectDeviceAsync(_connectedDevice);
            _connectedDevice = null;
            _connectionState.ConnectedDevice = null;
            UpdateConnectionStatus(ConnectionStatus.Disconnected, "Disconnected");
        }
    }

    public async Task StartPingingAsync(int intervalSeconds)
    {
        await StopPingingAsync();

        _pingTimer = new Timer(async _ => await PingDevice(), null,
            TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
    }

    public async Task StopPingingAsync()
    {
        _pingTimer?.Dispose();
        _pingTimer = null;
    }

    /// <summary>
    /// Sets the failed ping threshold for connection loss detection
    /// </summary>
    public void SetFailedPingThreshold(int threshold)
    {
        _failedPingThreshold = Math.Max(1, threshold); // Ensure at least 1
    }

    private async Task PingDevice()
    {
        if (_connectedDevice == null)
            return;

        try
        {
            // Use lock to prevent concurrent access during ping
            await _connectionLock.WaitAsync();

            try
            {
                // Check connection state first - more lightweight than GetServicesAsync
                if (_connectedDevice.State != Plugin.BLE.Abstractions.DeviceState.Connected)
                {
                    throw new Exception("Device not connected");
                }

                // Update RSSI to monitor signal strength
                await _connectedDevice.UpdateRssiAsync();
                var currentRssi = _connectedDevice.Rssi;
                
                _connectionState.LastPingTime = DateTime.Now;
                _connectionState.FailedPingCount = 0;
                _reconnectAttempts = 0; // Reset reconnect attempts on successful ping

                // Notify RSSI update for distance monitoring
                RssiUpdated?.Invoke(this, currentRssi);
                
                System.Diagnostics.Debug.WriteLine($"Ping successful - RSSI: {currentRssi} dBm");
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ping failed: {ex.Message}");
            _connectionState.FailedPingCount++;

            // Connection is likely lost - trigger appropriate handling
            if (_connectionState.FailedPingCount >= _failedPingThreshold)
            {
                await HandleConnectionLossAsync();
            }
        }
    }

    /// <summary>
    /// Handles connection loss with optional automatic reconnection
    /// </summary>
    private async Task HandleConnectionLossAsync()
    {
        if (_isReconnecting)
            return; // Already handling reconnection

        UpdateConnectionStatus(ConnectionStatus.Disconnected, "Connection lost");
        ConnectionLost?.Invoke(this, EventArgs.Empty);
        
        System.Diagnostics.Debug.WriteLine($"Connection lost - failed pings: {_connectionState.FailedPingCount}");
    }

    /// <summary>
    /// Attempts to reconnect to the device with exponential backoff
    /// </summary>
    public async Task<bool> AttemptReconnectAsync(int maxAttempts = 5, int initialDelaySeconds = 2)
    {
        if (_connectedDevice == null || _connectionState.ConnectedDevice == null)
            return false;

        _isReconnecting = true;
        UpdateConnectionStatus(ConnectionStatus.Reconnecting, "Attempting to reconnect...");

        try
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                _reconnectAttempts = attempt + 1;
                
                // Exponential backoff: 2s, 4s, 8s, 16s, 32s
                var delay = initialDelaySeconds * (int)Math.Pow(2, attempt);
                System.Diagnostics.Debug.WriteLine($"Reconnect attempt {_reconnectAttempts}/{maxAttempts} - waiting {delay}s");
                
                UpdateConnectionStatus(ConnectionStatus.Reconnecting, 
                    $"Reconnecting... (Attempt {_reconnectAttempts}/{maxAttempts})");

                await Task.Delay(TimeSpan.FromSeconds(delay));

                try
                {
                    // Try to reconnect
                    await _adapter.ConnectToDeviceAsync(_connectedDevice);
                    
                    if (_connectedDevice.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                    {
                        _connectionState.FailedPingCount = 0;
                        _reconnectAttempts = 0;
                        UpdateConnectionStatus(ConnectionStatus.Connected, 
                            $"Reconnected to {_connectionState.ConnectedDevice.DisplayName}");
                        
                        System.Diagnostics.Debug.WriteLine("Reconnection successful");
                        _isReconnecting = false;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Reconnect attempt {_reconnectAttempts} failed: {ex.Message}");
                }
            }

            // All reconnection attempts failed
            UpdateConnectionStatus(ConnectionStatus.Failed, 
                $"Reconnection failed after {maxAttempts} attempts");
            
            System.Diagnostics.Debug.WriteLine("All reconnection attempts failed");
            return false;
        }
        finally
        {
            _isReconnecting = false;
        }
    }

    public ConnectionState GetConnectionState()
    {
        return _connectionState;
    }

    public async Task<List<BleDevice>> GetPairedDevicesAsync()
    {
        var pairedDevices = new List<BleDevice>();
        
        try
        {
            // Get currently connected devices from the BLE adapter
            var connectedDevices = _adapter.ConnectedDevices;
            
            foreach (var device in connectedDevices)
            {
                var bleDevice = new BleDevice
                {
                    Id = device.Id.ToString(),
                    Name = !string.IsNullOrWhiteSpace(device.Name) ? device.Name.Trim() : "Unknown",
                    MacAddress = device.Id.ToString(),
                    RssiValue = device.Rssi,
                    Device = device,
                    LastSeen = DateTime.Now,
                    IsConnected = true
                };
                
                pairedDevices.Add(bleDevice);
                System.Diagnostics.Debug.WriteLine($"Found connected device: {bleDevice.DisplayName} ({bleDevice.Id})");
                
                // If no device is currently tracked as connected, set the first found device as connected
                if (_connectedDevice == null && pairedDevices.Count == 1)
                {
                    _connectedDevice = device;
                    _connectionState.ConnectedDevice = bleDevice;
                    UpdateConnectionStatus(ConnectionStatus.Connected, $"Recognized existing connection to {bleDevice.DisplayName}");
                    System.Diagnostics.Debug.WriteLine($"Set existing connection state for device: {bleDevice.DisplayName}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting paired/connected devices: {ex.Message}");
        }
        
        return pairedDevices;
    }

#if ANDROID
    /// <summary>
    /// Handler for devices discovered by the enhanced Android BLE scanner
    /// This provides better WearOS device discovery
    /// </summary>
    private void OnEnhancedScannerDeviceDiscovered(object? sender, global::Android.Bluetooth.BluetoothDevice androidDevice)
    {
        if (androidDevice == null)
            return;

        try
        {
            // Extract device information from Android BluetoothDevice
            string deviceName = androidDevice.Name ?? "Unknown";
            string deviceAddress = androidDevice.Address ?? "Unknown";

            // Try to get the corresponding Plugin.BLE device from the adapter
            // The adapter maintains discovered devices internally
            // Match by address - normalize both for comparison
            var normalizedAddress = deviceAddress.Replace(":", "").Replace("-", "").ToUpperInvariant();
            var pluginDevice = _adapter.DiscoveredDevices.FirstOrDefault(d => 
            {
                var deviceId = d.Id.ToString().Replace(":", "").Replace("-", "").ToUpperInvariant();
                return deviceId.Equals(normalizedAddress, StringComparison.OrdinalIgnoreCase);
            });

            // If device is not yet in Plugin.BLE's discovered devices, trigger the standard scan
            // This will cause Plugin.BLE to discover it and we'll get it via OnDeviceAdvertised
            if (pluginDevice == null)
            {
                // Create a minimal BLE device entry with the Android device info
                var bleDevice = new BleDevice
                {
                    Id = deviceAddress,
                    Name = !string.IsNullOrWhiteSpace(deviceName) ? deviceName.Trim() : "Unknown",
                    MacAddress = deviceAddress,
                    RssiValue = -100, // Temporary value - will be updated by Plugin.BLE when it discovers the device
                    Device = null, // Will be set when Plugin.BLE discovers it
                    LastSeen = DateTime.Now
                };

                // Log discovery for debugging
                System.Diagnostics.Debug.WriteLine(
                    $"EnhancedScanner: Device discovered - Name: {deviceName}, Address: {deviceAddress}");

                // Notify listeners - this helps populate the UI immediately
                DeviceDiscovered?.Invoke(this, bleDevice);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnEnhancedScannerDeviceDiscovered error: {ex.Message}");
        }
    }
#endif

    private void OnDeviceAdvertised(object? sender, DeviceEventArgs e)
    {
        // Extract the best possible name from the device
        string deviceName = "Unknown";

        if (!string.IsNullOrWhiteSpace(e.Device.Name))
        {
            deviceName = e.Device.Name.Trim();
        }

        var bleDevice = new BleDevice
        {
            Id = e.Device.Id.ToString(),
            Name = deviceName,
            MacAddress = e.Device.Id.ToString(), // BLE device ID is often the MAC address
            RssiValue = e.Device.Rssi,
            Device = e.Device,
            LastSeen = DateTime.Now
        };

        // Log device discovery for debugging
        System.Diagnostics.Debug.WriteLine($"Device discovered: Name='{deviceName}', ID={e.Device.Id}, RSSI={e.Device.Rssi}");

        DeviceDiscovered?.Invoke(this, bleDevice);
    }

    private void OnDeviceConnected(object? sender, DeviceEventArgs e)
    {
        if (e.Device.Id == _connectedDevice?.Id)
        {
            UpdateConnectionStatus(ConnectionStatus.Connected, $"Connected to {e.Device.Name}");
        }
    }

    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        if (e.Device.Id == _connectedDevice?.Id)
        {
            UpdateConnectionStatus(ConnectionStatus.Disconnected, "Device disconnected");
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateConnectionStatus(ConnectionStatus status, string message)
    {
        _connectionState.Status = status;
        _connectionState.StatusMessage = message;
        ConnectionStatusChanged?.Invoke(this, _connectionState);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unsubscribe from events
        _adapter.DeviceAdvertised -= OnDeviceAdvertised;
        _adapter.DeviceConnected -= OnDeviceConnected;
        _adapter.DeviceDisconnected -= OnDeviceDisconnected;

#if ANDROID
        // Cleanup enhanced scanner
        if (_enhancedScanner != null)
        {
            _enhancedScanner.DeviceDiscovered -= OnEnhancedScannerDeviceDiscovered;
            _enhancedScanner.Dispose();
            _enhancedScanner = null;
        }
#endif

        // Cleanup resources
        _pingTimer?.Dispose();
        _scanCancellationToken?.Dispose();
        _connectionLock?.Dispose();

        System.Diagnostics.Debug.WriteLine("BluetoothService disposed");
    }
}