using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;

namespace HouseHeroes.ApiService.Authentication;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<User?> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal)
    {
        if (!claimsPrincipal.IsAuthenticated())
            return null;

        var entraUserId = claimsPrincipal.GetEntraUserId();
        
        return await _context.Users
            .Include(u => u.Family)
            .FirstOrDefaultAsync(u => u.EntraUserId == entraUserId);
    }

    public async System.Threading.Tasks.Task<Guid> GetUserFamilyIdAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.FamilyId ?? Guid.Empty;
    }

    public async System.Threading.Tasks.Task UpdateLastLoginAsync(string entraUserId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EntraUserId == entraUserId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}