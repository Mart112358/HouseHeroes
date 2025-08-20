namespace HouseHeroes.ApiService.Models;

public class TaskAssignment
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    
    // Navigation properties
    public Task Task { get; set; } = null!;
    public User User { get; set; } = null!;
}