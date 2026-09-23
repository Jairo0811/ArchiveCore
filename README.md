# ArchiveCore

**Document & Records Management System**

ArchiveCore is a modern full-stack reconstruction of an academic project originally developed for **Bases de Datos Avanzadas (SOF-008)** at ITLA, imparted by **Carlos Caraballos**.

The reconstruction preserves the original SQL artifact while evolving the concept into a secure records, document-versioning, workflow and audit platform.

## Academic origin

- **Course:** Bases de Datos Avanzadas (SOF-008)
- **Professor:** Carlos Caraballos
- **Institution:** Instituto Tecnológico de Las Américas (ITLA)
- **Original artifact:** SQL Server database project
- **Modern reconstruction:** 2026

The original assignment emphasized conceptual/physical modeling, normalization, indexes, SQL queries, triggers, stored procedures and audit tracking. ArchiveCore keeps those database concerns first-class and adds a modern application around them.

## Stack

### Backend

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- Clean / Onion-style separation
- JWT + rotating refresh tokens
- ASP.NET Core password hashing

### Frontend

- React 19
- TypeScript
- Vite
- TanStack Query
- React Router

### Database

- Microsoft SQL Server
- 3NF operational model
- Primary/foreign/alternate keys
- Query-driven indexes
- Views
- Stored procedures
- Transactions
- Audit triggers
- JSON before/after snapshots
- Optimistic concurrency with `rowversion`

## Main capabilities

- Users, roles and administrator provisioning
- JWT authentication and refresh-token rotation
- Records / case files
- Document metadata and physical versions
- SHA-256 file integrity hashes
- Record transfers and closure workflow
- Movement history
- Database-backed audit history
- Dashboard metrics
- Responsive authenticated React interface

## Repository structure

```text
ArchiveCore/
├── database/            SQL Server schema and advanced SQL
├── docs/                academic, architecture, setup and phase documentation
├── frontend/            React + TypeScript SPA
├── src/
│   ├── ArchiveCore.Domain/
│   ├── ArchiveCore.Application/
│   ├── ArchiveCore.Infrastructure/
│   └── ArchiveCore.WebApi/
├── tests/
│   └── ArchiveCore.Api.Tests/
├── docker-compose.yml
├── ArchiveCore.slnx
├── ROADMAP.md
└── README.md
```

## Database execution order

See `database/README.md`. The schema is intentionally maintained as explicit SQL because advanced SQL Server design is part of ArchiveCore's academic and technical identity.

## Security

No JWT signing key or default administrator password is committed to the repository.

See:

`docs/setup/security-setup.md`

for the user-secrets/bootstrap procedure.

## Current status

**Phases 0–9 implemented. Feature construction is complete.**

The repository is now at the **local validation** milestone. We will next run the database scripts, restore/build both applications, start the stack and fix any runtime findings before declaring the release candidate locally verified.

See `ROADMAP.md` for the completed modernization plan.

---

ArchiveCore is an academic reconstruction and portfolio project.
