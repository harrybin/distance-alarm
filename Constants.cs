namespace DistanceAlarm;

/// <summary>
/// Shared constants for BLE communication between the companion phone app and Wear OS watch app.
/// </summary>
public static class AppConstants
{
    // Custom BLE GATT service UUID advertised by the Wear OS watch app.
    // The phone scans for this UUID to identify Distance Alarm watches automatically.
    public const string WatchServiceUuid = "a1b2c3d4-1234-5678-abcd-ef0123456789";

    // GATT characteristic the phone writes to push AlarmSettings to the watch.
    public const string SettingsCharacteristicUuid = "a1b2c3d5-1234-5678-abcd-ef0123456789";

    // Preferences keys used by BOTH the phone and watch apps.
    // On the phone: saved when the user first pairs with a watch.
    // On the watch: written by the GATT server when the phone pushes settings.
    public const string SavedDeviceIdKey   = "SavedDeviceId";
    public const string SavedDeviceNameKey = "SavedDeviceName";
}
