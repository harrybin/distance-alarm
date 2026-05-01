namespace DistanceAlarm.Services;

/// <summary>
/// Manages the Wear OS watch-side BLE peripheral role.
/// The watch acts as a GATT server and BLE peripheral so that the companion
/// phone app (running as a BLE Central) can discover and connect to it.
///
/// This interface is only implemented and registered on the WEAR_OS build.
/// </summary>
public interface IWearOsPeripheralService
{
    /// <summary>
    /// Raised on the calling thread when the companion phone connects.
    /// The event argument is the phone's Bluetooth address.
    /// </summary>
    event EventHandler<string> PhoneConnected;

    /// <summary>Raised when the companion phone disconnects.</summary>
    event EventHandler PhoneDisconnected;

    /// <summary>True while the companion phone has an active BLE connection to this watch.</summary>
    bool IsPhoneConnected { get; }

    /// <summary>
    /// Starts BLE advertising (so the phone can discover the watch) and opens the GATT server
    /// (so the phone can connect and push settings).
    /// </summary>
    Task StartAsync();

    /// <summary>Stops advertising and closes the GATT server.</summary>
    Task StopAsync();
}
