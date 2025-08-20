using System.ComponentModel.DataAnnotations;

namespace HouseHeroes.ApiService.Models;

public class Task
{
    public Guid Id { get; set; }
    
    public Guid FamilyId { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public Guid CreatedById { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public Family Family { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = [];
}