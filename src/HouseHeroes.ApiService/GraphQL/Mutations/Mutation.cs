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

        var assigneeIds = await GetFamilyMemberIdsAsync(context, user.FamilyId, input.AssigneeIds ?? []);

        var task = new Models.Task
        {
            Id = Guid.NewGuid(),
            FamilyId = user.FamilyId,
            Title = input.Title,
            Description = input.Description,
            CreatedById = user.Id,
            DueDate = input.DueDate,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false,
            TaskAssignments = assigneeIds.Select(id => new TaskAssignment { UserId = id }).ToList()
        };

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        return task;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<Models.Task> UpdateTask(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        UpdateTaskInput input)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);
        user.EnsureGuardian();

        var task = await context.Tasks
            .Include(t => t.TaskAssignments).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == input.TaskId && t.FamilyId == user.FamilyId);
        if (task == null)
            throw GraphQLErrors.NotFound("Task not found.");

        task.Title = input.Title;
        task.Description = input.Description;
        task.DueDate = input.DueDate;

        // Null leaves the assignees as they are; a list replaces them
        if (input.AssigneeIds != null)
        {
            var assigneeIds = await GetFamilyMemberIdsAsync(context, user.FamilyId, input.AssigneeIds);
            context.TaskAssignments.RemoveRange(task.TaskAssignments.Where(a => !assigneeIds.Contains(a.UserId)));
            foreach (var id in assigneeIds.Where(id => task.TaskAssignments.All(a => a.UserId != id)))
                task.TaskAssignments.Add(new TaskAssignment { TaskId = task.Id, UserId = id });
        }

        await context.SaveChangesAsync();

        return task;
    }

    // The distinct IDs, after checking they are all members of the family. Loads the users
    // so new assignments returned by the mutation carry their user.
    private static async System.Threading.Tasks.Task<List<Guid>> GetFamilyMemberIdsAsync(
        AppDbContext context,
        Guid familyId,
        IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if ((await context.Users.Where(u => ids.Contains(u.Id) && u.FamilyId == familyId).ToListAsync()).Count != ids.Count)
            throw GraphQLErrors.NotFound("Family member not found.");

        return ids;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<Models.Task> CompleteTask(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid taskId)
    {
        var task = await FindTaskToCompleteAsync(context, userService, claimsPrincipal, taskId);

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return task;
    }

    [Authorize]
    public async System.Threading.Tasks.Task<Models.Task> ReopenTask(
        AppDbContext context,
        [Service] IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid taskId)
    {
        var task = await FindTaskToCompleteAsync(context, userService, claimsPrincipal, taskId);

        task.IsCompleted = false;
        task.CompletedAt = null;

        await context.SaveChangesAsync();

        return task;
    }

    // A task the caller may complete or reopen: Guardians any family task, Children only their own
    private static async System.Threading.Tasks.Task<Models.Task> FindTaskToCompleteAsync(
        AppDbContext context,
        IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        Guid taskId)
    {
        var user = await userService.GetRegisteredUserAsync(claimsPrincipal);

        var task = await context.Tasks
            .Include(t => t.TaskAssignments)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == user.FamilyId);
        if (task == null)
            throw GraphQLErrors.NotFound("Task not found.");

        if (user.Role != UserRole.Guardian && task.TaskAssignments.All(ta => ta.UserId != user.Id))
            throw GraphQLErrors.Forbidden("You can only complete or reopen tasks assigned to you.");

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
            return AlreadyRegistered(existingUser);

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
            // Conflicts with a concurrent deleteMyAccount by the family's last member
            family.MembershipVersion++;
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
        catch (DbUpdateException)
        {
            // A concurrent request (e.g. a double tap) registered this user first: EntraUserId is unique
            context.ChangeTracker.Clear();
            existingUser = await userService.GetCurrentUserAsync(claimsPrincipal);
            if (existingUser == null)
                throw;

            return AlreadyRegistered(existingUser);
        }

        return new RegisterNewUserResult 
        { 
            Success = true, 
            Message = "Registration completed successfully",
            User = user,
            Family = family
        };
    }

    private static RegisterNewUserResult AlreadyRegistered(User user) => new()
    {
        Success = false,
        Message = "User already registered",
        User = user
    };

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
    DateTime? DueDate,
    IReadOnlyList<Guid>? AssigneeIds);

public record UpdateTaskInput(
    Guid TaskId,
    string Title,
    string? Description,
    DateTime? DueDate,
    IReadOnlyList<Guid>? AssigneeIds);

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