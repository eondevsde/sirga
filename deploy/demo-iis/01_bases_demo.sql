/* ==========================================================================================
   CREAR LAS BASES DE LA DEMO EN EL SERVIDOR DE PRODUCCIÓN
   ------------------------------------------------------------------------------------------
   Crea SIRGA_DB_DEMO y SINDICATO_DEMO a partir de los respaldos del 2026-11-05.
   Son copias del sistema real: se ven como producción, pero no tocan ninguna base real.

   QUÉ HACE (en orden)
     1. CONTROLES PREVIOS: que existan los .bak, que haya espacio en disco, que la instancia no
        pase el límite de 10 GB por base (SQL Express), y si las bases ya existen.
     2. RESTAURA las dos bases con MOVE a la carpeta de datos de la instancia, en SIMPLE.
     3. Deja las bases sanas: AUTO_CLOSE y AUTO_SHRINK apagados (evitan fragmentación).
     4. PERMISOS: crea los usuarios de las tres identidades de App Pool de IIS y les da db_owner.
     5. VERIFICA los datos y muestra el resumen con los números esperados.

   QUÉ NECESITÁS
     - Los dos respaldos copiados al servidor en @CarpetaBak:
           SIRGA_DB_DEMO.bak     (61 MB)
           SINDICATO_DEMO.bak    (655 MB)
     - Espacio libre en la carpeta de datos: ~3,5 GB.
     - Permiso para crear bases (sysadmin o dbcreator).

   SEGURIDAD
     - Arranca en @Simulacro = 1: no restaura nada, sólo informa.
     - Si una base ya existe NO la toca, salvo @Recrear = 1.
     - No toca ninguna base de producción: los nombres terminan en _DEMO.

   USO
     sqlcmd -S <instancia> -E -b -I -i 01_bases_demo.sql
     (o abrirlo en SSMS y ejecutarlo)
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

/* ============================== PARÁMETROS — EDITAR ACÁ ================================= */
DECLARE @Simulacro      bit = 1;               -- 1 = sólo informa | 0 = crea las bases
DECLARE @Recrear        bit = 0;               -- 1 = si la base DEMO existe, la borra y rehace
DECLARE @CarpetaBak     nvarchar(400) = N'C:\Demo\bak';    -- donde están los .bak en el servidor
DECLARE @CarpetaDatos   nvarchar(400) = NULL;  -- NULL = carpeta de datos por defecto de la instancia
DECLARE @ConcederAppPools bit = 1;             -- 1 = da permiso a las identidades de App Pool de IIS
/* ======================================================================================== */

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se crea ni se toca nada'
                                   ELSE '  MODO REAL: se crean las bases de la DEMO' END;
PRINT '  instancia    : ' + @@SERVERNAME;
PRINT '  respaldos en : ' + @CarpetaBak;
PRINT '==============================================================';

/* ======================= 1. CONTROLES PREVIOS ========================================== */
PRINT '';
PRINT '--- 1. CONTROLES PREVIOS';

PRINT '  edición: ' + CAST(SERVERPROPERTY('Edition') AS nvarchar(100));
PRINT '  versión: ' + CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(50));

IF @CarpetaDatos IS NULL
    SELECT @CarpetaDatos = CONVERT(nvarchar(400), SERVERPROPERTY('InstanceDefaultDataPath'));
IF @CarpetaDatos IS NULL
    SELECT @CarpetaDatos = CONVERT(nvarchar(400), SERVERPROPERTY('InstanceDefaultLogPath'));
IF RIGHT(@CarpetaDatos, 1) <> '\' SET @CarpetaDatos = @CarpetaDatos + '\';
PRINT '  carpeta de datos: ' + @CarpetaDatos;

-- Columna 3 = la base que se CREA (siempre _DEMO).
-- Columnas 4 y 5 = los nombres lógicos que tienen los archivos ADENTRO del .bak: son los que pide
--                   el MOVE. No son bases que se toquen; es sólo cómo se llaman los archivos
--                   dentro del respaldo (el .bak de la demo se copió de una base que se llamaba así).
DECLARE @trabajos TABLE (Orden int, Archivo nvarchar(200), Destino sysname, LogicoDatos sysname, LogicoLog sysname, MbAprox int);
INSERT INTO @trabajos VALUES
    (1, N'SIRGA_DB_DEMO.bak',  N'SIRGA_DB_DEMO',  N'SIRGA_DB',  N'SIRGA_DB_log', 400),
    (2, N'SINDICATO_DEMO.bak', N'SINDICATO_DEMO', N'sindicato', N'sindicato_log', 2600);

