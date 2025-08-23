using Microsoft.Identity.Client;

namespace HouseHeroes.Mobile.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResult?> SignInAsync();
    Task SignOutAsync();
    Task<AuthenticationResult?> AcquireTokenSilentAsync();
    Task<string?> GetAccessTokenAsync();
    bool IsUserAuthenticated { get; }
    string? GetUserDisplayName();
    string? GetUserEmail();
    event EventHandler<AuthenticationStateChangedEventArgs> AuthenticationStateChanged;
}

public class AuthenticationStateChangedEventArgs : EventArgs
{
    public bool IsAuthenticated { get; set; }
    public string? UserDisplayName { get; set; }
    public string? UserEmail { get; set; }
}