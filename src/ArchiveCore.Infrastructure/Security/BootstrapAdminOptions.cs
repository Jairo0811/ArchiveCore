namespace ArchiveCore.Infrastructure.Security;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public bool Enabled { get; init; }
    public string FirstName { get; init; } = "ArchiveCore";
    public string LastName { get; init; } = "Administrator";
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
