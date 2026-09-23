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

## Current milestone

**Feature construction complete.**

The next milestone is **local validation**: execute SQL scripts, configure secrets, restore/build, start the stack and fix any runtime or environment-specific findings.
