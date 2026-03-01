namespace DistanceAlarm.Platforms.Android;

/// <summary>
/// Custom MAUI permission class for Android Bluetooth runtime permissions.
/// Android 12+ (API 31+) requires BLUETOOTH_SCAN and BLUETOOTH_CONNECT at runtime.
/// Older versions require ACCESS_FINE_LOCATION for BLE scanning.
/// </summary>
public class BluetoothPermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.S
            ? new (string, bool)[]
            {
                (global::Android.Manifest.Permission.BluetoothScan, true),
                (global::Android.Manifest.Permission.BluetoothConnect, true),
            }
            : new (string, bool)[]
            {
                (global::Android.Manifest.Permission.AccessFineLocation, true),
            };
}
