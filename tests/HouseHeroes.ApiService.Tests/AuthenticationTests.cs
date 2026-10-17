using System.Net;
using HouseHeroes.ApiService.Tests.Infrastructure;

namespace HouseHeroes.ApiService.Tests;

public class AuthenticationTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async System.Threading.Tasks.Task Anonymous_callers_cannot_query_tasks()
    {
        var response = await GraphQL.ExecuteAsync("{ myTasks { id } }");

        response.AssertErrorCode("AUTH_NOT_AUTHENTICATED");
    }

    [Fact]
    public async System.Threading.Tasks.Task Anonymous_callers_cannot_create_tasks()
    {
        var response = await GraphQL.ExecuteAsync("""mutation { createTask(input: { title: "Sneaky" }) { id } }""");

        response.AssertErrorCode("AUTH_NOT_AUTHENTICATED");
    }

    [Fact]
    public async System.Threading.Tasks.Task All_tasks_query_is_gone()
    {
        var response = await GraphQL.ExecuteAsync("{ allTasks { id } }");

        AssertFieldDoesNotExist(response, "allTasks");
    }

    [Fact]
    public async System.Threading.Tasks.Task Anonymous_rest_tasks_endpoint_is_gone()
    {
        var response = await Http.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("createFamily")]
    [InlineData("createUser")]
    public async System.Threading.Tasks.Task Unscoped_create_mutations_are_gone(string mutation)
    {
        var response = await GraphQL.ExecuteAsync($"mutation {{ {mutation} {{ id }} }}");

        AssertFieldDoesNotExist(response, mutation);
    }

    [Fact]
    public async System.Threading.Tasks.Task Signed_in_users_who_have_not_registered_cannot_create_tasks()
    {
        var response = await GraphQL.ExecuteAsync(
            """mutation { createTask(input: { title: "Early" }) { id } }""",
            asEntraUserId: $"entra-unregistered-{Guid.NewGuid()}");

        response.AssertErrorCode("NOT_REGISTERED");
    }

    private static void AssertFieldDoesNotExist(GraphQLResponse response, string field)
    {
        Assert.Contains(response.Errors, e => e.GetProperty("message").GetString()!.Contains($"`{field}` does not exist"));
    }
}
