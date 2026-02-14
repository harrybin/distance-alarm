# Distance Alarm - Implementation & Reliability Guide

## Overview
This document describes the reliability improvements implemented for the Distance Alarm application, specifically optimized for Pixel phones and Wear OS devices.

## Core Reliability Features

### 1. Background Monitoring Service

**Purpose**: Ensures the app continues monitoring BLE connection even when the screen is locked or the app is in the background.

**Implementation**:
- `BluetoothMonitoringService`: Android foreground service with persistent notification
- Automatic WakeLock acquisition to prevent CPU sleep
- Battery optimization exemption request
- Survives app background/screen lock

**User Impact**:
- ✅ **Reliable detection** even with phone locked
- ✅ **Survives Doze mode** on Pixel phones (Android 12+)
- ⚠️ **Requires battery optimization exemption** - user will be prompted

**Usage**:
```csharp
await _backgroundService.StartMonitoringAsync(); // Start monitoring
await _backgroundService.StopMonitoringAsync();  // Stop monitoring
```

### 2. Safe Zones (Geofencing)

**Purpose**: Prevents false alarms when devices are intentionally separated in known safe locations (e.g., home).

**How It Works**:
- GPS-based circular zones with configurable radius
- Haversine formula for accurate distance calculation
- Multiple safe zones supported
- Can be enabled/disabled individually

**Configuration**:
```csharp
// Default "Home" safe zone created automatically
Settings.SafeZones[0].Latitude = 37.7749;  // Set to home location
Settings.SafeZones[0].Longitude = -122.4194;
Settings.SafeZones[0].RadiusMeters = 100;  // 100m radius
Settings.SafeZones[0].IsEnabled = true;    // Enable zone

// Enable safe zone feature
Settings.EnableSafeZones = true;
```

**User Benefit**:
- No alarm when leaving phone at home while wearing watch
- Configurable per-location (home, office, etc.)
- Can be quickly disabled for high-security mode

### 3. RSSI-Based Distance Monitoring

**Purpose**: Provides early warning when signal weakens before complete disconnection.

**How It Works**:
- Continuous RSSI (signal strength) monitoring during pings
- Configurable threshold (default: -80 dBm)
- Early warning when approaching threshold
- More accurate than simple connected/disconnected state

**Configuration**:
```csharp
Settings.RssiThreshold = -80; // Trigger when signal < -80 dBm

// Monitor RSSI updates
_bluetoothService.RssiUpdated += (sender, rssi) => {
    // rssi = current signal strength in dBm
    // Lower (more negative) = weaker signal
};
```

**Signal Strength Reference**:
- **-30 to -50 dBm**: Excellent (very close)
- **-50 to -70 dBm**: Good (normal room distance)
- **-70 to -80 dBm**: Fair (approaching range limit)
- **-80 to -90 dBm**: Weak (connection may drop soon)
- **< -90 dBm**: Very weak (disconnection imminent)

### 4. Automatic Reconnection

**Purpose**: Handles temporary disconnections without triggering alarm, reducing false positives.

**Strategy**: Exponential backoff
- Attempt 1: Wait 2 seconds
- Attempt 2: Wait 4 seconds
- Attempt 3: Wait 8 seconds
- Attempt 4: Wait 16 seconds
- Attempt 5: Wait 32 seconds

**Configuration**:
```csharp
Settings.EnableAutoReconnect = true;
Settings.ReconnectMaxAttempts = 5;          // Max tries
Settings.ReconnectInitialDelaySeconds = 2;  // Initial delay

// Manually trigger reconnection
await _bluetoothService.AttemptReconnectAsync(maxAttempts: 5, initialDelaySeconds: 2);
```

**Behavior**:
- ✅ Alarm suppressed during reconnection attempts
- ✅ Alarm stopped if reconnection succeeds
- ❌ Alarm triggered after all attempts fail

### 5. Improved Ping Mechanism

**Old Approach** (Unreliable):
```csharp
var services = await device.GetServicesAsync(); // Expensive, slow
```

**New Approach** (Efficient):
```csharp
await device.UpdateRssiAsync();  // Lightweight, fast
var rssi = device.Rssi;          // Get signal strength
```

**Benefits**:
- ⚡ **50-100x faster** than service discovery
- 📊 **Provides distance data** via RSSI
- 🔋 **Better battery life** (less BLE activity)
- ✅ **More reliable** detection of weak connections

**Configuration**:
```csharp
Settings.PingInterval = 10;  // Seconds between pings (default: 10)
Settings.FailedPingThreshold = 2;  // Failed pings before alarm (default: 2)

// Set threshold dynamically
if (_bluetoothService is BluetoothService bleService)
{
    bleService.SetFailedPingThreshold(2);
}
```

### 6. Enhanced Alarm System

**Improvements**:
1. **Sound Playback**: Looping alarm using Android system alarm tone
2. **Screen Wake**: WakeLock to turn on screen and ensure visibility
3. **Proper Cleanup**: MediaPlayer lifecycle management

**Features**:
```csharp
Settings.VibrationEnabled = true;
Settings.VibrationDuration = 1000;  // ms

Settings.SoundEnabled = true;
Settings.SoundVolume = 0.8;  // 0.0 to 1.0

Settings.NotificationEnabled = true;

// Alarm triggers:
await _alarmService.TriggerAlarmAsync(Settings);  // Start
await _alarmService.StopAlarmAsync();             // Stop
```

