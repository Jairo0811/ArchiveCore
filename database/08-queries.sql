USE ArchiveCoreDb;
GO

/* Active records assigned to each user */
SELECT
    AssignedToName,
    COUNT(*) AS ActiveRecordCount
FROM dbo.vw_ActiveRecords
GROUP BY AssignedToName
ORDER BY ActiveRecordCount DESC;
GO

/* Records with the largest number of active documents */
SELECT TOP (20)
    RecordId,
    RecordNumber,
    RecordTitle,
    ActiveDocumentCount
FROM dbo.vw_RecordDocumentSummary
ORDER BY ActiveDocumentCount DESC, RecordNumber;
GO

/* Full movement history for one record */
DECLARE @RecordId BIGINT = 1;

SELECT *
FROM dbo.vw_RecordMovementHistory
WHERE RecordId = @RecordId
ORDER BY PerformedAtUtc DESC;
GO

/* Current physical version for every active logical document */
SELECT
    RecordId,
    DocumentId,
    Title,
    VersionNumber,
    OriginalFileName,
    FileSizeBytes,
    UploadedAtUtc
FROM dbo.vw_CurrentDocumentVersions
ORDER BY UploadedAtUtc DESC;
GO

/* Audit activity grouped by entity and action */
SELECT
    EntityName,
    ActionType,
    COUNT(*) AS EventCount
FROM dbo.AuditEvents
GROUP BY EntityName, ActionType
ORDER BY EntityName, ActionType;
GO

/* Users with the most workflow activity */
SELECT TOP (20)
    u.UserId,
    CONCAT(u.FirstName, N' ', u.LastName) AS UserName,
    COUNT(rm.RecordMovementId) AS MovementCount
FROM dbo.Users u
INNER JOIN dbo.RecordMovements rm ON rm.PerformedByUserId = u.UserId
GROUP BY u.UserId, u.FirstName, u.LastName
ORDER BY MovementCount DESC;
GO
