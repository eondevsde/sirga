/* ==========================================================================================
   CREA LAS BASES DE DEMO (copia de las reales)  —  SIRGA + Sindicato
   ------------------------------------------------------------------------------------------
   Qué hace:
     1. Respalda las bases REALES (con COPY_ONLY: no altera la cadena de respaldos).
     2. Las restaura con otro nombre:  SIRGA_DB_DEMO  y  SINDICATO_DEMO.
     3. Deja el LOG de las DEMO chico (RECOVERY SIMPLE + SHRINK a 256 MB).
     4. Verifica y muestra cuántas filas quedaron en las tablas principales.

   NO toca las bases reales: sólo las lee para respaldarlas.

   ANTES DE EJECUTAR
     - Crear la carpeta de respaldos (o dejar que la cree el script) y verificar que haya
       ESPACIO LIBRE suficiente: la copia pesa como la suma de datos + log de las dos bases.
     - Ejecutar PRIMERO con @Simulacro = 1 (no escribe nada, informa qué haría).
       Si el informe es correcto, cambiarlo a 0 y volver a ejecutar.

   CÓMO SE EJECUTA (por si se usa sqlcmd en lugar de SSMS)
     sqlcmd -S localhost -E -b -i script_sirga_db_demo_crear.sql
     (editando las variables del bloque siguiente)
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ---------------------------------- PARÁMETROS ------------------------------------------ */
DECLARE @Simulacro       bit = 1;   -- 1 = NO toca nada (sólo valida e informa). 0 = ejecuta.
DECLARE @Recrear         bit = 0;   -- 1 = si la DEMO ya existe, la BORRA y la vuelve a crear.
DECLARE @CompactarLog    bit = 1;   -- 1 = deja el LOG de la DEMO en 256 MB.
DECLARE @ForzarEspacio   bit = 0;   -- 1 = ignora el control de espacio libre (usar con criterio).
DECLARE @CompactarDatos  bit = 1;   -- 1 = compacta los DATOS de la demo (devuelve el espacio vacio).
DECLARE @TamanoObjetivoMb int = 256; -- tamano al que se lleva el LOG de la demo
DECLARE @CarpetaRespaldos nvarchar(400) = N'C:\Proyectos\_RESPALDOS\demo';

DECLARE @OrigenSirga      sysname = N'SIRGA_DB';
DECLARE @DestinoSirga     sysname = N'SIRGA_DB_DEMO';
DECLARE @OrigenSindicato  sysname = N'Sindicato';
DECLARE @DestinoSindicato sysname = N'SINDICATO_DEMO';
/* --------------------------------------------------------------------------------------- */

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se escribe nada'
                                   ELSE '  MODO REAL: se van a crear las bases de demo' END;
PRINT '  carpeta de respaldos: ' + @CarpetaRespaldos;
PRINT '==============================================================';

-- La carpeta de respaldos: la creo si no existe (xp_create_subdir viene con SQL Server).
IF @Simulacro = 0
BEGIN
  BEGIN TRY
    IF OBJECT_ID('master.dbo.xp_create_subdir') IS NOT NULL
      EXEC master.dbo.xp_create_subdir @CarpetaRespaldos;
  END TRY
  BEGIN CATCH
    PRINT '  AVISO: no se pudo crear la carpeta. Crearla a mano y volver a ejecutar.';
  END CATCH
END

DECLARE @trabajos TABLE (Id int IDENTITY(1,1), Origen sysname, Destino sysname);
INSERT INTO @trabajos (Origen, Destino) VALUES
  (@OrigenSirga,     @DestinoSirga),
  (@OrigenSindicato, @DestinoSindicato);

DECLARE @origen sysname, @destino sysname, @sql nvarchar(max), @sqlRespaldo nvarchar(max);
DECLARE @dir nvarchar(400), @mbDatos decimal(18,1), @mbLog decimal(18,1), @mbLibre decimal(18,1);
DECLARE @archivoRespaldo nvarchar(500), @moves nvarchar(max), @logicoLog sysname;
DECLARE @necesita decimal(18,1), @mbUsado decimal(18,1);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Origen, Destino FROM @trabajos ORDER BY Id;
OPEN cur;
FETCH NEXT FROM cur INTO @origen, @destino;

