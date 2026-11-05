/* ==========================================================================================
   02 — ESQUEMA COMPLETO DE SIRGA (55 migraciones, generado por EF Core)
   ------------------------------------------------------------------------------------------
   Generado con:
     dotnet ef migrations script --idempotent --project SIRGA.Infrastructure 
       --startup-project SIRGA.WebUI --context SIRG_DbContext -o <archivo>
   y luego protegido columna por columna (cada ALTER TABLE ... ADD va dentro de un
   IF COL_LENGTH(...) IS NULL) para que se pueda correr sobre una base que ya tiene parte
   del esquema sin romper.

   IMPORTANTE — DOS ESCENARIOS:
     * Base EXISTENTE (ya tiene __EFMigrationsHistory): este script la PONE AL DÍA. Es el caso
       para el que está pensado y el que hay que usar en producción si la base ya existe.
     * Base VACÍA (nueva): NO usar este script. La cadena de migraciones de SIRGA no es
       reproducible desde cero (hay 8 columnas agregadas dos veces y migraciones escritas a
       mano que asumen un estado previo). Para una base nueva hay que partir de una copia
       sólo-esquema de SIRGA_DB. Ver CONTEXTO_SIRGA.md §10.18.

   CÓMO SE EJECUTA (con sqlcmd hay que forzar QUOTED_IDENTIFIER, si no fallan los índices filtrados):
     sqlcmd -S <servidor> -E -b -I -d SIRGA_DB -i script_sirga_db_produccion_esquema.sql
   En SSMS alcanza con abrirlo y ejecutar.
   ========================================================================================== */
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [AccountingStudios] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [ResponsibleUsername] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_AccountingStudios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [Companies] (
        [Id] int NOT NULL IDENTITY,
        [BusinessName] nvarchar(200) NOT NULL,
        [Cuit] nvarchar(20) NOT NULL,
        [HeadquarterProvince] nvarchar(100) NOT NULL,
        [BranchName] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [Dni] nvarchar(20) NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [HireDate] datetime2 NOT NULL,
        [TerminationDate] datetime2 NULL,
        [Status] int NOT NULL,
        [BiometricData] varbinary(max) NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [SalaryScales] (
        [Id] int NOT NULL IDENTITY,
        [Category] nvarchar(100) NOT NULL,
        [MinimumAmount] decimal(18,2) NOT NULL,
        [MaximumAmount] decimal(18,2) NOT NULL,
        [EffectiveStartDate] datetime2 NOT NULL,
        [EffectiveEndDate] datetime2 NOT NULL,
        CONSTRAINT [PK_SalaryScales] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [Unions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [JurisdictionProvince] nvarchar(100) NOT NULL,
        [CctCode] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Unions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [Assignments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [UnionId] int NOT NULL,
        [IsVoluntaryMember] bit NOT NULL,
        CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Assignments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Assignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Assignments_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [Declarations] (
        [Id] int NOT NULL IDENTITY,
        [Period] nvarchar(7) NOT NULL,
        [CompanyId] int NOT NULL,
        [UnionId] int NOT NULL,
        [Status] int NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [IsRectification] bit NOT NULL,
        [SubmissionDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Declarations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Declarations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Declarations_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE TABLE [DeclarationDetails] (
        [Id] int NOT NULL IDENTITY,
        [RegistryId] int NOT NULL,
        [AssignmentId] int NOT NULL,
        [BaseSalary] decimal(18,2) NOT NULL,
        [LawContribution] decimal(18,2) NOT NULL,
        [MemberContribution] decimal(18,2) NOT NULL,
        [TotalWithheld] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_DeclarationDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeclarationDetails_Assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [Assignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DeclarationDetails_Declarations_RegistryId] FOREIGN KEY ([RegistryId]) REFERENCES [Declarations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Assignments_CompanyId] ON [Assignments] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Assignments_EmployeeId] ON [Assignments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Assignments_UnionId] ON [Assignments] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Companies_Cuit] ON [Companies] ([Cuit]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeclarationDetails_AssignmentId] ON [DeclarationDetails] ([AssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeclarationDetails_RegistryId] ON [DeclarationDetails] ([RegistryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Declarations_CompanyId] ON [Declarations] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Declarations_UnionId] ON [Declarations] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Dni] ON [Employees] ([Dni]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506120447_InitialMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506120447_InitialMigration', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506133804_AddMercadoPagoTracking'
)
BEGIN
    IF COL_LENGTH('Declarations', 'ExternalReference') IS NULL ALTER TABLE [Declarations] ADD [ExternalReference] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506133804_AddMercadoPagoTracking'
)
BEGIN
    IF COL_LENGTH('Declarations', 'MercadoPagoPaymentId') IS NULL ALTER TABLE [Declarations] ADD [MercadoPagoPaymentId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506133804_AddMercadoPagoTracking'
)
BEGIN
    IF COL_LENGTH('Declarations', 'PaymentDate') IS NULL ALTER TABLE [Declarations] ADD [PaymentDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506133804_AddMercadoPagoTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506133804_AddMercadoPagoTracking', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryScales]') AND [c].[name] = N'Category');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [SalaryScales] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [SalaryScales] DROP COLUMN [Category];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryScales]') AND [c].[name] = N'EffectiveEndDate');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [SalaryScales] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [SalaryScales] DROP COLUMN [EffectiveEndDate];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    EXEC sp_rename N'[SalaryScales].[MinimumAmount]', N'BasicAmount', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    EXEC sp_rename N'[SalaryScales].[MaximumAmount]', N'AgreementNR2', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    EXEC sp_rename N'[SalaryScales].[EffectiveStartDate]', N'Period', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    IF COL_LENGTH('SalaryScales', 'AgreementNR1') IS NULL ALTER TABLE [SalaryScales] ADD [AgreementNR1] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    IF COL_LENGTH('SalaryScales', 'CategoryCode') IS NULL ALTER TABLE [SalaryScales] ADD [CategoryCode] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    IF COL_LENGTH('SalaryScales', 'IsActive') IS NULL ALTER TABLE [SalaryScales] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    IF COL_LENGTH('SalaryScales', 'UnionId') IS NULL ALTER TABLE [SalaryScales] ADD [UnionId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    CREATE INDEX [IX_SalaryScales_UnionId] ON [SalaryScales] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    ALTER TABLE [SalaryScales] ADD CONSTRAINT [FK_SalaryScales_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142132_UpdateSalaryScaleStructure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506142132_UpdateSalaryScaleStructure', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142329_ChangeCategoryCodeToString'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryScales]') AND [c].[name] = N'CategoryCode');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SalaryScales] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [SalaryScales] ALTER COLUMN [CategoryCode] nvarchar(50) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506142329_ChangeCategoryCodeToString'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506142329_ChangeCategoryCodeToString', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506164900_LinkCompanyToAccountingStudio'
)
BEGIN
    IF COL_LENGTH('Companies', 'AccountingStudioId') IS NULL ALTER TABLE [Companies] ADD [AccountingStudioId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506164900_LinkCompanyToAccountingStudio'
)
BEGIN
    CREATE INDEX [IX_Companies_AccountingStudioId] ON [Companies] ([AccountingStudioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506164900_LinkCompanyToAccountingStudio'
)
BEGIN
    ALTER TABLE [Companies] ADD CONSTRAINT [FK_Companies_AccountingStudios_AccountingStudioId] FOREIGN KEY ([AccountingStudioId]) REFERENCES [AccountingStudios] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506164900_LinkCompanyToAccountingStudio'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506164900_LinkCompanyToAccountingStudio', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506171957_AddCityJurisdiction'
)
BEGIN
    IF COL_LENGTH('Unions', 'JurisdictionCity') IS NULL ALTER TABLE [Unions] ADD [JurisdictionCity] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506171957_AddCityJurisdiction'
)
BEGIN
    IF COL_LENGTH('Companies', 'City') IS NULL ALTER TABLE [Companies] ADD [City] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506171957_AddCityJurisdiction'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506171957_AddCityJurisdiction', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    IF COL_LENGTH('Declarations', 'PresencialBarCode') IS NULL ALTER TABLE [Declarations] ADD [PresencialBarCode] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    IF COL_LENGTH('Declarations', 'PresencialReference') IS NULL ALTER TABLE [Declarations] ADD [PresencialReference] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    IF COL_LENGTH('Assignments', 'CategoryCode') IS NULL ALTER TABLE [Assignments] ADD [CategoryCode] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    IF COL_LENGTH('Assignments', 'CreatedAt') IS NULL ALTER TABLE [Assignments] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'Email') IS NULL ALTER TABLE [AccountingStudios] ADD [Email] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    CREATE TABLE [InterestRates] (
        [Id] int NOT NULL IDENTITY,
        [EffectiveDate] datetime2 NOT NULL,
        [MonthlyRate] decimal(18,2) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_InterestRates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    CREATE TABLE [PaymentTransactions] (
        [Id] int NOT NULL IDENTITY,
        [DeclarationId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [ExternalReference] nvarchar(max) NULL,
        [PaymentMethod] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentTransactions_Declarations_DeclarationId] FOREIGN KEY ([DeclarationId]) REFERENCES [Declarations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    CREATE INDEX [IX_PaymentTransactions_DeclarationId] ON [PaymentTransactions] ([DeclarationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506192809_AddPaymentTransactions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506192809_AddPaymentTransactions', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506195829_AddCompanyContactEmail'
)
BEGIN
    IF COL_LENGTH('Companies', 'ContactEmail') IS NULL ALTER TABLE [Companies] ADD [ContactEmail] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506195829_AddCompanyContactEmail'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506195829_AddCompanyContactEmail', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506200953_AddRectificationFields'
)
BEGIN
    IF COL_LENGTH('Declarations', 'BalanceAmount') IS NULL ALTER TABLE [Declarations] ADD [BalanceAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506200953_AddRectificationFields'
)
BEGIN
    IF COL_LENGTH('Declarations', 'PreviousAmount') IS NULL ALTER TABLE [Declarations] ADD [PreviousAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506200953_AddRectificationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506200953_AddRectificationFields', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507000342_AddIdentitySupport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507000342_AddIdentitySupport', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507142346_AddCompanyBalances'
)
BEGIN
    CREATE TABLE [BalanceHistories] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [DeclarationId] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Concept] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BalanceHistories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507142346_AddCompanyBalances'
)
BEGIN
    CREATE TABLE [CompanyBalances] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [CreditAmount] decimal(18,2) NOT NULL,
        [LastUpdate] datetime2 NOT NULL,
        CONSTRAINT [PK_CompanyBalances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CompanyBalances_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507142346_AddCompanyBalances'
)
BEGIN
    CREATE INDEX [IX_CompanyBalances_CompanyId] ON [CompanyBalances] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507142346_AddCompanyBalances'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507142346_AddCompanyBalances', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511105240_AddCompanyWorkflow'
)
BEGIN
    IF COL_LENGTH('Companies', 'Status') IS NULL ALTER TABLE [Companies] ADD [Status] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511105240_AddCompanyWorkflow'
)
BEGIN
    CREATE TABLE [AssociationRequests] (
        [Id] int NOT NULL IDENTITY,
        [AccountingStudioId] int NOT NULL,
        [CompanyCuit] nvarchar(max) NOT NULL,
        [BusinessName] nvarchar(max) NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [Observation] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [ResolutionDate] datetime2 NULL,
        CONSTRAINT [PK_AssociationRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssociationRequests_AccountingStudios_AccountingStudioId] FOREIGN KEY ([AccountingStudioId]) REFERENCES [AccountingStudios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511105240_AddCompanyWorkflow'
)
BEGIN
    CREATE INDEX [IX_AssociationRequests_AccountingStudioId] ON [AssociationRequests] ([AccountingStudioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511105240_AddCompanyWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511105240_AddCompanyWorkflow', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    ALTER TABLE [SalaryScales] DROP CONSTRAINT [FK_SalaryScales_Unions_UnionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryScales]') AND [c].[name] = N'CategoryCode');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SalaryScales] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [SalaryScales] DROP COLUMN [CategoryCode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AssociationRequests]') AND [c].[name] = N'BusinessName');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [AssociationRequests] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [AssociationRequests] DROP COLUMN [BusinessName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AssociationRequests]') AND [c].[name] = N'CompanyCuit');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [AssociationRequests] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [AssociationRequests] DROP COLUMN [CompanyCuit];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    EXEC sp_rename N'[AssociationRequests].[ResolutionDate]', N'ProcessedAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    EXEC sp_rename N'[AssociationRequests].[RequestDate]', N'CreatedAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    EXEC sp_rename N'[AssociationRequests].[Observation]', N'AdminComment', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    IF COL_LENGTH('Unions', 'LawContributionPercentage') IS NULL ALTER TABLE [Unions] ADD [LawContributionPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    IF COL_LENGTH('Unions', 'UnionMemberPercentage') IS NULL ALTER TABLE [Unions] ADD [UnionMemberPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    DROP INDEX [IX_SalaryScales_UnionId] ON [SalaryScales];
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryScales]') AND [c].[name] = N'UnionId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [SalaryScales] DROP CONSTRAINT ' + @var6 + ';');
    EXEC(N'UPDATE [SalaryScales] SET [UnionId] = 0 WHERE [UnionId] IS NULL');
    ALTER TABLE [SalaryScales] ALTER COLUMN [UnionId] int NOT NULL;
    ALTER TABLE [SalaryScales] ADD DEFAULT 0 FOR [UnionId];
    CREATE INDEX [IX_SalaryScales_UnionId] ON [SalaryScales] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    IF COL_LENGTH('SalaryScales', 'AntiguedadPercentage') IS NULL ALTER TABLE [SalaryScales] ADD [AntiguedadPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    IF COL_LENGTH('SalaryScales', 'EmployeeCategoryId') IS NULL ALTER TABLE [SalaryScales] ADD [EmployeeCategoryId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    IF COL_LENGTH('AssociationRequests', 'CompanyId') IS NULL ALTER TABLE [AssociationRequests] ADD [CompanyId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    IF COL_LENGTH('AssociationRequests', 'TargetUnionId') IS NULL ALTER TABLE [AssociationRequests] ADD [TargetUnionId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    CREATE TABLE [EmployeeCategories] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [UnionId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [UnionId1] int NULL,
        CONSTRAINT [PK_EmployeeCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeCategories_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeCategories_Unions_UnionId1] FOREIGN KEY ([UnionId1]) REFERENCES [Unions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    CREATE INDEX [IX_SalaryScales_EmployeeCategoryId] ON [SalaryScales] ([EmployeeCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    CREATE INDEX [IX_AssociationRequests_CompanyId] ON [AssociationRequests] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    CREATE INDEX [IX_AssociationRequests_TargetUnionId] ON [AssociationRequests] ([TargetUnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    CREATE INDEX [IX_EmployeeCategories_UnionId] ON [EmployeeCategories] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    CREATE INDEX [IX_EmployeeCategories_UnionId1] ON [EmployeeCategories] ([UnionId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    ALTER TABLE [AssociationRequests] ADD CONSTRAINT [FK_AssociationRequests_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    ALTER TABLE [AssociationRequests] ADD CONSTRAINT [FK_AssociationRequests_Unions_TargetUnionId] FOREIGN KEY ([TargetUnionId]) REFERENCES [Unions] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    ALTER TABLE [SalaryScales] ADD CONSTRAINT [FK_SalaryScales_EmployeeCategories_EmployeeCategoryId] FOREIGN KEY ([EmployeeCategoryId]) REFERENCES [EmployeeCategories] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    ALTER TABLE [SalaryScales] ADD CONSTRAINT [FK_SalaryScales_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511124532_UpdateStructureForHito3FinalV2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511124532_UpdateStructureForHito3FinalV2', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511133648_AddAuditLogs'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityName] nvarchar(max) NOT NULL,
        [EntityId] int NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [PreviousValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [UserId] nvarchar(max) NOT NULL,
        [UserEmail] nvarchar(max) NULL,
        [Timestamp] datetime2 NOT NULL,
        [IPAddress] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511133648_AddAuditLogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511133648_AddAuditLogs', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511141517_AddInspectionActs'
)
BEGIN
    CREATE TABLE [InspectionActs] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [UnionId] int NOT NULL,
        [ActNumber] nvarchar(max) NOT NULL,
        [IssueDate] datetime2 NOT NULL,
        [PeriodRange] nvarchar(max) NOT NULL,
        [EstimatedDebt] decimal(18,2) NOT NULL,
        [InterestAmount] decimal(18,2) NOT NULL,
        [Observations] nvarchar(max) NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_InspectionActs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InspectionActs_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InspectionActs_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511141517_AddInspectionActs'
)
BEGIN
    CREATE TABLE [InspectionActPeriods] (
        [Id] int NOT NULL IDENTITY,
        [InspectionActId] int NOT NULL,
        [Period] nvarchar(max) NOT NULL,
        [BaseAmount] decimal(18,2) NOT NULL,
        [CalculatedInterest] decimal(18,2) NOT NULL,
        [WasAlreadyDeclared] bit NOT NULL,
        CONSTRAINT [PK_InspectionActPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InspectionActPeriods_InspectionActs_InspectionActId] FOREIGN KEY ([InspectionActId]) REFERENCES [InspectionActs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511141517_AddInspectionActs'
)
BEGIN
    CREATE INDEX [IX_InspectionActPeriods_InspectionActId] ON [InspectionActPeriods] ([InspectionActId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511141517_AddInspectionActs'
)
BEGIN
    CREATE INDEX [IX_InspectionActs_CompanyId] ON [InspectionActs] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511141517_AddInspectionActs'
)
BEGIN
    CREATE INDEX [IX_InspectionActs_UnionId] ON [InspectionActs] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511141517_AddInspectionActs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511141517_AddInspectionActs', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511142905_AddPaymentPlans'
)
BEGIN
    CREATE TABLE [PaymentPlans] (
        [Id] int NOT NULL IDENTITY,
        [InspectionActId] int NOT NULL,
        [PlanNumber] nvarchar(max) NOT NULL,
        [SignatureDate] datetime2 NOT NULL,
        [TotalDebt] decimal(18,2) NOT NULL,
        [DownPayment] decimal(18,2) NOT NULL,
        [InstallmentsCount] int NOT NULL,
        [InstallmentAmount] decimal(18,2) NOT NULL,
        [FinancingInterestRate] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        CONSTRAINT [PK_PaymentPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentPlans_InspectionActs_InspectionActId] FOREIGN KEY ([InspectionActId]) REFERENCES [InspectionActs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511142905_AddPaymentPlans'
)
BEGIN
    CREATE TABLE [PaymentPlanInstallments] (
        [Id] int NOT NULL IDENTITY,
        [PaymentPlanId] int NOT NULL,
        [InstallmentNumber] int NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [IsPaid] bit NOT NULL,
        [PaymentDate] datetime2 NULL,
        [PaymentReference] nvarchar(max) NULL,
        CONSTRAINT [PK_PaymentPlanInstallments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId] FOREIGN KEY ([PaymentPlanId]) REFERENCES [PaymentPlans] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511142905_AddPaymentPlans'
)
BEGIN
    CREATE INDEX [IX_PaymentPlanInstallments_PaymentPlanId] ON [PaymentPlanInstallments] ([PaymentPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511142905_AddPaymentPlans'
)
BEGIN
    CREATE INDEX [IX_PaymentPlans_InspectionActId] ON [PaymentPlans] ([InspectionActId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511142905_AddPaymentPlans'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511142905_AddPaymentPlans', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511153408_AddInspectorsAndVisits'
)
BEGIN
    CREATE TABLE [Inspectors] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Dni] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Inspectors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511153408_AddInspectorsAndVisits'
)
BEGIN
    CREATE TABLE [PrejudicialNotices] (
        [Id] int NOT NULL IDENTITY,
        [InspectionActId] int NOT NULL,
        [NoticeNumber] nvarchar(max) NOT NULL,
        [IssueDate] datetime2 NOT NULL,
        [ExpirationDate] datetime2 NOT NULL,
        [WasNotified] bit NOT NULL,
        [Observations] nvarchar(max) NULL,
        [IsJudicial] bit NOT NULL,
        CONSTRAINT [PK_PrejudicialNotices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrejudicialNotices_InspectionActs_InspectionActId] FOREIGN KEY ([InspectionActId]) REFERENCES [InspectionActs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511153408_AddInspectorsAndVisits'
)
BEGIN
    CREATE TABLE [InspectionVisits] (
        [Id] int NOT NULL IDENTITY,
        [InspectionActId] int NOT NULL,
        [InspectorId] int NOT NULL,
        [VisitNumber] int NOT NULL,
        [VisitDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Observations] nvarchar(max) NULL,
        [DocumentNumber] nvarchar(max) NOT NULL,
        [DebtorSigned] bit NOT NULL,
        CONSTRAINT [PK_InspectionVisits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InspectionVisits_InspectionActs_InspectionActId] FOREIGN KEY ([InspectionActId]) REFERENCES [InspectionActs] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InspectionVisits_Inspectors_InspectorId] FOREIGN KEY ([InspectorId]) REFERENCES [Inspectors] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511153408_AddInspectorsAndVisits'
)
BEGIN
    CREATE INDEX [IX_InspectionVisits_InspectionActId] ON [InspectionVisits] ([InspectionActId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511153408_AddInspectorsAndVisits'
)
BEGIN
    CREATE INDEX [IX_InspectionVisits_InspectorId] ON [InspectionVisits] ([InspectorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511153408_AddInspectorsAndVisits'
)
BEGIN
    CREATE INDEX [IX_PrejudicialNotices_InspectionActId] ON [PrejudicialNotices] ([InspectionActId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511153408_AddInspectorsAndVisits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511153408_AddInspectorsAndVisits', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511224553_AddPropertiesToAccountingStudio'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'ApprovedAt') IS NULL ALTER TABLE [AccountingStudios] ADD [ApprovedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511224553_AddPropertiesToAccountingStudio'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'CreatedAt') IS NULL ALTER TABLE [AccountingStudios] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511224553_AddPropertiesToAccountingStudio'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'IsApproved') IS NULL ALTER TABLE [AccountingStudios] ADD [IsApproved] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511224553_AddPropertiesToAccountingStudio'
)
BEGIN
    CREATE TABLE [NoticeAttachments] (
        [Id] int NOT NULL IDENTITY,
        [PrejudicialNoticeId] int NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [FileType] nvarchar(max) NOT NULL,
        [FileUrl] nvarchar(max) NOT NULL,
        [UploadDate] datetime2 NOT NULL,
        CONSTRAINT [PK_NoticeAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NoticeAttachments_PrejudicialNotices_PrejudicialNoticeId] FOREIGN KEY ([PrejudicialNoticeId]) REFERENCES [PrejudicialNotices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511224553_AddPropertiesToAccountingStudio'
)
BEGIN
    CREATE INDEX [IX_NoticeAttachments_PrejudicialNoticeId] ON [NoticeAttachments] ([PrejudicialNoticeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260511224553_AddPropertiesToAccountingStudio'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511224553_AddPropertiesToAccountingStudio', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'Address') IS NULL ALTER TABLE [AccountingStudios] ADD [Address] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'CityId') IS NULL ALTER TABLE [AccountingStudios] ADD [CityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'ContactCellPhone') IS NULL ALTER TABLE [AccountingStudios] ADD [ContactCellPhone] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'ContactPhone') IS NULL ALTER TABLE [AccountingStudios] ADD [ContactPhone] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'IsActive') IS NULL ALTER TABLE [AccountingStudios] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    CREATE TABLE [Provinces] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Provinces] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    CREATE TABLE [Cities] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [ZipCode] nvarchar(10) NULL,
        [ProvinceId] int NOT NULL,
        CONSTRAINT [PK_Cities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cities_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [Provinces] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    CREATE INDEX [IX_AccountingStudios_CityId] ON [AccountingStudios] ([CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    CREATE INDEX [IX_Cities_ProvinceId] ON [Cities] ([ProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    ALTER TABLE [AccountingStudios] ADD CONSTRAINT [FK_AccountingStudios_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512123157_UpdateAccountingStudioAndLocations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512123157_UpdateAccountingStudioAndLocations', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'HeadquarterProvince');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Companies] DROP COLUMN [HeadquarterProvince];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    EXEC sp_rename N'[Companies].[City]', N'ReferencePhone', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'ContactEmail');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Companies] ALTER COLUMN [ContactEmail] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'BranchName');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Companies] ALTER COLUMN [BranchName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    IF COL_LENGTH('Companies', 'Address') IS NULL ALTER TABLE [Companies] ADD [Address] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    IF COL_LENGTH('Companies', 'CityId') IS NULL ALTER TABLE [Companies] ADD [CityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    IF COL_LENGTH('Companies', 'Phone') IS NULL ALTER TABLE [Companies] ADD [Phone] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    IF COL_LENGTH('Companies', 'ReferencePerson') IS NULL ALTER TABLE [Companies] ADD [ReferencePerson] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'ReferencePerson') IS NULL ALTER TABLE [AccountingStudios] ADD [ReferencePerson] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'ReferencePhone') IS NULL ALTER TABLE [AccountingStudios] ADD [ReferencePhone] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    CREATE INDEX [IX_Companies_CityId] ON [Companies] ([CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    ALTER TABLE [Companies] ADD CONSTRAINT [FK_Companies_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130217_ExpandCompanyEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512130217_ExpandCompanyEntity', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512163754_AddStudioApprovalAndContactFields'
)
BEGIN
    IF COL_LENGTH('Companies', 'ParentCompanyId') IS NULL ALTER TABLE [Companies] ADD [ParentCompanyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512163754_AddStudioApprovalAndContactFields'
)
BEGIN
    IF COL_LENGTH('AccountingStudios', 'RejectionReason') IS NULL ALTER TABLE [AccountingStudios] ADD [RejectionReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512163754_AddStudioApprovalAndContactFields'
)
BEGIN
    CREATE INDEX [IX_Companies_ParentCompanyId] ON [Companies] ([ParentCompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512163754_AddStudioApprovalAndContactFields'
)
BEGIN
    ALTER TABLE [Companies] ADD CONSTRAINT [FK_Companies_Companies_ParentCompanyId] FOREIGN KEY ([ParentCompanyId]) REFERENCES [Companies] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512163754_AddStudioApprovalAndContactFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512163754_AddStudioApprovalAndContactFields', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512175051_AllowMultipleBranchesPerCuit'
)
BEGIN
    DROP INDEX [IX_Companies_Cuit] ON [Companies];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512175051_AllowMultipleBranchesPerCuit'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Companies_Cuit_BranchName] ON [Companies] ([Cuit], [BranchName]) WHERE [BranchName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512175051_AllowMultipleBranchesPerCuit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512175051_AllowMultipleBranchesPerCuit', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124609_AddUnionSubscriptions'
)
BEGIN
    CREATE TABLE [UnionSubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [UnionId] int NOT NULL,
        [MonthlyFee] decimal(18,2) NOT NULL,
        [NextBillingDate] datetime2 NOT NULL,
        [IsServiceActive] bit NOT NULL,
        CONSTRAINT [PK_UnionSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnionSubscriptions_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124609_AddUnionSubscriptions'
)
BEGIN
    CREATE TABLE [UnionSubscriptionPayments] (
        [Id] int NOT NULL IDENTITY,
        [UnionSubscriptionId] int NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [ReceiptNumber] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_UnionSubscriptionPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnionSubscriptionPayments_UnionSubscriptions_UnionSubscriptionId] FOREIGN KEY ([UnionSubscriptionId]) REFERENCES [UnionSubscriptions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124609_AddUnionSubscriptions'
)
BEGIN
    CREATE INDEX [IX_UnionSubscriptionPayments_UnionSubscriptionId] ON [UnionSubscriptionPayments] ([UnionSubscriptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124609_AddUnionSubscriptions'
)
BEGIN
    CREATE INDEX [IX_UnionSubscriptions_UnionId] ON [UnionSubscriptions] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513124609_AddUnionSubscriptions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513124609_AddUnionSubscriptions', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513130738_AddUnionSubscriptionRequests'
)
BEGIN
    CREATE TABLE [UnionSubscriptionRequests] (
        [Id] int NOT NULL IDENTITY,
        [UnionName] nvarchar(200) NOT NULL,
        [Cuit] nvarchar(20) NOT NULL,
        [ContactEmail] nvarchar(max) NOT NULL,
        [ContactPhone] nvarchar(50) NULL,
        [RequestDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [AdminNotes] nvarchar(max) NULL,
        CONSTRAINT [PK_UnionSubscriptionRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513130738_AddUnionSubscriptionRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513130738_AddUnionSubscriptionRequests', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'CoverageArea') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [CoverageArea] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'Floor') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [Floor] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'Locality') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [Locality] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'Province') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [Province] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'ReferencePersonFullName') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [ReferencePersonFullName] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'Street') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [Street] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'StreetNumber') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [StreetNumber] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    IF COL_LENGTH('UnionSubscriptionRequests', 'WhatsApp') IS NULL ALTER TABLE [UnionSubscriptionRequests] ADD [WhatsApp] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513182926_ExpandSubscriptionRequestData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513182926_ExpandSubscriptionRequestData', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224613_AddAdminEmailToUnion'
)
BEGIN
    IF COL_LENGTH('Unions', 'AdminEmail') IS NULL ALTER TABLE [Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224613_AddAdminEmailToUnion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513224613_AddAdminEmailToUnion', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224623_FixedPendingChanges'
)
BEGIN
    IF COL_LENGTH('Unions', 'AdminEmail') IS NULL ALTER TABLE [Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224623_FixedPendingChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513224623_FixedPendingChanges', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224632_SyncModelV3'
)
BEGIN
    IF COL_LENGTH('Unions', 'AdminEmail') IS NULL ALTER TABLE [Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224632_SyncModelV3'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513224632_SyncModelV3', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224643_SyncModelV4'
)
BEGIN
    IF COL_LENGTH('Unions', 'AdminEmail') IS NULL ALTER TABLE [Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224643_SyncModelV4'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513224643_SyncModelV4', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224659_SyncModelV5'
)
BEGIN
    IF COL_LENGTH('Unions', 'AdminEmail') IS NULL ALTER TABLE [Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224659_SyncModelV5'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513224659_SyncModelV5', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224709_ConsolidateAllChanges'
)
BEGIN
    IF COL_LENGTH('Unions', 'AdminEmail') IS NULL ALTER TABLE [Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513224709_ConsolidateAllChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513224709_ConsolidateAllChanges', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513225007_AddSubscriptionsStructure'
)
BEGIN
    IF COL_LENGTH('Unions', 'AdminEmail') IS NULL ALTER TABLE [Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513225007_AddSubscriptionsStructure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513225007_AddSubscriptionsStructure', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815004910_AddAssignmentActiveFields'
)
BEGIN
    ALTER TABLE [EmployeeCategories] DROP CONSTRAINT [FK_EmployeeCategories_Unions_UnionId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815004910_AddAssignmentActiveFields'
)
BEGIN
    DROP INDEX [IX_EmployeeCategories_UnionId1] ON [EmployeeCategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815004910_AddAssignmentActiveFields'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeCategories]') AND [c].[name] = N'UnionId1');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeCategories] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [EmployeeCategories] DROP COLUMN [UnionId1];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815004910_AddAssignmentActiveFields'
)
BEGIN
    IF COL_LENGTH('Assignments', 'IsActive') IS NULL ALTER TABLE [Assignments] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815004910_AddAssignmentActiveFields'
)
BEGIN
    IF COL_LENGTH('Assignments', 'TerminationDate') IS NULL ALTER TABLE [Assignments] ADD [TerminationDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260815004910_AddAssignmentActiveFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260815004910_AddAssignmentActiveFields', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817150653_AddCompanyActiveFields'
)
BEGIN
    IF COL_LENGTH('Companies', 'IsActive') IS NULL ALTER TABLE [Companies] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817150653_AddCompanyActiveFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817150653_AddCompanyActiveFields', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818202737_AddCompanyNormalizedCuit'
)
BEGIN
    EXEC(N'ALTER TABLE [Companies] ADD [NormalizedCuit] AS ISNULL(REPLACE(REPLACE([Cuit], ''-'', ''''), '' '', ''''), '''') PERSISTED');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818202737_AddCompanyNormalizedCuit'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Companies_NormalizedCuit] ON [Companies] ([NormalizedCuit]) WHERE [ParentCompanyId] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818202737_AddCompanyNormalizedCuit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818202737_AddCompanyNormalizedCuit', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818221937_AddMembershipResignationFields'
)
BEGIN
    IF COL_LENGTH('Assignments', 'MembershipEndDate') IS NULL ALTER TABLE [Assignments] ADD [MembershipEndDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818221937_AddMembershipResignationFields'
)
BEGIN
    IF COL_LENGTH('Assignments', 'MembershipResignationReason') IS NULL ALTER TABLE [Assignments] ADD [MembershipResignationReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818221937_AddMembershipResignationFields'
)
BEGIN
    IF COL_LENGTH('Assignments', 'MembershipResignationRequested') IS NULL ALTER TABLE [Assignments] ADD [MembershipResignationRequested] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818221937_AddMembershipResignationFields'
)
BEGIN
    IF COL_LENGTH('Assignments', 'MembershipResignationRequestedAt') IS NULL ALTER TABLE [Assignments] ADD [MembershipResignationRequestedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818221937_AddMembershipResignationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818221937_AddMembershipResignationFields', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Declarations_CompanyId] ON [Declarations];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    DROP INDEX [IX_Declarations_UnionId] ON [Declarations];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Declarations_CompanyId_Period] ON [Declarations] ([CompanyId], [Period]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Declarations_Period] ON [Declarations] ([Period]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Declarations_Status_SubmissionDate] ON [Declarations] ([Status], [SubmissionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Declarations_UnionId_SubmissionDate] ON [Declarations] ([UnionId], [SubmissionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_BalanceHistories_CompanyId] ON [BalanceHistories] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165352_AddPerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930165352_AddPerformanceIndexes', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002111021_AddCompanyStudioHistory'
)
BEGIN
    CREATE TABLE [CompanyStudioHistories] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [AccountingStudioId] int NOT NULL,
        [UnionId] int NULL,
        [From] datetime2 NOT NULL,
        [To] datetime2 NULL,
        [AuthorizedByUsername] nvarchar(max) NULL,
        [Reason] nvarchar(max) NULL,
        CONSTRAINT [PK_CompanyStudioHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CompanyStudioHistories_AccountingStudios_AccountingStudioId] FOREIGN KEY ([AccountingStudioId]) REFERENCES [AccountingStudios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CompanyStudioHistories_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CompanyStudioHistories_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002111021_AddCompanyStudioHistory'
)
BEGIN
    CREATE INDEX [IX_CompanyStudioHistories_AccountingStudioId] ON [CompanyStudioHistories] ([AccountingStudioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002111021_AddCompanyStudioHistory'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CompanyStudioHistories_CompanyId] ON [CompanyStudioHistories] ([CompanyId]) WHERE [To] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002111021_AddCompanyStudioHistory'
)
BEGIN
    CREATE INDEX [IX_CompanyStudioHistories_UnionId] ON [CompanyStudioHistories] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261002111021_AddCompanyStudioHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261002111021_AddCompanyStudioHistory', N'10.0.8');
END;

COMMIT;
GO

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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005123240_AddCompanyTradeName'
)
BEGIN
    IF COL_LENGTH('Companies', 'TradeName') IS NULL ALTER TABLE [Companies] ADD [TradeName] nvarchar(200) NULL;
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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006032640_AddBanksAndUnionBankConfig'
)
BEGIN
    CREATE TABLE [Banks] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Branch] nvarchar(120) NULL,
        [Notes] nvarchar(300) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Banks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006032640_AddBanksAndUnionBankConfig'
)
BEGIN
    CREATE TABLE [UnionBankConfigs] (
        [Id] int NOT NULL IDENTITY,
        [UnionId] int NOT NULL,
        [BankId] int NOT NULL,
        [ClientNumber] nvarchar(30) NOT NULL,
        [EntityCode] nvarchar(4) NOT NULL,
        [DesignFormat] int NOT NULL,
        [FirstDueDay] int NOT NULL,
        [DaysToSecondDue] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_UnionBankConfigs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnionBankConfigs_Banks_BankId] FOREIGN KEY ([BankId]) REFERENCES [Banks] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UnionBankConfigs_Unions_UnionId] FOREIGN KEY ([UnionId]) REFERENCES [Unions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006032640_AddBanksAndUnionBankConfig'
)
BEGIN
    CREATE INDEX [IX_UnionBankConfigs_BankId] ON [UnionBankConfigs] ([BankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006032640_AddBanksAndUnionBankConfig'
)
BEGIN
    CREATE INDEX [IX_UnionBankConfigs_UnionId] ON [UnionBankConfigs] ([UnionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006032640_AddBanksAndUnionBankConfig'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006032640_AddBanksAndUnionBankConfig', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006132109_AddRama'
)
BEGIN
    CREATE TABLE [Ramas] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Notes] nvarchar(300) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Ramas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006132109_AddRama'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006132109_AddRama', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140432_AddJornadaToAssignments'
)
BEGIN
    IF COL_LENGTH('Assignments', 'Jornada') IS NULL ALTER TABLE [Assignments] ADD [Jornada] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006140432_AddJornadaToAssignments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006140432_AddJornadaToAssignments', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175817_ExtendInterestRates'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InterestRates]') AND [c].[name] = N'MonthlyRate');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [InterestRates] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [InterestRates] ALTER COLUMN [MonthlyRate] decimal(18,6) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175817_ExtendInterestRates'
)
BEGIN
    IF COL_LENGTH('InterestRates', 'DailyRate') IS NULL ALTER TABLE [InterestRates] ADD [DailyRate] decimal(18,8) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175817_ExtendInterestRates'
)
BEGIN
    IF COL_LENGTH('InterestRates', 'Norm') IS NULL ALTER TABLE [InterestRates] ADD [Norm] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175817_ExtendInterestRates'
)
BEGIN
    IF COL_LENGTH('InterestRates', 'TypeOfInterest') IS NULL ALTER TABLE [InterestRates] ADD [TypeOfInterest] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175817_ExtendInterestRates'
)
BEGIN
    IF COL_LENGTH('InterestRates', 'ValidTo') IS NULL ALTER TABLE [InterestRates] ADD [ValidTo] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175817_ExtendInterestRates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006175817_ExtendInterestRates', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175903_AddInterestRateUniqueIndex'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InterestRates_EffectiveDate_TypeOfInterest] ON [InterestRates] ([EffectiveDate], [TypeOfInterest]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006175903_AddInterestRateUniqueIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006175903_AddInterestRateUniqueIndex', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006180645_InterestRateSoftDelete'
)
BEGIN
    DROP INDEX [IX_InterestRates_EffectiveDate_TypeOfInterest] ON [InterestRates];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006180645_InterestRateSoftDelete'
)
BEGIN
    IF COL_LENGTH('InterestRates', 'IsActive') IS NULL ALTER TABLE [InterestRates] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006180645_InterestRateSoftDelete'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_InterestRates_EffectiveDate_TypeOfInterest] ON [InterestRates] ([EffectiveDate], [TypeOfInterest]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006180645_InterestRateSoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006180645_InterestRateSoftDelete', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006195056_LinkUnionsToRama'
)
BEGIN
    IF COL_LENGTH('Unions', 'RamaId') IS NULL ALTER TABLE [Unions] ADD [RamaId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006195056_LinkUnionsToRama'
)
BEGIN
    CREATE INDEX [IX_Unions_RamaId] ON [Unions] ([RamaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006195056_LinkUnionsToRama'
)
BEGIN
    ALTER TABLE [Unions] ADD CONSTRAINT [FK_Unions_Ramas_RamaId] FOREIGN KEY ([RamaId]) REFERENCES [Ramas] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006195056_LinkUnionsToRama'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006195056_LinkUnionsToRama', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN

                    IF OBJECT_ID('dbo._BackupScalesCategoriesUnionId') IS NULL
                    BEGIN
                        SELECT 'EmployeeCategories' AS Tabla, Id AS RegistroId, UnionId
                        INTO dbo._BackupScalesCategoriesUnionId
                        FROM EmployeeCategories
                        UNION ALL
                        SELECT 'SalaryScales', Id, UnionId FROM SalaryScales;
                    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    ALTER TABLE [EmployeeCategories] DROP CONSTRAINT [FK_EmployeeCategories_Unions_UnionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    IF COL_LENGTH('EmployeeCategories', 'RamaId') IS NULL ALTER TABLE [EmployeeCategories] ADD [RamaId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN

                    UPDATE c
                    SET c.RamaId = u.RamaId
                    FROM EmployeeCategories c
                    INNER JOIN Unions u ON u.Id = c.UnionId
                    WHERE u.RamaId IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN

                    IF EXISTS (SELECT 1 FROM EmployeeCategories WHERE RamaId IS NULL)
                        THROW 50000, 'Hay categorias cuyo sindicato no tiene rama asignada. Asignala antes de migrar (script_sirga_db_ramas_vincular_sindicatos.sql).', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeCategories]') AND [c].[name] = N'RamaId');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeCategories] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [EmployeeCategories] ALTER COLUMN [RamaId] int NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    DROP INDEX [IX_EmployeeCategories_UnionId] ON [EmployeeCategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeCategories]') AND [c].[name] = N'UnionId');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeCategories] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [EmployeeCategories] DROP COLUMN [UnionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    CREATE INDEX [IX_EmployeeCategories_RamaId] ON [EmployeeCategories] ([RamaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    ALTER TABLE [EmployeeCategories] ADD CONSTRAINT [FK_EmployeeCategories_Ramas_RamaId] FOREIGN KEY ([RamaId]) REFERENCES [Ramas] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    ALTER TABLE [SalaryScales] DROP CONSTRAINT [FK_SalaryScales_Unions_UnionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    DROP INDEX [IX_SalaryScales_UnionId] ON [SalaryScales];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryScales]') AND [c].[name] = N'UnionId');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [SalaryScales] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [SalaryScales] DROP COLUMN [UnionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006202839_MoveScalesAndCategoriesToRama'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006202839_MoveScalesAndCategoriesToRama', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006203620_AddSalaryScaleUniqueActiveIndex'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SalaryScales_Period_EmployeeCategoryId] ON [SalaryScales] ([Period], [EmployeeCategoryId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006203620_AddSalaryScaleUniqueActiveIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006203620_AddSalaryScaleUniqueActiveIndex', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007103921_AddRectificationVersion'
)
BEGIN
    IF COL_LENGTH('Declarations', 'VersionNumber') IS NULL ALTER TABLE [Declarations] ADD [VersionNumber] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007103921_AddRectificationVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007103921_AddRectificationVersion', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007113805_AddDetailUsaEscala'
)
BEGIN
    IF COL_LENGTH('DeclarationDetails', 'UsaEscala') IS NULL ALTER TABLE [DeclarationDetails] ADD [UsaEscala] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007113805_AddDetailUsaEscala'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007113805_AddDetailUsaEscala', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Address') IS NULL ALTER TABLE [Employees] ADD [Address] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'AddressNumber') IS NULL ALTER TABLE [Employees] ADD [AddressNumber] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'AffiliationNumber') IS NULL ALTER TABLE [Employees] ADD [AffiliationNumber] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Apartment') IS NULL ALTER TABLE [Employees] ADD [Apartment] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'BirthDate') IS NULL ALTER TABLE [Employees] ADD [BirthDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'CardNumber') IS NULL ALTER TABLE [Employees] ADD [CardNumber] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'DocumentNumber') IS NULL ALTER TABLE [Employees] ADD [DocumentNumber] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'DocumentTypeCode') IS NULL ALTER TABLE [Employees] ADD [DocumentTypeCode] nvarchar(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Email') IS NULL ALTER TABLE [Employees] ADD [Email] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'FirstName') IS NULL ALTER TABLE [Employees] ADD [FirstName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Floor') IS NULL ALTER TABLE [Employees] ADD [Floor] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'LastName') IS NULL ALTER TABLE [Employees] ADD [LastName] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Mobile') IS NULL ALTER TABLE [Employees] ADD [Mobile] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Neighborhood') IS NULL ALTER TABLE [Employees] ADD [Neighborhood] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Phone') IS NULL ALTER TABLE [Employees] ADD [Phone] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'Sex') IS NULL ALTER TABLE [Employees] ADD [Sex] nvarchar(1) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'WorkEmail') IS NULL ALTER TABLE [Employees] ADD [WorkEmail] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    IF COL_LENGTH('Employees', 'ZipCode') IS NULL ALTER TABLE [Employees] ADD [ZipCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104022048_AgregarDatosDeMaesocAEmpleados'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104022048_AgregarDatosDeMaesocAEmpleados', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    IF COL_LENGTH('Employees', 'HasFamilyCharge') IS NULL ALTER TABLE [Employees] ADD [HasFamilyCharge] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    IF COL_LENGTH('Employees', 'HealthInsuranceCode') IS NULL ALTER TABLE [Employees] ADD [HealthInsuranceCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    IF COL_LENGTH('Employees', 'IsRetired') IS NULL ALTER TABLE [Employees] ADD [IsRetired] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    IF COL_LENGTH('Assignments', 'ActivityCode') IS NULL ALTER TABLE [Assignments] ADD [ActivityCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    IF COL_LENGTH('Assignments', 'FileNumber') IS NULL ALTER TABLE [Assignments] ADD [FileNumber] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    CREATE TABLE [Activities] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Activities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    CREATE TABLE [HealthInsurances] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Acronym] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_HealthInsurances] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104135915_AgregarActividadObraSocialLegajo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104135915_AgregarActividadObraSocialLegajo', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104151000_AgregarBitacoraDeEmpleado'
)
BEGIN
    CREATE TABLE [EmployeeChangeLogs] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [AssignmentId] int NULL,
        [Campo] nvarchar(50) NOT NULL,
        [ValorAnterior] nvarchar(500) NULL,
        [ValorNuevo] nvarchar(500) NULL,
        [Origen] nvarchar(30) NOT NULL,
        [Usuario] nvarchar(200) NULL,
        [Fecha] datetime2 NOT NULL,
        [Detalle] nvarchar(300) NULL,
        CONSTRAINT [PK_EmployeeChangeLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeChangeLogs_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104151000_AgregarBitacoraDeEmpleado'
)
BEGIN
    CREATE INDEX [IX_EmployeeChangeLogs_EmployeeId] ON [EmployeeChangeLogs] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104151000_AgregarBitacoraDeEmpleado'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104151000_AgregarBitacoraDeEmpleado', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104151356_AgregarPropuestaEscalaAlDetalle'
)
BEGIN
    IF COL_LENGTH('DeclarationDetails', 'Difference') IS NULL ALTER TABLE [DeclarationDetails] ADD [Difference] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104151356_AgregarPropuestaEscalaAlDetalle'
)
BEGIN
    IF COL_LENGTH('DeclarationDetails', 'EnteredSalary') IS NULL ALTER TABLE [DeclarationDetails] ADD [EnteredSalary] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104151356_AgregarPropuestaEscalaAlDetalle'
)
BEGIN
    IF COL_LENGTH('DeclarationDetails', 'ScaleSalary') IS NULL ALTER TABLE [DeclarationDetails] ADD [ScaleSalary] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104151356_AgregarPropuestaEscalaAlDetalle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104151356_AgregarPropuestaEscalaAlDetalle', N'10.0.8');
END;

COMMIT;
GO

