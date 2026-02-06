INSERT INTO dbo.SyncRecords (Id, ExternalKey, PayLoadHash, Status, Message, Attempts, LastAttemptAt, CreatedAt, UpdatedAt)
VALUES
(NEWID(), 'KEY-001', 'h1', 0, '', 0, NULL, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'KEY-002', 'h1', 0, '', 0, NULL, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'KEY-003', 'h1', 0, '', 0, NULL, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
