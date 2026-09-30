/* =====================================================================
   SIRGA - Script de SEED (datos base) para PRODUCCION
   Generado: 2026-08-18  (valores tomados de la base de desarrollo)

   Uso: EJECUTAR DESPUES del script de instalacion/upgrade de esquema,
        la primera vez que se despliega produccion.

   Que hace:
       - Crea los ROLES de la aplicacion (Admin, Sindicato,
         EstudioContable, Empresa, Auditor).
       - Crea las CUENTAS BOOTSTRAP para poder ingresar por primera vez:
            admin@sirga.com      -> rol Admin      (administracion general)
            sindicato@sirga.com  -> rol Sindicato  (operacion sindical)
            auditor@sirga.com    -> rol Auditor    (auditoria/reportes)
         La password inicial de estas cuentas es:  12345
         (es la misma usada en desarrollo). CAMBIARLA apenas se ingrese
         por primera vez.

   IDEMPOTENTE: puede ejecutarse multiples veces; solo inserta lo que
        falte (verifica por email / nombre de rol / relacion).

   PERMISOS: requiere rol db_owner/sysadmin sobre SIRGA_DB.

   Ejecucion:
       sqlcmd -S servidor -E -d SIRGA_DB -b -i script_sirga_db_seed_produccion.sql

   Nota: los demas usuarios (estudios contables, empresas, empleados, etc.)
        se crean desde la aplicacion o con el ETL SIRGA.Migration.
   ===================================================================== */

SET NOCOUNT ON;
GO

USE [SIRGA_DB];
GO

-- Indices filtrados (Identity) exigen estas opciones SET
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- ============================================================
-- 1) ROLES
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'Admin')
    INSERT INTO [dbo].[AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp])
    VALUES ('4dce94ad-0e5e-40a7-ab15-0269b3c9e720', N'Admin', N'ADMIN', NEWID());
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'Sindicato')
    INSERT INTO [dbo].[AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp])
    VALUES ('f2c4fc61-9586-4e6a-bfb5-b0572c0992d1', N'Sindicato', N'SINDICATO', NEWID());
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'EstudioContable')
    INSERT INTO [dbo].[AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp])
    VALUES ('9a7b35f3-f4ba-49e9-b0b9-86c6a8dd0f0f', N'EstudioContable', N'ESTUDIOCONTABLE', NEWID());
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'Empresa')
    INSERT INTO [dbo].[AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp])
    VALUES ('035cbacb-fac3-4ec7-a0c1-1d4864c3ad66', N'Empresa', N'EMPRESA', NEWID());
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = N'Auditor')
    INSERT INTO [dbo].[AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp])
    VALUES ('b3df2d90-c50c-4c8f-a1fb-cef97681491a', N'Auditor', N'AUDITOR', NEWID());
GO

-- ============================================================
-- 2) USUARIOS BOOTSTRAP  (password inicial: 12345)
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUsers] WHERE [UserName] = N'admin@sirga.com')
    INSERT INTO [dbo].[AspNetUsers]
        ([Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
         [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumber],[PhoneNumberConfirmed],
         [TwoFactorEnabled],[LockoutEnd],[LockoutEnabled],[AccessFailedCount])
    VALUES
        ('65f6aebc-4024-4c82-a8c8-c7bc7b295a2f', N'admin@sirga.com', N'ADMIN@SIRGA.COM', N'admin@sirga.com', N'ADMIN@SIRGA.COM', 1,
         N'AQAAAAIAAYagAAAAEDpq6NnrbEj4vqVh9S6sqgl+G2noTe9Yi+L3hDYlAM7zvCJOGe+FmTxAaRbTGVMeZw==',
         N'b04a7787-8b3c-4ba1-ab30-347e3a20c701', N'423ec676-44c9-45fa-a478-5ad65c84b388',
         NULL, 0, 0, NULL, 1, 0);
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUsers] WHERE [UserName] = N'sindicato@sirga.com')
    INSERT INTO [dbo].[AspNetUsers]
        ([Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
         [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumber],[PhoneNumberConfirmed],
         [TwoFactorEnabled],[LockoutEnd],[LockoutEnabled],[AccessFailedCount])
    VALUES
        ('eadbcfed-f1b5-45c2-a731-60ead0019ad2', N'sindicato@sirga.com', N'SINDICATO@SIRGA.COM', N'sindicato@sirga.com', N'SINDICATO@SIRGA.COM', 1,
         N'AQAAAAIAAYagAAAAECGDEvyUT8esUpkufNO0CsL3hRZhQtWpfT8+1J3DfR83Y/+F1aKTNLf8HJl91p7R2A==',
         N'fb59d967-2bbd-46f6-b7d0-61f9f9e85d1e', N'75729f3c-75f4-4009-b7a6-00fcbfb24164',
         NULL, 0, 0, NULL, 1, 0);
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUsers] WHERE [UserName] = N'auditor@sirga.com')
    INSERT INTO [dbo].[AspNetUsers]
        ([Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
         [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumber],[PhoneNumberConfirmed],
         [TwoFactorEnabled],[LockoutEnd],[LockoutEnabled],[AccessFailedCount])
    VALUES
        ('0f6b2c2a-4bff-4082-a493-b7b9f3503c25', N'auditor@sirga.com', N'AUDITOR@SIRGA.COM', N'auditor@sirga.com', N'AUDITOR@SIRGA.COM', 1,
         N'AQAAAAIAAYagAAAAECCU1M12+x1o/ByS4XhlK7dBuy9OcDoc+9OujqkdP5zhusVUua4x/xvh/18Ew0nhig==',
         N'73e47073-95b4-439d-8f58-a1021520438f', N'6826d18c-fd33-40c4-b358-529ab167324b',
         NULL, 0, 0, NULL, 1, 0);
GO

-- ============================================================
-- 3) RELACION USUARIO <-> ROL
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUserRoles] WHERE [UserId] = '65f6aebc-4024-4c82-a8c8-c7bc7b295a2f' AND [RoleId] = '4dce94ad-0e5e-40a7-ab15-0269b3c9e720')
    INSERT INTO [dbo].[AspNetUserRoles] ([UserId],[RoleId]) VALUES ('65f6aebc-4024-4c82-a8c8-c7bc7b295a2f', '4dce94ad-0e5e-40a7-ab15-0269b3c9e720');
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUserRoles] WHERE [UserId] = 'eadbcfed-f1b5-45c2-a731-60ead0019ad2' AND [RoleId] = 'f2c4fc61-9586-4e6a-bfb5-b0572c0992d1')
    INSERT INTO [dbo].[AspNetUserRoles] ([UserId],[RoleId]) VALUES ('eadbcfed-f1b5-45c2-a731-60ead0019ad2', 'f2c4fc61-9586-4e6a-bfb5-b0572c0992d1');
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUserRoles] WHERE [UserId] = '0f6b2c2a-4bff-4082-a493-b7b9f3503c25' AND [RoleId] = 'b3df2d90-c50c-4c8f-a1fb-cef97681491a')
    INSERT INTO [dbo].[AspNetUserRoles] ([UserId],[RoleId]) VALUES ('0f6b2c2a-4bff-4082-a493-b7b9f3503c25', 'b3df2d90-c50c-4c8f-a1fb-cef97681491a');
GO

PRINT 'Seed de identidad OK. Usuarios iniciales: admin@sirga.com / sindicato@sirga.com / auditor@sirga.com (password 12345 - CAMBIAR)';
GO
