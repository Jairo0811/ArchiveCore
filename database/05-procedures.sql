USE ArchiveCoreDb;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =========================================================
   ArchiveCore — transactional procedures

   These procedures are the single source of truth for the
   critical workflow mutations. The .NET API executes them
   directly (see docs/architecture/stored-procedures-vs-ef.md);
   EF Core is used for queries and metadata-only updates.

   Business errors use THROW with stable numbers that the API
   maps to HTTP status codes:

   50001  OPEN record status missing            (500)
   50002  TRANSFER movement type missing        (500)
   50003  Record not found (transfer)           (404)
   50004  Close catalogs missing                (500)
   50005  Record not found (close)              (404)
   50006  ASSIGN movement type missing          (500)
   50010  Record number already exists          (409)
   50011  Target user not found or inactive     (400)
   50012  Acting user not found or inactive     (403)
   50013  Record is already closed/final        (409)
   50014  Record already assigned to that user  (409)
   50020  Document not found or deleted         (404)
   50021  Document belongs to a closed record   (409)
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_CreateRecord
    @RecordNumber       VARCHAR(40),
    @Title              NVARCHAR(200),
    @Description        NVARCHAR(1000) = NULL,
    @CreatedByUserId    INT,
    @AssignedToUserId   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OpenStatusId SMALLINT;
    DECLARE @AssignTypeId SMALLINT;

    SELECT @OpenStatusId = RecordStatusId FROM dbo.RecordStatuses WHERE Code = 'OPEN';
    SELECT @AssignTypeId = MovementTypeId FROM dbo.MovementTypes WHERE Code = 'ASSIGN';

    IF @OpenStatusId IS NULL
        THROW 50001, 'Required OPEN record status was not found.', 1;

    IF @AssignTypeId IS NULL
        THROW 50006, 'Required ASSIGN movement type was not found.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @CreatedByUserId AND IsActive = 1)
        THROW 50012, 'Acting user was not found or is inactive.', 1;

    IF @AssignedToUserId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @AssignedToUserId AND IsActive = 1)
        THROW 50011, 'Assigned user was not found or is inactive.', 1;

    EXEC sys.sp_set_session_context @key = N'UserId', @value = @CreatedByUserId;

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.Records WITH (UPDLOCK, HOLDLOCK) WHERE RecordNumber = @RecordNumber)
        THROW 50010, 'Record number already exists.', 1;

    INSERT INTO dbo.Records
    (RecordNumber, Title, Description, RecordStatusId, CreatedByUserId, AssignedToUserId)
    VALUES
    (@RecordNumber, @Title, @Description, @OpenStatusId, @CreatedByUserId, @AssignedToUserId);

    DECLARE @RecordId BIGINT = SCOPE_IDENTITY();

    IF @AssignedToUserId IS NOT NULL
    BEGIN
        INSERT INTO dbo.RecordMovements
        (RecordId, MovementTypeId, FromUserId, ToUserId, PerformedByUserId, Notes)
        VALUES
        (@RecordId, @AssignTypeId, NULL, @AssignedToUserId, @CreatedByUserId, N'Asignación inicial del expediente.');
    END

    COMMIT TRAN;

    SELECT * FROM dbo.Records WHERE RecordId = @RecordId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddDocumentVersion
    @DocumentId          BIGINT,
    @OriginalFileName    NVARCHAR(260),
    @StoredFileName      NVARCHAR(260),
    @StoragePath         NVARCHAR(1000),
    @FileExtension       VARCHAR(20) = NULL,
    @MimeType            NVARCHAR(150) = NULL,
    @FileSizeBytes       BIGINT,
    @Sha256Hash          CHAR(64),
    @UploadedByUserId    INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NextVersion INT;
    DECLARE @RecordIsFinal BIT;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @UploadedByUserId AND IsActive = 1)
        THROW 50012, 'Acting user was not found or is inactive.', 1;

    EXEC sys.sp_set_session_context @key = N'UserId', @value = @UploadedByUserId;

    BEGIN TRAN;

    /* Serialize concurrent uploads for the same document. */
    SELECT @RecordIsFinal = rs.IsFinal
    FROM dbo.Documents d WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN dbo.Records r ON r.RecordId = d.RecordId
    INNER JOIN dbo.RecordStatuses rs ON rs.RecordStatusId = r.RecordStatusId
    WHERE d.DocumentId = @DocumentId
      AND d.IsDeleted = 0;

    IF @RecordIsFinal IS NULL
        THROW 50020, 'Document was not found or has been deleted.', 1;

    IF @RecordIsFinal = 1
        THROW 50021, 'Document belongs to a closed record.', 1;

    SELECT @NextVersion = ISNULL(MAX(VersionNumber), 0) + 1
    FROM dbo.DocumentVersions WITH (UPDLOCK, HOLDLOCK)
    WHERE DocumentId = @DocumentId;

    UPDATE dbo.DocumentVersions
    SET IsCurrent = 0
    WHERE DocumentId = @DocumentId AND IsCurrent = 1;

    INSERT INTO dbo.DocumentVersions
    (DocumentId, VersionNumber, OriginalFileName, StoredFileName, StoragePath, FileExtension,
     MimeType, FileSizeBytes, Sha256Hash, UploadedByUserId, IsCurrent)
    VALUES
    (@DocumentId, @NextVersion, @OriginalFileName, @StoredFileName, @StoragePath, @FileExtension,
     @MimeType, @FileSizeBytes, @Sha256Hash, @UploadedByUserId, 1);

    UPDATE dbo.Documents
    SET UpdatedAtUtc = SYSUTCDATETIME()
    WHERE DocumentId = @DocumentId;

    COMMIT TRAN;

    SELECT *
    FROM dbo.DocumentVersions
    WHERE DocumentId = @DocumentId AND VersionNumber = @NextVersion;
