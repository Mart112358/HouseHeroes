using HotChocolate;

namespace HouseHeroes.ApiService.GraphQL;

// Errors returned to clients, identified by extensions.code.
// NOT_FOUND also covers records in another family, so callers can't probe for them.
public static class GraphQLErrors
{
    public static GraphQLException NotRegistered() =>
        Create("Complete registration before using this operation.", "NOT_REGISTERED");

    public static GraphQLException Forbidden(string message) => Create(message, "FORBIDDEN");

    public static GraphQLException NotFound(string message) => Create(message, "NOT_FOUND");

    // Someone else changed the same records at the same time; retrying is safe
    public static GraphQLException Conflict() =>
        Create("Your family changed while saving. Please try again.", "CONFLICT");

    private static GraphQLException Create(string message, string code) =>
        new(ErrorBuilder.New().SetMessage(message).SetCode(code).Build());
}
