namespace ArchiveCore.Domain.Entities;

public sealed class UserRole
{
    public int UserId { get; set; }
    public short RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public int? AssignedByUserId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
