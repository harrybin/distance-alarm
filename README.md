# Distance Alarm

A cross-platform .NET MAUI application for Android phones and Wear OS devices that implements a Bluetooth Low Energy (BLE) based distance alarm system.

## 🚀 Features

- **Cross-platform support** - Works on Android phones and Wear OS devices
- **Bluetooth Low Energy connectivity** - Seamless BLE connection between devices
- **Configurable ping intervals** - Customizable connection monitoring
- **Multiple alarm types** - Vibration, sound, and notification alerts
- **Distance-based triggers** - Automatic alarms when BLE connection is lost
- **Real-time monitoring** - Continuous connection status tracking

## 📱 Supported Platforms

- Android 7.0 (API level 24) and above
- Wear OS 2.0 and above

## 🛠️ Technology Stack

- **.NET MAUI** - Cross-platform UI framework
- **Plugin.BLE** - Bluetooth Low Energy functionality
- **MVVM Architecture** - Clean separation of concerns
- **Dependency Injection** - Platform-specific service implementations

## 📋 Prerequisites

- Visual Studio 2022 17.8 or later
- .NET 9.0 SDK
- Android SDK (API level 24+)
- Wear OS SDK (for Wear OS development)
- **Android Debug Bridge (ADB)** - Required for device management and VSCode tasks

### 🔧 Installing Android Debug Bridge (ADB)

ADB is essential for the VSCode tasks to work properly, especially the "📱 Check Devices" task. Here are installation methods for different platforms:

#### Windows (Recommended: winget)

```bash
# Install via Windows Package Manager (easiest method)
winget install Google.PlatformTools

# After installation, restart your terminal or VSCode
# Verify installation
adb version
adb devices
```

#### Windows (Alternative: Manual Installation)

