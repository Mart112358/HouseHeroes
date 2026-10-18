using System.ComponentModel.DataAnnotations;
using HotChocolate;

namespace HouseHeroes.ApiService.Models;

public class Family
{
    public Guid Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }

    // Bumped by every change to who is in the family or their roles. It's a concurrency token,
    // so two concurrent changes can't both pass the "keep at least one guardian" check.
    [GraphQLIgnore]
    public int MembershipVersion { get; set; }
    
    // Navigation properties
    public ICollection<User> Members { get; set; } = [];
    public ICollection<Task> Tasks { get; set; } = [];
}