using DistanceAlarm.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DistanceAlarm.Services;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This service is designed for Android platform only")]
public class BluetoothService : IBluetoothService
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private readonly ConnectionState _connectionState;
    private IDevice? _connectedDevice;
    private Timer? _pingTimer;
    private CancellationTokenSource? _scanCancellationToken;

    public event EventHandler<BleDevice>? DeviceDiscovered;
    public event EventHandler<ConnectionState>? ConnectionStatusChanged;
    public event EventHandler? ConnectionLost;

    public BluetoothService()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = _ble.Adapter;
        _connectionState = new ConnectionState();

        // Subscribe to adapter events
        _adapter.DeviceAdvertised += OnDeviceAdvertised;
        _adapter.DeviceConnected += OnDeviceConnected;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
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

        _scanCancellationToken = new CancellationTokenSource();
        await _adapter.StartScanningForDevicesAsync(cancellationToken: _scanCancellationToken.Token);
    }

    public async Task StopScanningAsync()
    {
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

    private async Task PingDevice()
    {
        if (_connectedDevice == null)
            return;

        try
        {
            // Try to read device information or send a ping
            var services = await _connectedDevice.GetServicesAsync();
            _connectionState.LastPingTime = DateTime.Now;
            _connectionState.FailedPingCount = 0;
        }
        catch (Exception)
        {
            _connectionState.FailedPingCount++;

            // If multiple pings fail, consider connection lost
            if (_connectionState.FailedPingCount >= 3)
            {
                UpdateConnectionStatus(ConnectionStatus.Disconnected, "Connection lost");
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
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
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting paired/connected devices: {ex.Message}");
        }
        
        return pairedDevices;
    }

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
}