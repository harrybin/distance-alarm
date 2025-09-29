using Plugin.BLE.Abstractions.Contracts;

namespace DistanceAlarm.Models;

public class BleDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public int RssiValue { get; set; }
    public bool IsConnected { get; set; }
    public DateTime LastSeen { get; set; }
    public IDevice? Device { get; set; }

    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : "Unknown Device";
    public string SignalStrength => $"{RssiValue} dBm";
}

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed
}

public class ConnectionState
{
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
    public BleDevice? ConnectedDevice { get; set; }
    public DateTime LastPingTime { get; set; }
    public bool IsAlarmActive { get; set; }
    public string StatusMessage { get; set; } = "Not connected";
    public int FailedPingCount { get; set; }
}