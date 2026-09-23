namespace ArchiveCore.Domain.Entities;

public sealed class Role
{
    public short RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
