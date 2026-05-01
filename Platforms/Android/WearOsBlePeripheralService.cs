#if WEAR_OS
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using DistanceAlarm.Models;
using DistanceAlarm.Services;
using Java.Util;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace DistanceAlarm.Platforms.Android;

/// <summary>
/// Wear OS implementation of <see cref="IWearOsPeripheralService"/>.
///
/// The watch acts as a BLE Peripheral / GATT Server:
///   • BluetoothLeAdvertiser broadcasts the custom Distance Alarm service UUID so that
///     the companion phone app (BLE Central) can discover and connect to the watch.
///   • BluetoothGattServer accepts incoming connections from the phone and exposes a
///     writable Settings characteristic so that the phone can push AlarmSettings to
///     the watch.  Received settings are persisted to Preferences so they survive
///     app restarts.
/// </summary>
[SuppressMessage("Interoperability", "CA1416", Justification = "Android-only, WEAR_OS build")]
public class WearOsBlePeripheralService : IWearOsPeripheralService, IDisposable
{
    // ── BLE objects ─────────────────────────────────────────────────────────
    private BluetoothManager?     _bluetoothManager;
    private BluetoothAdapter?     _bluetoothAdapter;
    private BluetoothLeAdvertiser? _advertiser;
    private BluetoothGattServer?  _gattServer;

    // ── Callbacks ────────────────────────────────────────────────────────────
    private AdvertiseCallback?    _advertiseCallback;
    private GattServerCallback?   _gattServerCallback;

    // ── State ────────────────────────────────────────────────────────────────
    private bool _isAdvertising  = false;
    private bool _disposed       = false;
    private BluetoothDevice? _connectedPhone = null;

    // ── IWearOsPeripheralService ────────────────────────────────────────────
    public event EventHandler<string>? PhoneConnected;
    public event EventHandler?         PhoneDisconnected;
    public bool IsPhoneConnected => _connectedPhone != null;

    // ── UUIDs ────────────────────────────────────────────────────────────────
    private static UUID WatchServiceUuid =>
        UUID.FromString(AppConstants.WatchServiceUuid)!;
    private static UUID SettingsCharacteristicUuid =>
        UUID.FromString(AppConstants.SettingsCharacteristicUuid)!;

