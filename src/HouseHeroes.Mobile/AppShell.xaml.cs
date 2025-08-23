using HouseHeroes.Mobile.Services;

namespace HouseHeroes.Mobile;

public partial class AppShell : Shell
{
	private readonly IAuthenticationService _authService;

	public AppShell(IAuthenticationService authService)
	{
		_authService = authService;
		InitializeComponent();
		
		// Subscribe to authentication state changes
		_authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
		
		// Set initial visibility after the shell is loaded
		Loaded += OnShellLoaded;
	}

	private async void OnShellLoaded(object? sender, EventArgs e)
	{
		// Remove the event handler to avoid multiple calls
		Loaded -= OnShellLoaded;
		
		// Set initial visibility based on authentication state
		await Task.Delay(50); // Small delay to ensure initialization
		UpdateVisibility(_authService?.IsUserAuthenticated == true);
	}

	private void UpdateVisibility(bool isAuthenticated)
	{
		try
		{
			// Find and update shell items visibility
			foreach (var item in Items)
			{
				if (item.Route == "login")
					item.IsVisible = !isAuthenticated;
				else if (item.Route == "main")
					item.IsVisible = isAuthenticated;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Visibility update error: {ex.Message}");
		}
	}

	private void OnAuthenticationStateChanged(object? sender, AuthenticationStateChangedEventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			UpdateVisibility(e.IsAuthenticated);
		});
	}
}
