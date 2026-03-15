---
applyTo: "{WearOsShell.xaml,WearOsShell.xaml.cs,Views/WearOsMainPage.*,ViewModels/WearOsViewModel.cs}"
---

## Wear OS (OnePlus Watch 3) – Conventions & Rules

These instructions apply to all WearOS-specific files.

### Build target

The WearOS build is activated with `-p:WearOSTarget=true`, which defines `WEAR_OS` at compile time. Every WearOS-specific code path must be wrapped in `#if WEAR_OS … #endif`.

```bash
# Build
dotnet build -f net9.0-android -c Debug -p:WearOSTarget=true -p:RuntimeIdentifiers=android-arm

# Deploy (watch connected via ADB-over-WiFi or USB)
adb -d install -r artifacts/wearos/com.harrybin.distancealarm-Signed.apk
```

### UI constraints (OnePlus Watch 3)

- Screen: **454 × 454 px** AMOLED (round)
- Use **black background** (`#000000`) for AMOLED efficiency
- Use **high-contrast text**: white or accent colour on black
- Minimum tap target: **48 dp** (preferably 56 dp)
- Avoid horizontal scroll; use `VerticalStackLayout`
- Keep UI to 2–3 interactive elements maximum
- No TabBar, no NavigationBar, no multi-page navigation

### Shell

`WearOsShell` is a `Shell` with a single `ShellContent` pointing to `WearOsMainPage`. Do not add routes, tabs, or flyout items.

### WearOsViewModel behaviour

1. `InitializeAsync()` is called from `WearOsMainPage.OnAppearing()` – not from constructor.
2. Auto-start background BLE monitoring (no user toggle needed).
3. Load all settings from `Preferences.Default` – never prompt the user to configure settings.
4. Alarm output priority: **vibration (primary)** → sound (secondary) → notification (tertiary).
5. Expose only: `StatusMessage`, `ConnectionStatus`, `CurrentRssi`, `IsAlarmActive`, `ConnectedDeviceName`, `IsConnected`.
6. Expose only two commands: `StopAlarmCommand` and `TestAlarmCommand`.

### Settings (read-only on watch)

The companion phone app writes settings. On the watch, always read:

```csharp
var pingInterval = Preferences.Default.Get("PingInterval", 10);
var rssiThreshold = Preferences.Default.Get("RssiThreshold", -80);
var vibrationEnabled = Preferences.Default.Get("VibrationEnabled", true);
```

Never write settings from the watch.

### BLE on WearOS

- For discovering the paired phone, use `EnhancedBleScanner` alongside Plugin.BLE.
- The watch typically pairs to a phone with a known device name or MAC prefix stored in `Preferences`.
- Reconnect with exponential back-off; restart background monitoring after reconnect.

### Alarm on WearOS

- Vibration is mandatory and must use a noticeable pattern (e.g., `[0, 500, 200, 500]`).
- Sound is optional (watch speaker may be absent); always check `SoundEnabled` setting.
- `StopAlarmAsync` must immediately cancel vibration AND sound AND clear `IsAlarmActive`.

### Performance

- Minimise allocations in the ping loop (runs every N seconds in the background).
- Use `CancellationToken` to stop all background loops cleanly on `OnDisappearing` / app suspend.
- Do not run location services on the watch (no GPS polling); safe-zone checks run on the phone only.
