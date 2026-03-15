---
applyTo: "ViewModels/**"
---

## ViewModels – Conventions & Rules

These instructions apply to all files under `ViewModels/`.

### Base class & source generators

All ViewModels **must** inherit `ObservableObject` from CommunityToolkit.Mvvm:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DistanceAlarm.ViewModels;

public partial class MyViewModel : ObservableObject { … }
```

The class must be `partial` for source-generator attributes to work.

### Observable properties

Use `[ObservableProperty]` for all bindable properties. The source generator creates the public property and `PropertyChanged` notifications automatically:

```csharp
[ObservableProperty]
private string _statusMessage = string.Empty;

// Generated: public string StatusMessage { get; set; }
```

Do **not** manually implement `INotifyPropertyChanged` or call `OnPropertyChanged`.

### Commands

Use `[RelayCommand]` for all commands. Async commands must end in `Async`:

```csharp
[RelayCommand]
private async Task ConnectToDeviceAsync(BleDevice device, CancellationToken ct = default)
{
    try { … }
    catch (Exception ex) { _logger.LogError(ex, "Connect failed"); }
}
// Generated: ConnectToDeviceCommand (IAsyncRelayCommand<BleDevice>)
```

### Constructor injection

Inject all dependencies via the constructor. DI is registered in `MauiProgram.cs`:

```csharp
public MainViewModel(
    IBluetoothService bluetoothService,
    IAlarmService alarmService,
    ILogger<MainViewModel> logger)
```

### Phone vs WearOS split

- `MainViewModel` and `SettingsViewModel` are **phone-only** (registered inside `#else` of `#if WEAR_OS`).
- `WearOsViewModel` is **watch-only** (registered inside `#if WEAR_OS`).
- Never add phone-specific UI logic to `WearOsViewModel`, and vice versa.

### WearOsViewModel rules

- Must call `InitializeAsync()` from `WearOsMainPage.OnAppearing()`.
- Must auto-start background monitoring – no user toggle.
- Reads settings from `Preferences.Default` (written by companion phone app).
- Alarm must trigger **vibration first**, then sound (secondary).

### Settings persistence

Use `Preferences.Default` for all settings; wrap in `SettingsViewModel.SaveSettingsAsync()`:

```csharp
Preferences.Default.Set("PingInterval", Settings.PingInterval);
```

### Thread safety

- Event handlers from services must update observable properties on the main thread:

```csharp
private void OnDeviceDiscovered(object? sender, BleDevice device)
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        if (!DiscoveredDevices.Any(d => d.MacAddress == device.MacAddress))
            DiscoveredDevices.Add(device);
    });
}
```

### Error handling & logging

Every command must catch exceptions, log with `ILogger`, and update a user-visible `StatusMessage`:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    StatusMessage = "An error occurred. Please try again.";
}
```

### No business logic in code-behind

Views (`*.xaml.cs`) must only:
1. Set `BindingContext = viewModel`
2. Call a ViewModel `*Command.ExecuteAsync()` in `OnAppearing` / `OnDisappearing`

All other logic belongs in the ViewModel.
