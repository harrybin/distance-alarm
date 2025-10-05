# 📱 APK Build System Documentation

## Overview

The Distance Alarm repository now provides multiple ways to get downloadable APK files for Android and Wear OS devices. This document explains the different workflows available and how to use them.

## 🚀 Available Workflows

### 1. 📱 Automatic APK Builds (`build-apks.yml`)

**Purpose**: Provides immediate access to development APKs for testing and installation.

**Triggers**:
- Every push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches  
- Manual trigger via GitHub Actions (with build type selection)

**Artifacts Generated**:
- `android-apks-[run_number]` - Contains Android APK and AAB files
- `wearos-apks-[run_number]` - Contains Wear OS APK files
- SHA256 checksums for all files

**File Naming**:
- `DistanceAlarm-android-[timestamp]-[commit].apk` - Android APK
- `DistanceAlarm-android-[timestamp]-[commit].aab` - Android AAB (Play Store)
- `DistanceAlarm-wearos-[timestamp]-[commit].apk` - Wear OS APK

### 2. 🚀 Release Builds (`release.yml`)

**Purpose**: Creates official releases with proper versioning for distribution.

**Triggers**: Manual trigger with approval required

**Features**:
- Manual approval workflow for security
- Semantic versioning support
- GitHub Releases creation
- Professional release notes generation
- Long-term artifact retention

## 📥 How to Download APKs

### For Development/Testing (Automatic Builds)

1. **Navigate to the [Actions tab](https://github.com/harrybin/distance-alarm/actions)**
2. **Click on "📱 Build APKs" workflow**
3. **Select the latest successful run** (green checkmark)
4. **Scroll down to the "Artifacts" section**
5. **Download the package you need**:
   - `android-apks-[number]` - For Android phones/tablets
   - `wearos-apks-[number]` - For Wear OS watches

### For Official Releases

1. **Navigate to the [Releases page](https://github.com/harrybin/distance-alarm/releases)**
2. **Find the release version you want**
3. **Download the files from the "Assets" section**:
   - `DistanceAlarm-v1.0.0-android.apk` - Android direct install
   - `DistanceAlarm-v1.0.0-android.aab` - Google Play Store bundle
   - `DistanceAlarm-v1.0.0-wearos.apk` - Wear OS optimized

## 📱 Installation Instructions

### Android Devices

1. **Download the APK file** from GitHub Actions artifacts or releases
2. **Extract the ZIP file** if downloading from artifacts
3. **Enable "Unknown Sources"** in your Android settings:
   - Go to Settings → Security → Unknown Sources (Android 7 and below)
   - Or Settings → Apps → Special access → Install unknown apps (Android 8+)
4. **Install the APK** by tapping on it

### Wear OS Devices

1. **Enable Developer Mode** on your Wear OS device:
   - Go to Settings → System → About → Build number (tap 7 times)
2. **Enable ADB debugging**:
   - Settings → Developer options → ADB debugging
3. **Connect via WiFi debugging** or USB (if supported)
4. **Install using ADB**:
   ```bash
   adb devices  # Verify your device is connected
   adb install DistanceAlarm-wearos-*.apk
   ```

## 🛡️ Security & Verification

### Checksum Verification

All APK packages include SHA256 checksum files:

```bash
# Verify file integrity
sha256sum DistanceAlarm-android-*.apk
# Compare with checksums-android.txt or checksums-all.txt
```

### Code Signing

- **Development builds**: Self-signed certificates (for testing only)
- **Release builds**: Can be configured with production certificates

## 🔧 Build Configuration

### Manual Workflow Triggers

The `build-apks.yml` workflow can be triggered manually with options:

- **both** (default) - Build Android and Wear OS APKs
- **android-only** - Build only Android APK and AAB
- **wearos-only** - Build only Wear OS APK

### Build Environments

- **Platform**: Ubuntu latest
- **.NET Version**: 9.0.x
- **Android SDK**: API level 34
- **Build Tools**: 34.0.0

## 📊 Artifact Retention

- **Development builds**: 30 days retention
- **CI artifacts**: 7 days retention  
- **Release builds**: Permanent (via GitHub Releases)

## 🚨 Troubleshooting

### Build Failures

1. **Check workflow logs** in GitHub Actions
2. **Verify .NET and Android SDK versions**
3. **Ensure all NuGet packages are restored**

### Installation Issues

1. **Android**: Verify "Unknown Sources" is enabled
2. **Wear OS**: Ensure Developer Mode and ADB debugging are enabled
3. **Architecture**: Wear OS devices typically use ARM (`armeabi-v7a`)

### APK Size

- **Android APK**: ~35 MB (includes all dependencies)
- **Wear OS APK**: ~35 MB (optimized for Wear OS)
- **Android AAB**: ~36 MB (Play Store bundle format)

## 🔄 CI/CD Integration

The build system integrates with the existing CI/CD pipeline:

- **`ci.yml`**: Validates code and runs tests
- **`build-apks.yml`**: Creates downloadable APKs
- **`release.yml`**: Creates official releases

All workflows run in parallel and provide comprehensive feedback through GitHub's status checks and summaries.