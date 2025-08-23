using System.Security.Claims;
using HouseHeroes.ApiService.Models;

namespace HouseHeroes.ApiService.Authentication;

public interface IUserService
{
    System.Threading.Tasks.Task<User?> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal);
    System.Threading.Tasks.Task<User> GetOrCreateUserAsync(ClaimsPrincipal claimsPrincipal);
    System.Threading.Tasks.Task<Guid> GetUserFamilyIdAsync(Guid userId);
    System.Threading.Tasks.Task UpdateLastLoginAsync(string entraUserId);
}