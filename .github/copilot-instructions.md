# Distance Alarm – .NET MAUI Project

## Project Overview

Distance Alarm is a .NET MAUI 9 application that consists of two builds sharing a single `.csproj`:

- **Android companion app** – full UI for configuring BLE monitoring, safe zones, and alarm settings
- **Wear OS app** – minimal watch-face UI optimised for the OnePlus Watch 3, controlled by the companion app

The app uses Bluetooth Low Energy (BLE) to monitor proximity between the phone and the watch. When the BLE link is lost (and the device is outside a configured safe zone) an alarm triggers on the watch and/or phone.

---

## Architecture

### Dual-build strategy (single .csproj)

The WearOS build is selected by passing `-p:WearOSTarget=true` to MSBuild, which adds the `WEAR_OS` compile-time constant.

```csharp
#if WEAR_OS
    // watch-only path – minimal UI, single-page shell, auto-start monitoring
#else
    // phone-only path – full TabBar UI, settings page, device scanner
#endif

#if ANDROID
    // Android-specific platform code (always true for both targets)
#endif
```

Always guard **all** WearOS-specific code with `#if WEAR_OS` and Android-specific code with `#if ANDROID`.

### MVVM with CommunityToolkit.Mvvm

- All ViewModels inherit `ObservableObject` from **CommunityToolkit.Mvvm 8.4.0**
- Use `[ObservableProperty]` source-generator attribute for bindable properties
- Use `[RelayCommand]` source-generator attribute for async commands
- Async commands must be named `*Async` and have a matching `*Command` property
- `MainViewModel` serves the phone app; `WearOsViewModel` serves the watch app

### Dependency Injection

Service registration lives in `MauiProgram.cs`. Follow this pattern when adding services:

```csharp
// Cross-platform interface registered once
builder.Services.AddSingleton<IMyService, MyService>();

// Platform implementations use conditional compilation
#if ANDROID
    builder.Services.AddSingleton<IPlatformService, Platforms.Android.AndroidPlatformService>();
#endif

// WearOS/Phone view-model split
#if WEAR_OS
    builder.Services.AddSingleton<WearOsViewModel>();
    builder.Services.AddSingleton<WearOsMainPage>();
#else
    builder.Services.AddSingleton<MainViewModel>();
    builder.Services.AddTransient<SettingsViewModel>();
    builder.Services.AddSingleton<MainPage>();
    builder.Services.AddTransient<SettingsPage>();
#endif
```

---

## Project Structure

```
DistanceAlarm.csproj          Single project, dual targets (phone + WearOS)
App.xaml / App.xaml.cs        Application root – selects shell at runtime via #if WEAR_OS
AppShell.xaml(.cs)            Phone navigation (TabBar + SettingsPage route)
WearOsShell.xaml(.cs)         Watch navigation (single-page, no chrome)
MainPage.xaml(.cs)            Phone main page (scan, connect, test alarm)
MauiProgram.cs                DI container, service & VM registration

Models/
  BleDevice.cs                IDevice wrapper + ConnectionStatus enum + ConnectionState
  AlarmSettings.cs            All user-configurable settings (ping interval, RSSI threshold, safe zones)
  SafeZone.cs                 GPS geofence (Haversine distance check)

Services/
  IBluetoothService.cs        BLE contract (scan, connect, ping, RSSI events)
  BluetoothService.cs         Plugin.BLE + EnhancedBleScanner implementation
  IAlarmService.cs            Alarm contract (trigger / stop)
  AlarmService.cs             Cross-platform base
  ILocationService.cs         GPS + safe-zone contract
  LocationService.cs          MAUI Essentials Geolocation implementation
  IBackgroundService.cs       Background monitoring contract
  IPermissionService.cs       Runtime-permission contract

ViewModels/
  MainViewModel.cs            Phone: device list, scan, connect, alarm test, safe-zone capture
  SettingsViewModel.cs        Phone: load/save/reset AlarmSettings via Preferences
  WearOsViewModel.cs          Watch: status display, alarm stop/test, auto-start monitoring

Views/
  SettingsPage.xaml(.cs)      Phone settings UI
  WearOsMainPage.xaml(.cs)    Watch main UI

Platforms/Android/
  MainActivity.cs             Activity entry; routes OnRequestPermissionsResult to PermissionRequestCallback
  MainApplication.cs          Application class
  AndroidAlarmService.cs      Vibration + MediaPlayer + Notification + WakeLock alarm
  AndroidBackgroundService.cs Foreground service (keeps monitoring alive; Doze-mode exempt)
  BluetoothMonitoringService.cs Foreground service for continuous BLE monitoring
  AndroidPermissionService.cs Runtime permission requests via static PermissionRequestCallback
  BluetoothPermissions.cs     MAUI BasePlatformPermission for BLUETOOTH_SCAN + BLUETOOTH_CONNECT
  EnhancedBleScanner.cs       Native Android BLE scanner (LowLatency + AllMatches + Aggressive)

Converters/
  ValueConverters.cs          XAML value converters

Resources/
  Styles/                     App-wide XAML styles and colours
  AppIcon/, Images/, Fonts/   Assets

.github/
  copilot-instructions.md     This file – repository-level Copilot instructions
  prompts/                    Reusable prompt files (skills) for common tasks
  instructions/               Scoped instruction files per code area

.vscode/
  tasks.json                  13 build/deploy tasks (see Build Commands below)
  launch.json                 Debug configurations for phone and watch
  settings.json               VS Code + OmniSharp settings
```

---

## Build Commands