END
GO

/*
   Assigns an unassigned record (ASSIGN) or transfers it between users (TRANSFER).
*/
CREATE OR ALTER PROCEDURE dbo.sp_TransferRecord
    @RecordId            BIGINT,
    @ToUserId            INT,
    @PerformedByUserId   INT,
    @Notes               NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @FromUserId INT;
    DECLARE @IsFinal BIT;
    DECLARE @TransferTypeId SMALLINT;
    DECLARE @AssignTypeId SMALLINT;
    DECLARE @MovementTypeId SMALLINT;

    SELECT @TransferTypeId = MovementTypeId FROM dbo.MovementTypes WHERE Code = 'TRANSFER';
    SELECT @AssignTypeId = MovementTypeId FROM dbo.MovementTypes WHERE Code = 'ASSIGN';

    IF @TransferTypeId IS NULL
        THROW 50002, 'Required TRANSFER movement type was not found.', 1;

    IF @AssignTypeId IS NULL
        THROW 50006, 'Required ASSIGN movement type was not found.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @PerformedByUserId AND IsActive = 1)
        THROW 50012, 'Acting user was not found or is inactive.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @ToUserId AND IsActive = 1)
        THROW 50011, 'Target user was not found or is inactive.', 1;

    EXEC sys.sp_set_session_context @key = N'UserId', @value = @PerformedByUserId;

    BEGIN TRAN;

    SELECT @FromUserId = r.AssignedToUserId,
           @IsFinal = rs.IsFinal
    FROM dbo.Records r WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN dbo.RecordStatuses rs ON rs.RecordStatusId = r.RecordStatusId
    WHERE r.RecordId = @RecordId;

    IF @IsFinal IS NULL
        THROW 50003, 'Record was not found.', 1;

    IF @IsFinal = 1
        THROW 50013, 'Record is closed and cannot be transferred.', 1;

    IF @FromUserId = @ToUserId
        THROW 50014, 'Record is already assigned to that user.', 1;

    SET @MovementTypeId = CASE WHEN @FromUserId IS NULL THEN @AssignTypeId ELSE @TransferTypeId END;

    UPDATE dbo.Records
    SET AssignedToUserId = @ToUserId,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE RecordId = @RecordId;

    INSERT INTO dbo.RecordMovements
    (RecordId, MovementTypeId, FromUserId, ToUserId, PerformedByUserId, Notes)
    VALUES
    (@RecordId, @MovementTypeId, @FromUserId, @ToUserId, @PerformedByUserId, @Notes);

    COMMIT TRAN;

    SELECT * FROM dbo.Records WHERE RecordId = @RecordId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CloseRecord
    @RecordId            BIGINT,
    @PerformedByUserId   INT,
    @Notes               NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ClosedStatusId SMALLINT;
    DECLARE @CloseTypeId SMALLINT;
    DECLARE @AssignedToUserId INT;
    DECLARE @IsFinal BIT;

    SELECT @ClosedStatusId = RecordStatusId FROM dbo.RecordStatuses WHERE Code = 'CLOSED';
    SELECT @CloseTypeId = MovementTypeId FROM dbo.MovementTypes WHERE Code = 'CLOSE';

    IF @ClosedStatusId IS NULL OR @CloseTypeId IS NULL
        THROW 50004, 'Required close catalogs were not found.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @PerformedByUserId AND IsActive = 1)
        THROW 50012, 'Acting user was not found or is inactive.', 1;

    EXEC sys.sp_set_session_context @key = N'UserId', @value = @PerformedByUserId;

    BEGIN TRAN;

    SELECT @AssignedToUserId = r.AssignedToUserId,
           @IsFinal = rs.IsFinal
    FROM dbo.Records r WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN dbo.RecordStatuses rs ON rs.RecordStatusId = r.RecordStatusId
    WHERE r.RecordId = @RecordId;

    IF @IsFinal IS NULL
        THROW 50005, 'Record was not found.', 1;

    IF @IsFinal = 1
        THROW 50013, 'Record is already closed.', 1;

    UPDATE dbo.Records
    SET RecordStatusId = @ClosedStatusId,
        ClosedAtUtc = SYSUTCDATETIME(),
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE RecordId = @RecordId;

    /* CK_RecordMovements_Users requires at least one user: fall back to the actor
       when the record was never assigned. */
    INSERT INTO dbo.RecordMovements
    (RecordId, MovementTypeId, FromUserId, ToUserId, PerformedByUserId, Notes)
    VALUES
    (@RecordId, @CloseTypeId,
     COALESCE(@AssignedToUserId, @PerformedByUserId),
     COALESCE(@AssignedToUserId, @PerformedByUserId),
     @PerformedByUserId, @Notes);

    COMMIT TRAN;

    SELECT * FROM dbo.Records WHERE RecordId = @RecordId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_SearchRecords
    @Search              NVARCHAR(200) = NULL,
    @RecordStatusId      SMALLINT = NULL,
    @AssignedToUserId    INT = NULL,
    @OpenedFromUtc       DATETIME2(0) = NULL,
    @OpenedToUtc         DATETIME2(0) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.RecordId, r.RecordNumber, r.Title, r.Description,
        r.RecordStatusId, rs.Name AS StatusName,
        r.AssignedToUserId, r.OpenedAtUtc, r.ClosedAtUtc, r.UpdatedAtUtc
    FROM dbo.Records r
    INNER JOIN dbo.RecordStatuses rs ON rs.RecordStatusId = r.RecordStatusId
    WHERE
        (@Search IS NULL OR r.RecordNumber LIKE '%' + @Search + '%' OR r.Title LIKE '%' + @Search + '%')
        AND (@RecordStatusId IS NULL OR r.RecordStatusId = @RecordStatusId)
        AND (@AssignedToUserId IS NULL OR r.AssignedToUserId = @AssignedToUserId)
        AND (@OpenedFromUtc IS NULL OR r.OpenedAtUtc >= @OpenedFromUtc)
        AND (@OpenedToUtc IS NULL OR r.OpenedAtUtc < @OpenedToUtc)
    ORDER BY r.OpenedAtUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetRecordHistory
    @RecordId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.vw_RecordMovementHistory
    WHERE RecordId = @RecordId
    ORDER BY PerformedAtUtc DESC, RecordMovementId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetEntityAudit
    @EntityName SYSNAME,
    @EntityKey NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT AuditEventId, UserId, EntityName, EntityKey, ActionType,
           OldValuesJson, NewValuesJson, IpAddress, CorrelationId, CreatedAtUtc
    FROM dbo.AuditEvents
    WHERE EntityName = @EntityName
      AND EntityKey = @EntityKey
    ORDER BY CreatedAtUtc DESC, AuditEventId DESC;
END
GO
