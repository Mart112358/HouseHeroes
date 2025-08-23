using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;
using HouseHeroes.ApiService.Authentication;
using HotChocolate.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace HouseHeroes.ApiService.GraphQL.Mutations;

public class Mutation
{
    public async Task<Family> CreateFamily(AppDbContext context, CreateFamilyInput input)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            CreatedAt = DateTime.UtcNow
        };

        context.Families.Add(family);
        await context.SaveChangesAsync();

        return family;
    }

    public async Task<User> CreateUser(AppDbContext context, CreateUserInput input)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            EntraUserId = input.EntraUserId,
            Email = input.Email,
            FirstName = input.FirstName,
            LastName = input.LastName,
            Role = input.Role,
            FamilyId = input.FamilyId,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<Models.Task> CreateTask(AppDbContext context, CreateTaskInput input)
    {
        var task = new Models.Task
        {
            Id = Guid.NewGuid(),
            FamilyId = input.FamilyId,
            Title = input.Title,
            Description = input.Description,
            CreatedById = input.CreatedById,
            DueDate = input.DueDate,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        return task;
    }

    public async Task<TaskAssignment> AssignTask(AppDbContext context, AssignTaskInput input)
    {
        var assignment = new TaskAssignment
        {
            TaskId = input.TaskId,
            UserId = input.UserId
        };

        context.TaskAssignments.Add(assignment);
        await context.SaveChangesAsync();

        return assignment;
    }

    public async Task<Models.Task> CompleteTask(AppDbContext context, Guid taskId)
    {
        var task = await context.Tasks.FindAsync(taskId);
        if (task == null)
            throw new ArgumentException("Task not found");

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return task;
    }

    public async Task<bool> DeleteTask(AppDbContext context, Guid taskId)
    {
        var task = await context.Tasks.FindAsync(taskId);
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

        // Create or get family
        Family family;
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
        else if (input.JoinFamilyId.HasValue)
        {
            family = await context.Families.FindAsync(input.JoinFamilyId.Value);
            if (family is null)
            {
                return new RegisterNewUserResult 
                { 
                    Success = false, 
                    Message = "Family not found" 
                };
            }
        }
        else
        {
            return new RegisterNewUserResult 
            { 
                Success = false, 
                Message = "Must either create new family or join existing family" 
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
            Role = input.CreateNewFamily ? UserRole.Guardian : UserRole.Child,
            FamilyId = family.Id,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return new RegisterNewUserResult 
        { 
            Success = true, 
            Message = "Registration completed successfully",
            User = user,
            Family = family
        };
    }
}

// Input types
public record CreateFamilyInput(string Name);

public record CreateUserInput(
    string EntraUserId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid FamilyId);

public record CreateTaskInput(
    Guid FamilyId,
    string Title,
    string? Description,
    Guid CreatedById,
    DateTime? DueDate);

public record AssignTaskInput(
    Guid TaskId,
    Guid UserId);

public record RegisterNewUserInput(
    bool CreateNewFamily,
    string? FamilyName,
    Guid? JoinFamilyId);

public class RegisterNewUserResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public Family? Family { get; set; }
}