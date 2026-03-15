---
applyTo: "Platforms/Android/**"
---

## Android Platform Code – Conventions & Rules

These instructions apply to all files under `Platforms/Android/`.

### API-level guards

Always check the API level before using newer Android APIs:

```csharp
if (Build.VERSION.SdkInt >= BuildVersionCodes.S)      // Android 12+ (API 31)
if (Build.VERSION.SdkInt >= BuildVersionCodes.O)      // Android 8+ (API 26)
if (Build.VERSION.SdkInt >= BuildVersionCodes.M)      // Android 6+ (API 23)
if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+ (API 33)
```

### Permission handling

- Delegate all runtime permission requests to `AndroidPermissionService`
- Use the `PermissionRequestCallback` static helper for async permission results
- Results arrive via `MainActivity.OnRequestPermissionsResult` → `PermissionRequestCallback`
- **BLE on Android 12+**: use `BluetoothPermissions` (BLUETOOTH_SCAN + BLUETOOTH_CONNECT)
- **BLE on Android < 12**: use `Permissions.LocationWhenInUse`
- Never request permissions from a Service or ViewModel directly

### Foreground services

- Declare `android:foregroundServiceType` in `AndroidManifest.xml` and the `[Service]` attribute
- Call `StartForeground()` in the first line of `OnStartCommand`
- Create the notification channel before `StartForeground` on API 26+
- Return `StartCommandResult.Sticky`
- Clean up in `OnDestroy`

### BLE scanning (EnhancedBleScanner)

- `EnhancedBleScanner` runs alongside Plugin.BLE – always run both concurrently
- `ScanMode.LowLatency` + `CallbackType.AllMatches` + `MatchMode.Aggressive` are required for WearOS discovery
- Always check for duplicate MAC addresses before adding a device to any collection
- `EnhancedBleScanner` results may have `BleDevice.Device == null` – handle gracefully

### Alarm delivery

- `AndroidAlarmService` handles vibration, sound, notification, and wake lock
- Use `VibrationEffect.CreateWaveform` (API 26+) or legacy `Vibrator.Vibrate(pattern)`
- Use `MediaPlayer` with `SetAudioAttributes` for alarm audio
- Notification must use a high-importance channel and `fullScreenIntent` for lock-screen visibility
- Wake lock must be `ScreenBright | AcquireCausesWakeup`; always release in `finally`

### Activity/lifecycle

- `MainActivity.OnRequestPermissionsResult` must forward to `PermissionRequestCallback`
- Avoid storing `Activity` references outside of `MainActivity` to prevent memory leaks
- Use `Platform.CurrentActivity` (MAUI Essentials) when a context is needed outside the activity

### Logging

- Use `Android.Util.Log` only for low-level native debugging; prefer `ILogger<T>` everywhere else
- Include class and method name in log tags: `"DistanceAlarm.EnhancedBleScanner"`
