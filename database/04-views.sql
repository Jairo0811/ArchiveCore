USE ArchiveCoreDb;
GO

CREATE OR ALTER VIEW dbo.vw_ActiveRecords
AS
SELECT
    r.RecordId,
    r.RecordNumber,
    r.Title,
    r.Description,
    rs.Code AS StatusCode,
    rs.Name AS StatusName,
    r.OpenedAtUtc,
    r.UpdatedAtUtc,
    creator.UserId AS CreatedByUserId,
    CONCAT(creator.FirstName, N' ', creator.LastName) AS CreatedByName,
    assignee.UserId AS AssignedToUserId,
    CASE WHEN assignee.UserId IS NULL THEN NULL
         ELSE CONCAT(assignee.FirstName, N' ', assignee.LastName) END AS AssignedToName
FROM dbo.Records r
INNER JOIN dbo.RecordStatuses rs ON rs.RecordStatusId = r.RecordStatusId
INNER JOIN dbo.Users creator ON creator.UserId = r.CreatedByUserId
LEFT JOIN dbo.Users assignee ON assignee.UserId = r.AssignedToUserId
WHERE rs.IsFinal = 0;
GO

CREATE OR ALTER VIEW dbo.vw_RecordDocumentSummary
AS
SELECT
    r.RecordId,
    r.RecordNumber,
    r.Title AS RecordTitle,
    COUNT(d.DocumentId) AS DocumentCount,
    SUM(CASE WHEN d.IsDeleted = 0 THEN 1 ELSE 0 END) AS ActiveDocumentCount,
    MAX(d.CreatedAtUtc) AS LastDocumentCreatedAtUtc
FROM dbo.Records r
LEFT JOIN dbo.Documents d ON d.RecordId = r.RecordId
GROUP BY r.RecordId, r.RecordNumber, r.Title;
GO

CREATE OR ALTER VIEW dbo.vw_CurrentDocumentVersions
AS
SELECT
    d.DocumentId,
    d.RecordId,
    d.DocumentCategoryId,
    d.DocumentNumber,
    d.Title,
    dv.DocumentVersionId,
    dv.VersionNumber,
    dv.OriginalFileName,
    dv.StoredFileName,
    dv.StoragePath,
    dv.FileExtension,
    dv.MimeType,
    dv.FileSizeBytes,
    dv.Sha256Hash,
    dv.UploadedByUserId,
    dv.UploadedAtUtc
FROM dbo.Documents d
INNER JOIN dbo.DocumentVersions dv
    ON dv.DocumentId = d.DocumentId
   AND dv.IsCurrent = 1
WHERE d.IsDeleted = 0;
GO

CREATE OR ALTER VIEW dbo.vw_RecordMovementHistory
AS
SELECT
    rm.RecordMovementId,
    rm.RecordId,
    r.RecordNumber,
    mt.Code AS MovementTypeCode,
    mt.Name AS MovementTypeName,
    rm.FromUserId,
    CASE WHEN fu.UserId IS NULL THEN NULL ELSE CONCAT(fu.FirstName, N' ', fu.LastName) END AS FromUserName,
    rm.ToUserId,
    CASE WHEN tu.UserId IS NULL THEN NULL ELSE CONCAT(tu.FirstName, N' ', tu.LastName) END AS ToUserName,
    rm.PerformedByUserId,
    CONCAT(pu.FirstName, N' ', pu.LastName) AS PerformedByName,
    rm.Notes,
    rm.PerformedAtUtc
FROM dbo.RecordMovements rm
INNER JOIN dbo.Records r ON r.RecordId = rm.RecordId
INNER JOIN dbo.MovementTypes mt ON mt.MovementTypeId = rm.MovementTypeId
LEFT JOIN dbo.Users fu ON fu.UserId = rm.FromUserId
LEFT JOIN dbo.Users tu ON tu.UserId = rm.ToUserId
INNER JOIN dbo.Users pu ON pu.UserId = rm.PerformedByUserId;
GO

CREATE OR ALTER VIEW dbo.vw_AuditOverview
AS
SELECT
    ae.AuditEventId,
    ae.EntityName,
    ae.EntityKey,
    ae.ActionType,
    ae.CreatedAtUtc,
    ae.UserId,
    CASE WHEN u.UserId IS NULL THEN NULL ELSE CONCAT(u.FirstName, N' ', u.LastName) END AS UserName,
    ae.IpAddress,
    ae.CorrelationId
FROM dbo.AuditEvents ae
LEFT JOIN dbo.Users u ON u.UserId = ae.UserId;
GO
