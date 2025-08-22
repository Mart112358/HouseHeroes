using Microsoft.EntityFrameworkCore;
using HouseHeroes.ApiService.Models;

namespace HouseHeroes.ApiService.Data;

public static class DatabaseSeeder
{
    public static async System.Threading.Tasks.Task SeedAsync(AppDbContext context)
    {
        // Check if data already exists
        if (await context.Families.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Create families
        var paquinFamily = new Family
        {
            Id = Guid.NewGuid(),
            Name = "The Paquin Family",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var johnsonFamily = new Family
        {
            Id = Guid.NewGuid(),
            Name = "The Johnson Family",
            CreatedAt = DateTime.UtcNow.AddDays(-45)
        };

        context.Families.AddRange(paquinFamily, johnsonFamily);
        await context.SaveChangesAsync();

        // Create users for Paquin family (divorced parents, shared custody)
        var sarah = new User
        {
            Id = Guid.NewGuid(),
            Email = "sarah.paquin@email.com",
            PasswordHash = "hashed_password_123", // In real app, use proper hashing
            FirstName = "Sarah",
            LastName = "Paquin",
            Role = UserRole.Guardian,
            FamilyId = paquinFamily.Id
        };

        var mike = new User
        {
            Id = Guid.NewGuid(),
            Email = "mike.paquin@email.com",
            PasswordHash = "hashed_password_456",
            FirstName = "Mike",
            LastName = "Paquin",
            Role = UserRole.Guardian,
            FamilyId = paquinFamily.Id
        };

        var emma = new User
        {
            Id = Guid.NewGuid(),
            Email = "emma.paquin@email.com",
            PasswordHash = "hashed_password_789",
            FirstName = "Emma",
            LastName = "Paquin",
            Role = UserRole.Child,
            FamilyId = paquinFamily.Id
        };

        var lucas = new User
        {
            Id = Guid.NewGuid(),
            Email = "lucas.paquin@email.com",
            PasswordHash = "hashed_password_101",
            FirstName = "Lucas",
            LastName = "Paquin",
            Role = UserRole.Child,
            FamilyId = paquinFamily.Id
        };

        // Create users for Johnson family (blended family)
        var marc = new User
        {
            Id = Guid.NewGuid(),
            Email = "marc.johnson@email.com",
            PasswordHash = "hashed_password_202",
            FirstName = "Marc",
            LastName = "Johnson",
            Role = UserRole.Guardian,
            FamilyId = johnsonFamily.Id
        };

        var jessica = new User
        {
            Id = Guid.NewGuid(),
            Email = "jessica.johnson@email.com",
            PasswordHash = "hashed_password_303",
            FirstName = "Jessica",
            LastName = "Johnson",
            Role = UserRole.Guardian,
            FamilyId = johnsonFamily.Id
        };

        var alex = new User
        {
            Id = Guid.NewGuid(),
            Email = "alex.johnson@email.com",
            PasswordHash = "hashed_password_404",
            FirstName = "Alex",
            LastName = "Johnson",
            Role = UserRole.Child,
            FamilyId = johnsonFamily.Id
        };

        var sophia = new User
        {
            Id = Guid.NewGuid(),
            Email = "sophia.johnson@email.com",
            PasswordHash = "hashed_password_505",
            FirstName = "Sophia",
            LastName = "Johnson",
            Role = UserRole.Child,
            FamilyId = johnsonFamily.Id
        };

        var ethan = new User
        {
            Id = Guid.NewGuid(),
            Email = "ethan.johnson@email.com",
            PasswordHash = "hashed_password_606",
            FirstName = "Ethan",
            LastName = "Johnson",
            Role = UserRole.Child,
            FamilyId = johnsonFamily.Id
        };

        var mia = new User
        {
            Id = Guid.NewGuid(),
            Email = "mia.johnson@email.com",
            PasswordHash = "hashed_password_707",
            FirstName = "Mia",
            LastName = "Johnson",
            Role = UserRole.Child,
            FamilyId = johnsonFamily.Id
        };

        context.Users.AddRange(sarah, mike, emma, lucas, marc, jessica, alex, sophia, ethan, mia);
        await context.SaveChangesAsync();

        // Create tasks for Paquin family
        var paquinTasks = new List<Models.Task>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = paquinFamily.Id,
                Title = "Take out trash",
                Description = "Put trash bins on the curb every Tuesday evening",
                CreatedById = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                DueDate = DateTime.UtcNow.AddDays(1),
                IsCompleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = paquinFamily.Id,
                Title = "Clean bedroom",
                Description = "Make bed, organize toys, vacuum floor",
                CreatedById = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = paquinFamily.Id,
                Title = "Load dishwasher",
                Description = "After dinner, load and start the dishwasher",
                CreatedById = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddHours(2),
                IsCompleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = paquinFamily.Id,
                Title = "Feed the dog",
                Description = "Give Rex his morning and evening meals",
                CreatedById = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                DueDate = DateTime.UtcNow.AddHours(-8),
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.AddHours(-8)
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = paquinFamily.Id,
                Title = "Homework time",
                Description = "Complete math and reading assignments",
                CreatedById = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddHours(4),
                IsCompleted = false
            }
        };

        // Create tasks for Johnson family
        var johnsonTasks = new List<Models.Task>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = johnsonFamily.Id,
                Title = "Mow the lawn",
                Description = "Cut grass in front and back yard, edge walkways",
                CreatedById = marc.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                DueDate = DateTime.UtcNow.AddDays(2),
                IsCompleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = johnsonFamily.Id,
                Title = "Grocery shopping",
                Description = "Buy items from the weekly grocery list",
                CreatedById = jessica.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                DueDate = DateTime.UtcNow.AddDays(1),
                IsCompleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = johnsonFamily.Id,
                Title = "Soccer practice pickup",
                Description = "Pick up kids from soccer practice at 6 PM",
                CreatedById = marc.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                DueDate = DateTime.UtcNow.AddHours(-1),
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = johnsonFamily.Id,
                Title = "Clean bathroom",
                Description = "Scrub toilet, clean shower, mop floor",
                CreatedById = jessica.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                DueDate = DateTime.UtcNow.AddDays(1),
                IsCompleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = johnsonFamily.Id,
                Title = "Piano practice",
                Description = "Practice piano for 30 minutes",
                CreatedById = jessica.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                DueDate = DateTime.UtcNow,
                IsCompleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                FamilyId = johnsonFamily.Id,
                Title = "Set table for dinner",
                Description = "Put out plates, silverware, and napkins",
                CreatedById = marc.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-4),
                DueDate = DateTime.UtcNow.AddHours(1),
                IsCompleted = false
            }
        };

        context.Tasks.AddRange(paquinTasks);
        context.Tasks.AddRange(johnsonTasks);
        await context.SaveChangesAsync();

        // Create task assignments
        var assignments = new List<TaskAssignment>
        {
            // Paquin family assignments
            new() { TaskId = paquinTasks[0].Id, UserId = lucas.Id }, // Lucas takes out trash
            new() { TaskId = paquinTasks[1].Id, UserId = emma.Id }, // Emma cleaned bedroom
            new() { TaskId = paquinTasks[2].Id, UserId = emma.Id }, // Emma loads dishwasher
            new() { TaskId = paquinTasks[2].Id, UserId = lucas.Id }, // Lucas also loads dishwasher
            new() { TaskId = paquinTasks[3].Id, UserId = emma.Id }, // Emma fed dog
            new() { TaskId = paquinTasks[4].Id, UserId = emma.Id }, // Emma homework
            new() { TaskId = paquinTasks[4].Id, UserId = lucas.Id }, // Lucas homework

            // Johnson family assignments
            new() { TaskId = johnsonTasks[0].Id, UserId = alex.Id }, // Alex mows lawn
            new() { TaskId = johnsonTasks[0].Id, UserId = ethan.Id }, // Ethan helps mow lawn
            new() { TaskId = johnsonTasks[1].Id, UserId = jessica.Id }, // Jessica grocery shopping
            new() { TaskId = johnsonTasks[2].Id, UserId = marc.Id }, // Marc picked up from soccer
            new() { TaskId = johnsonTasks[3].Id, UserId = sophia.Id }, // Sophia cleans bathroom
            new() { TaskId = johnsonTasks[3].Id, UserId = mia.Id }, // Mia helps clean bathroom
            new() { TaskId = johnsonTasks[4].Id, UserId = sophia.Id }, // Sophia piano practice
            new() { TaskId = johnsonTasks[5].Id, UserId = alex.Id }, // Alex sets table
        };

        context.TaskAssignments.AddRange(assignments);
        await context.SaveChangesAsync();
    }
}