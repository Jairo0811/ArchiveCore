<div align="center">

# ArchiveCore

<img src="https://img.shields.io/badge/ITLA-SOF--008-0057B8?style=for-the-badge" alt="ITLA SOF-008" />
<img src="https://img.shields.io/badge/Estado-Reconstrucci%C3%B3n%202026-2563EB?style=for-the-badge" alt="Reconstrucción 2026" />

<br/><br/>

<a href="https://github.com/Jairo0811/ArchiveCore/actions/workflows/ci.yml">
  <img src="https://github.com/Jairo0811/ArchiveCore/actions/workflows/ci.yml/badge.svg" alt="CI" />
</a>

<br/><br/>

**Document & Records Management System**

</div>

ArchiveCore is a modern full-stack reconstruction of an academic project originally developed for **Bases de Datos Avanzadas (SOF-008)** at ITLA, imparted by **Carlos Caraballos**.

The reconstruction preserves the original SQL artifact while evolving the concept into a secure records, document-versioning, workflow and audit platform.

## 🎓 Información académica

| Información | Detalle |
|---|---|
| 🏫 Institución | **Instituto Tecnológico de Las Américas (ITLA)** |
| 📖 Asignatura | **Bases de Datos Avanzadas (SOF-008)** |
| 👨‍🏫 Profesor | **Carlos Caraballos** |
| 📅 Período académico | **Pendiente de documentar en el repositorio** |
| 📁 Artefacto original | **Proyecto de base de datos SQL Server** |
| 🛠️ Reconstrucción | **2026** |

El proyecto académico original se enfocó en modelado conceptual/físico, normalización, índices, consultas SQL, triggers, procedimientos almacenados y trazabilidad de auditoría. ArchiveCore mantiene esos aspectos de base de datos como núcleo técnico y añade una aplicación moderna alrededor de ellos.

## 🧭 Continuidad académica

### 👨‍🏫 Continuidad por profesor

ArchiveCore comparte profesor con [**SalesIntel-DW**](https://github.com/Jairo0811/SalesIntel-DW). Ambos proyectos fueron desarrollados bajo la docencia de **Carlos Caraballos**, aunque corresponden a asignaturas y objetivos distintos.

| Orden | Asignatura | Proyecto | Período |
|---:|---|---|---|
| 1 | Bases de Datos Avanzadas (SOF-008) | **ArchiveCore** | Pendiente de documentar |
| 2 | Minería de Datos e Inteligencia de Negocios (SOF-014) | [**SalesIntel-DW**](https://github.com/Jairo0811/SalesIntel-DW) | 2017-C3 |

La relación es **académica y docente**, no una dependencia técnica entre repositorios.

## 🧱 Stack tecnológico

### ⚙️ Backend

<p>
  <img src="https://skillicons.dev/icons?i=dotnet,cs" alt=".NET y C#" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-Minimal%20API-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt="ASP.NET Core Minimal API" />
</p>

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- Clean / Onion-style separation
- JWT + rotating refresh tokens
- ASP.NET Core password hashing

### 🎨 Frontend

<p>
  <img src="https://skillicons.dev/icons?i=react,ts,vite" alt="React, TypeScript y Vite" />
</p>

- React 19
- TypeScript
- Vite
- TanStack Query
- React Router

### 🗄️ Base de datos

<p>
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/microsoftsqlserver/microsoftsqlserver-plain.svg" width="48" height="48" alt="Microsoft SQL Server" />
</p>

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
