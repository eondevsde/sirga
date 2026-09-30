/* =====================================================================
   SIRGA - Script de INSTALACION de base de datos (esquema completo)
   Generado: 2026-08-18  (generado automaticamente desde la DB de referencia)

   Alcance: crea la base SIRGA_DB (si no existe) y TODO el esquema: tablas,
            columnas calculadas, indices, indices filtrados, claves
            primarias/unicas, defaults, checks y claves foraneas,
            contemplando TODOS los cambios realizados hasta la fecha.

   IDEMPOTENTE: puede ejecutarse multiples veces sin errores; cada objeto
            se crea solo si no existe. Re-ejecutar no modifica nada.

   PERMISOS NECESARIOS:
            - Si SIRGA_DB no existe: rol sysadmin o dbcreator del servidor.
            - Si ya existe: rol sysadmin o db_owner de SIRGA_DB.
            El script valida esto al inicio y detiene la ejecucion con un
            mensaje claro si falta el permiso.

   OPCIONES SET (IMPORTANTE): se fijan ANSI_NULLS, QUOTED_IDENTIFIER,
            ARITHABORT, etc. porque son REQUERIDAS para poder crear el
            indice unico sobre la columna calculada [Companies].[NormalizedCuit]
            y los indices filtrados (error 1934 si estan apagadas).
            NO modificar este bloque.

   RECOMENDADO: hacer un BACKUP de la base antes de ejecutar en produccion
            y ejecutar con -b para que el script se detenga ante CUALQUIER
            error (sqlcmd -S servidor -E -b -i script_sirga_db_produccion.sql).

   Nota: los datos de referencia (usuarios Identity, roles, provincias y
            localidades) se siembran al iniciar la app (DbInitializer).
            Los datos del ETL se cargan con el proceso SIRGA.Migration.
   ===================================================================== */

SET NOCOUNT ON;
GO

-- ============================================================
-- Creacion de la base (solo si no existe) con check de permisos
IF DB_ID(N'SIRGA_DB') IS NULL
BEGIN
    IF IS_SRVROLEMEMBER('sysadmin') = 0 AND IS_SRVROLEMEMBER('dbcreator') = 0
        THROW 50001, 'La base SIRGA_DB no existe y el login actual no tiene rol sysadmin/dbcreator del servidor para crearla. Ejecutar con una cuenta con esos permisos.', 1;
    CREATE DATABASE [SIRGA_DB];
END;
GO

USE [SIRGA_DB];
GO

-- ============================================================
-- Check de permisos sobre la base ya conectada
IF IS_SRVROLEMEMBER('sysadmin') = 0 AND IS_MEMBER('db_owner') = 0
    THROW 50002, 'El login actual no tiene rol db_owner/sysadmin sobre SIRGA_DB, necesario para crear/alterar el esquema. Ejecutar con una cuenta con esos permisos.', 1;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- ============================================================
-- Tabla: [dbo].[__EFMigrationsHistory]
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AccountingStudios]
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AccountingStudios](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[ResponsibleUsername] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](200) NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsApproved] [bit] NOT NULL,
	[Address] [nvarchar](250) NULL,
	[CityId] [int] NULL,
	[ContactCellPhone] [nvarchar](50) NULL,
	[ContactPhone] [nvarchar](50) NULL,
	[IsActive] [bit] NOT NULL,
	[ReferencePerson] [nvarchar](100) NULL,
	[ReferencePhone] [nvarchar](50) NULL,
	[RejectionReason] [nvarchar](500) NULL,
 CONSTRAINT [PK_AccountingStudios] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_AccountingStudios_CityId] ON [dbo].[AccountingStudios]
(
	[CityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[AccountingStudios] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt]
ALTER TABLE [dbo].[AccountingStudios] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsApproved]
ALTER TABLE [dbo].[AccountingStudios] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsActive]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AspNetRoleClaims]
IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AspNetRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AspNetRoles]
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]
(
	[NormalizedName] ASC
)
WHERE ([NormalizedName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AspNetUserClaims]
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](450) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AspNetUserLogins]
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](450) NOT NULL,
	[ProviderKey] [nvarchar](450) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AspNetUserRoles]
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [nvarchar](450) NOT NULL,
	[RoleId] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AspNetUsers]
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AspNetUsers](
	[Id] [nvarchar](450) NOT NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AspNetUserTokens]
IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AspNetUserTokens](
	[UserId] [nvarchar](450) NOT NULL,
	[LoginProvider] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Assignments]
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Assignments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EmployeeId] [int] NOT NULL,
	[CompanyId] [int] NOT NULL,
	[UnionId] [int] NOT NULL,
	[IsVoluntaryMember] [bit] NOT NULL,
	[CategoryCode] [nvarchar](100) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[TerminationDate] [datetime2](7) NULL,
	[MembershipEndDate] [datetime2](7) NULL,
	[MembershipResignationReason] [nvarchar](500) NULL,
	[MembershipResignationRequested] [bit] NULL,
	[MembershipResignationRequestedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Assignments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Assignments_CompanyId] ON [dbo].[Assignments]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Assignments_EmployeeId] ON [dbo].[Assignments]
(
	[EmployeeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Assignments_UnionId] ON [dbo].[Assignments]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Assignments] ADD  DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt]
ALTER TABLE [dbo].[Assignments] ADD  DEFAULT (CONVERT([bit],(1))) FOR [IsActive]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AssociationRequests]
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AssociationRequests](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AccountingStudioId] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[AdminComment] [nvarchar](max) NULL,
	[Status] [int] NOT NULL,
	[ProcessedAt] [datetime2](7) NULL,
	[CompanyId] [int] NOT NULL,
	[TargetUnionId] [int] NOT NULL,
 CONSTRAINT [PK_AssociationRequests] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_AssociationRequests_AccountingStudioId] ON [dbo].[AssociationRequests]
(
	[AccountingStudioId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_AssociationRequests_CompanyId] ON [dbo].[AssociationRequests]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_AssociationRequests_TargetUnionId] ON [dbo].[AssociationRequests]
(
	[TargetUnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[AssociationRequests] ADD  DEFAULT ((0)) FOR [CompanyId]
ALTER TABLE [dbo].[AssociationRequests] ADD  DEFAULT ((0)) FOR [TargetUnionId]
END;
GO

-- ============================================================
-- Tabla: [dbo].[AuditLogs]
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[AuditLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EntityName] [nvarchar](max) NOT NULL,
	[EntityId] [int] NOT NULL,
	[Action] [nvarchar](max) NOT NULL,
	[PreviousValues] [nvarchar](max) NULL,
	[NewValues] [nvarchar](max) NULL,
	[UserId] [nvarchar](max) NOT NULL,
	[UserEmail] [nvarchar](max) NULL,
	[Timestamp] [datetime2](7) NOT NULL,
	[IPAddress] [nvarchar](max) NULL,
 CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[BalanceHistories]
IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[BalanceHistories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[DeclarationId] [int] NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Concept] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_BalanceHistories] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Cities]
IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Cities](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[ZipCode] [nvarchar](10) NULL,
	[ProvinceId] [int] NOT NULL,
 CONSTRAINT [PK_Cities] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Cities_ProvinceId] ON [dbo].[Cities]
(
	[ProvinceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Companies]
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Companies](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BusinessName] [nvarchar](200) NOT NULL,
	[Cuit] [nvarchar](20) NOT NULL,
	[BranchName] [nvarchar](200) NULL,
	[AccountingStudioId] [int] NULL,
	[ReferencePhone] [nvarchar](100) NULL,
	[ContactEmail] [nvarchar](100) NULL,
	[Status] [int] NOT NULL,
	[Address] [nvarchar](200) NOT NULL,
	[CityId] [int] NULL,
	[Phone] [nvarchar](100) NULL,
	[ReferencePerson] [nvarchar](200) NULL,
	[ParentCompanyId] [int] NULL,
	[IsActive] [bit] NOT NULL,
	[NormalizedCuit]  AS (isnull(replace(replace([Cuit],'-',''),' ',''),'')) PERSISTED NOT NULL,
 CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Companies_AccountingStudioId] ON [dbo].[Companies]
