---
applyTo: "Services/**"
---

## Services Layer – Conventions & Rules

These instructions apply to all files under `Services/`.

### Interface-first design

1. **Always define the interface in `I*.cs` first** before adding an implementation.
2. Interfaces must be cross-platform (no Android/MAUI types in return types or parameters).
3. Implementations for a single platform go in `Platforms/Android/` and are named `Android*.cs`.

### Async contracts

- All service methods that perform I/O or BLE operations must be `async Task` or `async Task<T>`.
- Long-running methods must accept `CancellationToken ct = default`.
- Never `throw` without logging; catch, log with `ILogger`, then rethrow or return a safe default.

### Events (BLE service)

`IBluetoothService` exposes these events – always raise them on the **main thread** when they update UI-bound state:

```csharp
event EventHandler<BleDevice> DeviceDiscovered;
event EventHandler<ConnectionState> ConnectionStatusChanged;
event EventHandler<BleDevice> ConnectionLost;
event EventHandler<(BleDevice device, int rssi)> RssiUpdated;
```

Use `MainThread.BeginInvokeOnMainThread(() => EventName?.Invoke(…))`.

### BLE service specifics

- `StartScanningAsync` must launch both Plugin.BLE scanner **and** `EnhancedBleScanner` concurrently (`#if ANDROID`).
- De-duplicate discovered devices by `BleDevice.MacAddress`.
- `EnhancedBleScanner` discoveries may have `Device == null`; check before accessing it.
- Reconnect logic must use exponential back-off: `2 s → 4 s → 8 s → 16 s → 32 s`.
- Respect `AlarmSettings.FailedPingThreshold` and `AlarmSettings.ReconnectMaxAttempts`.

### Location service specifics

- Use `MAUI Essentials Geolocation` for GPS coordinates.
- Safe-zone checks delegate to `SafeZone.IsLocationInZone()` (Haversine formula in `Models/SafeZone.cs`).
- Always request `LocationWhenInUse` permission before calling `Geolocation.GetLocationAsync`.
- For background tracking, additionally request `LocationAlways`.

### Configuration / settings

- Services should accept `AlarmSettings` through their constructor or via a configuration interface.
- Never read `Preferences` directly in a service; let the ViewModel pass settings in.

### Registration (MauiProgram.cs)

- Cross-platform singletons: `builder.Services.AddSingleton<IMyService, MyService>()`
- Android implementations: inside `#if ANDROID` block
- Never register concrete classes directly if an interface exists

### Error handling

```csharp
try
{
    await _adapter.StartScanningForDevicesAsync(ct);
}
catch (OperationCanceledException)
{
    _logger.LogInformation("Scan cancelled");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to start BLE scan");
    throw; // or return safe default
}
```
