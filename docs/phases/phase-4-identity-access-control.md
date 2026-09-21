# Phase 4 — Identity & Access Control

## Objective

Add production-oriented authentication and authorization primitives to ArchiveCore.

## Implemented

### Password security

- ASP.NET Core `PasswordHasher<User>`.
- No plain-text password persistence.
- Password hashes stay in `Users.PasswordHash`.

### JWT access tokens

- HMAC-SHA256 signing.
- Issuer and audience validation.
- 15-minute default lifetime.
- User ID, email and role claims.
- Signing key supplied outside source control.

### Refresh tokens

- Cryptographically random 64-byte tokens.
- SHA-256 hashes stored in SQL Server.
- Seven-day default lifetime.
- Rotation when refreshed.
- Explicit revocation.
- Replacement-chain tracking.
- Client IP metadata.

### Authorization

- Authenticated-user protection.
- Role claims.
- `AdministratorsOnly` policy.
- Administrator-only user provisioning.

### First administrator

A one-time, opt-in bootstrap process can create the initial administrator when the database contains no users. Credentials are supplied via user-secrets/environment configuration and are not committed.

## Database extension

`database/09-auth.sql` adds `RefreshTokens`, relational constraints and lookup indexes.

## API surface

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/revoke`
- `POST /api/v1/auth/users`
- `GET /api/v1/account/me`

## Next phase

Phase 5 will implement the Records and Documents application use cases and secured API endpoints on top of this identity layer.
