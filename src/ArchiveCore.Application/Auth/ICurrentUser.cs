namespace ArchiveCore.Application.Auth;

public interface ICurrentUser
{
    int? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
}
