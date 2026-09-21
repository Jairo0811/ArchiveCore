# ArchiveCore

**Document & Records Management System**

ArchiveCore is a modern reconstruction of an academic database project originally developed for **Bases de Datos Avanzadas (SOF-008)** at ITLA.

The original project focused on database design for an information system and included early entities such as `Administrador`, `Usuarios`, `IdArchivo` and `IdRegistro`. The reconstruction preserves that legacy while evolving the concept into a full document and records management platform.

## Academic origin

- **Course:** Bases de Datos Avanzadas (SOF-008)
- **Professor:** Carlos Caraballos
- **Institution:** Instituto Tecnológico de Las Américas (ITLA)
- **Original artifact:** SQL Server database project
- **Modernization:** 2026

The original assignment required:

- Business and information-needs analysis
- Application proposal
- Conceptual database model
- Physical database model
- SQL queries
- Normalization
- Indexing
- Audit tracking
- Triggers and stored procedures

## Product vision

ArchiveCore will manage:

- Users and roles
- Records / case files
- Documents and versions
- Document categories
- Record movements and assignments
- Status workflows
- Audit history
- Operational reports

The database remains a first-class component of the system rather than merely persistence for a CRUD application.

## Planned stack

### Backend
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- Clean / Onion Architecture
- JWT authentication and authorization

### Frontend
- React
- TypeScript
- Vite
- TanStack Query
- React Router

### Database
- Microsoft SQL Server
- Stored Procedures
- Views
- Triggers
- Transactions
- Indexes
- Constraints
- Audit trail
- Optimistic concurrency

## Repository structure

```text
ArchiveCore/
├── database/
│   └── README.md
├── docs/
│   ├── academic/
│   ├── architecture/
│   └── legacy/
├── src/
├── frontend/
├── .gitignore
├── ROADMAP.md
└── README.md
```

## Modernization strategy

The original SQL is retained under `docs/legacy/` and must not be treated as production-ready code.

The modernization follows four principles:

1. Preserve the academic history.
2. Redesign the data model using proper relational modeling and normalization.
3. Demonstrate advanced SQL Server capabilities required by SOF-008.
4. Build a modern full-stack implementation around the redesigned database.

## Current status

**Phase 0 — Legacy Analysis & Project Foundation**

See [ROADMAP.md](ROADMAP.md) for the complete modernization plan.

---

ArchiveCore is an academic reconstruction and portfolio project.
