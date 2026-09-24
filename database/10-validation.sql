USE ArchiveCoreDb;
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL THROW 51000, 'Users table is missing.', 1;
IF OBJECT_ID(N'dbo.Records', N'U') IS NULL THROW 51001, 'Records table is missing.', 1;
IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL THROW 51002, 'Documents table is missing.', 1;
IF OBJECT_ID(N'dbo.DocumentVersions', N'U') IS NULL THROW 51003, 'DocumentVersions table is missing.', 1;
IF OBJECT_ID(N'dbo.RecordMovements', N'U') IS NULL THROW 51004, 'RecordMovements table is missing.', 1;
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NULL THROW 51005, 'AuditEvents table is missing.', 1;
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL THROW 51006, 'RefreshTokens table is missing.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE [name] = N'UQ_Users_Email'
      AND parent_object_id = OBJECT_ID(N'dbo.Users')
)
    THROW 51007, 'UQ_Users_Email constraint is missing.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE [name] = N'FK_UserRoles_Users'
)
    THROW 51008, 'FK_UserRoles_Users is missing.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE [name] = N'CK_DocumentVersions_VersionNumber'
)
    THROW 51009, 'CK_DocumentVersions_VersionNumber is missing.', 1;

IF OBJECT_ID(N'dbo.vw_ActiveRecords', N'V') IS NULL THROW 51010, 'vw_ActiveRecords is missing.', 1;
IF OBJECT_ID(N'dbo.vw_CurrentDocumentVersions', N'V') IS NULL THROW 51011, 'vw_CurrentDocumentVersions is missing.', 1;
IF OBJECT_ID(N'dbo.vw_RecordMovementHistory', N'V') IS NULL THROW 51012, 'vw_RecordMovementHistory is missing.', 1;

IF OBJECT_ID(N'dbo.sp_CreateRecord', N'P') IS NULL THROW 51020, 'sp_CreateRecord is missing.', 1;
IF OBJECT_ID(N'dbo.sp_AddDocumentVersion', N'P') IS NULL THROW 51021, 'sp_AddDocumentVersion is missing.', 1;
IF OBJECT_ID(N'dbo.sp_TransferRecord', N'P') IS NULL THROW 51022, 'sp_TransferRecord is missing.', 1;
IF OBJECT_ID(N'dbo.sp_CloseRecord', N'P') IS NULL THROW 51023, 'sp_CloseRecord is missing.', 1;

IF OBJECT_ID(N'dbo.TR_Records_Audit', N'TR') IS NULL THROW 51030, 'TR_Records_Audit is missing.', 1;
IF OBJECT_ID(N'dbo.TR_Documents_Audit', N'TR') IS NULL THROW 51031, 'TR_Documents_Audit is missing.', 1;
IF OBJECT_ID(N'dbo.TR_DocumentVersions_Audit', N'TR') IS NULL THROW 51032, 'TR_DocumentVersions_Audit is missing.', 1;
IF OBJECT_ID(N'dbo.TR_Users_Audit', N'TR') IS NULL THROW 51033, 'TR_Users_Audit is missing.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'UX_Users_NationalId'
      AND object_id = OBJECT_ID(N'dbo.Users')
)
    THROW 51034, 'UX_Users_NationalId index is missing.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'IX_Records_Status_OpenedAt'
      AND object_id = OBJECT_ID(N'dbo.Records')
)
    THROW 51035, 'IX_Records_Status_OpenedAt index is missing.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [name] = N'UX_DocumentVersions_Current'
      AND object_id = OBJECT_ID(N'dbo.DocumentVersions')
)
    THROW 51036, 'UX_DocumentVersions_Current index is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'Administrator')
    THROW 51040, 'Administrator seed role is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.RecordStatuses WHERE Code = 'OPEN')
    THROW 51041, 'OPEN record status is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.RecordStatuses WHERE Code = 'CLOSED')
    THROW 51042, 'CLOSED record status is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.MovementTypes WHERE Code = 'TRANSFER')
    THROW 51043, 'TRANSFER movement type is missing.', 1;

PRINT 'ArchiveCore database validation passed.';
GO
