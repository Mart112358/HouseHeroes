using Microsoft.Extensions.DependencyInjection;

namespace HouseHeroes.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var appShell = Handler?.MauiContext?.Services.GetRequiredService<AppShell>();
		return new Window(appShell ?? throw new InvalidOperationException("AppShell not registered"));
	}
}