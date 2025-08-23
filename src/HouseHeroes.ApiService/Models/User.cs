using System.ComponentModel.DataAnnotations;

namespace HouseHeroes.ApiService.Models;

public class User
{
    public Guid Id { get; set; }
    
    [Required]
    public string EntraUserId { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public UserRole Role { get; set; }
    
    public Guid FamilyId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public Family Family { get; set; } = null!;
    public ICollection<Task> CreatedTasks { get; set; } = [];
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = [];
}