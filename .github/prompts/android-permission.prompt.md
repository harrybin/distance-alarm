---
mode: agent
description: Add a new Android runtime permission or extend permission handling in the app
---

## Context

Permission handling is split across:
- `Services/IPermissionService.cs` – cross-platform interface
- `Platforms/Android/AndroidPermissionService.cs` – Android implementation using `PermissionRequestCallback`
- `Platforms/Android/BluetoothPermissions.cs` – custom MAUI `BasePlatformPermission` for BLE
- `Platforms/Android/MainActivity.cs` – dispatches `OnRequestPermissionsResult` to `PermissionRequestCallback`
- `Platforms/Android/AndroidManifest.xml` – declares all permissions

## PermissionRequestCallback pattern

Results from `ActivityCompat.RequestPermissions` flow via `MainActivity.OnRequestPermissionsResult` into a static `PermissionRequestCallback` keyed by `requestCode` (thread-safe `Interlocked` counter).

```csharp
// In AndroidPermissionService
var tcs = new TaskCompletionSource<PermissionStatus>();
var requestCode = Interlocked.Increment(ref _nextRequestCode);
PermissionRequestCallback.Register(requestCode, (granted) =>
    tcs.TrySetResult(granted ? PermissionStatus.Granted : PermissionStatus.Denied));
ActivityCompat.RequestPermissions(activity, new[] { Manifest.Permission.MyPermission }, requestCode);
return await tcs.Task;
```

## BLE permission branching (Android 12+ vs older)

```csharp
if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // API 31+
{
    // Use custom BluetoothPermissions class
    var status = await Permissions.RequestAsync<BluetoothPermissions>();
}
else
{
    // Use location permission
    var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
}
```

## Custom BasePlatformPermission template

```csharp
// Platforms/Android/MyCustomPermissions.cs
public class MyCustomPermissions : BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[]
        {
            (Manifest.Permission.MyPermission, true),
            // add more as needed
        };
}
```

## AndroidManifest.xml entry

Always declare permissions in `Platforms/Android/AndroidManifest.xml`:

```xml
<!-- Normal permissions (no runtime request needed) -->
<uses-permission android:name="android.permission.MY_NORMAL_PERMISSION" />

<!-- Dangerous permissions (require runtime request) -->
<uses-permission android:name="android.permission.MY_DANGEROUS_PERMISSION" />

<!-- Android version restriction -->
<uses-permission android:name="android.permission.MY_PERMISSION"
    android:maxSdkVersion="30" />
```

## Rules

1. Always declare the permission in `AndroidManifest.xml` **and** request it at runtime if it is a "dangerous" permission.
2. Use `BasePlatformPermission` for grouped permissions (e.g., BLE needs SCAN + CONNECT together).
3. Never request permissions in a ViewModel; delegate to `IPermissionService`.
4. Check `PermissionStatus.Granted` before proceeding; show a user-friendly message on denial.
5. For Android 12+ BLE: use `BluetoothPermissions` (not `LocationWhenInUse`).
6. Background location (`ACCESS_BACKGROUND_LOCATION`) requires a separate runtime request after foreground location is granted.

## Task

${input:task:Describe the permission you need to add or the permission flow you need to fix}
