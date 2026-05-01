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

    /// <summary>
    /// Attempts to connect directly to the previously-paired watch using the
    /// device ID saved in <see cref="AppConstants.SavedDeviceIdKey"/>.
    /// Returns <c>false</c> when no device has been saved or connection fails.
    /// </summary>
    Task<bool> ConnectToSavedDeviceAsync();

    /// <summary>
    /// Pushes the current <paramref name="settings"/> to the watch by writing
    /// to the Settings GATT characteristic.  Must be called after a successful
    /// <see cref="ConnectToDeviceAsync"/> call.
    /// </summary>
    Task PushSettingsToDeviceAsync(AlarmSettings settings);
}

// Extension interface for configuration (implemented by BluetoothService)
public interface IBluetoothServiceConfiguration
{
    void SetFailedPingThreshold(int threshold);
}