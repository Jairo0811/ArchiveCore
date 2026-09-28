<div align="center">

# ArchiveCore

<img src="https://img.shields.io/badge/ITLA-SOF--008-0057B8?style=for-the-badge" alt="ITLA SOF-008" />
<img src="https://img.shields.io/badge/Estado-Validado%20localmente-16A34A?style=for-the-badge" alt="Validado localmente" />

<br/><br/>

<a href="https://github.com/Jairo0811/ArchiveCore/actions/workflows/ci.yml">
  <img src="https://github.com/Jairo0811/ArchiveCore/actions/workflows/ci.yml/badge.svg" alt="Integración continua" />
</a>

<br/><br/>

**Sistema de Gestión Documental y Expedientes**

</div>

ArchiveCore es una reconstrucción moderna de un proyecto académico desarrollado originalmente para la asignatura **Bases de Datos Avanzadas (SOF-008)** del **Instituto Tecnológico de Las Américas (ITLA)**, impartida por **Carlos Caraballos**.

El proyecto original estaba centrado en diseño y administración de bases de datos. La reconstrucción actual conserva ese origen y lo amplía hasta convertirlo en una plataforma completa para la **gestión de expedientes, documentos, versiones, movimientos, usuarios, roles y auditoría**.

ArchiveCore mantiene a **SQL Server como núcleo técnico del sistema**, incorporando una aplicación web moderna construida con **.NET 10, ASP.NET Core, React y TypeScript**.

## 🎓 Información académica

| Información | Detalle |
|---|---|
| 🏫 Institución | **Instituto Tecnológico de Las Américas (ITLA)** |
| 📖 Asignatura | **Bases de Datos Avanzadas (SOF-008)** |
| 👨‍🏫 Profesor | **Carlos Caraballos** |
| 📅 Período académico | **Pendiente de documentar en el repositorio** |
| 📁 Artefacto original | **Proyecto de base de datos en SQL Server** |
| 🛠️ Reconstrucción moderna | **2026** |

El trabajo académico original se enfocó en:

- Modelado conceptual y físico.
- Normalización.
- Índices.
- Consultas SQL.
- Procedimientos almacenados.
- Triggers.
- Auditoría y trazabilidad.

La reconstrucción moderna conserva esos objetivos y los integra dentro de una aplicación real de gestión documental.

## 🧭 Evolución del proyecto

El esquema original contenía conceptos iniciales como:

- `Administrador`
- `Usuarios`
- `IdArchivo`
- `IdRegistro`

En ArchiveCore estos conceptos evolucionaron a un modelo relacional completo:

| Concepto original | Evolución en ArchiveCore |
|---|---|
| `Administrador` | Usuarios + Roles + asignación de permisos |
| `Usuarios` | Gestión centralizada de usuarios |
| `IdRegistro` | Expedientes |
| `IdArchivo` | Documentos y versiones de documentos |

De esta forma, el proyecto mantiene su origen académico sin conservar las limitaciones técnicas del diseño inicial.

## 🧱 Tecnologías utilizadas

### ⚙️ Backend

<p>
  <img src="https://skillicons.dev/icons?i=dotnet,cs" alt=".NET y C#" />
  <img src="https://img.shields.io/badge/ASP.NET%20Core-Minimal%20API-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt="ASP.NET Core Minimal API" />
</p>

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- Arquitectura por capas inspirada en Clean Architecture / Onion Architecture
- Autenticación JWT
- Refresh tokens con rotación y revocación
- Hash seguro de contraseñas
- Inyección de dependencias
- OpenAPI
- Health checks

### 🎨 Frontend

<p>
  <img src="https://skillicons.dev/icons?i=react,ts,vite" alt="React, TypeScript y Vite" />
</p>

- React 19
- TypeScript
- Vite
- TanStack Query
- React Router
- Interfaz responsive
- Dashboard administrativo
- Navegación basada en roles

### 🗄️ Base de datos

<p>
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/microsoftsqlserver/microsoftsqlserver-plain.svg" width="48" height="48" alt="Microsoft SQL Server" />
</p>

