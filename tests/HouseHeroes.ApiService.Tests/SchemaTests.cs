using HotChocolate.Execution;
using HouseHeroes.ApiService.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HouseHeroes.ApiService.Tests;

// The mobile app generates its typed GraphQL client from mobile/schema.graphql.
// This test keeps that file in sync with the API. To regenerate it:
//   UPDATE_SCHEMA=1 dotnet test tests/HouseHeroes.ApiService.Tests --filter SchemaTests
public class SchemaTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async System.Threading.Tasks.Task Mobile_schema_matches_the_api()
    {
        var executor = await Services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
        var schema = executor.Schema.ToString().ReplaceLineEndings("\n").TrimEnd() + "\n";
        var path = Path.Combine(FindRepoRoot(), "mobile", "schema.graphql");

        if (Environment.GetEnvironmentVariable("UPDATE_SCHEMA") == "1")
            await File.WriteAllTextAsync(path, schema);

        Assert.True(
            File.Exists(path) && (await File.ReadAllTextAsync(path)).ReplaceLineEndings("\n") == schema,
            "mobile/schema.graphql is out of date. Regenerate it with: UPDATE_SCHEMA=1 dotnet test tests/HouseHeroes.ApiService.Tests --filter SchemaTests");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "HouseHeroes.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (HouseHeroes.slnx) not found.");
    }
}