-- GUARDA: este script es SÓLO para la demo. Si algún nombre de destino no termina en _DEMO, se corta.
IF EXISTS (SELECT 1 FROM @trabajos WHERE Destino NOT LIKE '%[_]DEMO')
BEGIN
    PRINT '  ERROR: hay un destino que no es _DEMO. Este script es sólo para la demo.';
    RETURN;
END

DECLARE @archivo nvarchar(200), @destino sysname, @logicoDatos sysname, @logicoLog sysname, @mbAprox int;
DECLARE @ruta nvarchar(500), @existe int, @sql nvarchar(max), @faltan bit = 0;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Archivo, Destino, LogicoDatos, LogicoLog, MbAprox FROM @trabajos ORDER BY Orden;
OPEN c;
FETCH NEXT FROM c INTO @archivo, @destino, @logicoDatos, @logicoLog, @mbAprox;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ruta = @CarpetaBak + '\' + @archivo;
    SET @existe = 0;
    BEGIN TRY
        EXEC master.dbo.xp_fileexist @ruta, @existe OUTPUT;
    END TRY
    BEGIN CATCH
        SET @existe = 1;
    END CATCH

    IF @existe = 0
    BEGIN
        PRINT '  FALTA el respaldo: ' + @ruta;
        SET @faltan = 1;
    END
    ELSE
        PRINT '  OK respaldo: ' + @archivo + '  (se van a necesitar ~' + CAST(CAST(@mbAprox / 1024.0 AS decimal(10,1)) AS varchar(10)) + ' GB de datos)';

    IF DB_ID(@destino) IS NOT NULL
        PRINT '  la base [' + @destino + '] YA EXISTE' + CASE WHEN @Recrear = 1 THEN ' (se va a rehacer)' ELSE ' (no se toca)' END;

    FETCH NEXT FROM c INTO @archivo, @destino, @logicoDatos, @logicoLog, @mbAprox;
END
CLOSE c;
DEALLOCATE c;

IF @faltan = 1
BEGIN
    PRINT '';
    PRINT '  No se puede continuar: faltan respaldos en ' + @CarpetaBak;
    PRINT '  Copiarlos y volver a ejecutar.';
    RETURN;
END

DECLARE @mbLibre decimal(18,1);
SELECT @mbLibre = CAST(available_bytes / 1048576.0 AS decimal(18,1)) FROM sys.dm_os_volume_stats(2, 1);
IF @mbLibre IS NULL
    PRINT '  espacio libre: (no se pudo consultar; se asume suficiente)';
ELSE
BEGIN
    PRINT '  espacio libre en el disco de datos: ' + CONVERT(varchar(20), @mbLibre) + ' MB';
    IF @mbLibre < 4096
    BEGIN
        PRINT '  AVISO: quedan menos de 4 GB. La demo necesita ~3,5 GB.';
    END
END

/* ======================= 2 y 3. RESTAURAR Y DEJAR SANAS ================================ */
PRINT '';
PRINT '--- 2. RESTAURACIÓN';

DECLARE @restauradas int = 0;

