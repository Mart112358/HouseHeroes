using Microsoft.EntityFrameworkCore;
using HouseHeroes.ApiService.Models;

namespace HouseHeroes.ApiService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Family> Families { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Models.Task> Tasks { get; set; }
    public DbSet<TaskAssignment> TaskAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Family configuration
        modelBuilder.Entity<Family>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            
            // One Family has many Users
            entity.HasMany(f => f.Members)
                  .WithOne(u => u.Family)
                  .HasForeignKey(u => u.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            // One Family has many Tasks
            entity.HasMany(f => f.Tasks)
                  .WithOne(t => t.Family)
                  .HasForeignKey(t => t.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasConversion<string>();
            
            // Create unique index on Email
            entity.HasIndex(e => e.Email).IsUnique();
            
            // One User creates many Tasks
            entity.HasMany(u => u.CreatedTasks)
                  .WithOne(t => t.CreatedBy)
                  .HasForeignKey(t => t.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Task configuration
        modelBuilder.Entity<Models.Task>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
        });

        // TaskAssignment configuration (many-to-many)
        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            // Composite primary key
            entity.HasKey(ta => new { ta.TaskId, ta.UserId });
            
            // Configure relationships
            entity.HasOne(ta => ta.Task)
                  .WithMany(t => t.TaskAssignments)
                  .HasForeignKey(ta => ta.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(ta => ta.User)
                  .WithMany(u => u.TaskAssignments)
                  .HasForeignKey(ta => ta.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}