(
	[AccountingStudioId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Companies_CityId] ON [dbo].[Companies]
(
	[CityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [IX_Companies_Cuit_BranchName] ON [dbo].[Companies]
(
	[Cuit] ASC,
	[BranchName] ASC
)
WHERE ([BranchName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF
CREATE UNIQUE NONCLUSTERED INDEX [IX_Companies_NormalizedCuit] ON [dbo].[Companies]
(
	[NormalizedCuit] ASC
)
WHERE ([ParentCompanyId] IS NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Companies_ParentCompanyId] ON [dbo].[Companies]
(
	[ParentCompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Companies] ADD  DEFAULT ((0)) FOR [Status]
ALTER TABLE [dbo].[Companies] ADD  DEFAULT (N'') FOR [Address]
ALTER TABLE [dbo].[Companies] ADD  DEFAULT (CONVERT([bit],(1))) FOR [IsActive]
END;
GO

-- ============================================================
-- Tabla: [dbo].[CompanyBalances]
IF OBJECT_ID(N'[dbo].[CompanyBalances]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[CompanyBalances](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[CreditAmount] [decimal](18, 2) NOT NULL,
	[LastUpdate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_CompanyBalances] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_CompanyBalances_CompanyId] ON [dbo].[CompanyBalances]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[DeclarationDetails]
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DeclarationDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RegistryId] [int] NOT NULL,
	[AssignmentId] [int] NOT NULL,
	[BaseSalary] [decimal](18, 2) NOT NULL,
	[LawContribution] [decimal](18, 2) NOT NULL,
	[MemberContribution] [decimal](18, 2) NOT NULL,
	[TotalWithheld] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_DeclarationDetails] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_DeclarationDetails_AssignmentId] ON [dbo].[DeclarationDetails]
(
	[AssignmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_DeclarationDetails_RegistryId] ON [dbo].[DeclarationDetails]
(
	[RegistryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Declarations]
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Declarations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Period] [nvarchar](7) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[UnionId] [int] NOT NULL,
	[Status] [int] NOT NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
	[IsRectification] [bit] NOT NULL,
	[SubmissionDate] [datetime2](7) NOT NULL,
	[ExternalReference] [nvarchar](100) NULL,
	[MercadoPagoPaymentId] [nvarchar](100) NULL,
	[PaymentDate] [datetime2](7) NULL,
	[PresencialBarCode] [nvarchar](200) NULL,
	[PresencialReference] [nvarchar](50) NULL,
	[BalanceAmount] [decimal](18, 2) NOT NULL,
	[PreviousAmount] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Declarations] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Declarations_CompanyId] ON [dbo].[Declarations]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Declarations_UnionId] ON [dbo].[Declarations]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[Declarations] ADD  DEFAULT ((0.0)) FOR [BalanceAmount]
ALTER TABLE [dbo].[Declarations] ADD  DEFAULT ((0.0)) FOR [PreviousAmount]
END;
GO

-- ============================================================
-- Tabla: [dbo].[EmployeeCategories]
IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[EmployeeCategories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[UnionId] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_EmployeeCategories] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_EmployeeCategories_UnionId] ON [dbo].[EmployeeCategories]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Employees]
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Employees](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Dni] [nvarchar](20) NOT NULL,
	[FullName] [nvarchar](200) NOT NULL,
	[HireDate] [datetime2](7) NOT NULL,
	[TerminationDate] [datetime2](7) NULL,
	[Status] [int] NOT NULL,
	[BiometricData] [varbinary](max) NULL,
 CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [IX_Employees_Dni] ON [dbo].[Employees]
(
	[Dni] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[InspectionActPeriods]
IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[InspectionActPeriods](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InspectionActId] [int] NOT NULL,
	[Period] [nvarchar](max) NOT NULL,
	[BaseAmount] [decimal](18, 2) NOT NULL,
	[CalculatedInterest] [decimal](18, 2) NOT NULL,
	[WasAlreadyDeclared] [bit] NOT NULL,
 CONSTRAINT [PK_InspectionActPeriods] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_InspectionActPeriods_InspectionActId] ON [dbo].[InspectionActPeriods]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[InspectionActs]
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[InspectionActs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[UnionId] [int] NOT NULL,
	[ActNumber] [nvarchar](max) NOT NULL,
	[IssueDate] [datetime2](7) NOT NULL,
	[PeriodRange] [nvarchar](max) NOT NULL,
	[EstimatedDebt] [decimal](18, 2) NOT NULL,
	[InterestAmount] [decimal](18, 2) NOT NULL,
	[Observations] [nvarchar](max) NULL,
	[Status] [int] NOT NULL,
 CONSTRAINT [PK_InspectionActs] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_InspectionActs_CompanyId] ON [dbo].[InspectionActs]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_InspectionActs_UnionId] ON [dbo].[InspectionActs]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[InspectionVisits]
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[InspectionVisits](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InspectionActId] [int] NOT NULL,
	[InspectorId] [int] NOT NULL,
	[VisitNumber] [int] NOT NULL,
	[VisitDate] [datetime2](7) NOT NULL,
	[Status] [nvarchar](max) NOT NULL,
	[Observations] [nvarchar](max) NULL,
	[DocumentNumber] [nvarchar](max) NOT NULL,
	[DebtorSigned] [bit] NOT NULL,
 CONSTRAINT [PK_InspectionVisits] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_InspectionVisits_InspectionActId] ON [dbo].[InspectionVisits]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_InspectionVisits_InspectorId] ON [dbo].[InspectionVisits]