DECLARE c2 CURSOR LOCAL FAST_FORWARD FOR SELECT Archivo, Destino, LogicoDatos, LogicoLog FROM @trabajos ORDER BY Orden;
OPEN c2;
FETCH NEXT FROM c2 INTO @archivo, @destino, @logicoDatos, @logicoLog;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ruta = @CarpetaBak + '\' + @archivo;
    PRINT '';
    PRINT '  [' + @destino + ']';

    IF DB_ID(@destino) IS NOT NULL AND @Recrear = 0
    BEGIN
        PRINT '     ya existe: no se toca (para rehacerla, @Recrear = 1).';
    END
    ELSE IF @Simulacro = 1
    BEGIN
        PRINT '     (simulacro) RESTORE DATABASE [' + @destino + '] FROM ''' + @ruta + '''';
        PRINT '     (simulacro) MOVE ' + @logicoDatos + ' -> ' + @CarpetaDatos + @destino + '.mdf';
        PRINT '     (simulacro) MOVE ' + @logicoLog + ' -> ' + @CarpetaDatos + @destino + '_log.ldf';
        PRINT '     (simulacro) ALTER DATABASE ... SET RECOVERY SIMPLE / AUTO_CLOSE OFF / AUTO_SHRINK OFF';
    END
    ELSE
    BEGIN
        BEGIN TRY
            IF DB_ID(@destino) IS NOT NULL
            BEGIN
                SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;'
                         + N' DROP DATABASE ' + QUOTENAME(@destino) + N';';
                EXEC sp_executesql @sql;
                PRINT '     base anterior borrada.';
            END

            SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@destino) + N' FROM DISK = ' + QUOTENAME(@ruta, '''')
                     + N' WITH MOVE ' + QUOTENAME(@logicoDatos, '''') + N' TO ' + QUOTENAME(@CarpetaDatos + @destino + N'.mdf', '''')
                     + N', MOVE ' + QUOTENAME(@logicoLog, '''') + N' TO ' + QUOTENAME(@CarpetaDatos + @destino + N'_log.ldf', '''')
                     + N', RECOVERY, STATS = 20;';
            EXEC sp_executesql @sql;

            SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET RECOVERY SIMPLE WITH NO_WAIT;'
                     + N' ALTER DATABASE ' + QUOTENAME(@destino) + N' SET AUTO_CLOSE OFF WITH NO_WAIT;'
                     + N' ALTER DATABASE ' + QUOTENAME(@destino) + N' SET AUTO_SHRINK OFF WITH NO_WAIT;';
            EXEC sp_executesql @sql;

            PRINT '     restaurada y configurada (SIMPLE, sin auto-close ni auto-shrink).';
            SET @restauradas = @restauradas + 1;
        END TRY
        BEGIN CATCH
            PRINT '     ERROR: ' + ERROR_MESSAGE();
        END CATCH
    END

    FETCH NEXT FROM c2 INTO @archivo, @destino, @logicoDatos, @logicoLog;
END
CLOSE c2;
DEALLOCATE c2;

/* ======================= 4. PERMISOS DE LAS APP POOLS ================================== */
IF @ConcederAppPools = 1
BEGIN
    PRINT '';
    PRINT '--- 3. PERMISOS PARA IIS (identidades de App Pool)';

    -- EDITAR SI LOS APP POOLS SE LLAMAN DISTINTO EN IIS.
    -- El nombre tiene que ser exactamente "IIS APPPOOL\" + el nombre del App Pool.
    DECLARE @pools TABLE (Nombre nvarchar(128));
    INSERT INTO @pools VALUES
        (N'IIS APPPOOL\SIRGA_DEMO'),
        (N'IIS APPPOOL\SIGESWEB_DEMO'),
        (N'IIS APPPOOL\SIGESWEB_DEMO_CARNET');

    DECLARE @pool nvarchar(128), @base sysname, @n int;

    DECLARE cp CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @pools;
    OPEN cp;
    FETCH NEXT FROM cp INTO @pool;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @n = COUNT(*) FROM sys.server_principals WHERE name = @pool;
        IF @n = 0
        BEGIN
            IF @Simulacro = 1
                PRINT '     (simulacro) CREATE LOGIN [' + @pool + '] FROM WINDOWS';
            ELSE
            BEGIN
                BEGIN TRY
                    SET @sql = N'CREATE LOGIN ' + QUOTENAME(@pool) + N' FROM WINDOWS;';
                    EXEC sp_executesql @sql;
                    PRINT '     login creado: ' + @pool;
                END TRY
                BEGIN CATCH
                    PRINT '     AVISO con ' + @pool + ': ' + ERROR_MESSAGE();
                    PRINT '           (el App Pool tiene que existir en IIS antes de correr esto)';
                END CATCH
            END
        END
        ELSE
            PRINT '     login ya existe: ' + @pool;

        FETCH NEXT FROM cp INTO @pool;
    END
    CLOSE cp;
    DEALLOCATE cp;

    DECLARE cb CURSOR LOCAL FAST_FORWARD FOR SELECT Destino FROM @trabajos ORDER BY Orden;
    OPEN cb;
    FETCH NEXT FROM cb INTO @base;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF DB_ID(@base) IS NOT NULL
        BEGIN
            DECLARE cp2 CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @pools;
            OPEN cp2;
            FETCH NEXT FROM cp2 INTO @pool;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                IF @Simulacro = 1
                    PRINT '     (simulacro) ' + @pool + ' -> db_owner en ' + @base;
                ELSE
                BEGIN
                    BEGIN TRY
                        SET @sql = N'USE ' + QUOTENAME(@base) + N';
                            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N' + QUOTENAME(@pool, '''') + N')
                                CREATE USER ' + QUOTENAME(@pool) + N' FOR LOGIN ' + QUOTENAME(@pool) + N';
                            IF IS_ROLEMEMBER(''db_owner'', N' + QUOTENAME(@pool, '''') + N') <> 1
                                ALTER ROLE db_owner ADD MEMBER ' + QUOTENAME(@pool) + N';';
                        EXEC sp_executesql @sql;
                    END TRY
                    BEGIN CATCH
                        PRINT '     AVISO en ' + @base + ' con ' + @pool + ': ' + ERROR_MESSAGE();
                    END CATCH
                END
                FETCH NEXT FROM cp2 INTO @pool;
            END
            CLOSE cp2;
            DEALLOCATE cp2;
        END
        FETCH NEXT FROM cb INTO @base;
    END
    CLOSE cb;
    DEALLOCATE cb;
END

/* ======================= 5. VERIFICACIÓN =============================================== */
PRINT '';
PRINT '--- 4. VERIFICACIÓN';

SELECT CAST(DB_NAME(m.database_id) AS varchar(20)) AS base,
       CAST(m.type_desc AS varchar(5)) AS tipo,
       CAST(m.size * 8.0 / 1024 AS decimal(18,1)) AS mb,
       CAST(d.recovery_model_desc AS varchar(10)) AS modelo
FROM sys.master_files m
JOIN sys.databases d ON d.database_id = m.database_id
WHERE d.name IN ('SIRGA_DB_DEMO', 'SINDICATO_DEMO')
ORDER BY base, tipo DESC;

IF DB_ID('SIRGA_DB_DEMO') IS NOT NULL
BEGIN
    SET @sql = N'SELECT ''SIRGA_DB_DEMO'' AS base,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Declarations) AS ddjj,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.DeclarationDetails) AS renglones,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Companies) AS empresas,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Employees) AS empleados,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.AccountingStudios) AS estudios,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.AspNetUsers) AS usuarios,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.__EFMigrationsHistory) AS migraciones,
                        (SELECT COUNT(*) FROM SIRGA_DB_DEMO.sys.tables) AS tablas;';
    EXEC sp_executesql @sql;
END
ELSE
    PRINT '  SIRGA_DB_DEMO no existe todavía.';

IF DB_ID('SINDICATO_DEMO') IS NOT NULL
BEGIN
    SET @sql = N'SELECT ''SINDICATO_DEMO'' AS base,
                        (SELECT COUNT(*) FROM SINDICATO_DEMO.fotos_.maesoc) AS socios,
                        (SELECT COUNT(*) FROM SINDICATO_DEMO.fotos_.socemp) AS pares,
                        (SELECT COUNT(*) FROM SINDICATO_DEMO.dbo.ddjjt) AS ddjjt,
                        (SELECT COUNT(*) FROM SINDICATO_DEMO.dbo.ddjj) AS renglones,
                        (SELECT COUNT(*) FROM SINDICATO_DEMO.sys.tables) AS tablas,
                        (SELECT COUNT(*) FROM SINDICATO_DEMO.sys.tables t
                           JOIN SINDICATO_DEMO.sys.schemas s ON s.schema_id = t.schema_id
                          WHERE s.name = ''carnet'') AS tablas_carnet;';
    EXEC sp_executesql @sql;
END
ELSE
    PRINT '  SINDICATO_DEMO no existe todavía.';

PRINT '';
PRINT '  ESPERADO:';
PRINT '    SIRGA_DB_DEMO    -> 252.165 ddjj | 1.293.502 renglones | 4.543 empresas | 28.160 empleados';
PRINT '                       | 876 estudios | 910 usuarios | 57 migraciones | 59 tablas';
PRINT '    SINDICATO_DEMO   -> 28.149 socios | 36.353 pares | 263.615 ddjjt | 1.359.606 renglones';

IF @Simulacro = 1
BEGIN
    PRINT '';
    PRINT '  Fue un SIMULACRO. Para crear las bases de verdad: @Simulacro = 0 y volver a ejecutar.';
END
ELSE
BEGIN
    PRINT '';
    PRINT '  Bases listas. Siguiente paso: instalar los sitios (03_instalar_iis.ps1) y verificar (04_verificar.ps1).';
END

PRINT '==============================================================';
