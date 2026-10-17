namespace HouseHeroes.ApiService.Tests.Infrastructure;

[Collection("api")]
public abstract class ApiTestBase(ApiFactory factory)
{
    protected HttpClient Http { get; } = factory.CreateClient();
    protected GraphQLClient GraphQL => new(Http);
    protected TestData Data { get; } = new(factory.Services);
}
