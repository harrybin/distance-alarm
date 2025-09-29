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
		builder.Services.AddSingleton<IAlarmService, AlarmService>();

		// Register ViewModels
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Register Pages
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
