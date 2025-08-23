using System.Security.Claims;

namespace HouseHeroes.ApiService.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string GetEntraUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value 
               ?? user.FindFirst("oid")?.Value 
               ?? throw new InvalidOperationException("User ID not found in claims");
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst("email")?.Value 
               ?? throw new InvalidOperationException("Email not found in claims");
    }

    public static string GetFirstName(this ClaimsPrincipal user)
    {
        return user.FindFirst("given_name")?.Value ?? "";
    }

    public static string GetLastName(this ClaimsPrincipal user)
    {
        return user.FindFirst("family_name")?.Value ?? "";
    }

    public static bool IsAuthenticated(this ClaimsPrincipal user)
    {
        return user?.Identity?.IsAuthenticated == true;
    }
}