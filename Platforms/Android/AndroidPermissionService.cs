using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using DistanceAlarm.Services;
using Microsoft.Maui.Authentication;

namespace DistanceAlarm.Platforms.Android;

public class AndroidPermissionService : IPermissionService
{
    private readonly MainActivity _activity;

    public AndroidPermissionService()
    {
        _activity = Platform.CurrentActivity as MainActivity
            ?? throw new InvalidOperationException("Current activity is not MainActivity");
    }

    public async Task<bool> RequestBluetoothPermissionsAsync()
    {
        var bluetoothPermissions = new[]
        {
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothAdmin
        };

        // For Android 12+ (API 31+), we need additional permissions
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.S)
        {
            bluetoothPermissions = bluetoothPermissions.Concat(new[]
            {
                Manifest.Permission.BluetoothConnect,
                Manifest.Permission.BluetoothScan,
                Manifest.Permission.BluetoothAdvertise
            }).ToArray();
        }

        return await RequestPermissionsAsync(bluetoothPermissions);
    }

    public async Task<bool> CheckBluetoothPermissionsAsync()
    {
        var bluetoothPermissions = new[]
        {
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothAdmin
        };

        // For Android 12+ (API 31+), we need additional permissions
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.S)
        {
            bluetoothPermissions = bluetoothPermissions.Concat(new[]
            {
                Manifest.Permission.BluetoothConnect,
                Manifest.Permission.BluetoothScan
            }).ToArray();
        }

        return CheckPermissions(bluetoothPermissions);
    }

    public async Task<bool> RequestLocationPermissionsAsync()
    {
        var locationPermissions = new[]
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        return await RequestPermissionsAsync(locationPermissions);
    }

    public async Task<bool> CheckLocationPermissionsAsync()
    {
        var locationPermissions = new[]
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        return CheckPermissions(locationPermissions);
    }

    public async Task<bool> RequestNotificationPermissionsAsync()
    {
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
        {
            return await RequestPermissionsAsync(new[] { Manifest.Permission.PostNotifications });
        }

        // Notifications don't require explicit permission on older Android versions
        return true;
    }

    public async Task<bool> CheckNotificationPermissionsAsync()
    {
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
        {
            return CheckPermissions(new[] { Manifest.Permission.PostNotifications });
        }

        // Notifications don't require explicit permission on older Android versions
        return true;
    }

    private async Task<bool> RequestPermissionsAsync(string[] permissions)
    {
        try
        {
            var permissionsToRequest = permissions
                .Where(p => ContextCompat.CheckSelfPermission(_activity, p) != global::Android.Content.PM.Permission.Granted)
                .ToArray();

            if (permissionsToRequest.Length == 0)
            {
                return true; // All permissions already granted
            }

            var tcs = new TaskCompletionSource<bool>();
            var requestCode = new Random().Next(1000, 9999);

            // Create a permission callback
            var callback = new PermissionCallback(tcs);

            // Request permissions
            ActivityCompat.RequestPermissions(_activity, permissionsToRequest, requestCode);

            // Wait for the result (timeout after 30 seconds)
            using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cancellationToken.Token.Register(() => tcs.TrySetResult(false));

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Permission request failed: {ex.Message}");
            return false;
        }
    }

    private bool CheckPermissions(string[] permissions)
    {
        return permissions.All(p =>
            ContextCompat.CheckSelfPermission(_activity, p) == global::Android.Content.PM.Permission.Granted);
    }

    private class PermissionCallback
    {
        private readonly TaskCompletionSource<bool> _tcs;

        public PermissionCallback(TaskCompletionSource<bool> tcs)
        {
            _tcs = tcs;
        }

        public void OnPermissionsResult(bool allGranted)
        {
            _tcs.TrySetResult(allGranted);
        }
    }
}