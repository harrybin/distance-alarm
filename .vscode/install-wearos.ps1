# Install APK on Wear OS device via ADB
# Automatically detects Wear OS devices and installs the app

Write-Host "Checking for connected Wear OS devices..." -ForegroundColor Cyan

# Get all connected devices
$devices = adb devices -l

# Filter for Wear OS devices (look for wear, watch, or specific device ID)
$wearDevices = $devices | Where-Object { 
    $_ -like "*wear*" -or 
    $_ -like "*watch*" -or 
    $_ -like "*OPWWE251*" 
}

if ($wearDevices) {
    # Get the first Wear OS device found
    $wearDevice = $wearDevices | Select-Object -First 1
    $deviceId = ($wearDevice -split '\s+')[0]
    
    # Skip the header line
    if ($deviceId -and $deviceId -ne "List") {
        Write-Host "Found Wear OS device: $deviceId" -ForegroundColor Green
        Write-Host "Installing APK..." -ForegroundColor Yellow
        
        # Install the APK
        $result = adb -s $deviceId install -r "bin\Debug\net9.0-android\com.distancealarm.app-Signed.apk"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Successfully installed on Wear OS device: $deviceId" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to install on device: $deviceId" -ForegroundColor Red
            Write-Host "Error: $result" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ No valid Wear OS device found in device list" -ForegroundColor Red
    }
} else {
    Write-Host "❌ No Wear OS device found" -ForegroundColor Red
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "  1. Your Wear OS device is connected" -ForegroundColor Yellow
    Write-Host "  2. ADB debugging is enabled on the device" -ForegroundColor Yellow
    Write-Host "  3. The device is authorized (check device screen)" -ForegroundColor Yellow
    Write-Host "" -ForegroundColor Yellow
    Write-Host "Run 'Check Devices' task to verify connected devices." -ForegroundColor Cyan
}