# VSCode Tasks for Distance Alarm

This directory contains VSCode configuration files for building and deploying the Distance Alarm .NET MAUI application.

## Available Tasks

### Build Tasks
- **Build for Android**: Compile the app for Android devices in Debug mode
- **Build for Android Release**: Compile the app for Android devices in Release mode
- **Clean Build Output**: Clean all build artifacts
- **Restore NuGet Packages**: Restore project dependencies

### Deployment Tasks
- **Install on Android Device**: Build and install directly on connected Android device
- **Install on Wear OS Device**: Build and install directly on connected Wear OS device
- **Deploy APK to Android Device**: Create APK for Android devices
- **Deploy APK to Wear OS Device**: Create APK optimized for Wear OS devices

### ADB Tasks
- **Check Connected Devices**: List all connected Android/Wear OS devices
- **Install APK via ADB (Android)**: Install pre-built APK on Android device
- **Install APK via ADB (Wear OS)**: Install pre-built APK on Wear OS device

## Prerequisites

1. **Android SDK**: Ensure ANDROID_HOME environment variable is set
2. **ADB**: Android Debug Bridge must be available in PATH
3. **Connected Device**: Have your Android or Wear OS device connected via USB or WiFi debugging
4. **Developer Options**: Enable Developer Options and USB Debugging on your device

## Usage

1. Open Command Palette (`Ctrl+Shift+P`)
2. Type "Tasks: Run Task"
3. Select the desired task from the list

## Device Detection

For Wear OS devices, ensure:
- Wear OS device is connected and recognized by ADB
- Developer options are enabled on the watch
- ADB debugging is enabled
- The watch is paired with your development phone (if required)

## Troubleshooting

- Run "Check Connected Devices" task to verify device connectivity
- Ensure all environment variables (ANDROID_HOME, JAVA_HOME) are properly set
- Check that the device is authorized for debugging (check device screen for authorization prompt)