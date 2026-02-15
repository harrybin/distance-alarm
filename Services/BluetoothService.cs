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
            // Request location permissions (required for BLE scanning on Android)
            var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (locationStatus != PermissionStatus.Granted)
            {
                System.Diagnostics.Debug.WriteLine("Location permission denied - BLE scanning may not work");
                return false;
            }

            // On Android 12+, we also need Bluetooth permissions
#if ANDROID
            if (DeviceInfo.Version.Major >= 12)
            {
                // For newer Android versions, check Bluetooth permissions through MAUI Essentials
                // Note: MAUI Essentials handles the platform-specific permission requests
                System.Diagnostics.Debug.WriteLine("Android 12+ detected - Bluetooth permissions handled by system");
            }
#endif

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
        // Use enhanced scanner on Android for better WearOS device discovery
        if (_enhancedScanner != null)
        {
            try
            {
                _enhancedScanner.StartScan();
                System.Diagnostics.Debug.WriteLine("Using EnhancedBleScanner for device discovery (optimized for WearOS)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner failed to start, falling back to Plugin.BLE: {ex.Message}");
                // Fallback to Plugin.BLE if enhanced scanner fails
                _scanCancellationToken = new CancellationTokenSource();
                await _adapter.StartScanningForDevicesAsync(cancellationToken: _scanCancellationToken.Token);
            }
        }
        else
        {
            // Fallback to Plugin.BLE
            _scanCancellationToken = new CancellationTokenSource();
            await _adapter.StartScanningForDevicesAsync(cancellationToken: _scanCancellationToken.Token);
        }
#else
        _scanCancellationToken = new CancellationTokenSource();
        await _adapter.StartScanningForDevicesAsync(cancellationToken: _scanCancellationToken.Token);
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

            return true;
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus(ConnectionStatus.Failed, $"Connection failed: {ex.Message}");
            return false;
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
            var pluginDevice = _adapter.DiscoveredDevices.FirstOrDefault(d => 
                d.Id.ToString().Equals(deviceAddress, StringComparison.OrdinalIgnoreCase));

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
                    RssiValue = 0, // RSSI not directly available from BluetoothDevice, will be updated via Plugin.BLE
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