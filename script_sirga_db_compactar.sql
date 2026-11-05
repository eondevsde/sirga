/* ==========================================================================================
   COMPACTAR UNA BASE DE PRODUCCIÓN (devuelve el espacio vacío del archivo)
   ------------------------------------------------------------------------------------------
   QUÉ PROBLEMA RESUELVE
     El archivo de datos crece y nunca se achica solo. En la copia del sindicato el archivo
     ocupaba 9,0 GB pero los datos reales eran 1,9 GB: el 83% era espacio vacío, y eso hace
     parecer que SQL Express está en el límite (10 GB POR BASE) cuando no lo está.
     Compactar devuelve ese espacio al disco.

   QUÉ HACE
     1. Informa cuánto ocupa el archivo, cuánto son datos reales y cuánto queda libre.
     2. Con @Simulacro = 0: hace CHECKPOINT y DBCC SHRINKDATABASE (deja 10% libre).
     3. Verifica y muestra el tamaño final.

   SEGURIDAD
     - Arranca con @Simulacro = 1: NO toca nada.
     - Antes de compactar en producción: **respaldo completo** y **ventana sin uso**.
     - Compactar no pierde datos: sólo mueve las páginas y devuelve el espacio sobrante.

   USO
     sqlcmd -S <servidor> -E -b -I -i script_sirga_db_compactar.sql
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

/* ---------------------------------- PARÁMETROS ------------------------------------------ */
DECLARE @Simulacro       bit = 1;                  -- 1 = sólo informa | 0 = compacta
DECLARE @Base            sysname = N'Sindicato';   -- base a compactar
DECLARE @PorcentajeLibre int = 10;                 -- % de espacio libre que se deja
/* --------------------------------------------------------------------------------------- */

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se compacta nada'
                                   ELSE '  MODO REAL: se va a compactar la base' END;
PRINT '  base: [' + @Base + '] | se deja ' + CAST(@PorcentajeLibre AS varchar(3)) + '% libre';
PRINT '==============================================================';

IF DB_ID(@Base) IS NULL
BEGIN
    PRINT '  ERROR: la base [' + @Base + '] no existe en esta instancia.';
    RETURN;
END

DECLARE @sql nvarchar(max), @logicoLog sysname, @mbDatos decimal(18,1), @mbLog decimal(18,1);
DECLARE @mbUsado decimal(18,1), @mbLibreDisco decimal(18,1);

SELECT @mbDatos = SUM(CASE WHEN type_desc = 'ROWS' THEN size ELSE 0 END) * 8.0 / 1024,
       @mbLog   = SUM(CASE WHEN type_desc = 'LOG'  THEN size ELSE 0 END) * 8.0 / 1024
FROM sys.master_files WHERE database_id = DB_ID(@Base);

SELECT @logicoLog = name FROM sys.master_files
WHERE database_id = DB_ID(@Base) AND type_desc = 'LOG';

SELECT @mbLibreDisco = CAST(available_bytes / 1048576.0 AS decimal(18,1))
FROM sys.dm_os_volume_stats(DB_ID(@Base), 1);

SET @sql = N'SELECT @mb = CAST(SUM(used_page_count) * 8.0 / 1024 AS decimal(18,1))
             FROM ' + QUOTENAME(@Base) + N'.sys.dm_db_partition_stats;';
EXEC sp_executesql @sql, N'@mb decimal(18,1) OUTPUT', @mbUsado OUTPUT;

PRINT '  archivo de datos : ' + CONVERT(varchar(20), @mbDatos) + ' MB';
PRINT '  datos reales     : ' + CONVERT(varchar(20), @mbUsado) + ' MB';
PRINT '  espacio vacio    : ' + CONVERT(varchar(20), @mbDatos - @mbUsado) + ' MB';
PRINT '  archivo de log   : ' + CONVERT(varchar(20), @mbLog) + ' MB';
PRINT '  libre en el disco: ' + CONVERT(varchar(20), @mbLibreDisco) + ' MB';

IF (@mbDatos - @mbUsado) < 100
BEGIN
    PRINT '  No hay espacio vacío suficiente para que valga la pena (menos de 100 MB).';
    RETURN;
END

IF @Simulacro = 1
BEGIN
    PRINT '';
    PRINT '  (simulacro) CHECKPOINT + DBCC SHRINKDATABASE (' + @Base + ', ' +
          CAST(@PorcentajeLibre AS varchar(3)) + ')';
    PRINT '  (simulacro) quedaría en ~' + CONVERT(varchar(20), @mbUsado * (1 + @PorcentajeLibre / 100.0)) + ' MB';
    PRINT '  Para compactar de verdad: @Simulacro = 0 (con respaldo previo y sin uso de la base).';
    RETURN;
END

PRINT '';
PRINT '  haciendo CHECKPOINT ...';
SET @sql = N'USE ' + QUOTENAME(@Base) + N'; CHECKPOINT;';
EXEC sp_executesql @sql;

PRINT '  compactando los datos ...';
BEGIN TRY
    SET @sql = N'DBCC SHRINKDATABASE (' + QUOTENAME(@Base, '''') + N', ' +
               CAST(@PorcentajeLibre AS varchar(3)) + N') WITH NO_INFOMSGS;';
    EXEC sp_executesql @sql;
    PRINT '  compactado.';
END TRY
BEGIN CATCH
    PRINT '  AVISO al compactar: ' + ERROR_MESSAGE();
END CATCH

-- El log también se puede dejar prolijo (mismo criterio que las copias)
IF @logicoLog IS NOT NULL AND @mbLog > 512
BEGIN
    BEGIN TRY
        SET @sql = N'USE ' + QUOTENAME(@Base) + N'; DBCC SHRINKFILE (' + QUOTENAME(@logicoLog, '''') + N', 512) WITH NO_INFOMSGS;';
        EXEC sp_executesql @sql;
        PRINT '  log reducido a 512 MB.';
    END TRY
    BEGIN CATCH
        PRINT '  AVISO con el log: ' + ERROR_MESSAGE();
    END CATCH
END

PRINT '';
PRINT '=== TAMAÑO FINAL ===';
SELECT CAST(DB_NAME(database_id) AS varchar(20)) AS base, CAST(type_desc AS varchar(5)) AS tipo,
       CAST(size * 8.0 / 1024 AS decimal(18,1)) AS mb
FROM sys.master_files WHERE database_id = DB_ID(@Base) ORDER BY type_desc DESC;
PRINT '==============================================================';
