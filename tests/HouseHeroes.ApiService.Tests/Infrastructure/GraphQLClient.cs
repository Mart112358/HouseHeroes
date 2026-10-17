using System.Net.Http.Json;
using System.Text.Json;

namespace HouseHeroes.ApiService.Tests.Infrastructure;

public class GraphQLClient(HttpClient httpClient)
{
    // Executes a GraphQL operation as the given Entra user, or anonymously when null.
    public async Task<GraphQLResponse> ExecuteAsync(string query, object? variables = null, string? asEntraUserId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new { query, variables })
        };
        if (asEntraUserId != null)
            request.Headers.Add(TestAuthHandler.UserHeader, asEntraUserId);

        using var response = await httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        return new GraphQLResponse(JsonDocument.Parse(json).RootElement.Clone());
    }
}

public record GraphQLResponse(JsonElement Root)
{
    public JsonElement Data => Root.GetProperty("data");

    public IReadOnlyList<JsonElement> Errors =>
        Root.TryGetProperty("errors", out var errors) ? errors.EnumerateArray().ToList() : [];

    public IReadOnlyList<string> ErrorCodes =>
        Errors
            .Select(e => e.TryGetProperty("extensions", out var ext) && ext.TryGetProperty("code", out var code)
                ? code.GetString() ?? ""
                : "")
            .ToList();

    public GraphQLResponse AssertNoErrors()
    {
        Assert.True(Errors.Count == 0, $"Expected no errors, got: {Root}");
        return this;
    }

    public GraphQLResponse AssertErrorCode(string code)
    {
        Assert.True(ErrorCodes.Contains(code), $"Expected error code {code}, got: {Root}");
        return this;
    }
}
