using DistanceAlarm.Models;

namespace DistanceAlarm.Services;

public interface IBluetoothService
{
    event EventHandler<BleDevice> DeviceDiscovered;
    event EventHandler<ConnectionState> ConnectionStatusChanged;
    event EventHandler ConnectionLost;
    event EventHandler<int> RssiUpdated;

    Task<bool> IsBluetoothEnabledAsync();
    Task<bool> RequestBluetoothPermissionsAsync();
    Task StartScanningAsync();
    Task StopScanningAsync();
    Task<bool> ConnectToDeviceAsync(BleDevice device);
    Task DisconnectAsync();
    Task StartPingingAsync(int intervalSeconds);
    Task StopPingingAsync();
    Task<bool> AttemptReconnectAsync(int maxAttempts = 5, int initialDelaySeconds = 2);
    ConnectionState GetConnectionState();
    Task<List<BleDevice>> GetPairedDevicesAsync();
}

// Extension interface for configuration (implemented by BluetoothService)
public interface IBluetoothServiceConfiguration
{
    void SetFailedPingThreshold(int threshold);
}