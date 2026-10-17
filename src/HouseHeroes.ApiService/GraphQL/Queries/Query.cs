using HotChocolate.Data;
using HotChocolate.Authorization;
using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;
using HouseHeroes.ApiService.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.GraphQL.Queries;

public class Query
{
    [Authorize]
    public async System.Threading.Tasks.Task<Family?> GetMyFamily(
        AppDbContext context, 
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal)
    {
        var user = await userService.GetCurrentUserAsync(claimsPrincipal);
        if (user == null || user.FamilyId == Guid.Empty)
            return null;

        return await context.Families
            .Include(f => f.Members)
            .Include(f => f.Tasks)
            .FirstOrDefaultAsync(f => f.Id == user.FamilyId);
    }

    [Authorize]
    public async System.Threading.Tasks.Task<IEnumerable<User>> GetFamilyMembers(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal)
    {
        var user = await userService.GetCurrentUserAsync(claimsPrincipal);
        if (user == null || user.FamilyId == Guid.Empty)
            return Enumerable.Empty<User>();

        return await context.Users
            .Where(u => u.FamilyId == user.FamilyId)
            .ToListAsync();
    }

    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async System.Threading.Tasks.Task<IQueryable<Models.Task>> GetMyTasks(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal)
    {
        var user = await userService.GetCurrentUserAsync(claimsPrincipal);
        if (user == null || user.FamilyId == Guid.Empty)
            return context.Tasks.Where(_ => false); // Return empty queryable

        return context.Tasks
            .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
            .Where(t => t.FamilyId == user.FamilyId);
    }

    [Authorize]
    public async System.Threading.Tasks.Task<User?> GetCurrentUser(
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal)
    {
        return await userService.GetCurrentUserAsync(claimsPrincipal);
    }

    [Authorize]
    public async System.Threading.Tasks.Task<Models.Task?> GetTaskById(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid id)
    {
        var user = await userService.GetCurrentUserAsync(claimsPrincipal);
        if (user == null || user.FamilyId == Guid.Empty)
            return null;

        return await context.Tasks
            .Include(t => t.TaskAssignments)
            .ThenInclude(ta => ta.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.FamilyId == user.FamilyId);
    }
}