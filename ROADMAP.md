# ArchiveCore Roadmap

## Phase 0 — Legacy Analysis & Project Foundation ✅

- [x] Preserve original SQL artifact.
- [x] Document academic requirements.
- [x] Document legacy limitations.
- [x] Define product vision.
- [x] Define target architecture.
- [x] Establish repository structure.

## Phase 1 — Database Redesign & Normalization ✅

- [x] Define entities and relationships.
- [x] Produce conceptual model.
- [x] Produce physical model.
- [x] Normalize operational schema to 3NF.
- [x] Define primary, alternate and foreign keys.
- [x] Create `ArchiveCoreDb` schema.

## Phase 2 — Advanced SQL Server ✅

- [x] Query-driven indexes.
- [x] Views.
- [x] Stored procedures.
- [x] Audit tables and triggers.
- [x] Transactional mutation procedures.
- [x] Advanced query examples.

## Phase 3 — .NET Backend Foundation ✅

- [x] .NET 10 solution.
- [x] Domain, Application, Infrastructure and WebApi layers.
- [x] EF Core mappings to ArchiveCoreDb.
- [x] SQL Server dependency registration.
- [x] OpenAPI and database health check.

## Phase 4 — Identity & Access Control ✅

- [x] Password hashing.
- [x] JWT access tokens.
- [x] Refresh-token rotation and revocation.
- [x] Role claims and Administrator policy.
- [x] Administrator-controlled user provisioning.
- [x] Secure first-administrator bootstrap.

## Phase 5 — Records & Documents ✅

- [x] Record queries, creation and updates.
- [x] Document metadata.
- [x] Document categories.
- [x] Versioned physical files.
- [x] SHA-256 file integrity hashes.
- [x] Replaceable file-storage abstraction.

## Phase 6 — Workflow & Movements ✅

- [x] Record transfers and assignments.
- [x] Record closure/status transition.
- [x] Movement history.
- [x] Authenticated workflow endpoints.

## Phase 7 — Audit & Traceability ✅

- [x] Database-trigger audit trail.
- [x] Before/after JSON snapshots.
- [x] Administrator audit search.
- [x] Operational dashboard metrics.
- [x] Audit/report query surface.

## Phase 8 — React Web Application ✅

- [x] React + TypeScript + Vite.
- [x] Authentication UX.
- [x] Responsive application shell.
- [x] Dashboard.
- [x] Records search/list.
- [x] Documents view.
- [x] Role-aware audit view.
- [x] TanStack Query and React Router.

## Phase 9 — Quality & Portfolio Hardening ✅

- [x] API smoke tests.
- [x] SQL validation script.
- [x] Docker API image.
- [x] Docker frontend image.
- [x] Docker Compose stack.
- [x] GitHub Actions CI.
- [x] Secret handling documentation.
- [x] Phase documentation.
- [x] Release-candidate repository structure.

## Phase 10 — Local Integration Validation ✅

- [x] Restore and build the .NET 10 solution.
- [x] Install frontend dependencies and complete a production Vite build.
- [x] Recreate and execute the complete ArchiveCoreDb script chain.
- [x] Validate critical constraints and indexes.
- [x] Validate SQL Server views, procedures and audit triggers.
- [x] Configure local User Secrets.
- [x] Bootstrap the first Administrator account.
- [x] Start the API in Development.
- [x] Validate the database health check.
- [x] Validate JWT login and refresh-token issuance.
- [x] Start the React frontend.
- [x] Validate authenticated Dashboard, Records, Documents and Audit navigation.
- [x] Confirm database-backed dashboard metrics and SQL Server audit events.
- [x] Disable the administrator bootstrap after initialization.

## Phase 11 — Academic SQL Lab ✅

- [x] Preserve the original SOF-008 SELECT queries inside the modern application.
- [x] Map legacy Administrador to Users + Roles + UserRoles.
- [x] Map legacy Usuarios to dbo.Users.
- [x] Provide administrator-only predefined read-only query execution.
- [x] Display legacy SQL, modern SQL and live SQL Server results side by side.
- [x] Keep arbitrary SQL execution disabled.

## Phase 12 — Final Hardening & Release Candidate ✅

- [x] Sanitize the public legacy SQL copy and rewrite Git history without the original personal values.
- [x] Refresh token in an HttpOnly/Secure/SameSite cookie; access token in memory; single-flight refresh, revoke on logout.
- [x] Rate limiting for login and refresh.
- [x] Role policies (Administrator, RecordsManager, User) on every endpoint, with 401/403 tests.
- [x] Correct audit actor, IP and correlation id through SESSION_CONTEXT on every pooled connection.
- [x] Request validation and ProblemDetails; concurrency conflicts return 409.
- [x] Stored procedures as the single source of truth for create/assign/transfer/close/add-version.
- [x] Records, documents, versions, download, audit filters and user administration completed in the UI.
- [x] Dashboard limited to real values; Spanish UI; responsive and accessibility pass.
- [x] Secure uploads/downloads with size limits aligned between Nginx and ASP.NET Core.
- [x] CORS, AllowedHosts, forwarded headers and security headers by environment.
- [x] Reproducible Docker initialization from a clean volume.
- [x] SQL Server integration tests, frontend tests, Docker end-to-end smoke and dependency scanning in CI.
- [x] Official logos deduplicated and optimized (PNG + WebP).

## Current milestone

**ArchiveCore v1.0.0 — FROZEN — PORTFOLIO RELEASE.**

Every release gate passes in CI on the final commit: Release build, 98 backend tests (including
SQL Server integration tests), frontend lint/build/tests, dependency scanning, Docker clean boot
with database validation and the end-to-end smoke test through Nginx.


## Project freeze

**Status: FROZEN — PORTFOLIO RELEASE (v1.0.0)**

ArchiveCore has completed its reconstruction, final hardening, academic SQL Lab integration and
final branding pass. `main` and `release/archivecore-v1.0.0-frozen` point to the same commit,
tagged `v1.0.0`.

There is no active feature-development milestone after this point. The project should only be reopened for corrective maintenance, security fixes, compatibility work or an explicit new scope decision.