WHILE @@FETCH_STATUS = 0
BEGIN
  PRINT '';
  PRINT '--- ' + @origen + '  ->  ' + @destino;

  -- 1) ¿existe la base origen?
  IF DB_ID(@origen) IS NULL
  BEGIN
    PRINT '    ERROR: la base origen [' + @origen + '] no existe en esta instancia. Se omite.';
    FETCH NEXT FROM cur INTO @origen, @destino;
    CONTINUE;
  END

  -- 2) ¿ya existe la demo?
  IF DB_ID(@destino) IS NOT NULL
  BEGIN
    IF @Recrear = 0
    BEGIN
      PRINT '    La demo [' + @destino + '] YA existe y @Recrear = 0. Se omite (no se toca).';
      FETCH NEXT FROM cur INTO @origen, @destino;
      CONTINUE;
    END
    PRINT '    La demo [' + @destino + '] existe y @Recrear = 1: se va a BORRAR y recrear.';
  END

  -- 3) Tamaños y espacio libre
  SELECT @mbDatos = SUM(CASE WHEN type_desc = 'ROWS' THEN size ELSE 0 END) * 8.0 / 1024.0,
         @mbLog   = SUM(CASE WHEN type_desc = 'LOG'  THEN size ELSE 0 END) * 8.0 / 1024.0
  FROM sys.master_files WHERE database_id = DB_ID(@origen);

  SELECT @mbLibre = CAST(available_bytes / 1048576.0 AS decimal(18,1))
  FROM sys.dm_os_volume_stats(DB_ID(@origen), 1);

  SELECT @dir = LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name))) + '\'
  FROM sys.master_files WHERE database_id = DB_ID(@origen) AND file_id = 1;

  PRINT '    datos: ' + CONVERT(varchar(20), @mbDatos) + ' MB | log: ' + CONVERT(varchar(20), @mbLog) +
        ' MB | libre en el volumen: ' + CONVERT(varchar(20), @mbLibre) + ' MB';
  PRINT '    carpeta destino de los archivos: ' + @dir;

  -- Espacio necesario EN EL PICO: durante la operación conviven tres cosas en el mismo disco:
  --   1) el archivo .bak  -> pesa los DATOS USADOS, no el tamaño del archivo
  --   2) la base restaurada -> se crea con el tamaño del archivo del origen (aunque esté lleno de vacío)
  --   3) el LOG -> se crea con su tamaño ORIGINAL; recién después @CompactarLog lo reduce
  -- Si el .bak va a otro disco, sólo cuenta (2) + (3).
  SET @mbUsado = NULL;
  BEGIN TRY
    SET @sql = N'SELECT @mb = CAST(SUM(used_page_count) * 8.0 / 1024.0 AS decimal(18,1))
                 FROM ' + QUOTENAME(@origen) + N'.sys.dm_db_partition_stats;';
    EXEC sp_executesql @sql, N'@mb decimal(18,1) OUTPUT', @mbUsado OUTPUT;
  END TRY
  BEGIN CATCH
    SET @mbUsado = @mbDatos;   -- sin permiso para medir: se usa el peor caso
  END CATCH
  SET @necesita = ISNULL(@mbUsado, @mbDatos) + @mbDatos + @mbLog;

  PRINT '    datos usados de verdad: ' + CONVERT(varchar(20), @mbUsado) +
        ' MB (es lo que pesa el .bak, aprox.)';
  PRINT '    pico necesario: ' + CONVERT(varchar(20), @necesita) +
        ' MB  = .bak (' + CONVERT(varchar(20), @mbUsado) + ') + base restaurada (' +
        CONVERT(varchar(20), @mbDatos) + ') + log transitorio (' + CONVERT(varchar(20), @mbLog) + ')';

  IF @mbLibre < @necesita AND @ForzarEspacio = 0
  BEGIN
    PRINT '    ERROR: falta espacio en este disco. Hacen falta ~' + CONVERT(varchar(20), @necesita) +
          ' MB y hay ' + CONVERT(varchar(20), @mbLibre) + ' MB.';
    PRINT '    Opciones: (a) poner @CarpetaRespaldos en OTRO disco; (b) hacer un BACKUP LOG y';
    PRINT '    compactar el log del ORIGEN (el log grande es de la base real, no de la demo);';
    PRINT '    (c) compactar los archivos del origen; (d) @ForzarEspacio = 1 si el espacio alcanza.';
    PRINT '    Se omite esta base.';
    FETCH NEXT FROM cur INTO @origen, @destino;
    CONTINUE;
  END

  SET @archivoRespaldo = @CarpetaRespaldos + '\' + @origen + '.bak';

  -- 4) Armar la lista de MOVE (una entrada por archivo, con el nombre lógico del origen)
  SET @moves = NULL;
  SELECT @moves = STUFF((
      SELECT ', MOVE ' + QUOTENAME(name, '''') + ' TO ' + QUOTENAME(
                 @dir + @destino +
                 CASE WHEN type_desc = 'LOG' THEN '_log.ldf'
                      WHEN file_id = 1        THEN '.mdf'
                      ELSE '_' + CONVERT(varchar(10), file_id) + '.mdf' END, '''')
      FROM sys.master_files
      WHERE database_id = DB_ID(@origen)
      ORDER BY CASE WHEN type_desc = 'ROWS' THEN 0 ELSE 1 END, file_id
      FOR XML PATH('')), 1, 2, '');

  SET @sqlRespaldo = N'BACKUP DATABASE ' + QUOTENAME(@origen) + N' TO DISK = ' +
                     QUOTENAME(@archivoRespaldo, '''') + N' WITH COPY_ONLY, INIT, CHECKSUM, STATS = 25;';

  SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@destino) + N' FROM DISK = ' +
             QUOTENAME(@archivoRespaldo, '''') + N' WITH ' + @moves + N', REPLACE, RECOVERY, STATS = 10;';

  IF @Simulacro = 1
  BEGIN
    PRINT '    (simulacro) respaldo: ' + @archivoRespaldo;
    PRINT '    (simulacro) ' + @sql;
  END
  ELSE
  BEGIN
    -- 5) Si hay que recrear: sacar a la demo del medio y borrarla
    IF DB_ID(@destino) IS NOT NULL
    BEGIN
      SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                   DROP DATABASE ' + QUOTENAME(@destino) + N';';
      EXEC sp_executesql @sql;
      PRINT '    demo anterior borrada.';
    END

    PRINT '    respaldando ' + @origen + ' ...';
    BEGIN TRY
      DECLARE @sqlRespaldoComprimido nvarchar(max) =
          N'BACKUP DATABASE ' + QUOTENAME(@origen) + N' TO DISK = ' +
          QUOTENAME(@archivoRespaldo, '''') + N' WITH COPY_ONLY, INIT, CHECKSUM, COMPRESSION, STATS = 25;';
      EXEC sp_executesql @sqlRespaldoComprimido;
      PRINT '    (con compresion)';
    END TRY
    BEGIN CATCH
      PRINT '    la edicion no soporta compresion; se respalda sin comprimir.';
      EXEC sp_executesql @sqlRespaldo;
    END CATCH

    -- Espacio real que quedó y tamaño del respaldo (dato útil para el operador)
    DECLARE @mbBak decimal(18,1);
    BEGIN TRY
      SET @sql = N'SELECT @mb = CAST(SUM(size)*8.0/1024 AS decimal(18,1)) FROM sys.master_files WHERE database_id = DB_ID(''' + @destino + ''');';
    END TRY
    BEGIN CATCH END CATCH
    PRINT '    respaldo hecho.';

    PRINT '    restaurando como ' + @destino + ' ...';
    SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@destino) + N' FROM DISK = ' +
               QUOTENAME(@archivoRespaldo, '''') + N' WITH ' + @moves + N', REPLACE, RECOVERY, STATS = 10;';
    EXEC sp_executesql @sql;

    -- 6) Log chico en la DEMO (no necesita recuperación a un punto en el tiempo)
    IF @CompactarLog = 1
    BEGIN
      SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET RECOVERY SIMPLE;';
      EXEC sp_executesql @sql;

      SELECT TOP 1 @logicoLog = name FROM sys.master_files
      WHERE database_id = DB_ID(@destino) AND type_desc = 'LOG';

      IF @logicoLog IS NOT NULL AND @mbLog > @TamanoObjetivoMb
      BEGIN
        SET @sql = N'USE ' + QUOTENAME(@destino) + N'; DBCC SHRINKFILE (' +
                   QUOTENAME(@logicoLog, '''') + N', 256);';
        BEGIN TRY
          EXEC sp_executesql @sql;
          PRINT '    log de la demo reducido a 256 MB.';
        END TRY
        BEGIN CATCH
          PRINT '    AVISO: no se pudo reducir el log: ' + ERROR_MESSAGE();
        END CATCH
      END
    END

    -- Los archivos se restauran con el tamaño del origen, que puede estar lleno de espacio vacío
    -- (Sindicato ocupa 9.224 MB pero adentro tiene sólo ~1.500 MB de datos). Esto es la COPIA: se puede
    -- compactar sin riesgo.
    IF @CompactarDatos = 1
    BEGIN
      BEGIN TRY
        SET @sql = N'DBCC SHRINKDATABASE (' + QUOTENAME(@destino, '''') + N', 10) WITH NO_INFOMSGS;';
        EXEC sp_executesql @sql;
        PRINT '    datos de la demo compactados.';
      END TRY
      BEGIN CATCH
        PRINT '    AVISO: no se pudieron compactar los datos: ' + ERROR_MESSAGE();
      END CATCH
    END

    PRINT '    OK: [' + @destino + '] creada.';
  END

  FETCH NEXT FROM cur INTO @origen, @destino;
END

CLOSE cur;
DEALLOCATE cur;

/* ------------------------------- VERIFICACIÓN FINAL ------------------------------------ */
PRINT '';
PRINT '=================== VERIFICACIÓN ===================';

IF DB_ID(@DestinoSirga) IS NOT NULL
BEGIN
  SET @sql = N'SELECT ''' + @DestinoSirga + N''' AS base,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSirga) + N'.dbo.Companies)    AS empresas,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSirga) + N'.dbo.Employees)    AS empleados,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSirga) + N'.dbo.Assignments)  AS asignaciones,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSirga) + N'.dbo.Declarations) AS ddjj;';
  EXEC sp_executesql @sql;
END
ELSE PRINT '  [' + @DestinoSirga + '] no existe.';

IF DB_ID(@DestinoSindicato) IS NOT NULL
BEGIN
  SET @sql = N'SELECT ''' + @DestinoSindicato + N''' AS base,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSindicato) + N'.fotos_.maesoc) AS socios,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSindicato) + N'.fotos_.socemp) AS socios_empresa,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSindicato) + N'.dbo.ddjj)      AS ddjj_detalle,
      (SELECT COUNT(*) FROM ' + QUOTENAME(@DestinoSindicato) + N'.dbo.ddjjt)     AS ddjj_cabeceras;';
  EXEC sp_executesql @sql;
END
ELSE PRINT '  [' + @DestinoSindicato + '] no existe.';

PRINT '';
PRINT '=== TAMAÑO FINAL DE LAS DEMO ===';
SELECT name AS base, type_desc AS tipo,
       CAST(size * 8.0 / 1024 AS decimal(18,1)) AS mb,
       physical_name
FROM sys.master_files
WHERE DB_NAME(database_id) IN (@DestinoSirga, @DestinoSindicato)
ORDER BY base, tipo DESC;

PRINT '';
PRINT 'RECORDATORIO: si la aplicación se conecta con un LOGIN SQL (no Windows), hay que darle';
PRINT 'acceso a las dos DEMO. Ejemplo (descomentar y cambiar el login):';
PRINT '  -- USE SIRGA_DB_DEMO;  CREATE USER [sqladmin] FOR LOGIN [sqladmin]; ALTER ROLE db_owner ADD MEMBER [sqladmin];';
PRINT '  -- USE SINDICATO_DEMO; CREATE USER [sqladmin] FOR LOGIN [sqladmin]; ALTER ROLE db_owner ADD MEMBER [sqladmin];';
PRINT '==============================================================';
