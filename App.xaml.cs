namespace DistanceAlarm;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Microsoft.Maui.Controls.Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
	{
#if WEAR_OS
		// Wear OS: minimal single-page shell — no TabBar, no NavBar
		return new Microsoft.Maui.Controls.Window(new WearOsShell());
#else
		// Phone (companion app): full TabBar navigation with settings
		return new Microsoft.Maui.Controls.Window(new AppShell());
#endif
	}

	protected override void OnStart()
	{
		base.OnStart();
		System.Diagnostics.Debug.WriteLine("App OnStart called");
	}

	protected override void OnSleep()
	{
		base.OnSleep();
		System.Diagnostics.Debug.WriteLine("App OnSleep called");
	}

	protected override void OnResume()
	{
		base.OnResume();
		System.Diagnostics.Debug.WriteLine("App OnResume called");
	}
}