using System.ComponentModel.DataAnnotations;

namespace HouseHeroes.ApiService.Models;

public class Family
{
    public Guid Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<User> Members { get; set; } = [];
    public ICollection<Task> Tasks { get; set; } = [];
}