/* ==========================================================================================
   01 — CREA LAS DOS BASES DE LA DEMO EN EL SERVIDOR
   ------------------------------------------------------------------------------------------
   Restaura SIRGA_DB_DEMO y SINDICATO_DEMO desde los respaldos del 2026-11-05.
   Son copias del sistema real: los datos se ven como producción, pero no tocan nada real.

   QUÉ NECESITÁS
     - Los dos archivos .bak copiados al servidor, en la carpeta de @CarpetaBak:
           SIRGA_DB_DEMO.bak     (61 MB)
           SINDICATO_DEMO.bak    (655 MB)
     - Permiso para crear bases (sysadmin o dbcreator).

   SEGURIDAD
     - Arranca en @Simulacro = 1: sólo informa.
     - Si la base ya existe NO la toca, salvo @Recrear = 1.
     - El límite de SQL Express es 10 GB POR BASE; estas copias ocupan ~61 MB y ~655 MB.

   USO
     sqlcmd -S <servidor> -E -b -I -i 01_bases_demo.sql
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

/* ---------------------------------- PARÁMETROS ------------------------------------------ */
DECLARE @Simulacro     bit = 1;                          -- 1 = informa | 0 = restaura
DECLARE @Recrear       bit = 0;                          -- 1 = borra la base demo si ya existe
DECLARE @CarpetaBak    nvarchar(400) = N'C:\Demo\bak';   -- donde están los .bak en el servidor
DECLARE @CarpetaDatos  nvarchar(400) = NULL;             -- NULL = carpeta por defecto de la instancia
DECLARE @LoginApp      nvarchar(128) = NULL;             -- opcional: login SQL que va a usar la app
/* --------------------------------------------------------------------------------------- */

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se restaura nada'
                                   ELSE '  MODO REAL: se crean las bases de la DEMO' END;
PRINT '  respaldos en: ' + @CarpetaBak;
PRINT '==============================================================';

IF @CarpetaDatos IS NULL
    SELECT @CarpetaDatos = CONVERT(nvarchar(400), SERVERPROPERTY('InstanceDefaultDataPath'));
IF RIGHT(@CarpetaDatos, 1) <> '\' SET @CarpetaDatos = @CarpetaDatos + '\';

DECLARE @trabajos TABLE (Orden int, Archivo nvarchar(200), Destino sysname, LogicoDatos sysname, LogicoLog sysname);
INSERT INTO @trabajos VALUES
    (1, N'SIRGA_DB_DEMO.bak',   N'SIRGA_DB_DEMO',   N'SIRGA_DB',  N'SIRGA_DB_log'),
    (2, N'SINDICATO_DEMO.bak',  N'SINDICATO_DEMO',  N'sindicato', N'sindicato_log');

