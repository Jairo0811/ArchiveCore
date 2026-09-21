USE ArchiveCoreDb;
GO

CREATE TABLE dbo.RefreshTokens
(
    RefreshTokenId      BIGINT IDENTITY(1,1) NOT NULL,
    UserId              INT NOT NULL,
    TokenHash           CHAR(64) NOT NULL,
    ExpiresAtUtc        DATETIME2(0) NOT NULL,
    CreatedAtUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    RevokedAtUtc        DATETIME2(0) NULL,
    ReplacedByTokenId   BIGINT NULL,
    CreatedByIp         VARCHAR(45) NULL,
    RevokedByIp         VARCHAR(45) NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (RefreshTokenId),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_RefreshTokens_ReplacedBy FOREIGN KEY (ReplacedByTokenId) REFERENCES dbo.RefreshTokens(RefreshTokenId),
    CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash),
    CONSTRAINT CK_RefreshTokens_Expiry CHECK (ExpiresAtUtc > CreatedAtUtc)
);
GO

CREATE INDEX IX_RefreshTokens_User_Expires
ON dbo.RefreshTokens (UserId, ExpiresAtUtc DESC)
INCLUDE (RevokedAtUtc, TokenHash);
GO