(
	[InspectorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Inspectors]
IF OBJECT_ID(N'[dbo].[Inspectors]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Inspectors](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[Dni] [nvarchar](max) NOT NULL,
	[Phone] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Inspectors] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[InterestRates]
IF OBJECT_ID(N'[dbo].[InterestRates]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[InterestRates](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EffectiveDate] [datetime2](7) NOT NULL,
	[MonthlyRate] [decimal](18, 2) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_InterestRates] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[NoticeAttachments]
IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[NoticeAttachments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PrejudicialNoticeId] [int] NOT NULL,
	[FileName] [nvarchar](max) NOT NULL,
	[FileType] [nvarchar](max) NOT NULL,
	[FileUrl] [nvarchar](max) NOT NULL,
	[UploadDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_NoticeAttachments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_NoticeAttachments_PrejudicialNoticeId] ON [dbo].[NoticeAttachments]
(
	[PrejudicialNoticeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[PaymentPlanInstallments]
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[PaymentPlanInstallments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PaymentPlanId] [int] NOT NULL,
	[InstallmentNumber] [int] NOT NULL,
	[DueDate] [datetime2](7) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[IsPaid] [bit] NOT NULL,
	[PaymentDate] [datetime2](7) NULL,
	[PaymentReference] [nvarchar](max) NULL,
 CONSTRAINT [PK_PaymentPlanInstallments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_PaymentPlanInstallments_PaymentPlanId] ON [dbo].[PaymentPlanInstallments]
(
	[PaymentPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[PaymentPlans]
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[PaymentPlans](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InspectionActId] [int] NOT NULL,
	[PlanNumber] [nvarchar](max) NOT NULL,
	[SignatureDate] [datetime2](7) NOT NULL,
	[TotalDebt] [decimal](18, 2) NOT NULL,
	[DownPayment] [decimal](18, 2) NOT NULL,
	[InstallmentsCount] [int] NOT NULL,
	[InstallmentAmount] [decimal](18, 2) NOT NULL,
	[FinancingInterestRate] [decimal](18, 2) NOT NULL,
	[Status] [int] NOT NULL,
 CONSTRAINT [PK_PaymentPlans] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_PaymentPlans_InspectionActId] ON [dbo].[PaymentPlans]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[PaymentTransactions]
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[PaymentTransactions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DeclarationId] [int] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[PaymentDate] [datetime2](7) NOT NULL,
	[ExternalReference] [nvarchar](max) NULL,
	[PaymentMethod] [nvarchar](max) NULL,
	[Notes] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_DeclarationId] ON [dbo].[PaymentTransactions]
(
	[DeclarationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[PrejudicialNotices]
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[PrejudicialNotices](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InspectionActId] [int] NOT NULL,
	[NoticeNumber] [nvarchar](max) NOT NULL,
	[IssueDate] [datetime2](7) NOT NULL,
	[ExpirationDate] [datetime2](7) NOT NULL,
	[WasNotified] [bit] NOT NULL,
	[Observations] [nvarchar](max) NULL,
	[IsJudicial] [bit] NOT NULL,
 CONSTRAINT [PK_PrejudicialNotices] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_PrejudicialNotices_InspectionActId] ON [dbo].[PrejudicialNotices]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Provinces]
IF OBJECT_ID(N'[dbo].[Provinces]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Provinces](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Provinces] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[SalaryScales]
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[SalaryScales](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BasicAmount] [decimal](18, 2) NOT NULL,
	[AgreementNR2] [decimal](18, 2) NOT NULL,
	[Period] [datetime2](7) NOT NULL,
	[AgreementNR1] [decimal](18, 2) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[UnionId] [int] NOT NULL,
	[AntiguedadPercentage] [decimal](18, 2) NOT NULL,
	[EmployeeCategoryId] [int] NOT NULL,
 CONSTRAINT [PK_SalaryScales] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_SalaryScales_EmployeeCategoryId] ON [dbo].[SalaryScales]
(
	[EmployeeCategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_SalaryScales_UnionId] ON [dbo].[SalaryScales]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[SalaryScales] ADD  DEFAULT ((0.0)) FOR [AgreementNR1]
ALTER TABLE [dbo].[SalaryScales] ADD  DEFAULT (CONVERT([bit],(0))) FOR [IsActive]
ALTER TABLE [dbo].[SalaryScales] ADD  DEFAULT ((0)) FOR [UnionId]
ALTER TABLE [dbo].[SalaryScales] ADD  DEFAULT ((0.0)) FOR [AntiguedadPercentage]
ALTER TABLE [dbo].[SalaryScales] ADD  DEFAULT ((0)) FOR [EmployeeCategoryId]
END;
GO

-- ============================================================
-- Tabla: [dbo].[Unions]
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[Unions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[JurisdictionProvince] [nvarchar](100) NOT NULL,
	[CctCode] [nvarchar](50) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[JurisdictionCity] [nvarchar](100) NULL,
	[LawContributionPercentage] [decimal](18, 2) NOT NULL,
	[UnionMemberPercentage] [decimal](18, 2) NOT NULL,
	[AdminEmail] [nvarchar](max) NULL,
 CONSTRAINT [PK_Unions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
ALTER TABLE [dbo].[Unions] ADD  DEFAULT ((0.0)) FOR [LawContributionPercentage]
ALTER TABLE [dbo].[Unions] ADD  DEFAULT ((0.0)) FOR [UnionMemberPercentage]
END;
GO

-- ============================================================
-- Tabla: [dbo].[UnionSubscriptionPayments]
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[UnionSubscriptionPayments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UnionSubscriptionId] [int] NOT NULL,
	[PaymentDate] [datetime2](7) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[PaymentMethod] [nvarchar](max) NOT NULL,
	[ReceiptNumber] [nvarchar](max) NULL,
	[Notes] [nvarchar](max) NULL,
 CONSTRAINT [PK_UnionSubscriptionPayments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_UnionSubscriptionPayments_UnionSubscriptionId] ON [dbo].[UnionSubscriptionPayments]
(
	[UnionSubscriptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Tabla: [dbo].[UnionSubscriptionRequests]
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[UnionSubscriptionRequests](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UnionName] [nvarchar](200) NOT NULL,
	[Cuit] [nvarchar](20) NOT NULL,
	[ContactEmail] [nvarchar](max) NOT NULL,
	[ContactPhone] [nvarchar](50) NULL,
	[RequestDate] [datetime2](7) NOT NULL,
	[Status] [int] NOT NULL,
	[AdminNotes] [nvarchar](max) NULL,
	[CoverageArea] [nvarchar](500) NULL,
	[Floor] [nvarchar](20) NULL,
	[Locality] [nvarchar](100) NOT NULL,
	[Province] [nvarchar](100) NOT NULL,
	[ReferencePersonFullName] [nvarchar](200) NOT NULL,
	[Street] [nvarchar](200) NOT NULL,
	[StreetNumber] [nvarchar](20) NOT NULL,
	[WhatsApp] [nvarchar](50) NULL,
 CONSTRAINT [PK_UnionSubscriptionRequests] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD  DEFAULT (N'') FOR [Locality]
ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD  DEFAULT (N'') FOR [Province]
ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD  DEFAULT (N'') FOR [ReferencePersonFullName]
ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD  DEFAULT (N'') FOR [Street]
ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD  DEFAULT (N'') FOR [StreetNumber]
END;
GO

-- ============================================================
-- Tabla: [dbo].[UnionSubscriptions]
IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NULL
BEGIN
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[UnionSubscriptions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UnionId] [int] NOT NULL,
	[MonthlyFee] [decimal](18, 2) NOT NULL,
	[NextBillingDate] [datetime2](7) NOT NULL,
	[IsServiceActive] [bit] NOT NULL,
 CONSTRAINT [PK_UnionSubscriptions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_UnionSubscriptions_UnionId] ON [dbo].[UnionSubscriptions]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- Claves foraneas (se agregan al final, cuando todas las tablas
-- ya existen, para no depender del orden)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AccountingStudios_Cities_CityId')
BEGIN
ALTER TABLE [dbo].[AccountingStudios]  WITH CHECK ADD  CONSTRAINT [FK_AccountingStudios_Cities_CityId] FOREIGN KEY([CityId])
REFERENCES [dbo].[Cities] ([Id])
ALTER TABLE [dbo].[AccountingStudios] CHECK CONSTRAINT [FK_AccountingStudios_Cities_CityId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetRoleClaims_AspNetRoles_RoleId')
BEGIN
ALTER TABLE [dbo].[AspNetRoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AspNetRoleClaims] CHECK CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserClaims_AspNetUsers_UserId')
BEGIN
ALTER TABLE [dbo].[AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AspNetUserClaims] CHECK CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserLogins_AspNetUsers_UserId')
BEGIN
ALTER TABLE [dbo].[AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AspNetUserLogins] CHECK CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetRoles_RoleId')
BEGIN
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetUsers_UserId')
BEGIN
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserTokens_AspNetUsers_UserId')
BEGIN
ALTER TABLE [dbo].[AspNetUserTokens]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AspNetUserTokens] CHECK CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Assignments_Companies_CompanyId')
BEGIN
ALTER TABLE [dbo].[Assignments]  WITH CHECK ADD  CONSTRAINT [FK_Assignments_Companies_CompanyId] FOREIGN KEY([CompanyId])
REFERENCES [dbo].[Companies] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Assignments] CHECK CONSTRAINT [FK_Assignments_Companies_CompanyId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Assignments_Employees_EmployeeId')
BEGIN
ALTER TABLE [dbo].[Assignments]  WITH CHECK ADD  CONSTRAINT [FK_Assignments_Employees_EmployeeId] FOREIGN KEY([EmployeeId])
REFERENCES [dbo].[Employees] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Assignments] CHECK CONSTRAINT [FK_Assignments_Employees_EmployeeId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Assignments_Unions_UnionId')
BEGIN
ALTER TABLE [dbo].[Assignments]  WITH CHECK ADD  CONSTRAINT [FK_Assignments_Unions_UnionId] FOREIGN KEY([UnionId])
REFERENCES [dbo].[Unions] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Assignments] CHECK CONSTRAINT [FK_Assignments_Unions_UnionId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssociationRequests_AccountingStudios_AccountingStudioId')
BEGIN
ALTER TABLE [dbo].[AssociationRequests]  WITH CHECK ADD  CONSTRAINT [FK_AssociationRequests_AccountingStudios_AccountingStudioId] FOREIGN KEY([AccountingStudioId])
REFERENCES [dbo].[AccountingStudios] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AssociationRequests] CHECK CONSTRAINT [FK_AssociationRequests_AccountingStudios_AccountingStudioId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssociationRequests_Companies_CompanyId')
BEGIN
ALTER TABLE [dbo].[AssociationRequests]  WITH CHECK ADD  CONSTRAINT [FK_AssociationRequests_Companies_CompanyId] FOREIGN KEY([CompanyId])
REFERENCES [dbo].[Companies] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AssociationRequests] CHECK CONSTRAINT [FK_AssociationRequests_Companies_CompanyId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssociationRequests_Unions_TargetUnionId')
BEGIN
ALTER TABLE [dbo].[AssociationRequests]  WITH CHECK ADD  CONSTRAINT [FK_AssociationRequests_Unions_TargetUnionId] FOREIGN KEY([TargetUnionId])
REFERENCES [dbo].[Unions] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[AssociationRequests] CHECK CONSTRAINT [FK_AssociationRequests_Unions_TargetUnionId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Cities_Provinces_ProvinceId')
BEGIN
ALTER TABLE [dbo].[Cities]  WITH CHECK ADD  CONSTRAINT [FK_Cities_Provinces_ProvinceId] FOREIGN KEY([ProvinceId])
REFERENCES [dbo].[Provinces] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Cities] CHECK CONSTRAINT [FK_Cities_Provinces_ProvinceId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Companies_AccountingStudios_AccountingStudioId')
BEGIN
ALTER TABLE [dbo].[Companies]  WITH CHECK ADD  CONSTRAINT [FK_Companies_AccountingStudios_AccountingStudioId] FOREIGN KEY([AccountingStudioId])
REFERENCES [dbo].[AccountingStudios] ([Id])
ALTER TABLE [dbo].[Companies] CHECK CONSTRAINT [FK_Companies_AccountingStudios_AccountingStudioId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Companies_Cities_CityId')
BEGIN
ALTER TABLE [dbo].[Companies]  WITH CHECK ADD  CONSTRAINT [FK_Companies_Cities_CityId] FOREIGN KEY([CityId])
REFERENCES [dbo].[Cities] ([Id])
ALTER TABLE [dbo].[Companies] CHECK CONSTRAINT [FK_Companies_Cities_CityId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Companies_Companies_ParentCompanyId')
BEGIN
ALTER TABLE [dbo].[Companies]  WITH CHECK ADD  CONSTRAINT [FK_Companies_Companies_ParentCompanyId] FOREIGN KEY([ParentCompanyId])
REFERENCES [dbo].[Companies] ([Id])
ALTER TABLE [dbo].[Companies] CHECK CONSTRAINT [FK_Companies_Companies_ParentCompanyId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CompanyBalances_Companies_CompanyId')
BEGIN
ALTER TABLE [dbo].[CompanyBalances]  WITH CHECK ADD  CONSTRAINT [FK_CompanyBalances_Companies_CompanyId] FOREIGN KEY([CompanyId])
REFERENCES [dbo].[Companies] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[CompanyBalances] CHECK CONSTRAINT [FK_CompanyBalances_Companies_CompanyId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DeclarationDetails_Assignments_AssignmentId')
BEGIN
ALTER TABLE [dbo].[DeclarationDetails]  WITH NOCHECK ADD  CONSTRAINT [FK_DeclarationDetails_Assignments_AssignmentId] FOREIGN KEY([AssignmentId])
REFERENCES [dbo].[Assignments] ([Id])
ALTER TABLE [dbo].[DeclarationDetails] CHECK CONSTRAINT [FK_DeclarationDetails_Assignments_AssignmentId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DeclarationDetails_Declarations_RegistryId')
BEGIN
ALTER TABLE [dbo].[DeclarationDetails]  WITH NOCHECK ADD  CONSTRAINT [FK_DeclarationDetails_Declarations_RegistryId] FOREIGN KEY([RegistryId])
REFERENCES [dbo].[Declarations] ([Id])
ALTER TABLE [dbo].[DeclarationDetails] CHECK CONSTRAINT [FK_DeclarationDetails_Declarations_RegistryId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Declarations_Companies_CompanyId')
BEGIN
ALTER TABLE [dbo].[Declarations]  WITH CHECK ADD  CONSTRAINT [FK_Declarations_Companies_CompanyId] FOREIGN KEY([CompanyId])
REFERENCES [dbo].[Companies] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Declarations] CHECK CONSTRAINT [FK_Declarations_Companies_CompanyId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Declarations_Unions_UnionId')
BEGIN
ALTER TABLE [dbo].[Declarations]  WITH CHECK ADD  CONSTRAINT [FK_Declarations_Unions_UnionId] FOREIGN KEY([UnionId])
REFERENCES [dbo].[Unions] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[Declarations] CHECK CONSTRAINT [FK_Declarations_Unions_UnionId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EmployeeCategories_Unions_UnionId')
BEGIN
ALTER TABLE [dbo].[EmployeeCategories]  WITH CHECK ADD  CONSTRAINT [FK_EmployeeCategories_Unions_UnionId] FOREIGN KEY([UnionId])
REFERENCES [dbo].[Unions] ([Id])
ALTER TABLE [dbo].[EmployeeCategories] CHECK CONSTRAINT [FK_EmployeeCategories_Unions_UnionId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionActPeriods_InspectionActs_InspectionActId')
BEGIN
ALTER TABLE [dbo].[InspectionActPeriods]  WITH CHECK ADD  CONSTRAINT [FK_InspectionActPeriods_InspectionActs_InspectionActId] FOREIGN KEY([InspectionActId])
REFERENCES [dbo].[InspectionActs] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[InspectionActPeriods] CHECK CONSTRAINT [FK_InspectionActPeriods_InspectionActs_InspectionActId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionActs_Companies_CompanyId')
BEGIN
ALTER TABLE [dbo].[InspectionActs]  WITH CHECK ADD  CONSTRAINT [FK_InspectionActs_Companies_CompanyId] FOREIGN KEY([CompanyId])
REFERENCES [dbo].[Companies] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[InspectionActs] CHECK CONSTRAINT [FK_InspectionActs_Companies_CompanyId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionActs_Unions_UnionId')
BEGIN
ALTER TABLE [dbo].[InspectionActs]  WITH CHECK ADD  CONSTRAINT [FK_InspectionActs_Unions_UnionId] FOREIGN KEY([UnionId])
REFERENCES [dbo].[Unions] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[InspectionActs] CHECK CONSTRAINT [FK_InspectionActs_Unions_UnionId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionVisits_InspectionActs_InspectionActId')
BEGIN
ALTER TABLE [dbo].[InspectionVisits]  WITH CHECK ADD  CONSTRAINT [FK_InspectionVisits_InspectionActs_InspectionActId] FOREIGN KEY([InspectionActId])
REFERENCES [dbo].[InspectionActs] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[InspectionVisits] CHECK CONSTRAINT [FK_InspectionVisits_InspectionActs_InspectionActId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionVisits_Inspectors_InspectorId')
BEGIN
ALTER TABLE [dbo].[InspectionVisits]  WITH CHECK ADD  CONSTRAINT [FK_InspectionVisits_Inspectors_InspectorId] FOREIGN KEY([InspectorId])
REFERENCES [dbo].[Inspectors] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[InspectionVisits] CHECK CONSTRAINT [FK_InspectionVisits_Inspectors_InspectorId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_NoticeAttachments_PrejudicialNotices_PrejudicialNoticeId')
BEGIN
ALTER TABLE [dbo].[NoticeAttachments]  WITH CHECK ADD  CONSTRAINT [FK_NoticeAttachments_PrejudicialNotices_PrejudicialNoticeId] FOREIGN KEY([PrejudicialNoticeId])
REFERENCES [dbo].[PrejudicialNotices] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[NoticeAttachments] CHECK CONSTRAINT [FK_NoticeAttachments_PrejudicialNotices_PrejudicialNoticeId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId')
BEGIN
ALTER TABLE [dbo].[PaymentPlanInstallments]  WITH CHECK ADD  CONSTRAINT [FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId] FOREIGN KEY([PaymentPlanId])
REFERENCES [dbo].[PaymentPlans] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[PaymentPlanInstallments] CHECK CONSTRAINT [FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentPlans_InspectionActs_InspectionActId')
BEGIN
ALTER TABLE [dbo].[PaymentPlans]  WITH CHECK ADD  CONSTRAINT [FK_PaymentPlans_InspectionActs_InspectionActId] FOREIGN KEY([InspectionActId])
REFERENCES [dbo].[InspectionActs] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[PaymentPlans] CHECK CONSTRAINT [FK_PaymentPlans_InspectionActs_InspectionActId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentTransactions_Declarations_DeclarationId')
BEGIN
ALTER TABLE [dbo].[PaymentTransactions]  WITH CHECK ADD  CONSTRAINT [FK_PaymentTransactions_Declarations_DeclarationId] FOREIGN KEY([DeclarationId])
REFERENCES [dbo].[Declarations] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[PaymentTransactions] CHECK CONSTRAINT [FK_PaymentTransactions_Declarations_DeclarationId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PrejudicialNotices_InspectionActs_InspectionActId')
BEGIN
ALTER TABLE [dbo].[PrejudicialNotices]  WITH CHECK ADD  CONSTRAINT [FK_PrejudicialNotices_InspectionActs_InspectionActId] FOREIGN KEY([InspectionActId])
REFERENCES [dbo].[InspectionActs] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[PrejudicialNotices] CHECK CONSTRAINT [FK_PrejudicialNotices_InspectionActs_InspectionActId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalaryScales_EmployeeCategories_EmployeeCategoryId')
BEGIN
ALTER TABLE [dbo].[SalaryScales]  WITH CHECK ADD  CONSTRAINT [FK_SalaryScales_EmployeeCategories_EmployeeCategoryId] FOREIGN KEY([EmployeeCategoryId])
REFERENCES [dbo].[EmployeeCategories] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[SalaryScales] CHECK CONSTRAINT [FK_SalaryScales_EmployeeCategories_EmployeeCategoryId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalaryScales_Unions_UnionId')
BEGIN
ALTER TABLE [dbo].[SalaryScales]  WITH CHECK ADD  CONSTRAINT [FK_SalaryScales_Unions_UnionId] FOREIGN KEY([UnionId])
REFERENCES [dbo].[Unions] ([Id])
ALTER TABLE [dbo].[SalaryScales] CHECK CONSTRAINT [FK_SalaryScales_Unions_UnionId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UnionSubscriptionPayments_UnionSubscriptions_UnionSubscriptionId')
BEGIN
ALTER TABLE [dbo].[UnionSubscriptionPayments]  WITH CHECK ADD  CONSTRAINT [FK_UnionSubscriptionPayments_UnionSubscriptions_UnionSubscriptionId] FOREIGN KEY([UnionSubscriptionId])
REFERENCES [dbo].[UnionSubscriptions] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[UnionSubscriptionPayments] CHECK CONSTRAINT [FK_UnionSubscriptionPayments_UnionSubscriptions_UnionSubscriptionId]
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UnionSubscriptions_Unions_UnionId')
BEGIN
ALTER TABLE [dbo].[UnionSubscriptions]  WITH CHECK ADD  CONSTRAINT [FK_UnionSubscriptions_Unions_UnionId] FOREIGN KEY([UnionId])
REFERENCES [dbo].[Unions] ([Id])
ON DELETE CASCADE
ALTER TABLE [dbo].[UnionSubscriptions] CHECK CONSTRAINT [FK_UnionSubscriptions_Unions_UnionId]
END;
GO
