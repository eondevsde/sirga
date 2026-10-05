BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005123240_AddCompanyTradeName'
)
BEGIN
    ALTER TABLE [Companies] ADD [TradeName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005123240_AddCompanyTradeName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005123240_AddCompanyTradeName', N'10.0.8');
END;

COMMIT;
GO

