/*
    ArchiveCoreDb
    Phase 1 — Database Redesign & Normalization
    Course origin: Bases de Datos Avanzadas (SOF-008)
*/

IF DB_ID(N'ArchiveCoreDb') IS NULL
BEGIN
    CREATE DATABASE ArchiveCoreDb;
END
GO

USE ArchiveCoreDb;
GO

/* =========================
   SECURITY / IDENTITY
   ========================= */

CREATE TABLE dbo.Users
(
    UserId              INT IDENTITY(1,1) NOT NULL,
    FirstName           NVARCHAR(100) NOT NULL,
    LastName            NVARCHAR(100) NOT NULL,
    NationalId          VARCHAR(20) NULL,
    Email               NVARCHAR(256) NOT NULL,
    PasswordHash        NVARCHAR(500) NOT NULL,
    Phone               VARCHAR(25) NULL,
    BirthDate           DATE NULL,
    IsActive            BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedAtUtc        DATETIME2(0) NULL,
    RowVersion          ROWVERSION NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId)
);
GO

CREATE TABLE dbo.Roles
(
    RoleId              SMALLINT IDENTITY(1,1) NOT NULL,
    Name                NVARCHAR(80) NOT NULL,
    Description         NVARCHAR(250) NULL,
    IsSystemRole        BIT NOT NULL CONSTRAINT DF_Roles_IsSystemRole DEFAULT (0),
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId)
);
GO

CREATE TABLE dbo.UserRoles
(
    UserId              INT NOT NULL,
    RoleId              SMALLINT NOT NULL,
    AssignedAtUtc       DATETIME2(0) NOT NULL CONSTRAINT DF_UserRoles_AssignedAtUtc DEFAULT (SYSUTCDATETIME()),
    AssignedByUserId    INT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserId, RoleId)
);
GO

/* =========================
   CATALOGS
   ========================= */

CREATE TABLE dbo.RecordStatuses
(
    RecordStatusId      SMALLINT IDENTITY(1,1) NOT NULL,
    Code                VARCHAR(40) NOT NULL,
    Name                NVARCHAR(100) NOT NULL,
    Description         NVARCHAR(250) NULL,
    IsFinal             BIT NOT NULL CONSTRAINT DF_RecordStatuses_IsFinal DEFAULT (0),
    SortOrder           SMALLINT NOT NULL CONSTRAINT DF_RecordStatuses_SortOrder DEFAULT (0),
    CONSTRAINT PK_RecordStatuses PRIMARY KEY CLUSTERED (RecordStatusId)
);
GO

CREATE TABLE dbo.DocumentCategories
(
    DocumentCategoryId  INT IDENTITY(1,1) NOT NULL,
    Code                VARCHAR(40) NOT NULL,
    Name                NVARCHAR(120) NOT NULL,
    Description         NVARCHAR(300) NULL,
    IsActive            BIT NOT NULL CONSTRAINT DF_DocumentCategories_IsActive DEFAULT (1),
    CONSTRAINT PK_DocumentCategories PRIMARY KEY CLUSTERED (DocumentCategoryId)
);
GO

CREATE TABLE dbo.MovementTypes
(
    MovementTypeId      SMALLINT IDENTITY(1,1) NOT NULL,
    Code                VARCHAR(40) NOT NULL,
    Name                NVARCHAR(100) NOT NULL,
    Description         NVARCHAR(250) NULL,
    CONSTRAINT PK_MovementTypes PRIMARY KEY CLUSTERED (MovementTypeId)
);
GO

/* =========================
   RECORDS
   ========================= */

