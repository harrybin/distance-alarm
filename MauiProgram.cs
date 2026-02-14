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
		builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
		builder.Services.AddSingleton<ILocationService, LocationService>();

#if ANDROID
		// Register Android-specific alarm service
		builder.Services.AddSingleton<IAlarmService, Platforms.Android.AndroidAlarmService>();
		builder.Services.AddSingleton<IBackgroundService, Platforms.Android.AndroidBackgroundService>();
#else
		// Fallback for other platforms
		builder.Services.AddSingleton<IAlarmService, AlarmService>();
#endif

		// Register ViewModels
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Register Pages
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

		return builder.Build();
	}
}