- Microsoft SQL Server
- Modelo relacional normalizado hasta 3NF
- Claves primarias, foráneas y alternativas
- Restricciones de integridad
- Índices orientados a patrones de consulta
- Vistas
- Procedimientos almacenados
- Transacciones
- Triggers de auditoría
- Historial de cambios mediante JSON
- Control de concurrencia con `rowversion`

## 🧩 Funcionalidades principales

### 👤 Usuarios y seguridad

- Inicio de sesión mediante JWT.
- Refresh tokens.
- Rotación y revocación de sesiones.
- Roles de usuario.
- Política exclusiva para administradores.
- Creación controlada de usuarios.
- Bootstrap seguro del primer administrador.
- Contraseñas almacenadas mediante hash.

### 📁 Expedientes

- Creación de expedientes.
- Consulta y búsqueda.
- Estados de expediente.
- Asignación de responsables.
- Cierre de expedientes.
- Historial de actividad.

### 📄 Documentos

- Asociación de documentos a expedientes.
- Categorías documentales.
- Versionado.
- Identificación de versión actual.
- Eliminación lógica.
- Almacenamiento físico desacoplado.
- Hash SHA-256 para validar integridad.

### 🔄 Flujo de trabajo

- Transferencia de expedientes entre usuarios.
- Registro de usuario origen y destino.
- Notas de movimiento.
- Historial cronológico.
- Cierre y cambios de estado.

### 🛡️ Auditoría

- Auditoría automática mediante triggers de SQL Server.
- Registro de inserciones, modificaciones y eliminaciones.
- Valores anteriores y posteriores almacenados como JSON.
- Identificación del usuario que ejecutó la acción.
- Consulta de auditoría por:
  - entidad,
  - registro,
  - usuario,
  - tipo de acción,
  - rango de fechas.

### 📊 Dashboard

El panel principal muestra indicadores operativos como:

- Expedientes activos.
- Expedientes cerrados.
- Documentos activos.
- Usuarios activos.
- Movimientos realizados en el día.
- Eventos de auditoría del día.

La interfaz visual utiliza una identidad oscura en tonos navy, azul y cian, alineada con el concepto de seguridad, trazabilidad y control documental de ArchiveCore.

## 🏗️ Arquitectura del repositorio

```text
ArchiveCore/
├── database/
│   ├── 01-schema.sql
│   ├── 02-constraints.sql
│   ├── 03-indexes.sql
│   ├── 04-views.sql
│   ├── 05-procedures.sql
│   ├── 06-triggers.sql
│   ├── 07-seed.sql
│   ├── 08-queries.sql
│   ├── 09-auth.sql
│   └── 10-validation.sql
│
├── docs/
│   ├── academic/
│   ├── architecture/
│   ├── legacy/
│   ├── phases/
│   └── setup/
│
├── frontend/
│   └── Aplicación React + TypeScript + Vite
│
├── src/
│   ├── ArchiveCore.Domain/
│   ├── ArchiveCore.Application/
│   ├── ArchiveCore.Infrastructure/
│   └── ArchiveCore.WebApi/
│
├── tests/
│   └── ArchiveCore.Api.Tests/
│
├── docker-compose.yml
├── ArchiveCore.slnx
├── ROADMAP.md
└── README.md
```

## 🧠 Arquitectura de software

El backend está organizado en cuatro capas principales:

### `ArchiveCore.Domain`

Contiene las entidades principales y reglas del dominio.

### `ArchiveCore.Application`

Define contratos, casos de uso y abstracciones de aplicación.

### `ArchiveCore.Infrastructure`

Implementa:

- acceso a datos,
- Entity Framework Core,
- SQL Server,
- seguridad,
- almacenamiento de archivos,
- servicios de expedientes,
- documentos,
- workflow,
- auditoría.

### `ArchiveCore.WebApi`

Actúa como punto de entrada HTTP y contiene:

- configuración de ASP.NET Core,
- autenticación,
- autorización,
- endpoints,
- OpenAPI,
- health checks.