DECLARE @orden int, @archivo nvarchar(200), @destino sysname, @logicoDatos sysname, @logicoLog sysname;
DECLARE @ruta nvarchar(500), @sql nvarchar(max), @existe int;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Orden, Archivo, Destino, LogicoDatos, LogicoLog FROM @trabajos ORDER BY Orden;
OPEN c;
FETCH NEXT FROM c INTO @orden, @archivo, @destino, @logicoDatos, @logicoLog;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ruta = @CarpetaBak + '\' + @archivo;
    PRINT '';
    PRINT '--- [' + @destino + '] desde ' + @archivo;

    SET @existe = 0;
    EXEC master.dbo.xp_fileexist @ruta, @existe OUTPUT;
    IF @existe = 0
    BEGIN
        PRINT '    ERROR: no existe ' + @ruta;
    END
    ELSE IF DB_ID(@destino) IS NOT NULL AND @Recrear = 0
    BEGIN
        PRINT '    Ya existe la base [' + @destino + ']: no se toca (para rehacerla, @Recrear = 1).';
    END
    ELSE
    BEGIN

        IF @Simulacro = 1
        BEGIN
            PRINT '    (simulacro) RESTORE DATABASE ' + @destino + ' FROM ' + @ruta;
            PRINT '    (simulacro) MOVE ' + @logicoDatos + ' -> ' + @CarpetaDatos + @destino + '.mdf';
            PRINT '    (simulacro) MOVE ' + @logicoLog   + ' -> ' + @CarpetaDatos + @destino + '_log.ldf';
        END
        ELSE
        BEGIN
            IF DB_ID(@destino) IS NOT NULL
            BEGIN
                SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'
                         + N' DROP DATABASE ' + QUOTENAME(@destino) + N';';
                EXEC sp_executesql @sql;
                PRINT '    base anterior borrada.';
            END

            SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@destino) + N' FROM DISK = ' + QUOTENAME(@ruta, '''')
                     + N' WITH MOVE ' + QUOTENAME(@logicoDatos, '''') + N' TO ' + QUOTENAME(@CarpetaDatos + @destino + N'.mdf', '''')
                     + N', MOVE ' + QUOTENAME(@logicoLog, '''') + N' TO ' + QUOTENAME(@CarpetaDatos + @destino + N'_log.ldf', '''')
                     + N', RECOVERY, STATS = 25;';
            EXEC sp_executesql @sql;

            SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET RECOVERY SIMPLE WITH NO_WAIT;';
            EXEC sp_executesql @sql;
            PRINT '    restaurada en SIMPLE.';
        END
    END

    FETCH NEXT FROM c INTO @orden, @archivo, @destino, @logicoDatos, @logicoLog;
END
CLOSE c;
DEALLOCATE c;

/* --------------------------- permiso para el login de la app --------------------------- */
IF @LoginApp IS NOT NULL AND @Simulacro = 0
BEGIN
    PRINT '';
    PRINT '--- permisos para el login [' + @LoginApp + ']';
    DECLARE @b sysname;
    DECLARE cb CURSOR LOCAL FAST_FORWARD FOR SELECT Destino FROM @trabajos;
    OPEN cb;
    FETCH NEXT FROM cb INTO @b;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF DB_ID(@b) IS NOT NULL
        BEGIN
            SET @sql = N'USE ' + QUOTENAME(@b) + N';
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N' + QUOTENAME(@LoginApp, '''') + N')
                BEGIN
                    CREATE USER ' + QUOTENAME(@LoginApp) + N' FOR LOGIN ' + QUOTENAME(@LoginApp) + N';
                    ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@LoginApp) + N';
                    ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@LoginApp) + N';
                    ALTER ROLE db_ddladmin   ADD MEMBER ' + QUOTENAME(@LoginApp) + N';
                END';
            BEGIN TRY
                EXEC sp_executesql @sql;
                PRINT '    usuario ' + @LoginApp + ' con permisos en ' + @b;
            END TRY
            BEGIN CATCH
                PRINT '    AVISO en ' + @b + ': ' + ERROR_MESSAGE();
            END CATCH
        END
        FETCH NEXT FROM cb INTO @b;
    END
    CLOSE cb;
    DEALLOCATE cb;
END

/* ------------------------------- verificación ------------------------------------------ */
PRINT '';
PRINT '=== VERIFICACIÓN ===';
SELECT CAST(DB_NAME(database_id) AS varchar(20)) AS base,
       CAST(type_desc AS varchar(5)) AS tipo,
       CAST(size * 8.0 / 1024 AS decimal(18,1)) AS mb
FROM sys.master_files
WHERE DB_NAME(database_id) IN ('SIRGA_DB_DEMO', 'SINDICATO_DEMO')
ORDER BY base, tipo DESC;

IF DB_ID('SIRGA_DB_DEMO') IS NOT NULL
BEGIN
    SET @sql = N'SELECT ''SIRGA_DB_DEMO'' AS base, (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Declarations) AS ddjj,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Companies) AS empresas,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Employees) AS empleados;';
    EXEC sp_executesql @sql;
END

IF DB_ID('SINDICATO_DEMO') IS NOT NULL
BEGIN
    SET @sql = N'SELECT ''SINDICATO_DEMO'' AS base, (SELECT COUNT(*) FROM SINDICATO_DEMO.fotos_.maesoc) AS socios,
                        (SELECT COUNT(*) FROM SINDICATO_DEMO.dbo.ddjjt) AS ddjjt;';
    EXEC sp_executesql @sql;
END

PRINT 'Esperado: SIRGA_DB_DEMO con 252.165 DDJJ, 4.543 empresas, 28.160 empleados.';
PRINT '          SINDICATO_DEMO con 28.149 socios y 263.615 ddjjt.';
PRINT 'Siguiente paso: 02_permisos.sql';
PRINT '==============================================================';
