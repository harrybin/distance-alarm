---
mode: agent
description: Implement a new BLE (Bluetooth Low Energy) feature or extend the existing BluetoothService
---

## Context

You are extending the BLE layer of the Distance Alarm app.

- BLE library: **Plugin.BLE 3.2.0** (`IBluetoothLE`, `IAdapter`, `IDevice`)
- Android-native scanner: `EnhancedBleScanner` in `Platforms/Android/EnhancedBleScanner.cs`
- Main service: `Services/BluetoothService.cs` (implements `IBluetoothService`)
- The service is registered as a singleton in `MauiProgram.cs`

## Rules

1. Always add new BLE functionality to `IBluetoothService` (interface first) before implementing in `BluetoothService`.
2. Guard any Android-specific native code with `#if ANDROID … #endif`.
3. All async methods must accept a `CancellationToken ct = default` parameter.
4. Event-based callbacks must marshal to the main thread via `MainThread.BeginInvokeOnMainThread()` when updating UI-bound state.
5. Scan results must check for duplicate devices by MAC address (`BleDevice.MacAddress`) before adding to a collection.
6. When both Plugin.BLE and `EnhancedBleScanner` are running, merge discoveries by matching device address – `EnhancedBleScanner` results may have `BleDevice.Device == null` until Plugin.BLE confirms the device.
7. For WearOS devices (typically named "Watch" or matching a known prefix), `EnhancedBleScanner` is mandatory – do not rely on Plugin.BLE alone.
8. Reconnect logic must use exponential back-off: `2 s → 4 s → 8 s → 16 s → 32 s` up to `ReconnectMaxAttempts`.
9. Log meaningful events with `ILogger<BluetoothService>` at appropriate levels (Debug for verbose, Warning for recoverable errors, Error for failures).

## Permissions prerequisite

Before any BLE operation:
- **Android 12+ (API 31+)**: request `BLUETOOTH_SCAN` + `BLUETOOTH_CONNECT` via `BluetoothPermissions`
- **Android < 12**: request `ACCESS_FINE_LOCATION`

```csharp
if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
    await _permissionService.RequestPermissionsAsync(new BluetoothPermissions());
else
    await _permissionService.RequestPermissionsAsync(new Permissions.LocationWhenInUse());
```

## Task

${input:task:Describe the BLE feature you want to implement}
