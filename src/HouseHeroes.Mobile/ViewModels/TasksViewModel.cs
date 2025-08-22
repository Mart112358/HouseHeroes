using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HouseHeroes.Mobile.ViewModels;

public partial class TasksViewModel : BaseViewModel
{
    private readonly IHouseHeroesClient _client;

    [ObservableProperty] private ObservableCollection<IGetTasks_Tasks> _tasks = [];

    [ObservableProperty] private bool _isRefreshing;

    [ObservableProperty] private Guid? _familyId;

    public TasksViewModel(IHouseHeroesClient client)
    {
        _client = client;
        Title = "Tasks";
    }

    [RelayCommand]
    private async Task GetTasksAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Tasks.Clear();

            var result = await _client.GetTasks.ExecuteAsync();

            if (result.Data?.Tasks != null)
            {
                foreach (var task in result.Data.Tasks)
                {
                    Tasks.Add(task);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Unable to get tasks: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task GetTasksRefreshAsync()
    {
        IsRefreshing = true;
        await GetTasksAsync();
    }

    [RelayCommand]
    private async Task CompleteTaskAsync(Guid taskId)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var result = await _client.CompleteTask.ExecuteAsync(taskId);

            if (result.Data?.CompleteTask != null)
            {
                await GetTasksAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Unable to complete task: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}