using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;
using HouseHeroes.ApiService.Authentication;
using HouseHeroes.ApiService.GraphQL;
using HotChocolate.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.GraphQL.Mutations;

public class Mutation
{
    [Authorize]
    public async System.Threading.Tasks.Task<Models.Task> CreateTask(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        CreateTaskInput input)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);
        user.EnsureGuardian();

        var task = new Models.Task
        {
            Id = Guid.NewGuid(),
            FamilyId = user.FamilyId,
            Title = input.Title,
            Description = input.Description,
            CreatedById = user.Id,
            DueDate = input.DueDate,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        return task;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<TaskAssignment> AssignTask(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        AssignTaskInput input)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);
        user.EnsureGuardian();

        if (!await context.Tasks.AnyAsync(t => t.Id == input.TaskId && t.FamilyId == user.FamilyId))
            throw GraphQLErrors.NotFound("Task not found.");

        if (!await context.Users.AnyAsync(u => u.Id == input.UserId && u.FamilyId == user.FamilyId))
            throw GraphQLErrors.NotFound("Family member not found.");

        var assignment = await context.TaskAssignments.FindAsync(input.TaskId, input.UserId);
        if (assignment != null)
            return assignment;

        assignment = new TaskAssignment
        {
            TaskId = input.TaskId,
            UserId = input.UserId
        };

        context.TaskAssignments.Add(assignment);
        await context.SaveChangesAsync();

        return assignment;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<Models.Task> CompleteTask(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid taskId)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);

        var task = await context.Tasks
            .Include(t => t.TaskAssignments)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == user.FamilyId);
        if (task == null)
            throw GraphQLErrors.NotFound("Task not found.");

        // Children can only complete tasks assigned to them
        if (user.Role != UserRole.Guardian && task.TaskAssignments.All(ta => ta.UserId != user.Id))
            throw GraphQLErrors.Forbidden("You can only complete tasks assigned to you.");

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return task;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<bool> DeleteTask(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid taskId)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);
        user.EnsureGuardian();

        var task = await context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == user.FamilyId);
        if (task == null)
            return false;

        context.Tasks.Remove(task);
        await context.SaveChangesAsync();

        return true;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<RegisterNewUserResult> RegisterNewUser(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        RegisterNewUserInput input)
    {
        // Check if user already exists
        var existingUser = await userService.GetCurrentUserAsync(claimsPrincipal);
        if (existingUser != null)
        {
            return new RegisterNewUserResult 
            { 
                Success = false, 
                Message = "User already registered",
                User = existingUser
            };
        }

        // Create a family, or join one with an invite
        Family family;
        FamilyInvite? invite = null;
        if (input.CreateNewFamily)
        {
            family = new Family
            {
                Id = Guid.NewGuid(),
                Name = input.FamilyName ?? $"{claimsPrincipal.GetFirstName()}'s Family",
                CreatedAt = DateTime.UtcNow
            };
            context.Families.Add(family);
        }
        else if (!string.IsNullOrWhiteSpace(input.InviteCode))
        {
            var code = input.InviteCode.Trim().ToUpperInvariant();
            invite = await context.FamilyInvites
                .FirstOrDefaultAsync(i => i.Code == code && i.UsedAt == null && i.ExpiresAt > DateTime.UtcNow);
            if (invite is null)
                return InvalidInvite();

            family = await context.Families.SingleAsync(f => f.Id == invite.FamilyId);
            invite.UsedAt = DateTime.UtcNow;
        }
        else
        {
            return new RegisterNewUserResult 
            { 
                Success = false, 
                Message = "Must either create a new family or join one with an invite code" 
            };
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            EntraUserId = claimsPrincipal.GetEntraUserId(),
            Email = claimsPrincipal.GetEmail(),
            FirstName = claimsPrincipal.GetFirstName(),
            LastName = claimsPrincipal.GetLastName(),
            Role = invite?.Role ?? UserRole.Guardian,
            FamilyId = family.Id,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else used the invite since we read it (UsedAt is a concurrency token)
            return InvalidInvite();
        }

        return new RegisterNewUserResult 
        { 
            Success = true, 
            Message = "Registration completed successfully",
            User = user,
            Family = family
        };
    }

    private static RegisterNewUserResult InvalidInvite() => new()
    {
        Success = false,
        Message = "This invite code is invalid, already used, or expired."
    };
}

// Input types
public record CreateTaskInput(
    string Title,
    string? Description,
    DateTime? DueDate);

public record AssignTaskInput(
    Guid TaskId,
    Guid UserId);

public record RegisterNewUserInput(
    bool CreateNewFamily,
    string? FamilyName,
    string? InviteCode);

public class RegisterNewUserResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public Family? Family { get; set; }
}