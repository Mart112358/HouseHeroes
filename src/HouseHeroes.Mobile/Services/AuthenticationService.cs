using Microsoft.Identity.Client;
using Microsoft.Extensions.Options;
using HouseHeroes.Mobile.Configuration;
using System.Diagnostics;

namespace HouseHeroes.Mobile.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;
    private readonly AuthenticationConfig _config;
    private AuthenticationResult? _lastAuthenticationResult;

    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

    public bool IsUserAuthenticated => _lastAuthenticationResult != null && !string.IsNullOrEmpty(_lastAuthenticationResult.AccessToken);

    public AuthenticationService(IOptions<AuthenticationConfig> config)
    {
        _config = config.Value;
        _scopes = _config.Scopes;

        try
        {
            _app = PublicClientApplicationBuilder
                .Create(_config.ClientId)
                .WithAuthority(_config.Authority)
                .WithRedirectUri(_config.RedirectUri)
#if ANDROID
                .WithParentActivityOrWindow(() => Platform.CurrentActivity)
#elif IOS
                .WithIosKeychainSecurityGroup("com.companyname.househeroes.mobile")
#endif
                .Build();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MSAL initialization error: {ex.Message}");
            // Fallback: try without keychain security group
            _app = PublicClientApplicationBuilder
                .Create(_config.ClientId)
                .WithAuthority(_config.Authority)
                .WithRedirectUri(_config.RedirectUri)
#if ANDROID
                .WithParentActivityOrWindow(() => Platform.CurrentActivity)
#endif
                .Build();
        }

        // Try to get cached token on startup
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await AcquireTokenSilentAsync();
                if (result != null)
                {
                    _lastAuthenticationResult = result;
                    OnAuthenticationStateChanged(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to acquire cached token: {ex.Message}");
            }
        });
    }

    public async Task<AuthenticationResult?> SignInAsync()
    {
        try
        {
            var accounts = await _app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            AuthenticationResult result;
            
            if (firstAccount != null)
            {
                // Try silent authentication first
                var silentResult = await AcquireTokenSilentAsync();
                if (silentResult != null)
                {
                    _lastAuthenticationResult = silentResult;
                    OnAuthenticationStateChanged(true);
                    return silentResult;
                }
            }

            // Interactive authentication (handles both sign-in and sign-up)
            result = await _app.AcquireTokenInteractive(_scopes)
#if ANDROID
                .WithParentActivityOrWindow(Platform.CurrentActivity)
#endif
                .WithPrompt(Prompt.SelectAccount) // Allow user to select account or create new one
                .ExecuteAsync();

            _lastAuthenticationResult = result;
            OnAuthenticationStateChanged(true);
            return result;
        }
        catch (MsalException msalEx)
        {
            Debug.WriteLine($"MSAL Exception: {msalEx.Message}");
            
            if (msalEx.ErrorCode == MsalError.AuthenticationCanceledError)
            {
                Debug.WriteLine("User cancelled authentication");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Authentication error: {ex.Message}");
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            var accounts = await _app.GetAccountsAsync();
            
            if (accounts.Any())
            {
                // Remove all accounts
                while (accounts.Any())
                {
                    await _app.RemoveAsync(accounts.First());
                    accounts = await _app.GetAccountsAsync();
                }
            }

            _lastAuthenticationResult = null;
            OnAuthenticationStateChanged(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Sign out error: {ex.Message}");
        }
    }

    public async Task<AuthenticationResult?> AcquireTokenSilentAsync()
    {
        try
        {
            var accounts = await _app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            if (firstAccount == null)
                return null;

            var result = await _app.AcquireTokenSilent(_scopes, firstAccount)
                .ExecuteAsync();

            _lastAuthenticationResult = result;
            return result;
        }
        catch (MsalUiRequiredException)
        {
            // Silent token acquisition failed, user interaction required
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Silent token acquisition error: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            // Try to get a fresh token silently
            var result = await AcquireTokenSilentAsync();
            
            if (result != null)
            {
                _lastAuthenticationResult = result;
                return result.AccessToken;
            }

            // If silent acquisition fails and we have a cached token, return it
            // (it might still be valid for a short time)
            return _lastAuthenticationResult?.AccessToken;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Get access token error: {ex.Message}");
            return null;
        }
    }

    public string? GetUserDisplayName()
    {
        if (_lastAuthenticationResult?.Account == null)
            return null;

        return _lastAuthenticationResult.Account.Username ?? 
               $"{_lastAuthenticationResult.ClaimsPrincipal.FindFirst("given_name")?.Value} {_lastAuthenticationResult.ClaimsPrincipal.FindFirst("family_name")?.Value}".Trim();
    }

    public string? GetUserEmail()
    {
        return _lastAuthenticationResult?.Account?.Username ?? 
               _lastAuthenticationResult?.ClaimsPrincipal.FindFirst("email")?.Value;
    }

    private void OnAuthenticationStateChanged(bool isAuthenticated)
    {
        AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs
        {
            IsAuthenticated = isAuthenticated,
            UserDisplayName = isAuthenticated ? GetUserDisplayName() : null,
            UserEmail = isAuthenticated ? GetUserEmail() : null
        });
    }
}