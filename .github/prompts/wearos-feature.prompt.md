---
mode: agent
description: Implement a new feature or fix a bug in the Wear OS watch app (OnePlus Watch 3)
---

## Context

You are working on the **Wear OS target** of the Distance Alarm app, optimised for the **OnePlus Watch 3**.

Key files:
- `WearOsShell.xaml(.cs)` – minimal single-page navigation shell
- `Views/WearOsMainPage.xaml(.cs)` – the only page on the watch
- `ViewModels/WearOsViewModel.cs` – all watch logic
- Activated via `#if WEAR_OS` compile-time constant (set by `-p:WearOSTarget=true`)

## Build & deploy

```bash
# Build watch APK
dotnet build -f net9.0-android -c Debug -p:WearOSTarget=true -p:RuntimeIdentifiers=android-arm

# Deploy via ADB (watch must be connected via ADB-over-WiFi or USB)
adb -d install -r <path-to-wearos.apk>

# Or use the VS Code task: "⌚ Install Wear OS via ADB"
```

## Rules

1. **Never** add TabBar, NavigationBar, or multi-page navigation to WearOS builds.
2. `WearOsViewModel` must auto-initialise (`InitializeAsync()` called from `WearOsMainPage.OnAppearing`).
3. Settings are **read-only** on the watch; the companion phone app writes them to `Preferences`. Load with `Preferences.Default.Get(key, defaultValue)`.
4. Keep the UI extremely minimal: status text + two buttons (Stop Alarm / Test Alarm) is the target pattern.
5. Background monitoring must auto-start in `InitializeAsync()` – the user should not need to toggle it.
6. All observable properties must be declared with `[ObservableProperty]` (CommunityToolkit.Mvvm source generator).
7. Guard every WearOS-specific code path with `#if WEAR_OS`.
8. Screen size is small (454×454 px on OnePlus Watch 3) – avoid large layouts; prefer `VerticalStackLayout` with generous `Padding`.
9. Use high-contrast colours for readability on AMOLED display (prefer black backgrounds).
10. Vibration is the **primary** alarm output on the watch; sound is secondary.

## Task

${input:task:Describe the Wear OS feature or bug fix}
