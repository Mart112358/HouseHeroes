using HouseHeroes.Mobile.ViewModels;

namespace HouseHeroes.Mobile;

public partial class MainPage : ContentPage
{
	private readonly TasksViewModel _viewModel;

	public MainPage(TasksViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.GetTasksCommand.ExecuteAsync(null);
	}

	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await _viewModel.GetTasksCommand.ExecuteAsync(null);
	}
}
