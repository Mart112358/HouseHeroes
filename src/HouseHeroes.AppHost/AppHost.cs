var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.HouseHeroes_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");


builder.Build().Run();