## 🗄️ Orden de ejecución de la base de datos

Los scripts deben ejecutarse en este orden:

```text
01-schema.sql
02-constraints.sql
07-seed.sql
03-indexes.sql
04-views.sql
05-procedures.sql
06-triggers.sql
09-auth.sql
10-validation.sql
```

El archivo `08-queries.sql` contiene consultas de ejemplo y puede ejecutarse de manera opcional.

Para más detalles consulta:

`database/README.md`

## 🔐 Seguridad y secretos

ArchiveCore **no almacena claves JWT ni contraseñas administrativas dentro del repositorio**.

Para desarrollo local se utilizan:

- .NET User Secrets.
- Variables de entorno.
- Configuración específica del entorno.

La guía de configuración se encuentra en:

`docs/setup/security-setup.md`

## 🐳 Docker

El proyecto incluye:

- Dockerfile para la API.
- Dockerfile para el frontend.
- Nginx para servir la SPA.
- Docker Compose.
- SQL Server 2022.
- Volumen persistente para la base de datos.
- Volumen persistente para documentos.

## 🧪 Calidad y validación

El repositorio incluye:

- Pruebas básicas de la API.
- Script de validación del esquema SQL.
- GitHub Actions.
- Build automatizado del backend.
- Build automatizado del frontend.
- Configuración para Docker.

## 🗺️ Fases de reconstrucción

ArchiveCore fue reconstruido en las siguientes fases:

1. Análisis del legado y fundación del proyecto.
2. Rediseño y normalización de la base de datos.
3. SQL Server avanzado.
4. Fundación del backend .NET.
5. Identidad y control de acceso.
6. Expedientes y documentos.
7. Workflow y movimientos.
8. Auditoría y trazabilidad.
9. Aplicación web en React.
10. Calidad, Docker y endurecimiento para portafolio.

El detalle completo se encuentra en:

`ROADMAP.md`

## 🎯 Estado actual

**ArchiveCore se encuentra funcionalmente completado y validado localmente.**

La validación integrada confirmó:

- Restauración y compilación correcta de la solución .NET 10.
- Build de producción correcto del frontend React + TypeScript + Vite.
- Ejecución completa de los scripts de `ArchiveCoreDb`.
- Validación satisfactoria de constraints, índices, vistas, procedimientos, triggers y catálogos.
- Health check de la API con respuesta `200 OK / Healthy`.
- Bootstrap seguro del primer administrador.
- Inicio de sesión JWT y emisión de access/refresh tokens.
- Integración frontend → API → Entity Framework Core → SQL Server.
- Dashboard operativo.
- Navegación y consulta de expedientes.
- Consulta de documentos por expediente.
- Vista de auditoría con eventos generados por triggers de SQL Server.
- Bootstrap administrativo desactivado después de crear la cuenta inicial.

El repositorio conserva además Docker, pruebas automatizadas, GitHub Actions y documentación de seguridad para futuras ejecuciones o despliegues.

A partir de esta validación, ArchiveCore queda **cerrado como reconstrucción académica moderna y proyecto de portafolio**, salvo mantenimiento correctivo o mejoras futuras deliberadas.

## 🧭 Continuidad académica

ArchiveCore comparte profesor con [**SalesIntel-DW**](https://github.com/Jairo0811/SalesIntel-DW).

Ambos proyectos fueron desarrollados bajo la docencia de **Carlos Caraballos**, aunque corresponden a asignaturas y objetivos diferentes.

| Orden | Asignatura | Proyecto | Período |
|---:|---|---|---|
| 1 | Bases de Datos Avanzadas (SOF-008) | **ArchiveCore** | Pendiente de documentar |
| 2 | Minería de Datos e Inteligencia de Negocios (SOF-014) | [**SalesIntel-DW**](https://github.com/Jairo0811/SalesIntel-DW) | 2017-C3 |

La relación entre ambos repositorios es exclusivamente **académica y docente**.

---

<div align="center">

**ArchiveCore — Información segura, trazable y bajo control.**

Proyecto académico reconstruido para portafolio.

</div>
