# Security setup

## JWT signing key

ArchiveCore does **not** store a signing secret in source control.

From `src/ArchiveCore.WebApi`, initialize user-secrets if necessary and configure a strong signing key:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<use-a-long-random-secret>"
```

Use at least 32 characters. In deployed environments, replace user-secrets with the platform's secret manager or environment variables.

## First administrator

ArchiveCore deliberately ships without a default password.

A one-time bootstrap mechanism is available. It creates an administrator only when:

1. `BootstrapAdmin:Enabled` is `true`.
2. `Users` is empty.
3. The seeded `Administrator` role exists.

Configure it with user-secrets:

```powershell
dotnet user-secrets set "BootstrapAdmin:Enabled" "true"
dotnet user-secrets set "BootstrapAdmin:FirstName" "ArchiveCore"
dotnet user-secrets set "BootstrapAdmin:LastName" "Administrator"
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.local"
dotnet user-secrets set "BootstrapAdmin:Password" "<strong-password>"
```

Run the API once. Then disable bootstrap and remove the password secret:

```powershell
dotnet user-secrets set "BootstrapAdmin:Enabled" "false"
dotnet user-secrets remove "BootstrapAdmin:Password"
```

After the first administrator exists, users are provisioned through the administrator-protected API.

## Endpoints

| Endpoint | Access |
|---|---|
| `POST /api/v1/auth/login` | Anonymous |
| `POST /api/v1/auth/refresh` | Anonymous |
| `POST /api/v1/auth/revoke` | Authenticated |
| `POST /api/v1/auth/users` | Administrator |
| `GET /api/v1/account/me` | Authenticated |

## Token policy

- Access token lifetime: 15 minutes by default.
- Refresh token lifetime: 7 days by default.
- Refresh tokens are generated cryptographically.
- Only SHA-256 refresh-token hashes are persisted.
- Refresh tokens rotate on use.
- Revoked tokens cannot be reused.
- JWT signing secrets are excluded from the repository.

## Password storage

Passwords are persisted only through ASP.NET Core's `PasswordHasher<TUser>`. Plain-text passwords are never stored in `ArchiveCoreDb`.
