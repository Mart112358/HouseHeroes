using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HouseHeroes.Mobile.Services;
using Microsoft.Identity.Client;
using System.Diagnostics;

namespace HouseHeroes.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private bool _isSigningIn;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isSignedIn;

    [ObservableProperty]
    private string _userDisplayName = string.Empty;

    public LoginViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        
        // Initialize state
        UpdateAuthenticationState();
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        IsSigningIn = true;
        StatusMessage = "Signing in...";

        try
        {
            var result = await _authService.SignInAsync();
            
            if (result != null)
            {
                StatusMessage = "Signed in successfully!";
                UpdateAuthenticationState();
                
                // Navigate to main app (this would typically be handled by the navigation service)
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                StatusMessage = "Sign in was cancelled or failed.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sign in failed: {ex.Message}";
            Debug.WriteLine($"Sign in error: {ex}");
        }
        finally
        {
            IsSigningIn = false;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        try
        {
            await _authService.SignOutAsync();
            StatusMessage = "Signed out successfully.";
            UpdateAuthenticationState();
            
            // Stay on login page or navigate as needed
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sign out failed: {ex.Message}";
            Debug.WriteLine($"Sign out error: {ex}");
        }
    }

    private void OnAuthenticationStateChanged(object? sender, AuthenticationStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateAuthenticationState();
        });
    }

    private void UpdateAuthenticationState()
    {
        IsSignedIn = _authService.IsUserAuthenticated;
        UserDisplayName = _authService.GetUserDisplayName() ?? "Unknown User";
        
        if (IsSignedIn)
        {
            StatusMessage = $"Welcome, {UserDisplayName}!";
        }
    }
}