BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002114349_AddUnionAuditors'
)
BEGIN
    CREATE TABLE [UnionAuditors] (
        [Id] int NOT NULL IDENTITY,
        [UnionId] int NOT NULL,
        [Username] nvarchar(max) NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [AssignedByUsername] nvarchar(max) NULL,
        CONSTRAINT [PK_UnionAuditors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnionAuditors_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002114349_AddUnionAuditors'
)
BEGIN
    CREATE INDEX [IX_UnionAuditors_UnionId] ON [UnionAuditors] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002114349_AddUnionAuditors'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002114349_AddUnionAuditors', N'10.0.8');
END;

COMMIT;
GO

