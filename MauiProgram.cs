using Microsoft.Extensions.Logging;
using DistanceAlarm.Services;
using DistanceAlarm.ViewModels;
using DistanceAlarm.Views;

namespace DistanceAlarm;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register Services
		builder.Services.AddSingleton<ILocationService, LocationService>();

#if ANDROID
		// Register Android-specific alarm service
		builder.Services.AddSingleton<IAlarmService, Platforms.Android.AndroidAlarmService>();
		builder.Services.AddSingleton<IBackgroundService, Platforms.Android.AndroidBackgroundService>();
#else
		// Fallback for other platforms
		builder.Services.AddSingleton<IAlarmService, AlarmService>();
#endif

#if WEAR_OS
		// Wear OS: the watch acts as a BLE Peripheral (GATT Server + Advertising).
		// The phone (companion app) is the BLE Central that connects to the watch.
		// IBluetoothService (Plugin.BLE Central) is NOT needed on the watch side.
		builder.Services.AddSingleton<IWearOsPeripheralService, Platforms.Android.WearOsBlePeripheralService>();
		builder.Services.AddSingleton<WearOsViewModel>();
		builder.Services.AddSingleton<WearOsMainPage>();
#else
		// Phone (companion app): BLE Central that scans for and connects to the watch.
		builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
		// Phone (companion app): register full UI with device management and settings
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<SettingsPage>();
#endif

#if DEBUG
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

		return builder.Build();
	}
}