    public WearOsBlePeripheralService()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            _bluetoothManager =
                (BluetoothManager?)context.GetSystemService(Context.BluetoothService);
            _bluetoothAdapter = _bluetoothManager?.Adapter;

            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: initialized");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService: init failed – {ex.Message}");
        }
    }

    public Task StartAsync()
    {
        try
        {
            if (_bluetoothAdapter == null || !_bluetoothAdapter.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine(
                    "WearOsBlePeripheralService: Bluetooth not available/enabled");
                return Task.CompletedTask;
            }

            StartGattServer();
            StartAdvertising();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService.StartAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        try
        {
            StopAdvertising();
            CloseGattServer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService.StopAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    // ── GATT Server ──────────────────────────────────────────────────────────

    private void StartGattServer()
    {
        if (_gattServer != null)
            return;

        if (_bluetoothManager == null)
            return;

        try
        {
            var context = global::Android.App.Application.Context;
            _gattServerCallback = new GattServerCallback(this);
            _gattServer = _bluetoothManager.OpenGattServer(context, _gattServerCallback);

            if (_gattServer == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "WearOsBlePeripheralService: OpenGattServer returned null");
                return;
            }

            // Primary service
            var service = new BluetoothGattService(
                WatchServiceUuid,
                GattServiceType.Primary);

            // Writable Settings characteristic (phone → watch)
            var settingsChar = new BluetoothGattCharacteristic(
                SettingsCharacteristicUuid,
                GattProperty.Write | GattProperty.WriteNoResponse,
                GattPermission.Write);

            service.AddCharacteristic(settingsChar);
            _gattServer.AddService(service);

            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: GATT server started");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService.StartGattServer failed: {ex.Message}");
        }
    }

    private void CloseGattServer()
    {
        try
        {
            _gattServer?.ClearServices();
            _gattServer?.Close();
            _gattServer = null;
            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: GATT server closed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService.CloseGattServer failed: {ex.Message}");
        }
    }

    // ── BLE Advertising ──────────────────────────────────────────────────────

    private void StartAdvertising()
    {
        if (_isAdvertising)
            return;

        if (_bluetoothAdapter == null)
            return;

        _advertiser = _bluetoothAdapter.BluetoothLeAdvertiser;
        if (_advertiser == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: BLE advertising not supported on this device");
            return;
        }

        try
        {
            var settings = new AdvertiseSettings.Builder()
                // Balanced mode: saves battery while still being discoverable in a reasonable
                // time (<2 s when phone is actively scanning).  LowLatency is appropriate for
                // scanning (EnhancedBleScanner) but would drain the watch battery if used for
                // continuous advertising here.
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true)                        // Must be connectable for phone to connect
                .SetTimeout(0)                               // Advertise indefinitely
                .Build();

            var data = new AdvertiseData.Builder()
                .AddServiceUuid(new global::Android.OS.ParcelUuid(WatchServiceUuid))
                .SetIncludeDeviceName(true)   // Broadcasts watch's Bluetooth name
                .Build();

            _advertiseCallback = new BleAdvertiseCallback();
            _advertiser.StartAdvertising(settings, data, _advertiseCallback);
            _isAdvertising = true;

            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: BLE advertising started");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService.StartAdvertising failed: {ex.Message}");
        }
    }

    private void StopAdvertising()
    {
        if (!_isAdvertising || _advertiser == null || _advertiseCallback == null)
            return;

        try
        {
            _advertiser.StopAdvertising(_advertiseCallback);
            _isAdvertising = false;
            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: BLE advertising stopped");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService.StopAdvertising failed: {ex.Message}");
        }
    }

    // ── Internal event dispatch ──────────────────────────────────────────────

    internal void OnPhoneConnected(BluetoothDevice device)
    {
        _connectedPhone = device;
        var address = device.Address ?? "Unknown";

        // Save the phone's address so the watch can recognise it on next startup
        Preferences.Default.Set(AppConstants.SavedDeviceIdKey, address);
        Preferences.Default.Set(AppConstants.SavedDeviceNameKey, device.Name ?? "Companion Phone");

        MainThread.BeginInvokeOnMainThread(() =>
            PhoneConnected?.Invoke(this, device.Name ?? address));

        System.Diagnostics.Debug.WriteLine(
            $"WearOsBlePeripheralService: Phone connected – {device.Name} ({address})");
    }

    internal void OnPhoneDisconnected(BluetoothDevice device)
    {
        if (_connectedPhone?.Address != device.Address)
            return;

        _connectedPhone = null;

        MainThread.BeginInvokeOnMainThread(() =>
            PhoneDisconnected?.Invoke(this, EventArgs.Empty));

        System.Diagnostics.Debug.WriteLine(
            $"WearOsBlePeripheralService: Phone disconnected – {device.Address}");
    }

    /// <summary>
    /// Persists AlarmSettings received from the phone to local Preferences.
    /// The WearOsViewModel reads these values via LoadSettingsFromPreferences().
    /// </summary>
    internal void OnSettingsReceived(byte[] data)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService: Settings received – {json}");

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("pingInterval", out var pi))
                Preferences.Default.Set("PingInterval", pi.GetInt32());
            if (root.TryGetProperty("vibrationEnabled", out var ve))
                Preferences.Default.Set("VibrationEnabled", ve.GetBoolean());
            if (root.TryGetProperty("vibrationDuration", out var vd))
                Preferences.Default.Set("VibrationDuration", vd.GetInt32());
            if (root.TryGetProperty("soundEnabled", out var se))
                Preferences.Default.Set("SoundEnabled", se.GetBoolean());
            if (root.TryGetProperty("rssiThreshold", out var rt))
                Preferences.Default.Set("RssiThreshold", rt.GetInt32());
            if (root.TryGetProperty("failedPingThreshold", out var fp))
                Preferences.Default.Set("FailedPingThreshold", fp.GetInt32());

            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: Settings saved to Preferences");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService.OnSettingsReceived parse error: {ex.Message}");
        }
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
    }

    // ── Nested callbacks ─────────────────────────────────────────────────────

    private class GattServerCallback : BluetoothGattServerCallback
    {
        private readonly WearOsBlePeripheralService _service;

        public GattServerCallback(WearOsBlePeripheralService service)
        {
            _service = service;
        }

        public override void OnConnectionStateChange(
            BluetoothDevice? device, ProfileState status, ProfileState newState)
        {
            if (device == null)
                return;

            if (newState == ProfileState.Connected)
                _service.OnPhoneConnected(device);
            else if (newState == ProfileState.Disconnected)
                _service.OnPhoneDisconnected(device);
        }

        public override void OnCharacteristicWriteRequest(
            BluetoothDevice? device,
            int requestId,
            BluetoothGattCharacteristic? characteristic,
            bool preparedWrite,
            bool responseNeeded,
            int offset,
            byte[]? value)
        {
            if (characteristic?.Uuid?.Equals(
                    UUID.FromString(AppConstants.SettingsCharacteristicUuid)) == true
                && value != null)
            {
                _service.OnSettingsReceived(value);
            }

            if (responseNeeded && device != null)
            {
                _service._gattServer?.SendResponse(
                    device, requestId, GattStatus.Success, 0, null);
            }
        }

        public override void OnServiceAdded(GattStatus status, BluetoothGattService? service)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService: GATT service added – status={status}");
        }
    }

    private class BleAdvertiseCallback : AdvertiseCallback
    {
        public override void OnStartSuccess(AdvertiseSettings? settingsInEffect)
        {
            System.Diagnostics.Debug.WriteLine(
                "WearOsBlePeripheralService: Advertising started successfully");
        }

        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            System.Diagnostics.Debug.WriteLine(
                $"WearOsBlePeripheralService: Advertising start failed – {errorCode}");
        }
    }
}
#endif
