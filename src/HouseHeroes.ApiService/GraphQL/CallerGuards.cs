using System.Security.Claims;
using HouseHeroes.ApiService.Authentication;
using HouseHeroes.ApiService.Models;

namespace HouseHeroes.ApiService.GraphQL;

public static class CallerGuards
{
    // The caller's user record, which carries their family and role.
    // Throws NOT_REGISTERED for signed-in users who haven't joined a family yet.
    public static async System.Threading.Tasks.Task<User> GetRegisteredUserAsync(
        this IUserService userService,
        ClaimsPrincipal claimsPrincipal)
    {
        var user = await userService.GetCurrentUserAsync(claimsPrincipal);
        if (user == null || user.FamilyId == Guid.Empty)
            throw GraphQLErrors.NotRegistered();

        return user;
    }

    public static void EnsureGuardian(this User user)
    {
        if (user.Role != UserRole.Guardian)
            throw GraphQLErrors.Forbidden("Only guardians can manage tasks.");
    }
}
