using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistanceAlarm.Models;
using DistanceAlarm.Services;

namespace DistanceAlarm.ViewModels;

/// <summary>
/// Minimal ViewModel for the Wear OS watch app.
///
/// The watch acts as a BLE Peripheral / GATT Server (via <see cref="IWearOsPeripheralService"/>).
/// The companion phone app is the BLE Central that discovers and connects to the watch.
/// When the phone disconnects, the watch triggers a vibration alarm.
/// All configuration is pushed from the phone app via the Settings GATT characteristic
/// and stored in local Preferences by <see cref="IWearOsPeripheralService"/>.
/// </summary>
public partial class WearOsViewModel : ObservableObject
{
    private readonly IWearOsPeripheralService _peripheralService;
    private readonly IAlarmService _alarmService;
    private readonly IBackgroundService? _backgroundService;

    [ObservableProperty]
    private string _statusMessage = "Initializing...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;

    [ObservableProperty]
    private int _currentRssi = 0;

    [ObservableProperty]
    private bool _isAlarmActive = false;

    [ObservableProperty]
    private string _connectedDeviceName = string.Empty;

    /// <summary>Derived property: true when the companion phone has an active BLE connection.</summary>
    public bool IsConnected => ConnectionStatus == ConnectionStatus.Connected;

    private bool _isInitialized  = false;
    private bool _isInitializing = false;

    public WearOsViewModel(IWearOsPeripheralService peripheralService,
        IAlarmService alarmService,
        IBackgroundService? backgroundService = null)
    {
        _peripheralService  = peripheralService;
        _alarmService       = alarmService;
        _backgroundService  = backgroundService;

        _peripheralService.PhoneConnected    += OnPhoneConnected;
        _peripheralService.PhoneDisconnected += OnPhoneDisconnected;
    }

    /// <summary>
    /// Starts BLE advertising and the GATT server so the phone can discover and connect to
    /// the watch.  Must be called from <c>WearOsMainPage.OnAppearing</c> so that the Activity
    /// is resumed before requesting BLE permissions.
    /// Safe to call multiple times; re-runs only when a previous attempt did not complete.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized || _isInitializing)
            return;

        _isInitializing = true;
        try
        {
            // Auto-start background monitoring so the watch keeps running with the screen off
            if (_backgroundService != null)
            {
                try
                {
                    await _backgroundService.StartMonitoringAsync();
                    System.Diagnostics.Debug.WriteLine("WearOS: background monitoring started");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"WearOS: background monitoring failed: {ex.Message}");
                }
            }

            // Start the BLE peripheral service (advertising + GATT server).
            // The phone will discover and connect to the watch automatically.
            await _peripheralService.StartAsync();

            StatusMessage = "Waiting for phone…";
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            StatusMessage = "Init error";
            System.Diagnostics.Debug.WriteLine(
                $"WearOsViewModel InitializeAsync error: {ex.Message}");
        }
        finally
        {
            _isInitializing = false;
        }
    }

    [RelayCommand]
    private async Task StopAlarmAsync()
    {
        try
        {
            await _alarmService.StopAlarmAsync();
            IsAlarmActive = false;
            StatusMessage = IsConnected
                ? $"Monitoring {ConnectedDeviceName}"
                : "Waiting for phone…";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WearOsViewModel StopAlarm error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestAlarmAsync()
    {
        try
        {
            var settings = LoadSettingsFromPreferences();
            await _alarmService.TriggerAlarmAsync(settings);
            IsAlarmActive = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WearOsViewModel TestAlarm error: {ex.Message}");
        }
    }

    // ── Peripheral service event handlers ────────────────────────────────────

    private void OnPhoneConnected(object? sender, string phoneName)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            ConnectionStatus    = ConnectionStatus.Connected;
            ConnectedDeviceName = phoneName;
            StatusMessage       = $"Monitoring · {phoneName}";
            IsAlarmActive       = false;
        });
    }

    private async void OnPhoneDisconnected(object? sender, EventArgs e)
    {
        try
        {
            if (Application.Current?.Dispatcher is not { } dispatcher)
                return;

            await dispatcher.DispatchAsync(async () =>
            {
                ConnectionStatus    = ConnectionStatus.Disconnected;
                ConnectedDeviceName = string.Empty;
                StatusMessage       = "Connection lost!";
                IsAlarmActive       = true;

                // Alarm: vibration (primary) → sound (secondary) → notification
                var settings = LoadSettingsFromPreferences();
                await _alarmService.TriggerAlarmAsync(settings);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsViewModel OnPhoneDisconnected error: {ex.Message}");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads alarm settings from device Preferences.
    /// On Wear OS the companion phone app writes these values via the Settings GATT
    /// characteristic; sensible defaults are used when not yet configured.
    /// </summary>
    private static AlarmSettings LoadSettingsFromPreferences() => new AlarmSettings
    {
        PingInterval      = Preferences.Default.Get("PingInterval",      10),
        VibrationEnabled  = Preferences.Default.Get("VibrationEnabled",  true),
        VibrationDuration = Preferences.Default.Get("VibrationDuration", 1000),
        // Sound disabled on watch by default (watch speakers are limited)
        SoundEnabled         = Preferences.Default.Get("SoundEnabled",      false),
        NotificationEnabled  = Preferences.Default.Get("NotificationEnabled", true),
    };
}

