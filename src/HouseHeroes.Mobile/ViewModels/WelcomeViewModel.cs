using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HouseHeroes.Mobile.Services;
using System.Diagnostics;

namespace HouseHeroes.Mobile.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private string _welcomeMessage = string.Empty;

    [ObservableProperty]
    private string _familyName = string.Empty;

    [ObservableProperty]
    private string _familyCode = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public WelcomeViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        
        var displayName = _authService.GetUserDisplayName() ?? "there";
        WelcomeMessage = $"Hi {displayName}! Let's get you set up with your family's task management.";
    }

    [RelayCommand]
    private void SelectCreateFamily()
    {
        // Focus on family name entry or highlight the create family option
        Debug.WriteLine("Create family option selected");
    }

    [RelayCommand]
    private void SelectJoinFamily()
    {
        // Focus on family code entry or highlight the join family option  
        Debug.WriteLine("Join family option selected");
    }

    [RelayCommand]
    private async Task CreateFamilyAsync()
    {
        if (string.IsNullOrWhiteSpace(FamilyName))
        {
            StatusMessage = "Please enter a family name.";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Creating your family...";

        try
        {
            // TODO: Call GraphQL mutation to register new user with new family
            await Task.Delay(1000); // Simulate API call
            
            StatusMessage = "Family created successfully!";
            
            // Navigate to main app
            await Shell.Current.GoToAsync("//main");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating family: {ex.Message}";
            Debug.WriteLine($"Create family error: {ex}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task JoinFamilyAsync()
    {
        if (string.IsNullOrWhiteSpace(FamilyCode))
        {
            StatusMessage = "Please enter a family invitation code.";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Joining family...";

        try
        {
            // TODO: Call GraphQL mutation to register new user with existing family
            await Task.Delay(1000); // Simulate API call
            
            StatusMessage = "Successfully joined family!";
            
            // Navigate to main app
            await Shell.Current.GoToAsync("//main");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error joining family: {ex.Message}";
            Debug.WriteLine($"Join family error: {ex}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task SkipSetupAsync()
    {
        try
        {
            // Create user without family assignment (they can join later)
            // TODO: Call GraphQL mutation to register new user without family
            await Task.Delay(500); // Simulate API call
            
            // Navigate to main app
            await Shell.Current.GoToAsync("//main");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            Debug.WriteLine($"Skip setup error: {ex}");
        }
    }
}