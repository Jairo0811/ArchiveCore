USE ArchiveCoreDb;
GO

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
    SELECT @OpenStatusId = RecordStatusId FROM dbo.RecordStatuses WHERE Code = 'OPEN';

    IF @OpenStatusId IS NULL
        THROW 50001, 'Required OPEN record status was not found.', 1;

    EXEC sys.sp_set_session_context @key=N'UserId', @value=@CreatedByUserId;

    BEGIN TRAN;

    INSERT INTO dbo.Records
    (RecordNumber, Title, Description, RecordStatusId, CreatedByUserId, AssignedToUserId)
    VALUES
    (@RecordNumber, @Title, @Description, @OpenStatusId, @CreatedByUserId, @AssignedToUserId);

    DECLARE @RecordId BIGINT = SCOPE_IDENTITY();

    IF @AssignedToUserId IS NOT NULL
    BEGIN
        DECLARE @AssignTypeId SMALLINT;
        SELECT @AssignTypeId = MovementTypeId FROM dbo.MovementTypes WHERE Code = 'ASSIGN';

        INSERT INTO dbo.RecordMovements
        (RecordId, MovementTypeId, FromUserId, ToUserId, PerformedByUserId, Notes)
        VALUES
        (@RecordId, @AssignTypeId, NULL, @AssignedToUserId, @CreatedByUserId, N'Initial record assignment.');
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

    EXEC sys.sp_set_session_context @key=N'UserId', @value=@UploadedByUserId;

    BEGIN TRAN;

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

    COMMIT TRAN;

    SELECT *
    FROM dbo.DocumentVersions
    WHERE DocumentId = @DocumentId AND VersionNumber = @NextVersion;
END
GO

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
    DECLARE @TransferTypeId SMALLINT;

    SELECT @TransferTypeId = MovementTypeId FROM dbo.MovementTypes WHERE Code = 'TRANSFER';

    IF @TransferTypeId IS NULL
        THROW 50002, 'Required TRANSFER movement type was not found.', 1;

    EXEC sys.sp_set_session_context @key=N'UserId', @value=@PerformedByUserId;

    BEGIN TRAN;

    SELECT @FromUserId = AssignedToUserId
    FROM dbo.Records WITH (UPDLOCK, HOLDLOCK)
    WHERE RecordId = @RecordId;

    IF @@ROWCOUNT = 0
        THROW 50003, 'Record was not found.', 1;

    UPDATE dbo.Records
    SET AssignedToUserId = @ToUserId,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE RecordId = @RecordId;

    INSERT INTO dbo.RecordMovements
    (RecordId, MovementTypeId, FromUserId, ToUserId, PerformedByUserId, Notes)
    VALUES
    (@RecordId, @TransferTypeId, @FromUserId, @ToUserId, @PerformedByUserId, @Notes);

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

    SELECT @ClosedStatusId = RecordStatusId FROM dbo.RecordStatuses WHERE Code = 'CLOSED';
    SELECT @CloseTypeId = MovementTypeId FROM dbo.MovementTypes WHERE Code = 'CLOSE';

    IF @ClosedStatusId IS NULL OR @CloseTypeId IS NULL
        THROW 50004, 'Required close catalogs were not found.', 1;

    EXEC sys.sp_set_session_context @key=N'UserId', @value=@PerformedByUserId;

    BEGIN TRAN;

    SELECT @AssignedToUserId = AssignedToUserId
    FROM dbo.Records WITH (UPDLOCK, HOLDLOCK)
    WHERE RecordId = @RecordId;

    IF @@ROWCOUNT = 0
        THROW 50005, 'Record was not found.', 1;

    UPDATE dbo.Records
    SET RecordStatusId = @ClosedStatusId,
        ClosedAtUtc = SYSUTCDATETIME(),
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE RecordId = @RecordId;

    INSERT INTO dbo.RecordMovements
    (RecordId, MovementTypeId, FromUserId, ToUserId, PerformedByUserId, Notes)
    VALUES
    (@RecordId, @CloseTypeId, @AssignedToUserId, @AssignedToUserId, @PerformedByUserId, @Notes);

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
