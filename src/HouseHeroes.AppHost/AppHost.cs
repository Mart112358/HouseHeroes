var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", secret: true);
var sqlServer = builder.AddSqlServer("sqlserver", password);

var houseHeroesDb = sqlServer.AddDatabase("househeroes");

builder.AddProject<Projects.HouseHeroes_ApiService>("apiservice")
    .WithReference(houseHeroesDb)
    .WaitFor(sqlServer)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
