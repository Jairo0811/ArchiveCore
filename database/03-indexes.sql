USE ArchiveCoreDb;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/* =========================================================
   ArchiveCore — Phase 2
   Performance indexes based on expected query patterns
   ========================================================= */

CREATE INDEX IX_Users_IsActive_LastName_FirstName
ON dbo.Users (IsActive, LastName, FirstName)
INCLUDE (Email, Phone);
GO

CREATE INDEX IX_Records_Status_OpenedAt
ON dbo.Records (RecordStatusId, OpenedAtUtc DESC)
INCLUDE (RecordNumber, Title, AssignedToUserId, CreatedByUserId, ClosedAtUtc);
GO

CREATE INDEX IX_Records_AssignedToUser_Status
ON dbo.Records (AssignedToUserId, RecordStatusId)
INCLUDE (RecordNumber, Title, OpenedAtUtc, UpdatedAtUtc);
GO

CREATE INDEX IX_Records_CreatedByUser
ON dbo.Records (CreatedByUserId, CreatedAtUtc DESC)
INCLUDE (RecordNumber, Title, RecordStatusId);
GO

CREATE INDEX IX_Documents_RecordId_IsDeleted
ON dbo.Documents (RecordId, IsDeleted)
INCLUDE (DocumentCategoryId, DocumentNumber, Title, CreatedAtUtc);
GO

CREATE INDEX IX_Documents_Category
ON dbo.Documents (DocumentCategoryId, CreatedAtUtc DESC)
INCLUDE (RecordId, Title, DocumentNumber, IsDeleted);
GO

CREATE INDEX IX_DocumentVersions_Document_Current
ON dbo.DocumentVersions (DocumentId, IsCurrent DESC, VersionNumber DESC)
INCLUDE (OriginalFileName, StoragePath, MimeType, FileSizeBytes, Sha256Hash, UploadedAtUtc);
GO

CREATE UNIQUE INDEX UX_DocumentVersions_Current
ON dbo.DocumentVersions (DocumentId)
WHERE IsCurrent = 1;
GO

CREATE INDEX IX_RecordMovements_Record_PerformedAt
ON dbo.RecordMovements (RecordId, PerformedAtUtc DESC)
INCLUDE (MovementTypeId, FromUserId, ToUserId, PerformedByUserId, Notes);
GO

CREATE INDEX IX_RecordMovements_ToUser_PerformedAt
ON dbo.RecordMovements (ToUserId, PerformedAtUtc DESC)
INCLUDE (RecordId, MovementTypeId, FromUserId, PerformedByUserId);
GO

CREATE INDEX IX_AuditEvents_Entity
ON dbo.AuditEvents (EntityName, EntityKey, CreatedAtUtc DESC)
INCLUDE (UserId, ActionType, CorrelationId);
GO

CREATE INDEX IX_AuditEvents_User
ON dbo.AuditEvents (UserId, CreatedAtUtc DESC)
INCLUDE (EntityName, EntityKey, ActionType);
GO
