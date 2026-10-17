using HouseHeroes.ApiService.Data;
using HouseHeroes.ApiService.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HouseHeroes.ApiService.Tests.Infrastructure;

public record TestFamily(Family Family, User Guardian, User Child);

// Inserts data directly through AppDbContext. Every record gets unique IDs and emails,
// so tests sharing one database don't interfere with each other.
public class TestData(IServiceProvider services)
{
    public async System.Threading.Tasks.Task<TestFamily> CreateFamilyAsync()
    {
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family", CreatedAt = DateTime.UtcNow };
        var guardian = NewUser(family, UserRole.Guardian);
        var child = NewUser(family, UserRole.Child);

        await SaveAsync(db =>
        {
            db.Families.Add(family);
            db.Users.AddRange(guardian, child);
        });

        return new TestFamily(family, guardian, child);
    }

    public async System.Threading.Tasks.Task<User> AddMemberAsync(TestFamily family, UserRole role)
    {
        var member = NewUser(family.Family, role);
        await SaveAsync(db => db.Users.Add(member));
        return member;
    }

    public async System.Threading.Tasks.Task<FamilyInvite> CreateInviteAsync(
        TestFamily family,
        UserRole role,
        DateTime? expiresAt = null,
        DateTime? usedAt = null)
    {
        var invite = new FamilyInvite
        {
            Id = Guid.NewGuid(),
            FamilyId = family.Family.Id,
            Code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Role = role,
            CreatedById = family.Guardian.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            UsedAt = usedAt
        };

        await SaveAsync(db => db.FamilyInvites.Add(invite));
        return invite;
    }

    public System.Threading.Tasks.Task<Models.Task> CreateTaskAsync(TestFamily family, params User[] assignees) =>
        CreateTaskByAsync(family, family.Guardian, assignees);

    public async System.Threading.Tasks.Task<Models.Task> CreateTaskByAsync(TestFamily family, User createdBy, params User[] assignees)
    {
        var task = new Models.Task
        {
            Id = Guid.NewGuid(),
            FamilyId = family.Family.Id,
            Title = "Test task",
            CreatedById = createdBy.Id,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAsync(db =>
        {
            db.Tasks.Add(task);
            db.TaskAssignments.AddRange(assignees.Select(a => new TaskAssignment { TaskId = task.Id, UserId = a.Id }));
        });

        return task;
    }

    public async System.Threading.Tasks.Task<T> QueryAsync<T>(Func<AppDbContext, System.Threading.Tasks.Task<T>> query)
    {
        using var scope = services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async System.Threading.Tasks.Task SaveAsync(Action<AppDbContext> change)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        change(db);
        await db.SaveChangesAsync();
    }

    private static User NewUser(Family family, UserRole role)
    {
        var entraUserId = $"entra-{Guid.NewGuid()}";
        return new User
        {
            Id = Guid.NewGuid(),
            EntraUserId = entraUserId,
            Email = $"{entraUserId}@test.househeroes",
            FirstName = role.ToString(),
            LastName = "Tester",
            Role = role,
            FamilyId = family.Id,
            CreatedAt = DateTime.UtcNow
        };
    }
}
