namespace DistanceAlarm;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
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