CREATE TABLE dbo.SyncRecords
(
	Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	ExternalKey NVARCHAR(200) NOT NULL,
	PayLoadHash NVARCHAR(100) NOT NULL CONSTRAINT DF_SyncRecords_PayLoadHash DEFAULT '',
	Status INT NOT NULL,
    Message NVARCHAR(2000) NOT NULL CONSTRAINT DF_SyncRecords_Message DEFAULT '',
    Attempts INT NOT NULL CONSTRAINT DF_SyncRecords_Attempts DEFAULT 0,
    LastAttemptAt DATETIMEOFFSET NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL
);

CREATE INDEX IX_SyncRecords_Status_CreatedAt ON dbo.SyncRecords(Status, CreatedAt);
CREATE UNIQUE INDEX UX_SyncRecords_ExternalKey ON dbo.SyncRecords(ExternalKey);