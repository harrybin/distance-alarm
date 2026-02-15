using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Java.Util;
using System.Diagnostics.CodeAnalysis;

namespace DistanceAlarm.Platforms.Android;

/// <summary>
/// Android-specific BLE scanner that uses modern ScanSettings for better WearOS device discovery
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This class is designed for Android platform only")]
public class EnhancedBleScanner : IDisposable
{
    private BluetoothManager? _bluetoothManager;
    private BluetoothAdapter? _bluetoothAdapter;
    private BluetoothLeScanner? _scanner;
    private ScanCallback? _scanCallback;
    private bool _isScanning = false;
    private bool _disposed = false;

    public event EventHandler<BluetoothDevice>? DeviceDiscovered;

    public EnhancedBleScanner()
    {
        try
        {
            var context = global::Android.App.Application.Context;
            _bluetoothManager = (BluetoothManager?)context.GetSystemService(global::Android.Content.Context.BluetoothService);
            _bluetoothAdapter = _bluetoothManager?.Adapter;
            _scanner = _bluetoothAdapter?.BluetoothLeScanner;
            
            System.Diagnostics.Debug.WriteLine("EnhancedBleScanner initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts BLE scanning with optimized settings for WearOS device discovery
    /// </summary>
    public void StartScan()
    {
        if (_isScanning)
        {
            System.Diagnostics.Debug.WriteLine("EnhancedBleScanner: Already scanning");
            return;
        }

        if (_scanner == null)
        {
            System.Diagnostics.Debug.WriteLine("EnhancedBleScanner: Scanner not available");
            return;
        }

        try
        {
            // Create scan callback
            _scanCallback = new BleScanCallback(this);

            // Create scan settings optimized for WearOS discovery
            // SCAN_MODE_LOW_LATENCY provides the best discovery performance
            var settings = new ScanSettings.Builder()
                .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowLatency) // High power mode for better discovery
                .SetCallbackType(ScanCallbackType.AllMatches) // Report all advertisements
                .SetMatchMode(BluetoothScanMatchMode.Aggressive) // Aggressive matching for better discovery
                .SetReportDelay(0) // Report immediately
                .Build();

            // No filters - discover all BLE devices including WearOS
            var filters = new List<ScanFilter>();

            _scanner.StartScan(filters, settings, _scanCallback);
            _isScanning = true;
            
            System.Diagnostics.Debug.WriteLine("EnhancedBleScanner: Started scanning with LOW_LATENCY mode for WearOS discovery");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner: StartScan failed: {ex.Message}");
            _isScanning = false;
        }
    }

    /// <summary>
    /// Stops BLE scanning
    /// </summary>
    public void StopScan()
    {
        if (!_isScanning || _scanner == null || _scanCallback == null)
        {
            return;
        }

        try
        {
            _scanner.StopScan(_scanCallback);
            _isScanning = false;
            System.Diagnostics.Debug.WriteLine("EnhancedBleScanner: Stopped scanning");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner: StopScan failed: {ex.Message}");
        }
    }

    public bool IsScanning => _isScanning;

    private void OnDeviceDiscovered(BluetoothDevice device)
    {
        DeviceDiscovered?.Invoke(this, device);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            StopScan();
            _scanCallback = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner: Dispose failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback for BLE scan results
    /// </summary>
    private class BleScanCallback : ScanCallback
    {
        private readonly EnhancedBleScanner _scanner;

        public BleScanCallback(EnhancedBleScanner scanner)
        {
            _scanner = scanner;
        }

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            if (result?.Device == null)
                return;

            try
            {
                var device = result.Device;
                var deviceName = device.Name ?? "Unknown";
                var rssi = result.Rssi;

                System.Diagnostics.Debug.WriteLine(
                    $"EnhancedBleScanner: Device found - Name: {deviceName}, Address: {device.Address}, RSSI: {rssi} dBm");

                _scanner.OnDeviceDiscovered(device);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner: OnScanResult error: {ex.Message}");
            }
        }

        public override void OnBatchScanResults(IList<ScanResult>? results)
        {
            if (results == null)
                return;

            foreach (var result in results)
            {
                OnScanResult(ScanCallbackType.AllMatches, result);
            }
        }

        public override void OnScanFailed(ScanFailure errorCode)
        {
            System.Diagnostics.Debug.WriteLine($"EnhancedBleScanner: Scan failed with error code: {errorCode}");
        }
    }
}