| Purpose | Command |
|---------|---------|
| Phone debug build | `dotnet build -f net9.0-android -c Debug` |
| Phone release build | `dotnet build -f net9.0-android -c Release` |
| Build & run on phone | `dotnet build -t:Run -f net9.0-android -c Debug` |
| Watch debug build | `dotnet build -f net9.0-android -c Debug -p:WearOSTarget=true -p:RuntimeIdentifiers=android-arm` |
| Watch release APK | `dotnet publish -f net9.0-android -c Release -p:WearOSTarget=true -p:RuntimeIdentifiers=android-arm -p:AndroidPackageFormat=apk` |
| Phone release APK | `dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=apk` |
| Phone release AAB | `dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab` |
| Restore packages | `dotnet restore --locked-mode` |
| Clean | `dotnet clean` |

---

## BLE Implementation

### Plugin.BLE + EnhancedBleScanner (dual-scanner strategy)

`BluetoothService.StartScanningAsync()` launches **both** scanners concurrently:
- **Plugin.BLE** scanner – high-level cross-platform API
- **EnhancedBleScanner** (Android-native, `Platforms/Android/EnhancedBleScanner.cs`) – uses `ScanMode.LowLatency + CallbackType.AllMatches + MatchMode.Aggressive` to discover WearOS devices that Plugin.BLE misses

Note: `EnhancedBleScanner` discoveries may return `BleDevice.Device == null` until Plugin.BLE independently advertises the same device.

### Connection lifecycle

```
Disconnected → Connecting → Connected → (ping loop)
                                       ↓ ping fails N times
                                    Reconnecting → Connected
                                                 → Failed (alarm triggers)
```

Pings are `UpdateRssiAsync()` calls on the connected `IDevice`. Exponential back-off on reconnect: `2 s → 4 s → 8 s → 16 s → 32 s`.

### RSSI threshold

`AlarmSettings.RssiThreshold` (default `-80 dBm`). Values weaker than the threshold trigger a weak-signal warning in `MainViewModel.OnRssiUpdated()`.

---

## Permissions

### Android 12+ (API 31+)

Use the custom `BluetoothPermissions` class (NOT `LocationWhenInUse`):

```csharp
if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
{
    // Request BLUETOOTH_SCAN + BLUETOOTH_CONNECT
    await _permissionService.RequestPermissionsAsync(new BluetoothPermissions());
}
else
{
    // Request ACCESS_FINE_LOCATION
    await _permissionService.RequestPermissionsAsync(new Permissions.LocationWhenInUse());
}
```

### PermissionRequestCallback pattern

`AndroidPermissionService` dispatches runtime permission results through a static `PermissionRequestCallback` keyed by `requestCode`. Results flow via `MainActivity.OnRequestPermissionsResult` override.

---

## Alarm System

`AndroidAlarmService` handles all alarm actions:
- **Vibration** – `VibrationEffect` (Android 8+) or legacy API
- **Sound** – `MediaPlayer` with system alarm URI, looping
- **Notification** – high-priority channel (Android 8+), full-screen intent
- **Wake lock** – `ScreenBright | AcquireCausesWakeup` to turn on screen

---

## WearOS / OnePlus Watch 3 Specifics

- Target: WearOS 2.x/3.x on **OnePlus Watch 3**
- Build flag: `-p:WearOSTarget=true`
- Shell: `WearOsShell` – no TabBar, no NavigationBar, single page
- ViewModel: `WearOsViewModel` – auto-initialises, auto-starts background monitoring
- Settings written by companion phone app; watch reads from `Preferences`
- Deploy via ADB: `adb -d install -r <wearos.apk>` or use `.vscode/install-wearos.ps1`
- For BLE discovery of WearOS devices, always use `EnhancedBleScanner` alongside Plugin.BLE

---

## Coding Conventions

- Target framework: **net9.0-android** only (iOS/macOS are commented out)
- C# 12 / .NET 9 features are available
- Use `async`/`await` for all I/O; commands are always `async Task`
- Never use `Thread.Sleep`; use `Task.Delay` or `CancellationToken`-aware waits
- Use `ILogger<T>` for logging; inject via DI (already registered in `MauiProgram.cs`)
- Prefer `Preferences.Default` for lightweight persistent settings
- Safe-zone distance uses the **Haversine formula** in `SafeZone.IsLocationInZone()`
- All public service methods must be cancellable where long-running (`CancellationToken ct`)
- Format XAML with 4-space indentation; keep code-behind minimal (logic in ViewModel)
- New Pages follow the constructor-injection + `BindingContext` pattern used by `SettingsPage`

---

## NuGet Dependencies

| Package | Version | Notes |
|---------|---------|-------|
| Microsoft.Maui.Controls | 9.0.21 | Core MAUI framework |
| Microsoft.Maui.Essentials | 9.0.21 | GPS, vibration, etc. |
| Plugin.BLE | 3.2.0 | BLE abstraction layer |
| CommunityToolkit.Mvvm | 8.4.0 | MVVM source generators |
| Microsoft.Extensions.Logging.Debug | 9.0.5 | Debug logging |

Do not add new packages without checking for security vulnerabilities first.

---

## CI / CD

Three GitHub Actions workflows:
- **ci.yml** – build debug+release APKs, code quality, security scan, runs on every push/PR
- **build-apks.yml** – produce unsigned phone APK/AAB and WearOS APK, triggered on main/develop
- **release.yml** – sign and publish release artifacts

Builds use locked-mode NuGet restore (`--locked-mode`). Update `packages.lock.json` with `dotnet restore --force-evaluate` after changing dependencies.
