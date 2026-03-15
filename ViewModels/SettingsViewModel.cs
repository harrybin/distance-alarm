using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DistanceAlarm.Models;

namespace DistanceAlarm.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private AlarmSettings _settings;

    public SettingsViewModel()
    {
        _settings = new AlarmSettings();
    }

    public SettingsViewModel(AlarmSettings settings)
    {
        _settings = settings;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            Preferences.Set("PingInterval", Settings.PingInterval);
            Preferences.Set("VibrationEnabled", Settings.VibrationEnabled);
            Preferences.Set("SoundEnabled", Settings.SoundEnabled);
            Preferences.Set("NotificationEnabled", Settings.NotificationEnabled);
            Preferences.Set("VibrationDuration", Settings.VibrationDuration);
            Preferences.Set("SoundVolume", Settings.SoundVolume);
            Preferences.Set("RssiThreshold", Settings.RssiThreshold);
            Preferences.Set("FailedPingThreshold", Settings.FailedPingThreshold);
            Preferences.Set("EnableAutoReconnect", Settings.EnableAutoReconnect);
            Preferences.Set("ReconnectMaxAttempts", Settings.ReconnectMaxAttempts);
            Preferences.Set("ReconnectInitialDelaySeconds", Settings.ReconnectInitialDelaySeconds);

            if (Application.Current?.MainPage is Page page)
                await page.DisplayAlert("Settings", "Settings saved successfully", "OK");
        }
        catch (Exception ex)
        {
            if (Application.Current?.MainPage is Page page)
                await page.DisplayAlert("Error", $"Failed to save settings: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        try
        {
            Settings.PingInterval = Preferences.Get("PingInterval", 5);
            Settings.VibrationEnabled = Preferences.Get("VibrationEnabled", true);
            Settings.SoundEnabled = Preferences.Get("SoundEnabled", true);
            Settings.NotificationEnabled = Preferences.Get("NotificationEnabled", true);
            Settings.VibrationDuration = Preferences.Get("VibrationDuration", 1000);
            Settings.SoundVolume = Preferences.Get("SoundVolume", 0.8);
            Settings.RssiThreshold = Preferences.Get("RssiThreshold", -80);
            Settings.FailedPingThreshold = Preferences.Get("FailedPingThreshold", 2);
            Settings.EnableAutoReconnect = Preferences.Get("EnableAutoReconnect", true);
            Settings.ReconnectMaxAttempts = Preferences.Get("ReconnectMaxAttempts", 5);
            Settings.ReconnectInitialDelaySeconds = Preferences.Get("ReconnectInitialDelaySeconds", 2);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        if (Application.Current?.MainPage is not Page page)
            return;

        var result = await page.DisplayAlert(
            "Reset Settings",
            "Are you sure you want to reset all settings to default values?",
            "Yes", "No");

        if (result)
        {
            Settings = new AlarmSettings();
            await SaveSettingsAsync();
        }
    }
}