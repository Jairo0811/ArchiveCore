USE ArchiveCoreDb;
GO

INSERT INTO dbo.Roles (Name, Description, IsSystemRole)
VALUES
(N'Administrator', N'Full system administration role.', 1),
(N'RecordsManager', N'Manages records, documents and workflow.', 1),
(N'User', N'Standard operational user.', 1);
GO

INSERT INTO dbo.RecordStatuses (Code, Name, Description, IsFinal, SortOrder)
VALUES
('OPEN', N'Open', N'Record is active and available for processing.', 0, 10),
('IN_REVIEW', N'In Review', N'Record is under review.', 0, 20),
('CLOSED', N'Closed', N'Record lifecycle is complete.', 1, 90),
('CANCELLED', N'Cancelled', N'Record was cancelled.', 1, 99);
GO

INSERT INTO dbo.MovementTypes (Code, Name, Description)
VALUES
('ASSIGN', N'Assignment', N'Assigns a record to a user.'),
('TRANSFER', N'Transfer', N'Transfers responsibility between users.'),
('RETURN', N'Return', N'Returns a record to a previous responsible user.'),
('CLOSE', N'Close', N'Closes the record workflow.');
GO

INSERT INTO dbo.DocumentCategories (Code, Name, Description)
VALUES
('GENERAL', N'General', N'General-purpose documentation.'),
('REQUEST', N'Request', N'Initial request or submission.'),
('EVIDENCE', N'Evidence', N'Supporting evidence or attachments.'),
('REPORT', N'Report', N'Formal report or result document.');
GO
