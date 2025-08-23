using HouseHeroes.Mobile.ViewModels;

namespace HouseHeroes.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnContinueToMainClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main");
    }
}