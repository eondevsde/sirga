/* ==========================================================================================
   01 — CREA LA BASE DE DATOS DE PRODUCCIÓN (SIRGA_DB)
   ------------------------------------------------------------------------------------------
   Crea la base si no existe, con tamaño y crecimiento explícitos y en modelo SIMPLE
   (decisión del 2026-11-04: sin backups de log programados, el FULL sólo infla el log).

   DESPUÉS de correr esto, ejecutar en este orden:
     02 — script_sirga_db_produccion_esquema.sql   (crea todas las tablas)
     03 — script_sirga_db_produccion_verificar.sql (comprueba que quedó todo)

   SEGURIDAD
     - Arranca con @Simulacro = 1: informa y no crea nada.
     - NO borra ni toca una base existente: si ya existe, avisa y no hace nada.

   USO
     sqlcmd -S <servidor> -E -b -i script_sirga_db_produccion_01_crear_base.sql
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ---------------------------------- PARÁMETROS ------------------------------------------ */
DECLARE @Simulacro      bit = 1;              -- 1 = sólo informa | 0 = crea
DECLARE @Base           sysname = N'SIRGA_DB';
DECLARE @CarpetaDatos   nvarchar(400) = NULL; -- NULL = carpeta por defecto de la instancia
DECLARE @TamanoDatosMb  int = 256;            -- tamaño inicial del .mdf
DECLARE @TamanoLogMb    int = 256;            -- tamaño inicial del .ldf
DECLARE @CrecimientoMb  int = 256;            -- cuánto crece de golpe (evita miles de archivos)
DECLARE @Recovery       nvarchar(10) = N'SIMPLE';
DECLARE @LoginApp       nvarchar(128) = NULL; -- opcional: login SQL que usará la aplicación
/* --------------------------------------------------------------------------------------- */

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se crea nada'
                                   ELSE '  MODO REAL: se crea la base' END;
PRINT '  base: ' + @Base + ' | modelo: ' + @Recovery;
PRINT '==============================================================';

DECLARE @dir nvarchar(400), @rutaDatos nvarchar(500), @rutaLog nvarchar(500);
DECLARE @sql nvarchar(max), @mbLibre decimal(18,1);

-- Carpeta de datos: la indicada o la de la instancia
IF @CarpetaDatos IS NULL
  SELECT @dir = CONVERT(nvarchar(400), SERVERPROPERTY('InstanceDefaultDataPath'));
ELSE
  SET @dir = @CarpetaDatos;

IF RIGHT(@dir, 1) <> '\' SET @dir = @dir + '\';

SET @rutaDatos = @dir + @Base + N'.mdf';
SET @rutaLog   = @dir + @Base + N'_log.ldf';

PRINT '  archivo de datos: ' + @rutaDatos;
PRINT '  archivo de log:   ' + @rutaLog;

-- Espacio libre en la carpeta destino
BEGIN TRY
  SELECT @mbLibre = CAST(available_bytes / 1048576.0 AS decimal(18,1))
  FROM sys.dm_os_volume_stats(DB_ID(), 1);
  PRINT '  espacio libre en el volumen: ' + CONVERT(varchar(20), @mbLibre) + ' MB';
END TRY
BEGIN CATCH END CATCH

/* ------------------------------- ¿ya existe la base? ----------------------------------- */
IF DB_ID(@Base) IS NOT NULL
BEGIN
  PRINT '';
  PRINT '  La base [' + @Base + '] YA existe. No se toca.';
  SELECT CAST(d.name AS varchar(20)) AS base, CAST(d.recovery_model_desc AS varchar(10)) AS modelo,
         CAST(SUM(mf.size) * 8.0 / 1024 AS decimal(18,1)) AS total_mb
  FROM sys.databases d JOIN sys.master_files mf ON mf.database_id = d.database_id
  WHERE d.name = @Base GROUP BY d.name, d.recovery_model_desc;
  PRINT '  Si lo que querés es crear las TABLAS, corré el script 02.';
  RETURN;
END

/* ------------------------------- crearlo ---------------------------------------------- */
SET @sql =
    N'CREATE DATABASE ' + QUOTENAME(@Base) + N'
      ON PRIMARY (
        NAME = N''' + @Base + N''',
        FILENAME = N''' + @rutaDatos + N''',
        SIZE = ' + CAST(@TamanoDatosMb AS nvarchar(10)) + N'MB,
        FILEGROWTH = ' + CAST(@CrecimientoMb AS nvarchar(10)) + N'MB
      )
      LOG ON (
        NAME = N''' + @Base + N'_log'',
        FILENAME = N''' + @rutaLog + N''',
        SIZE = ' + CAST(@TamanoLogMb AS nvarchar(10)) + N'MB,
        FILEGROWTH = ' + CAST(@CrecimientoMb AS nvarchar(10)) + N'MB
      );';

IF @Simulacro = 1
BEGIN
  PRINT '';
  PRINT '  (simulacro) se ejecutaría:';
  PRINT @sql;
  PRINT '  (simulacro) ALTER DATABASE [' + @Base + '] SET RECOVERY ' + @Recovery + ';';
END
ELSE
BEGIN
  PRINT '';
  PRINT '  creando la base ...';
  EXEC sp_executesql @sql;

  SET @sql = N'ALTER DATABASE ' + QUOTENAME(@Base) + N' SET RECOVERY ' + @Recovery + N' WITH NO_WAIT;';
  EXEC sp_executesql @sql;

  PRINT '  base creada en modelo ' + @Recovery + '.';

  -- Login SQL opcional (la app puede usar autenticación de Windows o un login SQL)
  IF @LoginApp IS NOT NULL
  BEGIN
    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginApp)
    BEGIN
      SET @sql = N'USE ' + QUOTENAME(@Base) + N';
                   IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = ''' + @LoginApp + N''')
                     CREATE USER ' + QUOTENAME(@LoginApp) + N' FOR LOGIN ' + QUOTENAME(@LoginApp) + N';
                   ALTER ROLE db_owner ADD MEMBER ' + QUOTENAME(@LoginApp) + N';';
      EXEC sp_executesql @sql;
      PRINT '  login SQL [' + @LoginApp + '] habilitado como db_owner en la base.';
    END
    ELSE
      PRINT '  AVISO: el login SQL [' + @LoginApp + '] no existe en el servidor; no se creó el usuario.';
  END
END

/* ------------------------------- verificación ----------------------------------------- */
PRINT '';
PRINT '=== RESULTADO ===';
IF DB_ID(@Base) IS NOT NULL
BEGIN
  SELECT CAST(d.name AS varchar(20)) AS base, CAST(d.recovery_model_desc AS varchar(10)) AS modelo,
         CAST(d.state_desc AS varchar(10)) AS estado,
         CAST(mf.size * 8.0 / 1024 AS decimal(18,1)) AS mb, mf.physical_name
  FROM sys.databases d JOIN sys.master_files mf ON mf.database_id = d.database_id
  WHERE d.name = @Base ORDER BY mf.type_desc DESC;
  PRINT 'Siguiente paso: 02 — script_sirga_db_produccion_esquema.sql';
END
ELSE
  PRINT 'La base [' + @Base + '] no existe todavía (modo simulacro).';
PRINT '==============================================================';