**Alarm Logic Flow**:
```
Connection Lost
    ↓
Check Safe Zones
    ↓
Not in Safe Zone? → Wake Screen → Vibrate + Sound + Notification
    ↓
Auto-Reconnect Enabled?
    ↓
Attempt Reconnection (exponential backoff)
    ↓
Success? → Stop Alarm
Failure? → Keep Alarm Active
```

## Permission Requirements

### Required Permissions (AndroidManifest.xml)

```xml
<!-- BLE Permissions -->
<uses-permission android:name="android.permission.BLUETOOTH" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />

<!-- Location for BLE scanning -->
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />

<!-- Background monitoring -->
<uses-permission android:name="android.permission.WAKE_LOCK" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />

<!-- Battery optimization & alarms -->
<uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS" />
<uses-permission android:name="android.permission.SCHEDULE_EXACT_ALARM" />
```

### Runtime Permission Requests

**BLE Permissions**:
```csharp
await _bluetoothService.RequestBluetoothPermissionsAsync();
```

**Location Permissions** (for safe zones):
```csharp
await _locationService.RequestLocationPermissionsAsync();
```

**Battery Optimization Exemption**:
```csharp
await _backgroundService.RequestBatteryOptimizationExemptionAsync();
```

## Best Practices for Pixel Phone & Wear OS

### Pixel Phone Optimization

1. **Battery Optimization**:
   - Request exemption during first setup
   - Explain to user why it's needed
   - Test in Doze mode (adb shell dumpsys battery set status 3)

2. **Android 12+ BLE Permissions**:
   - Request BLUETOOTH_SCAN without location when possible
   - Use neverForLocation flag in manifest if not using location data

3. **Background Restrictions**:
   - Always use foreground service with notification
   - Keep notification priority LOW to avoid annoyance
   - Make notification informative (show connection status)

### Wear OS Optimization

1. **Screen Size**:
   - Design compact UI for small circular/rectangular screens
   - Use Wear OS specific layouts
   - Test on different screen sizes

2. **Battery Life**:
   - Increase ping interval on Wear OS (15-20 seconds)
   - Use ambient mode when display is off
   - Consider device role (phone monitors watch, or vice versa)

3. **Always-On Display**:
   - Update UI efficiently
   - Use minimal graphics
   - Show connection status clearly

## Testing Recommendations

### Unit Tests
```csharp
// Test safe zone calculation
[Test]
public void SafeZone_IsLocationInZone_ReturnsTrue()
{
    var zone = new SafeZone 
    { 
        Latitude = 37.7749, 
        Longitude = -122.4194, 
        RadiusMeters = 100 
    };
    
    Assert.IsTrue(zone.IsLocationInZone(37.7749, -122.4194));
}

// Test reconnection exponential backoff
[Test]
public void Reconnection_ExponentialBackoff_CorrectDelays()
{
    var delays = new[] { 2, 4, 8, 16, 32 };
    // Test implementation...
}
```

### Integration Tests

1. **BLE Connection Stability**:
   - Connect to device
   - Walk to edge of range
   - Verify RSSI warning before disconnection
   - Verify alarm triggers

2. **Safe Zone Functionality**:
   - Set home location as safe zone
   - Disconnect devices at home
   - Verify no alarm triggered
   - Move outside safe zone
   - Verify alarm triggers

3. **Background Monitoring**:
   - Start monitoring
   - Lock screen for 5 minutes
   - Disconnect device
   - Verify alarm still triggers

4. **Auto-Reconnection**:
   - Temporarily move devices apart (trigger disconnect)
   - Bring devices back together within 60 seconds
   - Verify reconnection succeeds
   - Verify alarm stops

### Device-Specific Testing

**Pixel Phone (Android 12+)**:
- Test with battery saver enabled
- Test in Doze mode (adb shell dumpsys deviceidle force-idle)
- Test with app in background for 30+ minutes
- Verify notifications appear correctly

**Wear OS**:
- Test on circular and square watch faces
- Test with watch in ambient mode
- Test with different battery optimization settings
- Verify vibration works (may differ from phone)

## Troubleshooting

### Issue: Alarm doesn't trigger in background
**Solution**: 
- Verify foreground service is running
- Check battery optimization exemption granted
- Ensure WAKE_LOCK permission granted
- Check logs for service lifecycle

### Issue: Too many false alarms
**Solution**:
- Increase FailedPingThreshold (2 → 3)
- Increase PingInterval (10s → 15s)
- Enable AutoReconnect
- Configure safe zones for known locations

### Issue: Poor battery life
**Solution**:
- Increase PingInterval (10s → 20s)
- Reduce RSSI monitoring frequency
- Disable safe zones if not needed
- Check for BLE connection leaks

### Issue: Reconnection fails
**Solution**:
- Verify devices are in range
- Check BLE permissions
- Increase reconnection attempts (5 → 10)
- Increase initial delay (2s → 5s)

## Performance Metrics

Expected battery impact:
- **Ping every 10s**: ~2-3% per hour
- **Ping every 20s**: ~1-2% per hour
- **Safe zones enabled**: +0.5% per hour (GPS)
- **Background service**: +0.5% per hour (wake lock)

Expected detection times:
- **Strong signal loss**: 0-10 seconds
- **Gradual signal loss**: 10-20 seconds (RSSI warning)
- **Complete disconnection**: 20-40 seconds (with reconnection)

## Future Improvements

1. **ML-based prediction**: Predict disconnection based on RSSI trends
2. **Bluetooth 5.0 features**: Use extended range if available  
3. **Mesh networking**: Support multiple devices
4. **Cloud sync**: Sync safe zones across devices
5. **Statistics dashboard**: Track connection reliability
6. **Power profile adaptation**: Adjust ping rate based on battery level
