USE ArchiveCoreDb;
GO

/* =========================================================
   Audit convention
   The application/SP layer may set SESSION_CONTEXT('UserId').
   Triggers fall back to entity creator/uploader where possible.
   ========================================================= */

CREATE OR ALTER TRIGGER dbo.TR_Records_Audit
ON dbo.Records
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditEvents
    (UserId, EntityName, EntityKey, ActionType, OldValuesJson, NewValuesJson)
    SELECT
        COALESCE(TRY_CONVERT(INT, SESSION_CONTEXT(N'UserId')), i.CreatedByUserId, d.CreatedByUserId),
        N'Records',
        CONVERT(NVARCHAR(200), COALESCE(i.RecordId, d.RecordId)),
        CASE
            WHEN i.RecordId IS NOT NULL AND d.RecordId IS NULL THEN 'INSERT'
            WHEN i.RecordId IS NOT NULL AND d.RecordId IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        CASE WHEN d.RecordId IS NULL THEN NULL ELSE
            (SELECT d.RecordNumber, d.Title, d.Description, d.RecordStatusId, d.AssignedToUserId,
                    d.OpenedAtUtc, d.ClosedAtUtc
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END,
        CASE WHEN i.RecordId IS NULL THEN NULL ELSE
            (SELECT i.RecordNumber, i.Title, i.Description, i.RecordStatusId, i.AssignedToUserId,
                    i.OpenedAtUtc, i.ClosedAtUtc
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON d.RecordId = i.RecordId;
END
GO

CREATE OR ALTER TRIGGER dbo.TR_Documents_Audit
ON dbo.Documents
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditEvents
    (UserId, EntityName, EntityKey, ActionType, OldValuesJson, NewValuesJson)
    SELECT
        COALESCE(TRY_CONVERT(INT, SESSION_CONTEXT(N'UserId')), i.CreatedByUserId, d.CreatedByUserId),
        N'Documents',
        CONVERT(NVARCHAR(200), COALESCE(i.DocumentId, d.DocumentId)),
        CASE
            WHEN i.DocumentId IS NOT NULL AND d.DocumentId IS NULL THEN 'INSERT'
            WHEN i.DocumentId IS NOT NULL AND d.DocumentId IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        CASE WHEN d.DocumentId IS NULL THEN NULL ELSE
            (SELECT d.RecordId, d.DocumentCategoryId, d.DocumentNumber, d.Title, d.Description, d.IsDeleted
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END,
        CASE WHEN i.DocumentId IS NULL THEN NULL ELSE
            (SELECT i.RecordId, i.DocumentCategoryId, i.DocumentNumber, i.Title, i.Description, i.IsDeleted
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON d.DocumentId = i.DocumentId;
END
GO

CREATE OR ALTER TRIGGER dbo.TR_DocumentVersions_Audit
ON dbo.DocumentVersions
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditEvents
    (UserId, EntityName, EntityKey, ActionType, OldValuesJson, NewValuesJson)
    SELECT
        COALESCE(TRY_CONVERT(INT, SESSION_CONTEXT(N'UserId')), i.UploadedByUserId, d.UploadedByUserId),
        N'DocumentVersions',
        CONVERT(NVARCHAR(200), COALESCE(i.DocumentVersionId, d.DocumentVersionId)),
        CASE
            WHEN i.DocumentVersionId IS NOT NULL AND d.DocumentVersionId IS NULL THEN 'INSERT'
            WHEN i.DocumentVersionId IS NOT NULL AND d.DocumentVersionId IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        CASE WHEN d.DocumentVersionId IS NULL THEN NULL ELSE
            (SELECT d.DocumentId, d.VersionNumber, d.OriginalFileName, d.StoragePath,
                    d.FileSizeBytes, d.Sha256Hash, d.IsCurrent
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END,
        CASE WHEN i.DocumentVersionId IS NULL THEN NULL ELSE
            (SELECT i.DocumentId, i.VersionNumber, i.OriginalFileName, i.StoragePath,
                    i.FileSizeBytes, i.Sha256Hash, i.IsCurrent
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON d.DocumentVersionId = i.DocumentVersionId;
END
GO

CREATE OR ALTER TRIGGER dbo.TR_Users_Audit
ON dbo.Users
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditEvents
    (UserId, EntityName, EntityKey, ActionType, OldValuesJson, NewValuesJson)
    SELECT
        TRY_CONVERT(INT, SESSION_CONTEXT(N'UserId')),
        N'Users',
        CONVERT(NVARCHAR(200), COALESCE(i.UserId, d.UserId)),
        CASE
            WHEN i.UserId IS NOT NULL AND d.UserId IS NULL THEN 'INSERT'
            WHEN i.UserId IS NOT NULL AND d.UserId IS NOT NULL THEN 'UPDATE'
            ELSE 'DELETE'
        END,
        CASE WHEN d.UserId IS NULL THEN NULL ELSE
            (SELECT d.FirstName, d.LastName, d.NationalId, d.Email, d.Phone, d.BirthDate, d.IsActive
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END,
        CASE WHEN i.UserId IS NULL THEN NULL ELSE
            (SELECT i.FirstName, i.LastName, i.NationalId, i.Email, i.Phone, i.BirthDate, i.IsActive
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON d.UserId = i.UserId;
END
GO