1. **Download Android Platform Tools** from [Google](https://developer.android.com/studio/releases/platform-tools)
2. **Extract** the ZIP file to `C:\platform-tools\`
3. **Add to PATH**:
   - Open System Properties → Environment Variables
   - Add `C:\platform-tools\` to the PATH variable
   - Restart terminal/VSCode
4. **Verify**: `adb version`

#### macOS

```bash
# Via Homebrew (recommended)
brew install android-platform-tools

# Via MacPorts
sudo port install android-platform-tools

# Verify installation
adb version
```

#### Linux (Ubuntu/Debian)

```bash
# Install via apt
sudo apt update
sudo apt install android-tools-adb android-tools-fastboot

# Verify installation
adb version
```

#### Linux (Other Distributions)

```bash
# Arch Linux
sudo pacman -S android-tools

# Fedora/CentOS/RHEL
sudo dnf install android-tools

# openSUSE
sudo zypper install android-tools
```

#### Verification

After installation, verify ADB is working:

```bash
# Check ADB version
adb version

# List connected devices
adb devices

# Start ADB server (if needed)
adb start-server
```

#### Troubleshooting ADB Installation

**Windows Issues:**

```bash
# If PATH is not updated after winget install:
# 1. Restart terminal/VSCode completely
# 2. Or manually refresh PATH in current session:
$env:PATH = [System.Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' + [System.Environment]::GetEnvironmentVariable('PATH', 'User')
```

**Permission Issues (Linux/macOS):**

```bash
# Add user to plugdev group (Linux)
sudo usermod -a -G plugdev $USER

# Set up udev rules for Android devices (Linux)
sudo wget -S -O /etc/udev/rules.d/51-android.rules https://raw.githubusercontent.com/M0Rf30/android-udev-rules/master/51-android.rules
sudo chmod a+r /etc/udev/rules.d/51-android.rules
sudo udevadm control --reload-rules
```

**Device Not Detected:**

```bash
# Restart ADB server
adb kill-server
adb start-server

# Check device authorization (should show on device screen)
adb devices

# Enable USB debugging on device
# Settings → Developer Options → USB Debugging
```

## 🔧 Setup & Installation

### Development Environment Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/harrybin/distance-alarm.git
   cd distance-alarm
   ```

2. **Install dependencies**

   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

## 🎨 VS Code Tasks

The project includes pre-configured VS Code tasks for streamlined development. Access them via `Ctrl+Shift+P` → "Tasks: Run Task":

| Task                               | Emoji    | Color            | Purpose                 | Requirements              |
| ---------------------------------- | -------- | ---------------- | ----------------------- | ------------------------- |
| **🤖 Android Debug**               | Robot    | Green (#4CAF50)  | Basic Android build     | .NET SDK                  |
| **🤖 Android Release**             | Robot    | Orange (#FF9800) | Release build           | .NET SDK                  |
| **📱 Install Android**             | Phone    | Green (#4CAF50)  | Android deployment      | Android SDK + Device      |
| **⌚ Install Wear OS**             | Watch    | Blue (#1A237E)   | Build for Wear OS (ARM) | Android SDK + Wear Device |
| **🔧 Install Wear OS APK via ADB** | Wrench   | Blue (#1A237E)   | Install APK via ADB     | **ADB + Wear OS Device**  |
| **📦 Deploy Android APK**          | Package  | Orange (#FF9800) | APK creation            | Android SDK               |
| **📦 Deploy Wear OS APK**          | Package  | Orange (#FF9800) | Wear OS APK             | Android SDK               |
| **🧹 Clean**                       | Broom    | Gray (#607D8B)   | Clean builds            | .NET SDK                  |
| **📥 Restore Packages**            | Download | Gray (#607D8B)   | Package restore         | .NET SDK                  |
| **📱 Check Devices**               | Phone    | Brown (#795548)  | Device discovery        | **ADB Required**          |
| **🔧 Install APK (Android)**       | Wrench   | Brown (#795548)  | ADB Android install     | **ADB Required**          |
| **🔧 Install APK (Wear OS)**       | Wrench   | Brown (#795548)  | ADB Wear OS install     | **ADB Required**          |

### 🔍 ADB-Dependent Tasks

The following tasks require ADB to be installed and available in PATH:

- **📱 Check Devices** - Lists all connected Android/Wear OS devices
- **🔧 Install Wear OS APK via ADB** - Builds for ARM architecture and installs APK on Wear OS device via ADB
- **🔧 Install APK (Android)** - Installs APK on Android device via ADB
- **🔧 Install APK (Wear OS)** - Installs APK on Wear OS device via ADB

**Wear OS Installation Workflow:**

1. Run **⌚ Install Wear OS** to build for ARM architecture
2. Run **🔧 Install Wear OS APK via ADB** to install via ADB (auto-detects Wear OS device)
3. Or use the combined workflow by running **🔧 Install Wear OS APK via ADB** directly

**Installation Status Check:**

```bash
# Verify ADB is available for VSCode tasks
adb version
adb devices
```

If ADB is not found, please follow the [ADB installation instructions](#-installing-android-debug-bridge-adb) above.

### Quick Start with VS Code Tasks

1. Open the project in VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
3. Type "Tasks: Run Task"
4. Select your desired task from the colorful list
5. The task will run with visual feedback in the status bar

## 🤖 GitHub Actions CI/CD

The project includes automated CI/CD pipelines for building and releasing the app:

### 🔄 Continuous Integration (CI)

Automatically triggered on:

- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches

**CI Pipeline includes:**

- 🚀 **Android Build** - Debug and Release builds
- 🔍 **Code Quality Checks** - Code analysis and linting
- 🔒 **Security Scan** - Vulnerability detection in dependencies
- 📊 **Build Status Summary** - Comprehensive status reporting

### 🚀 Release Pipeline

Manually triggered via GitHub Actions with approval:

**Release Features:**

- 🔐 **Manual Approval Required** - Protected release environment
- 📱 **Android APK & AAB** - Both direct install and Play Store formats
- ⌚ **Wear OS APK** - Optimized for Wear OS devices
- 🏷️ **GitHub Releases** - Automatic release creation with assets
- 🛡️ **SHA256 Checksums** - Security verification for all artifacts
- 📝 **Auto-generated Release Notes** - Professional release documentation

### 🎯 How to Create a Release

1. **Navigate to Actions tab** in your GitHub repository
2. **Select "🚀 Release Build & Deploy"** workflow
3. **Click "Run workflow"** and fill in:
   - **Version**: `v1.0.0` (follow semantic versioning)
   - **Release Notes**: Description of changes
   - **Create Release**: Check to create GitHub release
   - **Pre-release**: Check for beta/alpha releases
4. **Approve the release** when prompted
5. **Download artifacts** from the GitHub release page

### 📦 Release Artifacts

Each release generates:

- `DistanceAlarm-v1.0.0-android.apk` - Android direct install
- `DistanceAlarm-v1.0.0-android.aab` - Google Play Store bundle
- `DistanceAlarm-v1.0.0-wearos.apk` - Wear OS optimized
- `checksums-all.txt` - SHA256 verification file

### 🔐 Security & Environment Setup

For releases, configure these GitHub repository settings:

1. **Go to Settings → Environments**
2. **Create environment**: `release-approval`
3. **Add protection rules**: Required reviewers
4. **Configure secrets** if needed for code signing

### 📱 Android Installation

#### Option 1: Direct Installation via Visual Studio

1. **Enable Developer Mode on Android device**

   - Go to Settings → About Phone
   - Tap "Build Number" 7 times
   - Go back to Settings → Developer Options
   - Enable "USB Debugging"

2. **Connect device and deploy**
   ```bash
   # Connect Android device via USB
   dotnet build -t:Run -f net9.0-android
   ```

#### Option 2: APK Installation

1. **Build release APK**

   ```bash
   dotnet publish -f net9.0-android -c Release
   ```

2. **Install APK manually**
   - Locate the APK in `bin/Release/net9.0-android/publish/`
   - Transfer to Android device
   - Enable "Install from Unknown Sources" in Settings
   - Install the APK file

#### Option 3: Android Debug Bridge (ADB)

1. **Install via ADB**

   ```bash
   # Build the APK
   dotnet build -f net9.0-android -c Release

   # Install using ADB
   adb install bin/Release/net9.0-android/com.distancealarm.app-Signed.apk
   ```

### ⌚ Wear OS Installation

#### Prerequisites

- Wear OS device with Developer Mode enabled
- Wear OS companion app installed on paired phone

#### Installation Steps

1. **Enable Developer Mode on Wear OS**

   - Go to Settings → System → About
   - Tap "Build Number" 7 times
   - Go back to Settings → Developer Options
   - Enable "ADB Debugging" and "Debug over Wi-Fi"

2. **Prepare Wear OS device for ADB connection**

   **Important**: Before you can install on Wear OS, you first need to do an `adb pair ...` then `adb connect`.

   **Hint**: Set the developer option "Keep display on during charging" or extend your display on time to give ADB longer time to pair and connect.

   ```bash
   # Step 1: Pair with Wear OS device (first time only)
   # Find the pairing information in Developer Options → Wireless debugging
   # Look for "Pair device with pairing code"
   adb pair [WEAR_OS_IP]:[PAIRING_PORT]
   # Enter the 6-digit pairing code shown on your Wear OS device

   # Step 2: Connect to Wear OS device
   # Use the IP and port from "IP address & Port" in Wireless debugging
   adb connect [WEAR_OS_IP]:5555

   # Verify connection
   adb devices
   ```

3. **Connect Wear OS device (detailed steps)**

   ```bash
   # Example pairing process:
   # 1. On Wear OS: Settings → Developer Options → Wireless debugging → Pair device with pairing code
   # 2. Note the IP address and pairing port (e.g., 192.168.1.100:12345)
   # 3. Note the 6-digit pairing code

   adb pair 192.168.1.100:12345
   # Enter pairing code when prompted: 123456

   # After successful pairing, connect using the regular port (usually 5555)
   adb connect 192.168.1.100:5555

   # Verify your Wear OS device appears in the list
   adb devices
   ```

4. **Deploy to Wear OS**

   **Important**: Wear OS devices typically use ARM architecture (`armeabi-v7a`), so you need to build specifically for this architecture.

   ```bash
   # Method 1: Build for ARM architecture and install via dotnet (Recommended)
   dotnet build -f net9.0-android -c Debug -p:WearOSTarget=true -p:RuntimeIdentifiers=android-arm

   # Then manually install the APK via ADB
   adb -s [WEAR_OS_IP]:5555 install -r "bin\Debug\net9.0-android\com.distancealarm.app-Signed.apk"

   # Method 2: Use the updated VS Code task
   # Press Ctrl+Shift+P → Tasks: Run Task → ⌚ Install Wear OS
   ```

   **Architecture Check**: If installation fails with `INSTALL_FAILED_NO_MATCHING_ABIS`, check your device architecture:

   ```bash
   # Check Wear OS device architecture
   adb -s [WEAR_OS_IP]:5555 shell getprop ro.product.cpu.abi
   # Common result: armeabi-v7a (ARM 32-bit)

   # Build for the correct architecture
   dotnet build -f net9.0-android -c Debug -p:WearOSTarget=true -p:RuntimeIdentifiers=android-arm
   ```

#### Wear OS Connection Tips

- **Keep display active**: Enable "Keep display on during charging" in Developer Options to prevent the watch from sleeping during the pairing process
- **Extend screen timeout**: Go to Settings → Display → Screen timeout and set it to the maximum time (30 seconds or more)
- **Stay on Wi-Fi debugging screen**: Keep the Wireless debugging screen open during the pairing process
- **Re-pair if needed**: If connection fails, you may need to unpair and pair again
- **Check firewall**: Ensure your computer's firewall allows ADB connections on the specified ports

#### Troubleshooting Wear OS ADB Connection

```bash
# If pairing fails, restart wireless debugging
# On Wear OS: Developer Options → Wireless debugging → Turn off/on

# Check if device is paired but not connected
adb devices  # Should show device as "offline" or "unauthorized"

# Force disconnect and reconnect
adb disconnect [WEAR_OS_IP]:5555
adb connect [WEAR_OS_IP]:5555

# Reset ADB server if connection issues persist
adb kill-server
adb start-server
```

#### Alternative: Wear OS via Visual Studio

1. Open project in Visual Studio 2022
2. Select Wear OS device from deployment targets
3. Click "Deploy" or press F5

## 🐛 Debugging

### Visual Studio Debugging

#### Android Debugging

1. **Setup breakpoints** in your C# code
2. **Connect Android device** via USB with USB Debugging enabled
3. **Select Android device** from deployment targets
4. **Press F5** or click "Start Debugging"
5. **Monitor output** in Visual Studio Output window and Device Log

#### Wear OS Debugging

1. **Enable Wi-Fi debugging** on Wear OS device
2. **Connect via ADB** using device IP address
3. **Select Wear OS target** in Visual Studio
4. **Deploy and debug** using F5

### Command Line Debugging

#### View Android Logs

```bash
# View all logs
adb logcat

# Filter for your app
adb logcat | grep "DistanceAlarm"

# View only error logs
adb logcat *:E

# Clear logs and start fresh
adb logcat -c && adb logcat
```

#### Wear OS Specific Logs

```bash
# Connect to Wear OS device
adb connect [WEAR_OS_IP]:5555

# View Wear OS logs
adb -s [WEAR_DEVICE_ID] logcat

# Monitor BLE connections
adb logcat | grep -E "(bluetooth|BLE)"
```

### Debugging BLE Issues

#### Common BLE Debugging Commands

```bash
# Check Bluetooth status
adb shell dumpsys bluetooth_manager

# View BLE scan results
adb logcat | grep "BluetoothLeScanner"

# Monitor GATT connections
adb logcat | grep "BluetoothGatt"
```

#### BLE Debugging Tips

- **Check permissions**: Ensure location and Bluetooth permissions are granted
- **Monitor connection state**: Use logs to track BLE connection lifecycle
- **Test on multiple devices**: BLE behavior can vary between Android versions
- **Use BLE scanner apps**: Install third-party BLE scanners to verify device advertising

### Performance Debugging

#### Memory and Performance Monitoring

```bash
# Monitor memory usage
adb shell dumpsys meminfo com.distancealarm.app

# CPU usage monitoring
adb shell top | grep distancealarm

# Battery usage analysis
adb shell dumpsys batterystats | grep distancealarm
```

### Troubleshooting Common Issues

#### Build Issues

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# Clear NuGet cache
dotnet nuget locals all --clear
```

#### Deployment Issues

```bash
# Check connected devices
adb devices

# Restart ADB server
adb kill-server
adb start-server

# Verify app installation
adb shell pm list packages | grep distancealarm
```

#### Permission Issues

```bash
# Grant permissions manually via ADB
adb shell pm grant com.distancealarm.app android.permission.ACCESS_FINE_LOCATION
adb shell pm grant com.distancealarm.app android.permission.BLUETOOTH
adb shell pm grant com.distancealarm.app android.permission.BLUETOOTH_ADMIN
```

### Debug Configuration

#### Enable Debug Logging in Code

Add this to your `MauiProgram.cs` for enhanced logging:

```csharp
#if DEBUG
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif
```

## 🏗️ Project Structure

```
DistanceAlarm/
├── Models/           # Data models and settings
├── Services/         # BLE and alarm services
├── ViewModels/       # MVVM view models
├── Views/           # UI pages and controls
├── Platforms/       # Platform-specific implementations
├── Resources/       # Images, fonts, and other assets
└── Properties/      # App properties and configuration
```

## 🔐 Permissions

The app requires the following permissions:

### Android

- `BLUETOOTH` - Basic Bluetooth functionality
- `BLUETOOTH_ADMIN` - Bluetooth device discovery
- `ACCESS_COARSE_LOCATION` - Required for BLE scanning
- `ACCESS_FINE_LOCATION` - Precise location for BLE
- `VIBRATE` - Vibration alerts

### Wear OS

- `WAKE_LOCK` - Keep device awake for monitoring
- `BODY_SENSORS` - Enhanced device interaction

## 🚀 Usage

1. **Pair Devices**: Launch the app on both devices and establish a BLE connection
2. **Configure Settings**: Set your preferred ping interval and alarm types
3. **Start Monitoring**: Enable distance monitoring to receive alerts when devices are separated
4. **Customize Alerts**: Choose from vibration, sound, or notification alerts

## 🔧 Configuration

The app allows customization of:

- **Ping Interval**: How frequently devices check connection (1-60 seconds)
- **Alarm Types**: Vibration, sound, notifications, or combinations
- **Connection Timeout**: How long to wait before triggering distance alarm
- **Auto-reconnect**: Automatic reconnection when devices come back in range

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 🐛 Known Issues

- Initial BLE pairing may require device restart on some Android versions
- Wear OS battery optimization may affect background monitoring

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

If you encounter any issues or have questions:

- Open an issue on GitHub
- Check the [troubleshooting guide](docs/troubleshooting.md)
- Review the [FAQ](docs/faq.md)

## 🎯 Roadmap

- [ ] Enhanced Wear OS UI optimization
- [ ] GPS-based distance calculation as fallback
- [ ] Advanced BLE connection algorithms
- [ ] Battery optimization improvements
- [ ] Advanced analytics and reporting

---

**Made with ❤️ using .NET MAUI**
