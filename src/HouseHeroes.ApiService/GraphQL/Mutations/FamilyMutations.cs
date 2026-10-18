using System.Security.Claims;
using System.Security.Cryptography;
using HotChocolate.Authorization;
using HouseHeroes.ApiService.Authentication;
using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.GraphQL.Mutations;

// Invites, member management, and deleting your own account.
[ExtendObjectType(OperationTypeNames.Mutation)]
public class FamilyMutations
{
    // No 0/O or 1/I, so codes are easy to read out and type
    private const string InviteCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int InviteCodeLength = 8;
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    [Authorize]
    public async System.Threading.Tasks.Task<FamilyInvite> CreateFamilyInvite(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        UserRole role)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);
        user.EnsureGuardian();

        var invite = new FamilyInvite
        {
            Id = Guid.NewGuid(),
            FamilyId = user.FamilyId,
            Code = RandomNumberGenerator.GetString(InviteCodeAlphabet, InviteCodeLength),
            Role = role,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(InviteLifetime)
        };

        context.FamilyInvites.Add(invite);
        await context.SaveChangesAsync();

        return invite;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<User> UpdateFamilyMember(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        UpdateFamilyMemberInput input)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);
        user.EnsureGuardian();

        var member = await context.Users.FirstOrDefaultAsync(u => u.Id == input.UserId && u.FamilyId == user.FamilyId);
        if (member == null)
            throw GraphQLErrors.NotFound("Family member not found.");

        if (member.Role == UserRole.Guardian && input.Role != UserRole.Guardian &&
            !await context.Users.AnyAsync(u => u.FamilyId == user.FamilyId && u.Role == UserRole.Guardian && u.Id != member.Id))
            throw GraphQLErrors.Forbidden("A family needs at least one guardian.");

        member.FirstName = input.FirstName;
        member.LastName = input.LastName;
        member.Role = input.Role;

        await context.SaveChangesAsync();

        return member;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<bool> RemoveFamilyMember(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid userId)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);
        user.EnsureGuardian();

        // The remover always stays, so the family keeps at least one guardian
        if (userId == user.Id)
            throw GraphQLErrors.Forbidden("You can't remove yourself from the family.");

        var member = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.FamilyId == user.FamilyId);
        if (member == null)
            return false;

        await DeleteMemberAsync(context, member, inheritorId: user.Id);

        return true;
    }

    // Deletes the caller's account (required by the app stores). The last member takes
    // the family with them; the last guardian of a family with other members can't leave.
    [Authorize]
    public async System.Threading.Tasks.Task<bool> DeleteMyAccount(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);

        var otherMembers = await context.Users.Where(u => u.FamilyId == user.FamilyId && u.Id != user.Id).ToListAsync();
        if (otherMembers.Count == 0)
        {
            await DeleteFamilyAsync(context, user);
            return true;
        }

        // Families always keep a guardian, so a child leaving always finds one
        var inheritor = otherMembers.FirstOrDefault(m => m.Role == UserRole.Guardian);
        if (inheritor == null)
            throw GraphQLErrors.Forbidden("Make another member a parent before deleting your account.");

        await DeleteMemberAsync(context, user, inheritor.Id);

        return true;
    }

    // Deletes a member, handing the tasks and invites they created to another member
    private static async System.Threading.Tasks.Task DeleteMemberAsync(AppDbContext context, User member, Guid inheritorId)
    {
        foreach (var task in await context.Tasks.Where(t => t.CreatedById == member.Id).ToListAsync())
            task.CreatedById = inheritorId;
        foreach (var invite in await context.FamilyInvites.Where(i => i.CreatedById == member.Id).ToListAsync())
            invite.CreatedById = inheritorId;

        context.TaskAssignments.RemoveRange(await context.TaskAssignments.Where(a => a.UserId == member.Id).ToListAsync());
        context.Users.Remove(member);
        await context.SaveChangesAsync();
    }

    // Deletes a family whose only member is `lastMember`, with everything in it. Deletes are explicit
    // (tasks and invites reference the member with NO ACTION keys), in one save so EF orders them.
    private static async System.Threading.Tasks.Task DeleteFamilyAsync(AppDbContext context, User lastMember)
    {
        context.TaskAssignments.RemoveRange(await context.TaskAssignments.Where(a => a.Task.FamilyId == lastMember.FamilyId).ToListAsync());
        context.Tasks.RemoveRange(await context.Tasks.Where(t => t.FamilyId == lastMember.FamilyId).ToListAsync());
        context.FamilyInvites.RemoveRange(await context.FamilyInvites.Where(i => i.FamilyId == lastMember.FamilyId).ToListAsync());
        context.Users.Remove(lastMember);
        context.Families.Remove(await context.Families.SingleAsync(f => f.Id == lastMember.FamilyId));
        await context.SaveChangesAsync();
    }
}

public record UpdateFamilyMemberInput(
    Guid UserId,
    string FirstName,
    string LastName,
    UserRole Role);
