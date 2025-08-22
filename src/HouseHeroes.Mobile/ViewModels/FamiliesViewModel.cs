using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HouseHeroes.Mobile.ViewModels;

public partial class FamiliesViewModel : BaseViewModel
{
    private readonly IHouseHeroesClient _client;

    [ObservableProperty] private ObservableCollection<IGetFamilies_Families> _families = [];

    [ObservableProperty] private bool _isRefreshing;

    public FamiliesViewModel(IHouseHeroesClient client)
    {
        _client = client;
        Title = "Families";
    }

    [RelayCommand]
    private async Task GetFamiliesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Families.Clear();

            var result = await _client.GetFamilies.ExecuteAsync();

            if (result.Data?.Families != null)
            {
                foreach (var family in result.Data.Families)
                {
                    Families.Add(family);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Unable to get families: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task GetFamiliesRefreshAsync()
    {
        IsRefreshing = true;
        await GetFamiliesAsync();
    }
}