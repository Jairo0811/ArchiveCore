namespace ArchiveCore.Application.Auth;

public interface ICurrentUser
{
    int? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }

    /// <summary>Client IP (after trusted forwarded headers), propagated to SQL Server audit.</summary>
    string? IpAddress { get; }

    /// <summary>Per-request correlation identifier, propagated to SQL Server audit.</summary>
    Guid? CorrelationId { get; }

    bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
}

public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string RecordsManager = "RecordsManager";
    public const string User = "User";

    public static readonly IReadOnlyList<string> All = [Administrator, RecordsManager, User];
}