CREATE TABLE dbo.Records
(
    RecordId            BIGINT IDENTITY(1,1) NOT NULL,
    RecordNumber        VARCHAR(40) NOT NULL,
    Title               NVARCHAR(200) NOT NULL,
    Description         NVARCHAR(1000) NULL,
    RecordStatusId      SMALLINT NOT NULL,
    CreatedByUserId     INT NOT NULL,
    AssignedToUserId    INT NULL,
    OpenedAtUtc         DATETIME2(0) NOT NULL CONSTRAINT DF_Records_OpenedAtUtc DEFAULT (SYSUTCDATETIME()),
    ClosedAtUtc         DATETIME2(0) NULL,
    CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Records_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedAtUtc        DATETIME2(0) NULL,
    RowVersion          ROWVERSION NOT NULL,
    CONSTRAINT PK_Records PRIMARY KEY CLUSTERED (RecordId)
);
GO

/* =========================
   DOCUMENTS / VERSIONS
   ========================= */

CREATE TABLE dbo.Documents
(
    DocumentId          BIGINT IDENTITY(1,1) NOT NULL,
    RecordId            BIGINT NOT NULL,
    DocumentCategoryId  INT NOT NULL,
    DocumentNumber      VARCHAR(60) NULL,
    Title               NVARCHAR(200) NOT NULL,
    Description         NVARCHAR(1000) NULL,
    IsDeleted           BIT NOT NULL CONSTRAINT DF_Documents_IsDeleted DEFAULT (0),
    CreatedByUserId     INT NOT NULL,
    CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_Documents_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedAtUtc        DATETIME2(0) NULL,
    RowVersion          ROWVERSION NOT NULL,
    CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (DocumentId)
);
GO

CREATE TABLE dbo.DocumentVersions
(
    DocumentVersionId   BIGINT IDENTITY(1,1) NOT NULL,
    DocumentId          BIGINT NOT NULL,
    VersionNumber       INT NOT NULL,
    OriginalFileName    NVARCHAR(260) NOT NULL,
    StoredFileName      NVARCHAR(260) NOT NULL,
    StoragePath         NVARCHAR(1000) NOT NULL,
    FileExtension       VARCHAR(20) NULL,
    MimeType            NVARCHAR(150) NULL,
    FileSizeBytes       BIGINT NOT NULL,
    Sha256Hash          CHAR(64) NOT NULL,
    UploadedByUserId    INT NOT NULL,
    UploadedAtUtc       DATETIME2(0) NOT NULL CONSTRAINT DF_DocumentVersions_UploadedAtUtc DEFAULT (SYSUTCDATETIME()),
    IsCurrent           BIT NOT NULL CONSTRAINT DF_DocumentVersions_IsCurrent DEFAULT (1),
    CONSTRAINT PK_DocumentVersions PRIMARY KEY CLUSTERED (DocumentVersionId)
);
GO

/* =========================
   WORKFLOW / MOVEMENTS
   ========================= */

CREATE TABLE dbo.RecordMovements
(
    RecordMovementId    BIGINT IDENTITY(1,1) NOT NULL,
    RecordId            BIGINT NOT NULL,
    MovementTypeId      SMALLINT NOT NULL,
    FromUserId          INT NULL,
    ToUserId            INT NULL,
    PerformedByUserId   INT NOT NULL,
    Notes               NVARCHAR(1000) NULL,
    PerformedAtUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_RecordMovements_PerformedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_RecordMovements PRIMARY KEY CLUSTERED (RecordMovementId)
);
GO

/* =========================
   AUDIT
   ========================= */

CREATE TABLE dbo.AuditEvents
(
    AuditEventId        BIGINT IDENTITY(1,1) NOT NULL,
    UserId              INT NULL,
    EntityName          SYSNAME NOT NULL,
    EntityKey           NVARCHAR(200) NOT NULL,
    ActionType          VARCHAR(20) NOT NULL,
    OldValuesJson       NVARCHAR(MAX) NULL,
    NewValuesJson       NVARCHAR(MAX) NULL,
    IpAddress           VARCHAR(45) NULL,
    CorrelationId       UNIQUEIDENTIFIER NULL,
    CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_AuditEvents_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_AuditEvents PRIMARY KEY CLUSTERED (AuditEventId)
);
GO
