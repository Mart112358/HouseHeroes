using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CommunityToolkit.Maui;
using HouseHeroes.Mobile.ViewModels;
using HouseHeroes.Mobile.Services;
using HouseHeroes.Mobile.Configuration;
using HouseHeroes.Mobile.Views;
using System.Reflection;

namespace HouseHeroes.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Add configuration
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("HouseHeroes.Mobile.appsettings.json");
		if (stream != null)
		{
			var config = new ConfigurationBuilder()
				.AddJsonStream(stream)
				.Build();
			builder.Configuration.AddConfiguration(config);
		}

		// Configure authentication
		builder.Services.Configure<AuthenticationConfig>(
			builder.Configuration.GetSection(AuthenticationConfig.SectionName));
		builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

		// Configure GraphQL client
		var graphqlEndpoint = builder.Configuration["ApiSettings:GraphQLEndpoint"]!;
		builder.Services
			.AddHouseHeroesClient()
			.ConfigureHttpClient(client => client.BaseAddress = new Uri(graphqlEndpoint));

		builder.Services.AddSingleton<FamiliesViewModel>();
		builder.Services.AddSingleton<TasksViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<WelcomeViewModel>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<WelcomePage>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
