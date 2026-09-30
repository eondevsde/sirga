/* =====================================================================
   SIRGA - Script de ACTUALIZACION (upgrade) de base de datos
   Generado: 2026-08-18  (generado automaticamente desde la DB de referencia)

   Uso: para una base SIRGA existente (produccion). Agrega TODO lo que
        falte para dejarla igual al esquema de referencia: tablas nuevas,
        columnas nuevas (incluidas columnas calculadas), defaults, indices,
        indices filtrados, claves primarias/unicas, checks y claves foraneas.
        Incluye TODOS los cambios realizados hasta la fecha.

   IDEMPOTENTE: puede ejecutarse multiples veces sin errores; cada objeto
        se crea solo si no existe. Re-ejecutar no modifica nada.

   SI LA BASE NO EXISTE: este script tambien la crea completa.

   COLUMNAS NOT NULL: si se agrega una columna NOT NULL a una tabla CON
        DATOS, se respeta su DEFAULT si lo tiene; si NO tiene default se usa
        un patron seguro: se agrega anulable, se rellenan las filas con un
        valor por defecto (0 / vacio / fecha) y se convierte a NOT NULL.
        Asi nunca falla por filas existentes.

   TABLA __EFMigrationsHistory: es interna de EF Core y se excluye de este
        script. Si en el futuro se ejecuta 'dotnet ef database update' en
        produccion, el historial ya aplicado no genera conflictos.

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

   VALIDACION DE DATOS: antes de crear indices unicos y claves foraneas, el
            script verifica que no existan datos duplicados (que harian
            fallar un indice unico) ni huerfanos (que harian fallar una FK)
            y detiene la ejecucion con un mensaje claro indicando la tabla
            y la columna a revisar.

   RECOMENDADO: hacer un BACKUP de la base antes de ejecutar en produccion
            y ejecutar con -b para que el script se detenga ante CUALQUIER
            error (sqlcmd -S servidor -E -d SIRGA_DB -b -i script_sirga_db_upgrade_produccion.sql).
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
-- 1) TABLAS FALTANTES (se crean completas)
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
-- 2) COLUMNAS FALTANTES
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'Name')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AccountingStudios])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AccountingStudios] ADD [Name] nvarchar(200) NULL');
        EXEC(N'UPDATE [dbo].[AccountingStudios] SET [Name] = '''' WHERE [Name] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AccountingStudios] ALTER COLUMN [Name] nvarchar(200) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AccountingStudios] ADD [Name] nvarchar(200) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'ResponsibleUsername')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AccountingStudios])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AccountingStudios] ADD [ResponsibleUsername] nvarchar(100) NULL');
        EXEC(N'UPDATE [dbo].[AccountingStudios] SET [ResponsibleUsername] = '''' WHERE [ResponsibleUsername] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AccountingStudios] ALTER COLUMN [ResponsibleUsername] nvarchar(100) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AccountingStudios] ADD [ResponsibleUsername] nvarchar(100) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'Email')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [Email] nvarchar(200) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'ApprovedAt')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [ApprovedAt] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [CreatedAt] datetime2(7) NOT NULL DEFAULT ('0001-01-01T00:00:00.0000000');
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'CreatedAt')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[AccountingStudios]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'CreatedAt'))
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt];
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'IsApproved')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [IsApproved] bit NOT NULL DEFAULT (CONVERT([bit],(0)));
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'IsApproved')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[AccountingStudios]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'IsApproved'))
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD DEFAULT (CONVERT([bit],(0))) FOR [IsApproved];
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'Address')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [Address] nvarchar(250) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'CityId')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [CityId] int NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'ContactCellPhone')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [ContactCellPhone] nvarchar(50) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'ContactPhone')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [ContactPhone] nvarchar(50) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'IsActive')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [IsActive] bit NOT NULL DEFAULT (CONVERT([bit],(0)));
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'IsActive')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[AccountingStudios]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'IsActive'))
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD DEFAULT (CONVERT([bit],(0))) FOR [IsActive];
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'ReferencePerson')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [ReferencePerson] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'ReferencePhone')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [ReferencePhone] nvarchar(50) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'RejectionReason')
BEGIN
    ALTER TABLE [dbo].[AccountingStudios] ADD [RejectionReason] nvarchar(500) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[AspNetRoleClaims] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND name = N'RoleId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetRoleClaims])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetRoleClaims] ADD [RoleId] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetRoleClaims] SET [RoleId] = '''' WHERE [RoleId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetRoleClaims] ALTER COLUMN [RoleId] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetRoleClaims] ADD [RoleId] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND name = N'ClaimType')
BEGIN
    ALTER TABLE [dbo].[AspNetRoleClaims] ADD [ClaimType] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND name = N'ClaimValue')
BEGIN
    ALTER TABLE [dbo].[AspNetRoleClaims] ADD [ClaimValue] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'Id')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetRoles])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetRoles] ADD [Id] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetRoles] SET [Id] = '''' WHERE [Id] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetRoles] ALTER COLUMN [Id] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetRoles] ADD [Id] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'Name')
BEGIN
    ALTER TABLE [dbo].[AspNetRoles] ADD [Name] nvarchar(256) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'NormalizedName')
BEGIN
    ALTER TABLE [dbo].[AspNetRoles] ADD [NormalizedName] nvarchar(256) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'ConcurrencyStamp')
BEGIN
    ALTER TABLE [dbo].[AspNetRoles] ADD [ConcurrencyStamp] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[AspNetUserClaims] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND name = N'UserId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserClaims])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserClaims] ADD [UserId] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserClaims] SET [UserId] = '''' WHERE [UserId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserClaims] ALTER COLUMN [UserId] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserClaims] ADD [UserId] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND name = N'ClaimType')
BEGIN
    ALTER TABLE [dbo].[AspNetUserClaims] ADD [ClaimType] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND name = N'ClaimValue')
BEGIN
    ALTER TABLE [dbo].[AspNetUserClaims] ADD [ClaimValue] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND name = N'LoginProvider')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserLogins])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserLogins] ADD [LoginProvider] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserLogins] SET [LoginProvider] = '''' WHERE [LoginProvider] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserLogins] ALTER COLUMN [LoginProvider] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserLogins] ADD [LoginProvider] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND name = N'ProviderKey')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserLogins])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserLogins] ADD [ProviderKey] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserLogins] SET [ProviderKey] = '''' WHERE [ProviderKey] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserLogins] ALTER COLUMN [ProviderKey] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserLogins] ADD [ProviderKey] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND name = N'ProviderDisplayName')
BEGIN
    ALTER TABLE [dbo].[AspNetUserLogins] ADD [ProviderDisplayName] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND name = N'UserId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserLogins])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserLogins] ADD [UserId] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserLogins] SET [UserId] = '''' WHERE [UserId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserLogins] ALTER COLUMN [UserId] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserLogins] ADD [UserId] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND name = N'UserId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserRoles])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserRoles] ADD [UserId] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserRoles] SET [UserId] = '''' WHERE [UserId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserRoles] ALTER COLUMN [UserId] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserRoles] ADD [UserId] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND name = N'RoleId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserRoles])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserRoles] ADD [RoleId] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserRoles] SET [RoleId] = '''' WHERE [RoleId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserRoles] ALTER COLUMN [RoleId] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserRoles] ADD [RoleId] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'Id')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUsers])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ADD [Id] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUsers] SET [Id] = '''' WHERE [Id] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ALTER COLUMN [Id] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUsers] ADD [Id] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'UserName')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [UserName] nvarchar(256) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'NormalizedUserName')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [NormalizedUserName] nvarchar(256) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'Email')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [Email] nvarchar(256) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'NormalizedEmail')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [NormalizedEmail] nvarchar(256) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'EmailConfirmed')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUsers])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ADD [EmailConfirmed] bit NULL');
        EXEC(N'UPDATE [dbo].[AspNetUsers] SET [EmailConfirmed] = 0 WHERE [EmailConfirmed] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ALTER COLUMN [EmailConfirmed] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUsers] ADD [EmailConfirmed] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'PasswordHash')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [PasswordHash] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'SecurityStamp')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [SecurityStamp] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'ConcurrencyStamp')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [ConcurrencyStamp] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'PhoneNumber')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [PhoneNumber] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'PhoneNumberConfirmed')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUsers])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ADD [PhoneNumberConfirmed] bit NULL');
        EXEC(N'UPDATE [dbo].[AspNetUsers] SET [PhoneNumberConfirmed] = 0 WHERE [PhoneNumberConfirmed] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ALTER COLUMN [PhoneNumberConfirmed] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUsers] ADD [PhoneNumberConfirmed] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'TwoFactorEnabled')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUsers])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ADD [TwoFactorEnabled] bit NULL');
        EXEC(N'UPDATE [dbo].[AspNetUsers] SET [TwoFactorEnabled] = 0 WHERE [TwoFactorEnabled] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ALTER COLUMN [TwoFactorEnabled] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUsers] ADD [TwoFactorEnabled] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'LockoutEnd')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [LockoutEnd] datetimeoffset(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'LockoutEnabled')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUsers])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ADD [LockoutEnabled] bit NULL');
        EXEC(N'UPDATE [dbo].[AspNetUsers] SET [LockoutEnabled] = 0 WHERE [LockoutEnabled] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ALTER COLUMN [LockoutEnabled] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUsers] ADD [LockoutEnabled] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'AccessFailedCount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUsers])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ADD [AccessFailedCount] int NULL');
        EXEC(N'UPDATE [dbo].[AspNetUsers] SET [AccessFailedCount] = 0 WHERE [AccessFailedCount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUsers] ALTER COLUMN [AccessFailedCount] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUsers] ADD [AccessFailedCount] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND name = N'UserId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserTokens])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserTokens] ADD [UserId] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserTokens] SET [UserId] = '''' WHERE [UserId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserTokens] ALTER COLUMN [UserId] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserTokens] ADD [UserId] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND name = N'LoginProvider')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserTokens])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserTokens] ADD [LoginProvider] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserTokens] SET [LoginProvider] = '''' WHERE [LoginProvider] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserTokens] ALTER COLUMN [LoginProvider] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserTokens] ADD [LoginProvider] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND name = N'Name')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AspNetUserTokens])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AspNetUserTokens] ADD [Name] nvarchar(450) NULL');
        EXEC(N'UPDATE [dbo].[AspNetUserTokens] SET [Name] = '''' WHERE [Name] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AspNetUserTokens] ALTER COLUMN [Name] nvarchar(450) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AspNetUserTokens] ADD [Name] nvarchar(450) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND name = N'Value')
BEGIN
    ALTER TABLE [dbo].[AspNetUserTokens] ADD [Value] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'EmployeeId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Assignments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Assignments] ADD [EmployeeId] int NULL');
        EXEC(N'UPDATE [dbo].[Assignments] SET [EmployeeId] = 0 WHERE [EmployeeId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Assignments] ALTER COLUMN [EmployeeId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Assignments] ADD [EmployeeId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'CompanyId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Assignments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Assignments] ADD [CompanyId] int NULL');
        EXEC(N'UPDATE [dbo].[Assignments] SET [CompanyId] = 0 WHERE [CompanyId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Assignments] ALTER COLUMN [CompanyId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Assignments] ADD [CompanyId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'UnionId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Assignments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Assignments] ADD [UnionId] int NULL');
        EXEC(N'UPDATE [dbo].[Assignments] SET [UnionId] = 0 WHERE [UnionId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Assignments] ALTER COLUMN [UnionId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Assignments] ADD [UnionId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'IsVoluntaryMember')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Assignments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Assignments] ADD [IsVoluntaryMember] bit NULL');
        EXEC(N'UPDATE [dbo].[Assignments] SET [IsVoluntaryMember] = 0 WHERE [IsVoluntaryMember] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Assignments] ALTER COLUMN [IsVoluntaryMember] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Assignments] ADD [IsVoluntaryMember] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'CategoryCode')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [CategoryCode] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [CreatedAt] datetime2(7) NOT NULL DEFAULT ('0001-01-01T00:00:00.0000000');
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'CreatedAt')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Assignments]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'CreatedAt'))
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD DEFAULT ('0001-01-01T00:00:00.0000000') FOR [CreatedAt];
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'IsActive')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [IsActive] bit NOT NULL DEFAULT (CONVERT([bit],(1)));
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'IsActive')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Assignments]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'IsActive'))
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD DEFAULT (CONVERT([bit],(1))) FOR [IsActive];
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'TerminationDate')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [TerminationDate] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'MembershipEndDate')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [MembershipEndDate] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'MembershipResignationReason')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [MembershipResignationReason] nvarchar(500) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'MembershipResignationRequested')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [MembershipResignationRequested] bit NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'MembershipResignationRequestedAt')
BEGIN
    ALTER TABLE [dbo].[Assignments] ADD [MembershipResignationRequestedAt] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[AssociationRequests] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'AccountingStudioId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AssociationRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AssociationRequests] ADD [AccountingStudioId] int NULL');
        EXEC(N'UPDATE [dbo].[AssociationRequests] SET [AccountingStudioId] = 0 WHERE [AccountingStudioId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AssociationRequests] ALTER COLUMN [AccountingStudioId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AssociationRequests] ADD [AccountingStudioId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'CreatedAt')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AssociationRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AssociationRequests] ADD [CreatedAt] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[AssociationRequests] SET [CreatedAt] = ''1900-01-01'' WHERE [CreatedAt] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AssociationRequests] ALTER COLUMN [CreatedAt] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AssociationRequests] ADD [CreatedAt] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'AdminComment')
BEGIN
    ALTER TABLE [dbo].[AssociationRequests] ADD [AdminComment] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'Status')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AssociationRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AssociationRequests] ADD [Status] int NULL');
        EXEC(N'UPDATE [dbo].[AssociationRequests] SET [Status] = 0 WHERE [Status] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AssociationRequests] ALTER COLUMN [Status] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AssociationRequests] ADD [Status] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'ProcessedAt')
BEGIN
    ALTER TABLE [dbo].[AssociationRequests] ADD [ProcessedAt] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'CompanyId')
BEGIN
    ALTER TABLE [dbo].[AssociationRequests] ADD [CompanyId] int NOT NULL DEFAULT ((0));
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'CompanyId')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[AssociationRequests]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'CompanyId'))
BEGIN
    ALTER TABLE [dbo].[AssociationRequests] ADD DEFAULT ((0)) FOR [CompanyId];
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'TargetUnionId')
BEGIN
    ALTER TABLE [dbo].[AssociationRequests] ADD [TargetUnionId] int NOT NULL DEFAULT ((0));
END;
GO
IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'TargetUnionId')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[AssociationRequests]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'TargetUnionId'))
BEGIN
    ALTER TABLE [dbo].[AssociationRequests] ADD DEFAULT ((0)) FOR [TargetUnionId];
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[AuditLogs] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'EntityName')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AuditLogs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ADD [EntityName] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[AuditLogs] SET [EntityName] = '''' WHERE [EntityName] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ALTER COLUMN [EntityName] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AuditLogs] ADD [EntityName] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'EntityId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AuditLogs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ADD [EntityId] int NULL');
        EXEC(N'UPDATE [dbo].[AuditLogs] SET [EntityId] = 0 WHERE [EntityId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ALTER COLUMN [EntityId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AuditLogs] ADD [EntityId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'Action')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AuditLogs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ADD [Action] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[AuditLogs] SET [Action] = '''' WHERE [Action] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ALTER COLUMN [Action] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AuditLogs] ADD [Action] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'PreviousValues')
BEGIN
    ALTER TABLE [dbo].[AuditLogs] ADD [PreviousValues] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'NewValues')
BEGIN
    ALTER TABLE [dbo].[AuditLogs] ADD [NewValues] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'UserId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AuditLogs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ADD [UserId] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[AuditLogs] SET [UserId] = '''' WHERE [UserId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ALTER COLUMN [UserId] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AuditLogs] ADD [UserId] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'UserEmail')
BEGIN
    ALTER TABLE [dbo].[AuditLogs] ADD [UserEmail] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'Timestamp')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[AuditLogs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ADD [Timestamp] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[AuditLogs] SET [Timestamp] = ''1900-01-01'' WHERE [Timestamp] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[AuditLogs] ALTER COLUMN [Timestamp] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[AuditLogs] ADD [Timestamp] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'IPAddress')
BEGIN
    ALTER TABLE [dbo].[AuditLogs] ADD [IPAddress] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BalanceHistories]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[BalanceHistories] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BalanceHistories]') AND name = N'CompanyId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[BalanceHistories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ADD [CompanyId] int NULL');
        EXEC(N'UPDATE [dbo].[BalanceHistories] SET [CompanyId] = 0 WHERE [CompanyId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ALTER COLUMN [CompanyId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[BalanceHistories] ADD [CompanyId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BalanceHistories]') AND name = N'DeclarationId')
BEGIN
    ALTER TABLE [dbo].[BalanceHistories] ADD [DeclarationId] int NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BalanceHistories]') AND name = N'Amount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[BalanceHistories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ADD [Amount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[BalanceHistories] SET [Amount] = 0 WHERE [Amount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ALTER COLUMN [Amount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[BalanceHistories] ADD [Amount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BalanceHistories]') AND name = N'Concept')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[BalanceHistories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ADD [Concept] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[BalanceHistories] SET [Concept] = '''' WHERE [Concept] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ALTER COLUMN [Concept] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[BalanceHistories] ADD [Concept] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BalanceHistories]') AND name = N'CreatedAt')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[BalanceHistories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ADD [CreatedAt] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[BalanceHistories] SET [CreatedAt] = ''1900-01-01'' WHERE [CreatedAt] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[BalanceHistories] ALTER COLUMN [CreatedAt] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[BalanceHistories] ADD [CreatedAt] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Cities]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Cities] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Cities]') AND name = N'Name')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Cities])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Cities] ADD [Name] nvarchar(100) NULL');
        EXEC(N'UPDATE [dbo].[Cities] SET [Name] = '''' WHERE [Name] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Cities] ALTER COLUMN [Name] nvarchar(100) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Cities] ADD [Name] nvarchar(100) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Cities]') AND name = N'ZipCode')
BEGIN
    ALTER TABLE [dbo].[Cities] ADD [ZipCode] nvarchar(10) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Cities]') AND name = N'ProvinceId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Cities])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Cities] ADD [ProvinceId] int NULL');
        EXEC(N'UPDATE [dbo].[Cities] SET [ProvinceId] = 0 WHERE [ProvinceId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Cities] ALTER COLUMN [ProvinceId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Cities] ADD [ProvinceId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'BusinessName')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Companies])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Companies] ADD [BusinessName] nvarchar(200) NULL');
        EXEC(N'UPDATE [dbo].[Companies] SET [BusinessName] = '''' WHERE [BusinessName] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Companies] ALTER COLUMN [BusinessName] nvarchar(200) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Companies] ADD [BusinessName] nvarchar(200) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Cuit')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Companies])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Companies] ADD [Cuit] nvarchar(20) NULL');
        EXEC(N'UPDATE [dbo].[Companies] SET [Cuit] = '''' WHERE [Cuit] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Companies] ALTER COLUMN [Cuit] nvarchar(20) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Companies] ADD [Cuit] nvarchar(20) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'BranchName')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [BranchName] nvarchar(200) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'AccountingStudioId')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [AccountingStudioId] int NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'ReferencePhone')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [ReferencePhone] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'ContactEmail')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [ContactEmail] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Status')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [Status] int NOT NULL DEFAULT ((0));
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Status')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Companies]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Status'))
BEGIN
    ALTER TABLE [dbo].[Companies] ADD DEFAULT ((0)) FOR [Status];
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Address')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [Address] nvarchar(200) NOT NULL DEFAULT (N'');
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Address')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Companies]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Address'))
BEGIN
    ALTER TABLE [dbo].[Companies] ADD DEFAULT (N'') FOR [Address];
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'CityId')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [CityId] int NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'Phone')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [Phone] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'ReferencePerson')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [ReferencePerson] nvarchar(200) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'ParentCompanyId')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [ParentCompanyId] int NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IsActive')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [IsActive] bit NOT NULL DEFAULT (CONVERT([bit],(1)));
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IsActive')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Companies]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IsActive'))
BEGIN
    ALTER TABLE [dbo].[Companies] ADD DEFAULT (CONVERT([bit],(1))) FOR [IsActive];
END;
GO
IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'NormalizedCuit')
BEGIN
    ALTER TABLE [dbo].[Companies] ADD [NormalizedCuit] AS (isnull(replace(replace([Cuit],'-',''),' ',''),'')) PERSISTED NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[CompanyBalances]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyBalances]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[CompanyBalances] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[CompanyBalances]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyBalances]') AND name = N'CompanyId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[CompanyBalances])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[CompanyBalances] ADD [CompanyId] int NULL');
        EXEC(N'UPDATE [dbo].[CompanyBalances] SET [CompanyId] = 0 WHERE [CompanyId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[CompanyBalances] ALTER COLUMN [CompanyId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[CompanyBalances] ADD [CompanyId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[CompanyBalances]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyBalances]') AND name = N'CreditAmount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[CompanyBalances])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[CompanyBalances] ADD [CreditAmount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[CompanyBalances] SET [CreditAmount] = 0 WHERE [CreditAmount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[CompanyBalances] ALTER COLUMN [CreditAmount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[CompanyBalances] ADD [CreditAmount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[CompanyBalances]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyBalances]') AND name = N'LastUpdate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[CompanyBalances])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[CompanyBalances] ADD [LastUpdate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[CompanyBalances] SET [LastUpdate] = ''1900-01-01'' WHERE [LastUpdate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[CompanyBalances] ALTER COLUMN [LastUpdate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[CompanyBalances] ADD [LastUpdate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[DeclarationDetails] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'RegistryId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[DeclarationDetails])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ADD [RegistryId] int NULL');
        EXEC(N'UPDATE [dbo].[DeclarationDetails] SET [RegistryId] = 0 WHERE [RegistryId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ALTER COLUMN [RegistryId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[DeclarationDetails] ADD [RegistryId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'AssignmentId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[DeclarationDetails])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ADD [AssignmentId] int NULL');
        EXEC(N'UPDATE [dbo].[DeclarationDetails] SET [AssignmentId] = 0 WHERE [AssignmentId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ALTER COLUMN [AssignmentId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[DeclarationDetails] ADD [AssignmentId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'BaseSalary')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[DeclarationDetails])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ADD [BaseSalary] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[DeclarationDetails] SET [BaseSalary] = 0 WHERE [BaseSalary] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ALTER COLUMN [BaseSalary] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[DeclarationDetails] ADD [BaseSalary] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'LawContribution')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[DeclarationDetails])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ADD [LawContribution] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[DeclarationDetails] SET [LawContribution] = 0 WHERE [LawContribution] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ALTER COLUMN [LawContribution] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[DeclarationDetails] ADD [LawContribution] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'MemberContribution')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[DeclarationDetails])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ADD [MemberContribution] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[DeclarationDetails] SET [MemberContribution] = 0 WHERE [MemberContribution] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ALTER COLUMN [MemberContribution] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[DeclarationDetails] ADD [MemberContribution] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'TotalWithheld')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[DeclarationDetails])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ADD [TotalWithheld] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[DeclarationDetails] SET [TotalWithheld] = 0 WHERE [TotalWithheld] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[DeclarationDetails] ALTER COLUMN [TotalWithheld] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[DeclarationDetails] ADD [TotalWithheld] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'Period')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Declarations])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Declarations] ADD [Period] nvarchar(7) NULL');
        EXEC(N'UPDATE [dbo].[Declarations] SET [Period] = '''' WHERE [Period] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Declarations] ALTER COLUMN [Period] nvarchar(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Declarations] ADD [Period] nvarchar(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'CompanyId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Declarations])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Declarations] ADD [CompanyId] int NULL');
        EXEC(N'UPDATE [dbo].[Declarations] SET [CompanyId] = 0 WHERE [CompanyId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Declarations] ALTER COLUMN [CompanyId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Declarations] ADD [CompanyId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'UnionId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Declarations])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Declarations] ADD [UnionId] int NULL');
        EXEC(N'UPDATE [dbo].[Declarations] SET [UnionId] = 0 WHERE [UnionId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Declarations] ALTER COLUMN [UnionId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Declarations] ADD [UnionId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'Status')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Declarations])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Declarations] ADD [Status] int NULL');
        EXEC(N'UPDATE [dbo].[Declarations] SET [Status] = 0 WHERE [Status] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Declarations] ALTER COLUMN [Status] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Declarations] ADD [Status] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'TotalAmount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Declarations])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Declarations] ADD [TotalAmount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[Declarations] SET [TotalAmount] = 0 WHERE [TotalAmount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Declarations] ALTER COLUMN [TotalAmount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Declarations] ADD [TotalAmount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'IsRectification')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Declarations])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Declarations] ADD [IsRectification] bit NULL');
        EXEC(N'UPDATE [dbo].[Declarations] SET [IsRectification] = 0 WHERE [IsRectification] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Declarations] ALTER COLUMN [IsRectification] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Declarations] ADD [IsRectification] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'SubmissionDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Declarations])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Declarations] ADD [SubmissionDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[Declarations] SET [SubmissionDate] = ''1900-01-01'' WHERE [SubmissionDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Declarations] ALTER COLUMN [SubmissionDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Declarations] ADD [SubmissionDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'ExternalReference')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [ExternalReference] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'MercadoPagoPaymentId')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [MercadoPagoPaymentId] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'PaymentDate')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [PaymentDate] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'PresencialBarCode')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [PresencialBarCode] nvarchar(200) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'PresencialReference')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [PresencialReference] nvarchar(50) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'BalanceAmount')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [BalanceAmount] decimal(18,2) NOT NULL DEFAULT ((0.0));
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'BalanceAmount')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Declarations]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'BalanceAmount'))
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD DEFAULT ((0.0)) FOR [BalanceAmount];
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'PreviousAmount')
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD [PreviousAmount] decimal(18,2) NOT NULL DEFAULT ((0.0));
END;
GO
IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'PreviousAmount')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Declarations]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'PreviousAmount'))
BEGIN
    ALTER TABLE [dbo].[Declarations] ADD DEFAULT ((0.0)) FOR [PreviousAmount];
END;
GO
IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCategories]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[EmployeeCategories] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCategories]') AND name = N'Code')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[EmployeeCategories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ADD [Code] nvarchar(50) NULL');
        EXEC(N'UPDATE [dbo].[EmployeeCategories] SET [Code] = '''' WHERE [Code] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ALTER COLUMN [Code] nvarchar(50) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[EmployeeCategories] ADD [Code] nvarchar(50) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCategories]') AND name = N'Name')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[EmployeeCategories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ADD [Name] nvarchar(200) NULL');
        EXEC(N'UPDATE [dbo].[EmployeeCategories] SET [Name] = '''' WHERE [Name] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ALTER COLUMN [Name] nvarchar(200) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[EmployeeCategories] ADD [Name] nvarchar(200) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCategories]') AND name = N'UnionId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[EmployeeCategories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ADD [UnionId] int NULL');
        EXEC(N'UPDATE [dbo].[EmployeeCategories] SET [UnionId] = 0 WHERE [UnionId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ALTER COLUMN [UnionId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[EmployeeCategories] ADD [UnionId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCategories]') AND name = N'IsActive')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[EmployeeCategories])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ADD [IsActive] bit NULL');
        EXEC(N'UPDATE [dbo].[EmployeeCategories] SET [IsActive] = 0 WHERE [IsActive] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[EmployeeCategories] ALTER COLUMN [IsActive] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[EmployeeCategories] ADD [IsActive] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Employees] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'Dni')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Employees])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Employees] ADD [Dni] nvarchar(20) NULL');
        EXEC(N'UPDATE [dbo].[Employees] SET [Dni] = '''' WHERE [Dni] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Employees] ALTER COLUMN [Dni] nvarchar(20) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Employees] ADD [Dni] nvarchar(20) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'FullName')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Employees])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Employees] ADD [FullName] nvarchar(200) NULL');
        EXEC(N'UPDATE [dbo].[Employees] SET [FullName] = '''' WHERE [FullName] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Employees] ALTER COLUMN [FullName] nvarchar(200) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Employees] ADD [FullName] nvarchar(200) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'HireDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Employees])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Employees] ADD [HireDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[Employees] SET [HireDate] = ''1900-01-01'' WHERE [HireDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Employees] ALTER COLUMN [HireDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Employees] ADD [HireDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'TerminationDate')
BEGIN
    ALTER TABLE [dbo].[Employees] ADD [TerminationDate] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'Status')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Employees])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Employees] ADD [Status] int NULL');
        EXEC(N'UPDATE [dbo].[Employees] SET [Status] = 0 WHERE [Status] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Employees] ALTER COLUMN [Status] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Employees] ADD [Status] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'BiometricData')
BEGIN
    ALTER TABLE [dbo].[Employees] ADD [BiometricData] varbinary(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[InspectionActPeriods] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'InspectionActId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActPeriods])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ADD [InspectionActId] int NULL');
        EXEC(N'UPDATE [dbo].[InspectionActPeriods] SET [InspectionActId] = 0 WHERE [InspectionActId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ALTER COLUMN [InspectionActId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActPeriods] ADD [InspectionActId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'Period')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActPeriods])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ADD [Period] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActPeriods] SET [Period] = '''' WHERE [Period] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ALTER COLUMN [Period] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActPeriods] ADD [Period] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'BaseAmount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActPeriods])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ADD [BaseAmount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActPeriods] SET [BaseAmount] = 0 WHERE [BaseAmount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ALTER COLUMN [BaseAmount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActPeriods] ADD [BaseAmount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'CalculatedInterest')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActPeriods])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ADD [CalculatedInterest] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActPeriods] SET [CalculatedInterest] = 0 WHERE [CalculatedInterest] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ALTER COLUMN [CalculatedInterest] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActPeriods] ADD [CalculatedInterest] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'WasAlreadyDeclared')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActPeriods])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ADD [WasAlreadyDeclared] bit NULL');
        EXEC(N'UPDATE [dbo].[InspectionActPeriods] SET [WasAlreadyDeclared] = 0 WHERE [WasAlreadyDeclared] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActPeriods] ALTER COLUMN [WasAlreadyDeclared] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActPeriods] ADD [WasAlreadyDeclared] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[InspectionActs] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'CompanyId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [CompanyId] int NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [CompanyId] = 0 WHERE [CompanyId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [CompanyId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [CompanyId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'UnionId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [UnionId] int NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [UnionId] = 0 WHERE [UnionId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [UnionId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [UnionId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'ActNumber')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [ActNumber] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [ActNumber] = '''' WHERE [ActNumber] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [ActNumber] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [ActNumber] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'IssueDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [IssueDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [IssueDate] = ''1900-01-01'' WHERE [IssueDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [IssueDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [IssueDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'PeriodRange')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [PeriodRange] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [PeriodRange] = '''' WHERE [PeriodRange] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [PeriodRange] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [PeriodRange] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'EstimatedDebt')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [EstimatedDebt] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [EstimatedDebt] = 0 WHERE [EstimatedDebt] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [EstimatedDebt] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [EstimatedDebt] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'InterestAmount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [InterestAmount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [InterestAmount] = 0 WHERE [InterestAmount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [InterestAmount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [InterestAmount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'Observations')
BEGIN
    ALTER TABLE [dbo].[InspectionActs] ADD [Observations] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'Status')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionActs])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ADD [Status] int NULL');
        EXEC(N'UPDATE [dbo].[InspectionActs] SET [Status] = 0 WHERE [Status] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionActs] ALTER COLUMN [Status] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionActs] ADD [Status] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[InspectionVisits] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'InspectionActId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionVisits])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ADD [InspectionActId] int NULL');
        EXEC(N'UPDATE [dbo].[InspectionVisits] SET [InspectionActId] = 0 WHERE [InspectionActId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ALTER COLUMN [InspectionActId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionVisits] ADD [InspectionActId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'InspectorId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionVisits])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ADD [InspectorId] int NULL');
        EXEC(N'UPDATE [dbo].[InspectionVisits] SET [InspectorId] = 0 WHERE [InspectorId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ALTER COLUMN [InspectorId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionVisits] ADD [InspectorId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'VisitNumber')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionVisits])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ADD [VisitNumber] int NULL');
        EXEC(N'UPDATE [dbo].[InspectionVisits] SET [VisitNumber] = 0 WHERE [VisitNumber] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ALTER COLUMN [VisitNumber] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionVisits] ADD [VisitNumber] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'VisitDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionVisits])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ADD [VisitDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[InspectionVisits] SET [VisitDate] = ''1900-01-01'' WHERE [VisitDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ALTER COLUMN [VisitDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionVisits] ADD [VisitDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'Status')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionVisits])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ADD [Status] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[InspectionVisits] SET [Status] = '''' WHERE [Status] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ALTER COLUMN [Status] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionVisits] ADD [Status] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'Observations')
BEGIN
    ALTER TABLE [dbo].[InspectionVisits] ADD [Observations] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'DocumentNumber')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionVisits])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ADD [DocumentNumber] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[InspectionVisits] SET [DocumentNumber] = '''' WHERE [DocumentNumber] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ALTER COLUMN [DocumentNumber] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionVisits] ADD [DocumentNumber] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'DebtorSigned')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InspectionVisits])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ADD [DebtorSigned] bit NULL');
        EXEC(N'UPDATE [dbo].[InspectionVisits] SET [DebtorSigned] = 0 WHERE [DebtorSigned] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InspectionVisits] ALTER COLUMN [DebtorSigned] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InspectionVisits] ADD [DebtorSigned] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Inspectors]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Inspectors]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Inspectors] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Inspectors]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Inspectors]') AND name = N'Name')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Inspectors])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Inspectors] ADD [Name] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[Inspectors] SET [Name] = '''' WHERE [Name] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Inspectors] ALTER COLUMN [Name] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Inspectors] ADD [Name] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Inspectors]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Inspectors]') AND name = N'Dni')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Inspectors])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Inspectors] ADD [Dni] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[Inspectors] SET [Dni] = '''' WHERE [Dni] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Inspectors] ALTER COLUMN [Dni] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Inspectors] ADD [Dni] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Inspectors]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Inspectors]') AND name = N'Phone')
BEGIN
    ALTER TABLE [dbo].[Inspectors] ADD [Phone] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Inspectors]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Inspectors]') AND name = N'IsActive')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Inspectors])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Inspectors] ADD [IsActive] bit NULL');
        EXEC(N'UPDATE [dbo].[Inspectors] SET [IsActive] = 0 WHERE [IsActive] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Inspectors] ALTER COLUMN [IsActive] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Inspectors] ADD [IsActive] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InterestRates]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InterestRates]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[InterestRates] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[InterestRates]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InterestRates]') AND name = N'EffectiveDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InterestRates])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InterestRates] ADD [EffectiveDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[InterestRates] SET [EffectiveDate] = ''1900-01-01'' WHERE [EffectiveDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InterestRates] ALTER COLUMN [EffectiveDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InterestRates] ADD [EffectiveDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InterestRates]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InterestRates]') AND name = N'MonthlyRate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InterestRates])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InterestRates] ADD [MonthlyRate] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[InterestRates] SET [MonthlyRate] = 0 WHERE [MonthlyRate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InterestRates] ALTER COLUMN [MonthlyRate] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InterestRates] ADD [MonthlyRate] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[InterestRates]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InterestRates]') AND name = N'Description')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[InterestRates])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[InterestRates] ADD [Description] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[InterestRates] SET [Description] = '''' WHERE [Description] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[InterestRates] ALTER COLUMN [Description] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[InterestRates] ADD [Description] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[NoticeAttachments] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'PrejudicialNoticeId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[NoticeAttachments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ADD [PrejudicialNoticeId] int NULL');
        EXEC(N'UPDATE [dbo].[NoticeAttachments] SET [PrejudicialNoticeId] = 0 WHERE [PrejudicialNoticeId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ALTER COLUMN [PrejudicialNoticeId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[NoticeAttachments] ADD [PrejudicialNoticeId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'FileName')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[NoticeAttachments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ADD [FileName] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[NoticeAttachments] SET [FileName] = '''' WHERE [FileName] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ALTER COLUMN [FileName] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[NoticeAttachments] ADD [FileName] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'FileType')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[NoticeAttachments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ADD [FileType] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[NoticeAttachments] SET [FileType] = '''' WHERE [FileType] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ALTER COLUMN [FileType] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[NoticeAttachments] ADD [FileType] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'FileUrl')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[NoticeAttachments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ADD [FileUrl] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[NoticeAttachments] SET [FileUrl] = '''' WHERE [FileUrl] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ALTER COLUMN [FileUrl] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[NoticeAttachments] ADD [FileUrl] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'UploadDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[NoticeAttachments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ADD [UploadDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[NoticeAttachments] SET [UploadDate] = ''1900-01-01'' WHERE [UploadDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[NoticeAttachments] ALTER COLUMN [UploadDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[NoticeAttachments] ADD [UploadDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'PaymentPlanId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlanInstallments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [PaymentPlanId] int NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlanInstallments] SET [PaymentPlanId] = 0 WHERE [PaymentPlanId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ALTER COLUMN [PaymentPlanId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [PaymentPlanId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'InstallmentNumber')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlanInstallments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [InstallmentNumber] int NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlanInstallments] SET [InstallmentNumber] = 0 WHERE [InstallmentNumber] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ALTER COLUMN [InstallmentNumber] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [InstallmentNumber] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'DueDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlanInstallments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [DueDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlanInstallments] SET [DueDate] = ''1900-01-01'' WHERE [DueDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ALTER COLUMN [DueDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [DueDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'Amount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlanInstallments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [Amount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlanInstallments] SET [Amount] = 0 WHERE [Amount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ALTER COLUMN [Amount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [Amount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'IsPaid')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlanInstallments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [IsPaid] bit NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlanInstallments] SET [IsPaid] = 0 WHERE [IsPaid] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlanInstallments] ALTER COLUMN [IsPaid] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [IsPaid] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'PaymentDate')
BEGIN
    ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [PaymentDate] datetime2(7) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'PaymentReference')
BEGIN
    ALTER TABLE [dbo].[PaymentPlanInstallments] ADD [PaymentReference] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[PaymentPlans] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'InspectionActId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [InspectionActId] int NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [InspectionActId] = 0 WHERE [InspectionActId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [InspectionActId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [InspectionActId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'PlanNumber')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [PlanNumber] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [PlanNumber] = '''' WHERE [PlanNumber] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [PlanNumber] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [PlanNumber] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'SignatureDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [SignatureDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [SignatureDate] = ''1900-01-01'' WHERE [SignatureDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [SignatureDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [SignatureDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'TotalDebt')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [TotalDebt] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [TotalDebt] = 0 WHERE [TotalDebt] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [TotalDebt] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [TotalDebt] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'DownPayment')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [DownPayment] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [DownPayment] = 0 WHERE [DownPayment] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [DownPayment] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [DownPayment] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'InstallmentsCount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [InstallmentsCount] int NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [InstallmentsCount] = 0 WHERE [InstallmentsCount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [InstallmentsCount] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [InstallmentsCount] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'InstallmentAmount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [InstallmentAmount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [InstallmentAmount] = 0 WHERE [InstallmentAmount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [InstallmentAmount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [InstallmentAmount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'FinancingInterestRate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [FinancingInterestRate] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [FinancingInterestRate] = 0 WHERE [FinancingInterestRate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [FinancingInterestRate] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [FinancingInterestRate] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'Status')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentPlans])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ADD [Status] int NULL');
        EXEC(N'UPDATE [dbo].[PaymentPlans] SET [Status] = 0 WHERE [Status] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentPlans] ALTER COLUMN [Status] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentPlans] ADD [Status] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'DeclarationId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentTransactions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ADD [DeclarationId] int NULL');
        EXEC(N'UPDATE [dbo].[PaymentTransactions] SET [DeclarationId] = 0 WHERE [DeclarationId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ALTER COLUMN [DeclarationId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentTransactions] ADD [DeclarationId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'Amount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentTransactions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ADD [Amount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[PaymentTransactions] SET [Amount] = 0 WHERE [Amount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ALTER COLUMN [Amount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentTransactions] ADD [Amount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'PaymentDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentTransactions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ADD [PaymentDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[PaymentTransactions] SET [PaymentDate] = ''1900-01-01'' WHERE [PaymentDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ALTER COLUMN [PaymentDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentTransactions] ADD [PaymentDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'ExternalReference')
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] ADD [ExternalReference] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'PaymentMethod')
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] ADD [PaymentMethod] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'Notes')
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] ADD [Notes] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'CreatedAt')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PaymentTransactions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ADD [CreatedAt] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[PaymentTransactions] SET [CreatedAt] = ''1900-01-01'' WHERE [CreatedAt] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PaymentTransactions] ALTER COLUMN [CreatedAt] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PaymentTransactions] ADD [CreatedAt] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[PrejudicialNotices] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'InspectionActId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PrejudicialNotices])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ADD [InspectionActId] int NULL');
        EXEC(N'UPDATE [dbo].[PrejudicialNotices] SET [InspectionActId] = 0 WHERE [InspectionActId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ALTER COLUMN [InspectionActId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PrejudicialNotices] ADD [InspectionActId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'NoticeNumber')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PrejudicialNotices])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ADD [NoticeNumber] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[PrejudicialNotices] SET [NoticeNumber] = '''' WHERE [NoticeNumber] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ALTER COLUMN [NoticeNumber] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PrejudicialNotices] ADD [NoticeNumber] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'IssueDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PrejudicialNotices])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ADD [IssueDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[PrejudicialNotices] SET [IssueDate] = ''1900-01-01'' WHERE [IssueDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ALTER COLUMN [IssueDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PrejudicialNotices] ADD [IssueDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'ExpirationDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PrejudicialNotices])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ADD [ExpirationDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[PrejudicialNotices] SET [ExpirationDate] = ''1900-01-01'' WHERE [ExpirationDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ALTER COLUMN [ExpirationDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PrejudicialNotices] ADD [ExpirationDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'WasNotified')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PrejudicialNotices])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ADD [WasNotified] bit NULL');
        EXEC(N'UPDATE [dbo].[PrejudicialNotices] SET [WasNotified] = 0 WHERE [WasNotified] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ALTER COLUMN [WasNotified] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PrejudicialNotices] ADD [WasNotified] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'Observations')
BEGIN
    ALTER TABLE [dbo].[PrejudicialNotices] ADD [Observations] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'IsJudicial')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[PrejudicialNotices])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ADD [IsJudicial] bit NULL');
        EXEC(N'UPDATE [dbo].[PrejudicialNotices] SET [IsJudicial] = 0 WHERE [IsJudicial] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[PrejudicialNotices] ALTER COLUMN [IsJudicial] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[PrejudicialNotices] ADD [IsJudicial] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Provinces]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Provinces]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Provinces] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Provinces]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Provinces]') AND name = N'Name')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Provinces])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Provinces] ADD [Name] nvarchar(100) NULL');
        EXEC(N'UPDATE [dbo].[Provinces] SET [Name] = '''' WHERE [Name] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Provinces] ALTER COLUMN [Name] nvarchar(100) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Provinces] ADD [Name] nvarchar(100) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'BasicAmount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[SalaryScales])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[SalaryScales] ADD [BasicAmount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[SalaryScales] SET [BasicAmount] = 0 WHERE [BasicAmount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[SalaryScales] ALTER COLUMN [BasicAmount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[SalaryScales] ADD [BasicAmount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'AgreementNR2')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[SalaryScales])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[SalaryScales] ADD [AgreementNR2] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[SalaryScales] SET [AgreementNR2] = 0 WHERE [AgreementNR2] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[SalaryScales] ALTER COLUMN [AgreementNR2] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[SalaryScales] ADD [AgreementNR2] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'Period')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[SalaryScales])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[SalaryScales] ADD [Period] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[SalaryScales] SET [Period] = ''1900-01-01'' WHERE [Period] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[SalaryScales] ALTER COLUMN [Period] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[SalaryScales] ADD [Period] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'AgreementNR1')
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD [AgreementNR1] decimal(18,2) NOT NULL DEFAULT ((0.0));
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'AgreementNR1')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SalaryScales]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'AgreementNR1'))
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD DEFAULT ((0.0)) FOR [AgreementNR1];
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'IsActive')
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD [IsActive] bit NOT NULL DEFAULT (CONVERT([bit],(0)));
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'IsActive')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SalaryScales]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'IsActive'))
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD DEFAULT (CONVERT([bit],(0))) FOR [IsActive];
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'UnionId')
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD [UnionId] int NOT NULL DEFAULT ((0));
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'UnionId')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SalaryScales]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'UnionId'))
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD DEFAULT ((0)) FOR [UnionId];
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'AntiguedadPercentage')
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD [AntiguedadPercentage] decimal(18,2) NOT NULL DEFAULT ((0.0));
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'AntiguedadPercentage')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SalaryScales]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'AntiguedadPercentage'))
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD DEFAULT ((0.0)) FOR [AntiguedadPercentage];
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'EmployeeCategoryId')
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD [EmployeeCategoryId] int NOT NULL DEFAULT ((0));
END;
GO
IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'EmployeeCategoryId')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[SalaryScales]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'EmployeeCategoryId'))
BEGIN
    ALTER TABLE [dbo].[SalaryScales] ADD DEFAULT ((0)) FOR [EmployeeCategoryId];
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[Unions] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'Name')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Unions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Unions] ADD [Name] nvarchar(200) NULL');
        EXEC(N'UPDATE [dbo].[Unions] SET [Name] = '''' WHERE [Name] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Unions] ALTER COLUMN [Name] nvarchar(200) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Unions] ADD [Name] nvarchar(200) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'JurisdictionProvince')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Unions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Unions] ADD [JurisdictionProvince] nvarchar(100) NULL');
        EXEC(N'UPDATE [dbo].[Unions] SET [JurisdictionProvince] = '''' WHERE [JurisdictionProvince] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Unions] ALTER COLUMN [JurisdictionProvince] nvarchar(100) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Unions] ADD [JurisdictionProvince] nvarchar(100) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'CctCode')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Unions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Unions] ADD [CctCode] nvarchar(50) NULL');
        EXEC(N'UPDATE [dbo].[Unions] SET [CctCode] = '''' WHERE [CctCode] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Unions] ALTER COLUMN [CctCode] nvarchar(50) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Unions] ADD [CctCode] nvarchar(50) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'IsActive')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[Unions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[Unions] ADD [IsActive] bit NULL');
        EXEC(N'UPDATE [dbo].[Unions] SET [IsActive] = 0 WHERE [IsActive] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[Unions] ALTER COLUMN [IsActive] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Unions] ADD [IsActive] bit NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'JurisdictionCity')
BEGIN
    ALTER TABLE [dbo].[Unions] ADD [JurisdictionCity] nvarchar(100) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'LawContributionPercentage')
BEGIN
    ALTER TABLE [dbo].[Unions] ADD [LawContributionPercentage] decimal(18,2) NOT NULL DEFAULT ((0.0));
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'LawContributionPercentage')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Unions]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'LawContributionPercentage'))
BEGIN
    ALTER TABLE [dbo].[Unions] ADD DEFAULT ((0.0)) FOR [LawContributionPercentage];
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'UnionMemberPercentage')
BEGIN
    ALTER TABLE [dbo].[Unions] ADD [UnionMemberPercentage] decimal(18,2) NOT NULL DEFAULT ((0.0));
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'UnionMemberPercentage')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Unions]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'UnionMemberPercentage'))
BEGIN
    ALTER TABLE [dbo].[Unions] ADD DEFAULT ((0.0)) FOR [UnionMemberPercentage];
END;
GO
IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'AdminEmail')
BEGIN
    ALTER TABLE [dbo].[Unions] ADD [AdminEmail] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'UnionSubscriptionId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionPayments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [UnionSubscriptionId] int NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionPayments] SET [UnionSubscriptionId] = 0 WHERE [UnionSubscriptionId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ALTER COLUMN [UnionSubscriptionId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [UnionSubscriptionId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'PaymentDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionPayments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [PaymentDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionPayments] SET [PaymentDate] = ''1900-01-01'' WHERE [PaymentDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ALTER COLUMN [PaymentDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [PaymentDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'Amount')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionPayments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [Amount] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionPayments] SET [Amount] = 0 WHERE [Amount] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ALTER COLUMN [Amount] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [Amount] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'PaymentMethod')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionPayments])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [PaymentMethod] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionPayments] SET [PaymentMethod] = '''' WHERE [PaymentMethod] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionPayments] ALTER COLUMN [PaymentMethod] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [PaymentMethod] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'ReceiptNumber')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [ReceiptNumber] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'Notes')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD [Notes] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'UnionName')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [UnionName] nvarchar(200) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionRequests] SET [UnionName] = '''' WHERE [UnionName] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ALTER COLUMN [UnionName] nvarchar(200) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [UnionName] nvarchar(200) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Cuit')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Cuit] nvarchar(20) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionRequests] SET [Cuit] = '''' WHERE [Cuit] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ALTER COLUMN [Cuit] nvarchar(20) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Cuit] nvarchar(20) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'ContactEmail')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [ContactEmail] nvarchar(max) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionRequests] SET [ContactEmail] = '''' WHERE [ContactEmail] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ALTER COLUMN [ContactEmail] nvarchar(max) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [ContactEmail] nvarchar(max) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'ContactPhone')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [ContactPhone] nvarchar(50) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'RequestDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [RequestDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionRequests] SET [RequestDate] = ''1900-01-01'' WHERE [RequestDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ALTER COLUMN [RequestDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [RequestDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Status')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptionRequests])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Status] int NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptionRequests] SET [Status] = 0 WHERE [Status] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptionRequests] ALTER COLUMN [Status] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Status] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'AdminNotes')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [AdminNotes] nvarchar(max) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'CoverageArea')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [CoverageArea] nvarchar(500) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Floor')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Floor] nvarchar(20) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Locality')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Locality] nvarchar(100) NOT NULL DEFAULT (N'');
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Locality')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Locality'))
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD DEFAULT (N'') FOR [Locality];
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Province')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Province] nvarchar(100) NOT NULL DEFAULT (N'');
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Province')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Province'))
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD DEFAULT (N'') FOR [Province];
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'ReferencePersonFullName')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [ReferencePersonFullName] nvarchar(200) NOT NULL DEFAULT (N'');
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'ReferencePersonFullName')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'ReferencePersonFullName'))
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD DEFAULT (N'') FOR [ReferencePersonFullName];
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Street')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [Street] nvarchar(200) NOT NULL DEFAULT (N'');
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Street')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'Street'))
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD DEFAULT (N'') FOR [Street];
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'StreetNumber')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [StreetNumber] nvarchar(20) NOT NULL DEFAULT (N'');
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'StreetNumber')
   AND NOT EXISTS (SELECT 1 FROM sys.default_constraints dc WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]')
       AND dc.parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'StreetNumber'))
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD DEFAULT (N'') FOR [StreetNumber];
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'WhatsApp')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD [WhatsApp] nvarchar(50) NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptions]') AND name = N'Id')
BEGIN
    ALTER TABLE [dbo].[UnionSubscriptions] ADD [Id] int IDENTITY(1,1) NOT NULL;
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptions]') AND name = N'UnionId')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ADD [UnionId] int NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptions] SET [UnionId] = 0 WHERE [UnionId] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ALTER COLUMN [UnionId] int NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptions] ADD [UnionId] int NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptions]') AND name = N'MonthlyFee')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ADD [MonthlyFee] decimal(18,2) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptions] SET [MonthlyFee] = 0 WHERE [MonthlyFee] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ALTER COLUMN [MonthlyFee] decimal(18,2) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptions] ADD [MonthlyFee] decimal(18,2) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptions]') AND name = N'NextBillingDate')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ADD [NextBillingDate] datetime2(7) NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptions] SET [NextBillingDate] = ''1900-01-01'' WHERE [NextBillingDate] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ALTER COLUMN [NextBillingDate] datetime2(7) NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptions] ADD [NextBillingDate] datetime2(7) NOT NULL;
    END
END;
GO
IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptions]') AND name = N'IsServiceActive')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[UnionSubscriptions])
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ADD [IsServiceActive] bit NULL');
        EXEC(N'UPDATE [dbo].[UnionSubscriptions] SET [IsServiceActive] = 0 WHERE [IsServiceActive] IS NULL');
        EXEC(N'ALTER TABLE [dbo].[UnionSubscriptions] ALTER COLUMN [IsServiceActive] bit NOT NULL');
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[UnionSubscriptions] ADD [IsServiceActive] bit NOT NULL;
    END
END;
GO
-- ============================================================
-- 2b) VALIDACION DE DUPLICADOS antes de crear indices unicos
--      (los duplicados harian fallar la creacion del indice unico)
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'RoleNameIndex')
BEGIN
    IF EXISTS (SELECT 1 FROM (SELECT [NormalizedName], COUNT(*) AS n FROM [dbo].[AspNetRoles] WHERE ([NormalizedName] IS NOT NULL) GROUP BY [NormalizedName] HAVING COUNT(*) > 1) d)
        THROW 52001, 'Datos duplicados: existen filas con el mismo valor en ([NormalizedName]) de [dbo].[AspNetRoles] (RoleNameIndex). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'UserNameIndex')
BEGIN
    IF EXISTS (SELECT 1 FROM (SELECT [NormalizedUserName], COUNT(*) AS n FROM [dbo].[AspNetUsers] WHERE ([NormalizedUserName] IS NOT NULL) GROUP BY [NormalizedUserName] HAVING COUNT(*) > 1) d)
        THROW 52002, 'Datos duplicados: existen filas con el mismo valor en ([NormalizedUserName]) de [dbo].[AspNetUsers] (UserNameIndex). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IX_Companies_Cuit_BranchName')
BEGIN
    IF EXISTS (SELECT 1 FROM (SELECT [Cuit], [BranchName], COUNT(*) AS n FROM [dbo].[Companies] WHERE ([BranchName] IS NOT NULL) GROUP BY [Cuit], [BranchName] HAVING COUNT(*) > 1) d)
        THROW 52003, 'Datos duplicados: existen filas con el mismo valor en ([Cuit], [BranchName]) de [dbo].[Companies] (IX_Companies_Cuit_BranchName). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IX_Companies_NormalizedCuit')
BEGIN
    IF EXISTS (SELECT 1 FROM (SELECT [NormalizedCuit], COUNT(*) AS n FROM [dbo].[Companies] WHERE ([ParentCompanyId] IS NULL) GROUP BY [NormalizedCuit] HAVING COUNT(*) > 1) d)
        THROW 52004, 'Datos duplicados: existen filas con el mismo valor en ([NormalizedCuit]) de [dbo].[Companies] (IX_Companies_NormalizedCuit). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'IX_Employees_Dni')
BEGIN
    IF EXISTS (SELECT 1 FROM (SELECT [Dni], COUNT(*) AS n FROM [dbo].[Employees] GROUP BY [Dni] HAVING COUNT(*) > 1) d)
        THROW 52005, 'Datos duplicados: existen filas con el mismo valor en ([Dni]) de [dbo].[Employees] (IX_Employees_Dni). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

-- ============================================================
-- 3) INDICES FALTANTES (incluye PK, unicos y filtrados)
IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'IX_AccountingStudios_CityId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_AccountingStudios_CityId] ON [dbo].[AccountingStudios]
(
	[CityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AccountingStudios]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AccountingStudios]') AND name = N'PK_AccountingStudios')
BEGIN
ALTER TABLE [dbo].[AccountingStudios] ADD  CONSTRAINT [PK_AccountingStudios] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND name = N'IX_AspNetRoleClaims_RoleId')
BEGIN
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoleClaims]') AND name = N'PK_AspNetRoleClaims')
BEGIN
ALTER TABLE [dbo].[AspNetRoleClaims] ADD  CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'PK_AspNetRoles')
BEGIN
SET ANSI_PADDING ON
ALTER TABLE [dbo].[AspNetRoles] ADD  CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'RoleNameIndex')
BEGIN
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]
(
	[NormalizedName] ASC
)
WHERE ([NormalizedName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND name = N'IX_AspNetUserClaims_UserId')
BEGIN
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserClaims]') AND name = N'PK_AspNetUserClaims')
BEGIN
ALTER TABLE [dbo].[AspNetUserClaims] ADD  CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND name = N'IX_AspNetUserLogins_UserId')
BEGIN
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserLogins]') AND name = N'PK_AspNetUserLogins')
BEGIN
SET ANSI_PADDING ON
ALTER TABLE [dbo].[AspNetUserLogins] ADD  CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND name = N'IX_AspNetUserRoles_RoleId')
BEGIN
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserRoles]') AND name = N'PK_AspNetUserRoles')
BEGIN
SET ANSI_PADDING ON
ALTER TABLE [dbo].[AspNetUserRoles] ADD  CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'EmailIndex')
BEGIN
SET ANSI_PADDING ON
CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'PK_AspNetUsers')
BEGIN
SET ANSI_PADDING ON
ALTER TABLE [dbo].[AspNetUsers] ADD  CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'UserNameIndex')
BEGIN
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUserTokens]') AND name = N'PK_AspNetUserTokens')
BEGIN
SET ANSI_PADDING ON
ALTER TABLE [dbo].[AspNetUserTokens] ADD  CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'IX_Assignments_CompanyId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Assignments_CompanyId] ON [dbo].[Assignments]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'IX_Assignments_EmployeeId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Assignments_EmployeeId] ON [dbo].[Assignments]
(
	[EmployeeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'IX_Assignments_UnionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Assignments_UnionId] ON [dbo].[Assignments]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Assignments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Assignments]') AND name = N'PK_Assignments')
BEGIN
ALTER TABLE [dbo].[Assignments] ADD  CONSTRAINT [PK_Assignments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'IX_AssociationRequests_AccountingStudioId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_AssociationRequests_AccountingStudioId] ON [dbo].[AssociationRequests]
(
	[AccountingStudioId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'IX_AssociationRequests_CompanyId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_AssociationRequests_CompanyId] ON [dbo].[AssociationRequests]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'IX_AssociationRequests_TargetUnionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_AssociationRequests_TargetUnionId] ON [dbo].[AssociationRequests]
(
	[TargetUnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AssociationRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AssociationRequests]') AND name = N'PK_AssociationRequests')
BEGIN
ALTER TABLE [dbo].[AssociationRequests] ADD  CONSTRAINT [PK_AssociationRequests] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND name = N'PK_AuditLogs')
BEGIN
ALTER TABLE [dbo].[AuditLogs] ADD  CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[BalanceHistories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[BalanceHistories]') AND name = N'PK_BalanceHistories')
BEGIN
ALTER TABLE [dbo].[BalanceHistories] ADD  CONSTRAINT [PK_BalanceHistories] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Cities]') AND name = N'IX_Cities_ProvinceId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Cities_ProvinceId] ON [dbo].[Cities]
(
	[ProvinceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Cities]') AND name = N'PK_Cities')
BEGIN
ALTER TABLE [dbo].[Cities] ADD  CONSTRAINT [PK_Cities] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IX_Companies_AccountingStudioId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Companies_AccountingStudioId] ON [dbo].[Companies]
(
	[AccountingStudioId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IX_Companies_CityId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Companies_CityId] ON [dbo].[Companies]
(
	[CityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IX_Companies_Cuit_BranchName')
BEGIN
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [IX_Companies_Cuit_BranchName] ON [dbo].[Companies]
(
	[Cuit] ASC,
	[BranchName] ASC
)
WHERE ([BranchName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IX_Companies_NormalizedCuit')
BEGIN
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
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'IX_Companies_ParentCompanyId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Companies_ParentCompanyId] ON [dbo].[Companies]
(
	[ParentCompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Companies]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Companies]') AND name = N'PK_Companies')
BEGIN
ALTER TABLE [dbo].[Companies] ADD  CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[CompanyBalances]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CompanyBalances]') AND name = N'IX_CompanyBalances_CompanyId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_CompanyBalances_CompanyId] ON [dbo].[CompanyBalances]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[CompanyBalances]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CompanyBalances]') AND name = N'PK_CompanyBalances')
BEGIN
ALTER TABLE [dbo].[CompanyBalances] ADD  CONSTRAINT [PK_CompanyBalances] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'IX_DeclarationDetails_AssignmentId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_DeclarationDetails_AssignmentId] ON [dbo].[DeclarationDetails]
(
	[AssignmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'IX_DeclarationDetails_RegistryId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_DeclarationDetails_RegistryId] ON [dbo].[DeclarationDetails]
(
	[RegistryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[DeclarationDetails]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[DeclarationDetails]') AND name = N'PK_DeclarationDetails')
BEGIN
ALTER TABLE [dbo].[DeclarationDetails] ADD  CONSTRAINT [PK_DeclarationDetails] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'IX_Declarations_CompanyId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Declarations_CompanyId] ON [dbo].[Declarations]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'IX_Declarations_UnionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_Declarations_UnionId] ON [dbo].[Declarations]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Declarations]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Declarations]') AND name = N'PK_Declarations')
BEGIN
ALTER TABLE [dbo].[Declarations] ADD  CONSTRAINT [PK_Declarations] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCategories]') AND name = N'IX_EmployeeCategories_UnionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_EmployeeCategories_UnionId] ON [dbo].[EmployeeCategories]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[EmployeeCategories]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCategories]') AND name = N'PK_EmployeeCategories')
BEGIN
ALTER TABLE [dbo].[EmployeeCategories] ADD  CONSTRAINT [PK_EmployeeCategories] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'IX_Employees_Dni')
BEGIN
SET ANSI_PADDING ON
CREATE UNIQUE NONCLUSTERED INDEX [IX_Employees_Dni] ON [dbo].[Employees]
(
	[Dni] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = N'PK_Employees')
BEGIN
ALTER TABLE [dbo].[Employees] ADD  CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'IX_InspectionActPeriods_InspectionActId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_InspectionActPeriods_InspectionActId] ON [dbo].[InspectionActPeriods]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionActPeriods]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActPeriods]') AND name = N'PK_InspectionActPeriods')
BEGIN
ALTER TABLE [dbo].[InspectionActPeriods] ADD  CONSTRAINT [PK_InspectionActPeriods] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'IX_InspectionActs_CompanyId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_InspectionActs_CompanyId] ON [dbo].[InspectionActs]
(
	[CompanyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'IX_InspectionActs_UnionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_InspectionActs_UnionId] ON [dbo].[InspectionActs]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionActs]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionActs]') AND name = N'PK_InspectionActs')
BEGIN
ALTER TABLE [dbo].[InspectionActs] ADD  CONSTRAINT [PK_InspectionActs] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'IX_InspectionVisits_InspectionActId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_InspectionVisits_InspectionActId] ON [dbo].[InspectionVisits]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'IX_InspectionVisits_InspectorId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_InspectionVisits_InspectorId] ON [dbo].[InspectionVisits]
(
	[InspectorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InspectionVisits]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InspectionVisits]') AND name = N'PK_InspectionVisits')
BEGIN
ALTER TABLE [dbo].[InspectionVisits] ADD  CONSTRAINT [PK_InspectionVisits] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Inspectors]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Inspectors]') AND name = N'PK_Inspectors')
BEGIN
ALTER TABLE [dbo].[Inspectors] ADD  CONSTRAINT [PK_Inspectors] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[InterestRates]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[InterestRates]') AND name = N'PK_InterestRates')
BEGIN
ALTER TABLE [dbo].[InterestRates] ADD  CONSTRAINT [PK_InterestRates] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'IX_NoticeAttachments_PrejudicialNoticeId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_NoticeAttachments_PrejudicialNoticeId] ON [dbo].[NoticeAttachments]
(
	[PrejudicialNoticeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[NoticeAttachments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[NoticeAttachments]') AND name = N'PK_NoticeAttachments')
BEGIN
ALTER TABLE [dbo].[NoticeAttachments] ADD  CONSTRAINT [PK_NoticeAttachments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'IX_PaymentPlanInstallments_PaymentPlanId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_PaymentPlanInstallments_PaymentPlanId] ON [dbo].[PaymentPlanInstallments]
(
	[PaymentPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PaymentPlanInstallments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlanInstallments]') AND name = N'PK_PaymentPlanInstallments')
BEGIN
ALTER TABLE [dbo].[PaymentPlanInstallments] ADD  CONSTRAINT [PK_PaymentPlanInstallments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'IX_PaymentPlans_InspectionActId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_PaymentPlans_InspectionActId] ON [dbo].[PaymentPlans]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PaymentPlans]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PaymentPlans]') AND name = N'PK_PaymentPlans')
BEGIN
ALTER TABLE [dbo].[PaymentPlans] ADD  CONSTRAINT [PK_PaymentPlans] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'IX_PaymentTransactions_DeclarationId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_DeclarationId] ON [dbo].[PaymentTransactions]
(
	[DeclarationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = N'PK_PaymentTransactions')
BEGIN
ALTER TABLE [dbo].[PaymentTransactions] ADD  CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'IX_PrejudicialNotices_InspectionActId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_PrejudicialNotices_InspectionActId] ON [dbo].[PrejudicialNotices]
(
	[InspectionActId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[PrejudicialNotices]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PrejudicialNotices]') AND name = N'PK_PrejudicialNotices')
BEGIN
ALTER TABLE [dbo].[PrejudicialNotices] ADD  CONSTRAINT [PK_PrejudicialNotices] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Provinces]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Provinces]') AND name = N'PK_Provinces')
BEGIN
ALTER TABLE [dbo].[Provinces] ADD  CONSTRAINT [PK_Provinces] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'IX_SalaryScales_EmployeeCategoryId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_SalaryScales_EmployeeCategoryId] ON [dbo].[SalaryScales]
(
	[EmployeeCategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'IX_SalaryScales_UnionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_SalaryScales_UnionId] ON [dbo].[SalaryScales]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[SalaryScales]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalaryScales]') AND name = N'PK_SalaryScales')
BEGIN
ALTER TABLE [dbo].[SalaryScales] ADD  CONSTRAINT [PK_SalaryScales] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[Unions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Unions]') AND name = N'PK_Unions')
BEGIN
ALTER TABLE [dbo].[Unions] ADD  CONSTRAINT [PK_Unions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'IX_UnionSubscriptionPayments_UnionSubscriptionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_UnionSubscriptionPayments_UnionSubscriptionId] ON [dbo].[UnionSubscriptionPayments]
(
	[UnionSubscriptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionPayments]') AND name = N'PK_UnionSubscriptionPayments')
BEGIN
ALTER TABLE [dbo].[UnionSubscriptionPayments] ADD  CONSTRAINT [PK_UnionSubscriptionPayments] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptionRequests]') AND name = N'PK_UnionSubscriptionRequests')
BEGIN
ALTER TABLE [dbo].[UnionSubscriptionRequests] ADD  CONSTRAINT [PK_UnionSubscriptionRequests] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptions]') AND name = N'IX_UnionSubscriptions_UnionId')
BEGIN
CREATE NONCLUSTERED INDEX [IX_UnionSubscriptions_UnionId] ON [dbo].[UnionSubscriptions]
(
	[UnionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

IF OBJECT_ID(N'[dbo].[UnionSubscriptions]', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UnionSubscriptions]') AND name = N'PK_UnionSubscriptions')
BEGIN
ALTER TABLE [dbo].[UnionSubscriptions] ADD  CONSTRAINT [PK_UnionSubscriptions] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END;
GO

-- ============================================================
-- 4) CHECKS FALTANTES
-- ============================================================
-- 4b) VALIDACION DE HUERFANOS antes de crear claves foraneas
--      (filas sin relacion harian fallar la creacion de la FK)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AccountingStudios_Cities_CityId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AccountingStudios] c
        LEFT JOIN [dbo].[Cities] p
            ON p.[Id] = c.[CityId]
        WHERE c.[CityId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53001, 'Datos huerfanos: [dbo].[AccountingStudios] tiene filas sin relacion en [dbo].[Cities] (FK FK_AccountingStudios_Cities_CityId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetRoleClaims_AspNetRoles_RoleId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AspNetRoleClaims] c
        LEFT JOIN [dbo].[AspNetRoles] p
            ON p.[Id] = c.[RoleId]
        WHERE c.[RoleId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53002, 'Datos huerfanos: [dbo].[AspNetRoleClaims] tiene filas sin relacion en [dbo].[AspNetRoles] (FK FK_AspNetRoleClaims_AspNetRoles_RoleId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserClaims_AspNetUsers_UserId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AspNetUserClaims] c
        LEFT JOIN [dbo].[AspNetUsers] p
            ON p.[Id] = c.[UserId]
        WHERE c.[UserId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53003, 'Datos huerfanos: [dbo].[AspNetUserClaims] tiene filas sin relacion en [dbo].[AspNetUsers] (FK FK_AspNetUserClaims_AspNetUsers_UserId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserLogins_AspNetUsers_UserId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AspNetUserLogins] c
        LEFT JOIN [dbo].[AspNetUsers] p
            ON p.[Id] = c.[UserId]
        WHERE c.[UserId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53004, 'Datos huerfanos: [dbo].[AspNetUserLogins] tiene filas sin relacion en [dbo].[AspNetUsers] (FK FK_AspNetUserLogins_AspNetUsers_UserId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetRoles_RoleId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AspNetUserRoles] c
        LEFT JOIN [dbo].[AspNetRoles] p
            ON p.[Id] = c.[RoleId]
        WHERE c.[RoleId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53005, 'Datos huerfanos: [dbo].[AspNetUserRoles] tiene filas sin relacion en [dbo].[AspNetRoles] (FK FK_AspNetUserRoles_AspNetRoles_RoleId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetUsers_UserId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AspNetUserRoles] c
        LEFT JOIN [dbo].[AspNetUsers] p
            ON p.[Id] = c.[UserId]
        WHERE c.[UserId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53006, 'Datos huerfanos: [dbo].[AspNetUserRoles] tiene filas sin relacion en [dbo].[AspNetUsers] (FK FK_AspNetUserRoles_AspNetUsers_UserId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserTokens_AspNetUsers_UserId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AspNetUserTokens] c
        LEFT JOIN [dbo].[AspNetUsers] p
            ON p.[Id] = c.[UserId]
        WHERE c.[UserId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53007, 'Datos huerfanos: [dbo].[AspNetUserTokens] tiene filas sin relacion en [dbo].[AspNetUsers] (FK FK_AspNetUserTokens_AspNetUsers_UserId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Assignments_Companies_CompanyId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Assignments] c
        LEFT JOIN [dbo].[Companies] p
            ON p.[Id] = c.[CompanyId]
        WHERE c.[CompanyId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53008, 'Datos huerfanos: [dbo].[Assignments] tiene filas sin relacion en [dbo].[Companies] (FK FK_Assignments_Companies_CompanyId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Assignments_Employees_EmployeeId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Assignments] c
        LEFT JOIN [dbo].[Employees] p
            ON p.[Id] = c.[EmployeeId]
        WHERE c.[EmployeeId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53009, 'Datos huerfanos: [dbo].[Assignments] tiene filas sin relacion en [dbo].[Employees] (FK FK_Assignments_Employees_EmployeeId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Assignments_Unions_UnionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Assignments] c
        LEFT JOIN [dbo].[Unions] p
            ON p.[Id] = c.[UnionId]
        WHERE c.[UnionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53010, 'Datos huerfanos: [dbo].[Assignments] tiene filas sin relacion en [dbo].[Unions] (FK FK_Assignments_Unions_UnionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssociationRequests_AccountingStudios_AccountingStudioId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AssociationRequests] c
        LEFT JOIN [dbo].[AccountingStudios] p
            ON p.[Id] = c.[AccountingStudioId]
        WHERE c.[AccountingStudioId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53011, 'Datos huerfanos: [dbo].[AssociationRequests] tiene filas sin relacion en [dbo].[AccountingStudios] (FK FK_AssociationRequests_AccountingStudios_AccountingStudioId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssociationRequests_Companies_CompanyId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AssociationRequests] c
        LEFT JOIN [dbo].[Companies] p
            ON p.[Id] = c.[CompanyId]
        WHERE c.[CompanyId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53012, 'Datos huerfanos: [dbo].[AssociationRequests] tiene filas sin relacion en [dbo].[Companies] (FK FK_AssociationRequests_Companies_CompanyId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssociationRequests_Unions_TargetUnionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[AssociationRequests] c
        LEFT JOIN [dbo].[Unions] p
            ON p.[Id] = c.[TargetUnionId]
        WHERE c.[TargetUnionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53013, 'Datos huerfanos: [dbo].[AssociationRequests] tiene filas sin relacion en [dbo].[Unions] (FK FK_AssociationRequests_Unions_TargetUnionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Cities_Provinces_ProvinceId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Cities] c
        LEFT JOIN [dbo].[Provinces] p
            ON p.[Id] = c.[ProvinceId]
        WHERE c.[ProvinceId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53014, 'Datos huerfanos: [dbo].[Cities] tiene filas sin relacion en [dbo].[Provinces] (FK FK_Cities_Provinces_ProvinceId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Companies_AccountingStudios_AccountingStudioId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Companies] c
        LEFT JOIN [dbo].[AccountingStudios] p
            ON p.[Id] = c.[AccountingStudioId]
        WHERE c.[AccountingStudioId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53015, 'Datos huerfanos: [dbo].[Companies] tiene filas sin relacion en [dbo].[AccountingStudios] (FK FK_Companies_AccountingStudios_AccountingStudioId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Companies_Cities_CityId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Companies] c
        LEFT JOIN [dbo].[Cities] p
            ON p.[Id] = c.[CityId]
        WHERE c.[CityId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53016, 'Datos huerfanos: [dbo].[Companies] tiene filas sin relacion en [dbo].[Cities] (FK FK_Companies_Cities_CityId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Companies_Companies_ParentCompanyId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Companies] c
        LEFT JOIN [dbo].[Companies] p
            ON p.[Id] = c.[ParentCompanyId]
        WHERE c.[ParentCompanyId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53017, 'Datos huerfanos: [dbo].[Companies] tiene filas sin relacion en [dbo].[Companies] (FK FK_Companies_Companies_ParentCompanyId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CompanyBalances_Companies_CompanyId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[CompanyBalances] c
        LEFT JOIN [dbo].[Companies] p
            ON p.[Id] = c.[CompanyId]
        WHERE c.[CompanyId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53018, 'Datos huerfanos: [dbo].[CompanyBalances] tiene filas sin relacion en [dbo].[Companies] (FK FK_CompanyBalances_Companies_CompanyId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DeclarationDetails_Assignments_AssignmentId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[DeclarationDetails] c
        LEFT JOIN [dbo].[Assignments] p
            ON p.[Id] = c.[AssignmentId]
        WHERE c.[AssignmentId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53019, 'Datos huerfanos: [dbo].[DeclarationDetails] tiene filas sin relacion en [dbo].[Assignments] (FK FK_DeclarationDetails_Assignments_AssignmentId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DeclarationDetails_Declarations_RegistryId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[DeclarationDetails] c
        LEFT JOIN [dbo].[Declarations] p
            ON p.[Id] = c.[RegistryId]
        WHERE c.[RegistryId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53020, 'Datos huerfanos: [dbo].[DeclarationDetails] tiene filas sin relacion en [dbo].[Declarations] (FK FK_DeclarationDetails_Declarations_RegistryId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Declarations_Companies_CompanyId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Declarations] c
        LEFT JOIN [dbo].[Companies] p
            ON p.[Id] = c.[CompanyId]
        WHERE c.[CompanyId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53021, 'Datos huerfanos: [dbo].[Declarations] tiene filas sin relacion en [dbo].[Companies] (FK FK_Declarations_Companies_CompanyId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Declarations_Unions_UnionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[Declarations] c
        LEFT JOIN [dbo].[Unions] p
            ON p.[Id] = c.[UnionId]
        WHERE c.[UnionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53022, 'Datos huerfanos: [dbo].[Declarations] tiene filas sin relacion en [dbo].[Unions] (FK FK_Declarations_Unions_UnionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EmployeeCategories_Unions_UnionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[EmployeeCategories] c
        LEFT JOIN [dbo].[Unions] p
            ON p.[Id] = c.[UnionId]
        WHERE c.[UnionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53023, 'Datos huerfanos: [dbo].[EmployeeCategories] tiene filas sin relacion en [dbo].[Unions] (FK FK_EmployeeCategories_Unions_UnionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionActPeriods_InspectionActs_InspectionActId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[InspectionActPeriods] c
        LEFT JOIN [dbo].[InspectionActs] p
            ON p.[Id] = c.[InspectionActId]
        WHERE c.[InspectionActId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53024, 'Datos huerfanos: [dbo].[InspectionActPeriods] tiene filas sin relacion en [dbo].[InspectionActs] (FK FK_InspectionActPeriods_InspectionActs_InspectionActId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionActs_Companies_CompanyId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[InspectionActs] c
        LEFT JOIN [dbo].[Companies] p
            ON p.[Id] = c.[CompanyId]
        WHERE c.[CompanyId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53025, 'Datos huerfanos: [dbo].[InspectionActs] tiene filas sin relacion en [dbo].[Companies] (FK FK_InspectionActs_Companies_CompanyId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionActs_Unions_UnionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[InspectionActs] c
        LEFT JOIN [dbo].[Unions] p
            ON p.[Id] = c.[UnionId]
        WHERE c.[UnionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53026, 'Datos huerfanos: [dbo].[InspectionActs] tiene filas sin relacion en [dbo].[Unions] (FK FK_InspectionActs_Unions_UnionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionVisits_InspectionActs_InspectionActId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[InspectionVisits] c
        LEFT JOIN [dbo].[InspectionActs] p
            ON p.[Id] = c.[InspectionActId]
        WHERE c.[InspectionActId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53027, 'Datos huerfanos: [dbo].[InspectionVisits] tiene filas sin relacion en [dbo].[InspectionActs] (FK FK_InspectionVisits_InspectionActs_InspectionActId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InspectionVisits_Inspectors_InspectorId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[InspectionVisits] c
        LEFT JOIN [dbo].[Inspectors] p
            ON p.[Id] = c.[InspectorId]
        WHERE c.[InspectorId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53028, 'Datos huerfanos: [dbo].[InspectionVisits] tiene filas sin relacion en [dbo].[Inspectors] (FK FK_InspectionVisits_Inspectors_InspectorId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_NoticeAttachments_PrejudicialNotices_PrejudicialNoticeId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[NoticeAttachments] c
        LEFT JOIN [dbo].[PrejudicialNotices] p
            ON p.[Id] = c.[PrejudicialNoticeId]
        WHERE c.[PrejudicialNoticeId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53029, 'Datos huerfanos: [dbo].[NoticeAttachments] tiene filas sin relacion en [dbo].[PrejudicialNotices] (FK FK_NoticeAttachments_PrejudicialNotices_PrejudicialNoticeId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[PaymentPlanInstallments] c
        LEFT JOIN [dbo].[PaymentPlans] p
            ON p.[Id] = c.[PaymentPlanId]
        WHERE c.[PaymentPlanId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53030, 'Datos huerfanos: [dbo].[PaymentPlanInstallments] tiene filas sin relacion en [dbo].[PaymentPlans] (FK FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentPlans_InspectionActs_InspectionActId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[PaymentPlans] c
        LEFT JOIN [dbo].[InspectionActs] p
            ON p.[Id] = c.[InspectionActId]
        WHERE c.[InspectionActId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53031, 'Datos huerfanos: [dbo].[PaymentPlans] tiene filas sin relacion en [dbo].[InspectionActs] (FK FK_PaymentPlans_InspectionActs_InspectionActId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentTransactions_Declarations_DeclarationId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[PaymentTransactions] c
        LEFT JOIN [dbo].[Declarations] p
            ON p.[Id] = c.[DeclarationId]
        WHERE c.[DeclarationId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53032, 'Datos huerfanos: [dbo].[PaymentTransactions] tiene filas sin relacion en [dbo].[Declarations] (FK FK_PaymentTransactions_Declarations_DeclarationId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PrejudicialNotices_InspectionActs_InspectionActId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[PrejudicialNotices] c
        LEFT JOIN [dbo].[InspectionActs] p
            ON p.[Id] = c.[InspectionActId]
        WHERE c.[InspectionActId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53033, 'Datos huerfanos: [dbo].[PrejudicialNotices] tiene filas sin relacion en [dbo].[InspectionActs] (FK FK_PrejudicialNotices_InspectionActs_InspectionActId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalaryScales_EmployeeCategories_EmployeeCategoryId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[SalaryScales] c
        LEFT JOIN [dbo].[EmployeeCategories] p
            ON p.[Id] = c.[EmployeeCategoryId]
        WHERE c.[EmployeeCategoryId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53034, 'Datos huerfanos: [dbo].[SalaryScales] tiene filas sin relacion en [dbo].[EmployeeCategories] (FK FK_SalaryScales_EmployeeCategories_EmployeeCategoryId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalaryScales_Unions_UnionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[SalaryScales] c
        LEFT JOIN [dbo].[Unions] p
            ON p.[Id] = c.[UnionId]
        WHERE c.[UnionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53035, 'Datos huerfanos: [dbo].[SalaryScales] tiene filas sin relacion en [dbo].[Unions] (FK FK_SalaryScales_Unions_UnionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UnionSubscriptionPayments_UnionSubscriptions_UnionSubscriptionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[UnionSubscriptionPayments] c
        LEFT JOIN [dbo].[UnionSubscriptions] p
            ON p.[Id] = c.[UnionSubscriptionId]
        WHERE c.[UnionSubscriptionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53036, 'Datos huerfanos: [dbo].[UnionSubscriptionPayments] tiene filas sin relacion en [dbo].[UnionSubscriptions] (FK FK_UnionSubscriptionPayments_UnionSubscriptions_UnionSubscriptionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UnionSubscriptions_Unions_UnionId')
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[UnionSubscriptions] c
        LEFT JOIN [dbo].[Unions] p
            ON p.[Id] = c.[UnionId]
        WHERE c.[UnionId] IS NOT NULL AND p.[Id] IS NULL
    )
        THROW 53037, 'Datos huerfanos: [dbo].[UnionSubscriptions] tiene filas sin relacion en [dbo].[Unions] (FK FK_UnionSubscriptions_Unions_UnionId). Revisar y corregir los datos antes de continuar.', 1;
END;
GO

-- ============================================================
-- 5) CLAVES FORANEAS FALTANTES
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

