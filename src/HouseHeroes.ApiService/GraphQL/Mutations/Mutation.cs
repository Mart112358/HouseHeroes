using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;

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
            Email = input.Email,
            PasswordHash = input.PasswordHash,
            FirstName = input.FirstName,
            LastName = input.LastName,
            Role = input.Role,
            FamilyId = input.FamilyId
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
}

// Input types
public record CreateFamilyInput(string Name);

public record CreateUserInput(
    string Email,
    string PasswordHash,
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