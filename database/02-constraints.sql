USE ArchiveCoreDb;
GO

/* =========================
   UNIQUE CONSTRAINTS
   ========================= */

ALTER TABLE dbo.Users
ADD CONSTRAINT UQ_Users_Email UNIQUE (Email);
GO

CREATE UNIQUE INDEX UX_Users_NationalId
ON dbo.Users (NationalId)
WHERE NationalId IS NOT NULL;
GO

ALTER TABLE dbo.Roles
ADD CONSTRAINT UQ_Roles_Name UNIQUE (Name);
GO

ALTER TABLE dbo.RecordStatuses
ADD CONSTRAINT UQ_RecordStatuses_Code UNIQUE (Code),
    CONSTRAINT UQ_RecordStatuses_Name UNIQUE (Name);
GO

ALTER TABLE dbo.DocumentCategories
ADD CONSTRAINT UQ_DocumentCategories_Code UNIQUE (Code),
    CONSTRAINT UQ_DocumentCategories_Name UNIQUE (Name);
GO

ALTER TABLE dbo.MovementTypes
ADD CONSTRAINT UQ_MovementTypes_Code UNIQUE (Code),
    CONSTRAINT UQ_MovementTypes_Name UNIQUE (Name);
GO

ALTER TABLE dbo.Records
ADD CONSTRAINT UQ_Records_RecordNumber UNIQUE (RecordNumber);
GO

ALTER TABLE dbo.DocumentVersions
ADD CONSTRAINT UQ_DocumentVersions_Document_Version UNIQUE (DocumentId, VersionNumber);
GO

/* =========================
   FOREIGN KEYS
   ========================= */

ALTER TABLE dbo.UserRoles
ADD CONSTRAINT FK_UserRoles_Users
    FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_UserRoles_Roles
    FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
    CONSTRAINT FK_UserRoles_AssignedByUser
    FOREIGN KEY (AssignedByUserId) REFERENCES dbo.Users(UserId);
GO

ALTER TABLE dbo.Records
ADD CONSTRAINT FK_Records_RecordStatuses
    FOREIGN KEY (RecordStatusId) REFERENCES dbo.RecordStatuses(RecordStatusId),
    CONSTRAINT FK_Records_CreatedByUser
    FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Records_AssignedToUser
    FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(UserId);
GO

ALTER TABLE dbo.Documents
ADD CONSTRAINT FK_Documents_Records
    FOREIGN KEY (RecordId) REFERENCES dbo.Records(RecordId),
    CONSTRAINT FK_Documents_DocumentCategories
    FOREIGN KEY (DocumentCategoryId) REFERENCES dbo.DocumentCategories(DocumentCategoryId),
    CONSTRAINT FK_Documents_CreatedByUser
    FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId);
GO

ALTER TABLE dbo.DocumentVersions
ADD CONSTRAINT FK_DocumentVersions_Documents
    FOREIGN KEY (DocumentId) REFERENCES dbo.Documents(DocumentId),
    CONSTRAINT FK_DocumentVersions_UploadedByUser
    FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users(UserId);
GO

ALTER TABLE dbo.RecordMovements
ADD CONSTRAINT FK_RecordMovements_Records
    FOREIGN KEY (RecordId) REFERENCES dbo.Records(RecordId),
    CONSTRAINT FK_RecordMovements_MovementTypes
    FOREIGN KEY (MovementTypeId) REFERENCES dbo.MovementTypes(MovementTypeId),
    CONSTRAINT FK_RecordMovements_FromUser
    FOREIGN KEY (FromUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_RecordMovements_ToUser
    FOREIGN KEY (ToUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_RecordMovements_PerformedByUser
    FOREIGN KEY (PerformedByUserId) REFERENCES dbo.Users(UserId);
GO

ALTER TABLE dbo.AuditEvents
ADD CONSTRAINT FK_AuditEvents_Users
    FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId);
GO

/* =========================
   CHECK CONSTRAINTS
   ========================= */

ALTER TABLE dbo.DocumentVersions
ADD CONSTRAINT CK_DocumentVersions_VersionNumber
    CHECK (VersionNumber > 0),
    CONSTRAINT CK_DocumentVersions_FileSizeBytes
    CHECK (FileSizeBytes >= 0),
    CONSTRAINT CK_DocumentVersions_Sha256Hash
    CHECK (LEN(Sha256Hash) = 64);
GO

ALTER TABLE dbo.Records
ADD CONSTRAINT CK_Records_CloseDate
    CHECK (ClosedAtUtc IS NULL OR ClosedAtUtc >= OpenedAtUtc);
GO

ALTER TABLE dbo.RecordMovements
ADD CONSTRAINT CK_RecordMovements_Users
    CHECK (FromUserId IS NOT NULL OR ToUserId IS NOT NULL);
GO

ALTER TABLE dbo.AuditEvents
ADD CONSTRAINT CK_AuditEvents_ActionType
    CHECK (ActionType IN ('INSERT','UPDATE','DELETE','LOGIN','LOGOUT','DOWNLOAD','OTHER'));
GO
