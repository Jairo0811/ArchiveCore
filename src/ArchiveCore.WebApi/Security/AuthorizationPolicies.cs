using ArchiveCore.Application.Auth;
using Microsoft.AspNetCore.Authorization;

namespace ArchiveCore.WebApi.Security;

/// <summary>
/// Reusable authorization policies. Authorization is always enforced here, in the API;
/// the SPA only hides actions for convenience.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Administrator only: user administration, audit search and SQL Lab.</summary>
    public const string Administrators = "AdministratorsOnly";

    /// <summary>Administrator or RecordsManager: records, documents, versions and workflow mutations.</summary>
    public const string RecordsManagers = "RecordsManagement";

    /// <summary>Any ArchiveCore role (including User): read-only operational access.</summary>
    public const string Readers = "ArchiveCoreReaders";

    public static AuthorizationBuilder AddArchiveCorePolicies(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(Administrators, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(RoleNames.Administrator))
            .AddPolicy(RecordsManagers, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(RoleNames.Administrator, RoleNames.RecordsManager))
            .AddPolicy(Readers, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(RoleNames.Administrator, RoleNames.RecordsManager, RoleNames.User))
            // Endpoints without explicit metadata still require an authenticated user.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
}
