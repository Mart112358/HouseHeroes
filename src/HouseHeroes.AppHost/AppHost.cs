var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    // .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb();

var houseHeroesDb = postgres.AddDatabase("househeroes");

builder.AddProject<Projects.HouseHeroes_ApiService>("apiservice")
    .WithReference(houseHeroesDb)
    .WaitFor(postgres)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
