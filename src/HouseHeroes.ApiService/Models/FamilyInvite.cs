using System.ComponentModel.DataAnnotations;

namespace HouseHeroes.ApiService.Models;

// A single-use code a Guardian shares so someone can join their family with the given role.
public class FamilyInvite
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public Guid CreatedById { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }
}
