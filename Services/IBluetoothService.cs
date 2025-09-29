using DistanceAlarm.Models;

namespace DistanceAlarm.Services;

public interface IBluetoothService
{
    event EventHandler<BleDevice> DeviceDiscovered;
    event EventHandler<ConnectionState> ConnectionStatusChanged;
    event EventHandler ConnectionLost;

    Task<bool> IsBluetoothEnabledAsync();
    Task<bool> RequestBluetoothPermissionsAsync();
    Task StartScanningAsync();
    Task StopScanningAsync();
    Task<bool> ConnectToDeviceAsync(BleDevice device);
    Task DisconnectAsync();
    Task StartPingingAsync(int intervalSeconds);
    Task StopPingingAsync();
    ConnectionState GetConnectionState();
    Task<List<BleDevice>> GetPairedDevicesAsync();
}