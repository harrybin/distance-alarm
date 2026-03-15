---
mode: agent
description: Scaffold a new MVVM ViewModel for either the phone or the Wear OS target
---

## Context

ViewModels live in `ViewModels/`. The project uses **CommunityToolkit.Mvvm 8.4.0** with C# source generators.

Existing ViewModels:
- `MainViewModel` – phone companion app (scan, connect, settings, alarm)
- `SettingsViewModel` – phone settings (load/save via `Preferences`)
- `WearOsViewModel` – watch app (status, auto-monitoring, alarm stop/test)

## Required ViewModel structure

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DistanceAlarm.ViewModels;

public partial class MyViewModel : ObservableObject
{
    // --- constructor injection ---
    private readonly IMyService _myService;
    private readonly ILogger<MyViewModel> _logger;

    public MyViewModel(IMyService myService, ILogger<MyViewModel> logger)
    {
        _myService = myService;
        _logger = logger;
    }

    // --- observable properties (source-generated) ---
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // --- commands (source-generated, always async) ---
    [RelayCommand]
    private async Task DoSomethingAsync(CancellationToken ct = default)
    {
        try
        {
            // implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to do something");
        }
    }
}
```

## Registration in MauiProgram.cs

```csharp
// Singleton for long-lived VMs (main app VMs)
builder.Services.AddSingleton<MyViewModel>();

// Transient for page-scoped VMs (e.g., settings, dialogs)
builder.Services.AddTransient<MyViewModel>();
```

## Conditional registration for WearOS / Phone

```csharp
#if WEAR_OS
    builder.Services.AddSingleton<WearOsViewModel>();
#else
    builder.Services.AddSingleton<MyNewPhoneViewModel>();
#endif
```

## Page binding (code-behind)

```csharp
public partial class MyPage : ContentPage
{
    public MyPage(MyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MyViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
```

## Rules

1. Always use `[ObservableProperty]` for all bindable properties (not manual `OnPropertyChanged`).
2. Always use `[RelayCommand]` for all commands; name methods `*Async` for async commands.
3. Never put business logic in code-behind; all logic must be in the ViewModel.
4. Use constructor injection for all dependencies.
5. Commands must handle exceptions internally and log with `ILogger`.
6. Never block the UI thread; use `await Task.Run()` for CPU-bound work.
7. For collections, use `ObservableCollection<T>` so the UI auto-refreshes.

## Task

${input:task:Describe the ViewModel you want to create and which target it is for (phone / WearOS / both)}
