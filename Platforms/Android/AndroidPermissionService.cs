using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using DistanceAlarm.Services;

namespace DistanceAlarm.Platforms.Android;

public class AndroidPermissionService : IPermissionService
{
    private MainActivity? GetCurrentActivity()
    {
        return Platform.CurrentActivity as MainActivity;
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
            var activity = GetCurrentActivity();
            if (activity == null)
            {
                System.Diagnostics.Debug.WriteLine("No current activity available for permission request");
                return false;
            }

            var permissionsToRequest = permissions
                .Where(p => ContextCompat.CheckSelfPermission(activity, p) != global::Android.Content.PM.Permission.Granted)
                .ToArray();

            if (permissionsToRequest.Length == 0)
            {
                return true; // All permissions already granted
            }

            // Request permissions and register a callback. Results are dispatched back via
            // MainActivity.OnRequestPermissionsResult → PermissionRequestCallback.OnResult.
            var tcs = new TaskCompletionSource<bool>();
            var requestCode = PermissionRequestCallback.NextRequestCode();
            PermissionRequestCallback.Register(requestCode, tcs, permissionsToRequest, activity);

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
        var activity = GetCurrentActivity();
        if (activity == null)
        {
            System.Diagnostics.Debug.WriteLine("No current activity available for permission check");
            return false;
        }

        return permissions.All(p =>
            ContextCompat.CheckSelfPermission(activity, p) == global::Android.Content.PM.Permission.Granted);
    }

    /// <summary>
    /// Manages pending permission request callbacks keyed by request code.
    /// MainActivity.OnRequestPermissionsResult dispatches results here.
    /// </summary>
    internal static class PermissionRequestCallback
    {
        private static int _nextRequestCode = 1000;
        private static readonly Dictionary<int, TaskCompletionSource<bool>> _pending = new();

        internal static void Register(int requestCode, TaskCompletionSource<bool> tcs,
            string[] permissions, global::Android.App.Activity activity)
        {
            lock (_pending)
            {
                _pending[requestCode] = tcs;
            }
            ActivityCompat.RequestPermissions(activity, permissions, requestCode);
        }

        internal static int NextRequestCode() =>
            Interlocked.Increment(ref _nextRequestCode);

        internal static void OnResult(int requestCode, global::Android.Content.PM.Permission[] grantResults)
        {
            TaskCompletionSource<bool>? tcs;
            lock (_pending)
            {
                if (!_pending.TryGetValue(requestCode, out tcs))
                    return;
                _pending.Remove(requestCode);
            }
            var allGranted = grantResults.Length > 0 &&
                             grantResults.All(r => r == global::Android.Content.PM.Permission.Granted);
            tcs.TrySetResult(allGranted);
        }
    }
}