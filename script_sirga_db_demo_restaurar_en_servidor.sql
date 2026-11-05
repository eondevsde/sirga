/* ==========================================================================================
   DEMO — RESTAURA LAS BASES DE DEMO EN EL SERVIDOR DESDE LOS .bak
   ------------------------------------------------------------------------------------------
   PARA QUÉ SIRVE
     Evita respaldar las bases reales en el servidor: se llevan los dos .bak generados en
     desarrollo (ya compactados y comprimidos) y acá se restauran con los nombres de demo.

   QUÉ COPIAR AL SERVIDOR
     SIRGA_DB.bak     (~57 MB)    -> se restaura como  SIRGA_DB_DEMO
     Sindicato.bak    (~457 MB)   -> se restaura como  SINDICATO_DEMO
     Dejar los dos en la carpeta indicada en @CarpetaBak.

   QUÉ HACE
     1. Verifica que los .bak estén (xp_fileexist) y muestra su contenido (FILELISTONLY).
     2. Restaura con MOVE a la carpeta de datos de la instancia.
     3. Deja las dos en SIMPLE, hace checkpoint y compacta los datos (son copias).
     4. Opcional: habilita un login SQL en las bases.
     5. Verifica filas y tamaños al final.

   SEGURIDAD
     - Arranca con @Simulacro = 1: NO escribe nada, sólo muestra los .bak.
     - NO toca las bases reales: únicamente crea/restaura las DEMO.
     - Si la DEMO ya existe no la toca, salvo @Recrear = 1.

   USO
     sqlcmd -S <servidor> -E -b -i script_sirga_db_demo_restaurar_en_servidor.sql
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ---------------------------------- PARÁMETROS ------------------------------------------ */
DECLARE @Simulacro      bit = 1;                          -- 1 = sólo informa | 0 = restaura
DECLARE @Recrear        bit = 0;                          -- 1 = si la DEMO existe, la borra y recrea
DECLARE @CarpetaBak     nvarchar(400) = N'C:\Demo\bak';   -- donde están copiados los .bak
DECLARE @CarpetaDatos   nvarchar(400) = NULL;             -- NULL = carpeta por defecto de la instancia
DECLARE @CompactarDatos bit = 1;                          -- 1 = devuelve el espacio vacío de las copias
DECLARE @LoginApp       nvarchar(128) = NULL;             -- opcional: login SQL (ej. sqladmin)
/* --------------------------------------------------------------------------------------- */

DECLARE @trabajos TABLE (Id int IDENTITY(1,1), Archivo nvarchar(200), Destino sysname,
                         LogicoDatos sysname, LogicoLog sysname);
INSERT INTO @trabajos (Archivo, Destino, LogicoDatos, LogicoLog) VALUES
  (N'SIRGA_DB.bak',  N'SIRGA_DB_DEMO',  N'SIRGA_DB',  N'SIRGA_DB_log'),
  (N'Sindicato.bak', N'SINDICATO_DEMO', N'sindicato', N'sindicato_log');

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se escribe nada'
                                   ELSE '  MODO REAL: se restauran las bases de demo' END;
PRINT '  carpeta de los .bak: ' + @CarpetaBak;
PRINT '==============================================================';

DECLARE @dir nvarchar(400);
IF @CarpetaDatos IS NULL
  SELECT @dir = CONVERT(nvarchar(400), SERVERPROPERTY('InstanceDefaultDataPath'));
ELSE
  SET @dir = @CarpetaDatos;
IF RIGHT(@dir, 1) <> '\' SET @dir = @dir + '\';

DECLARE @archivo nvarchar(500), @destino sysname, @logicoDatos sysname, @logicoLog sysname;
DECLARE @sql nvarchar(max), @mbLibre decimal(18,1), @existeBase bit, @existeArchivo int;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
  SELECT Archivo, Destino, LogicoDatos, LogicoLog FROM @trabajos ORDER BY Id;
OPEN cur;
FETCH NEXT FROM cur INTO @archivo, @destino, @logicoDatos, @logicoLog;

WHILE @@FETCH_STATUS = 0
BEGIN
  DECLARE @ruta nvarchar(500) = @CarpetaBak + '\' + @archivo;
  PRINT '';
  PRINT '--- ' + @ruta + '  ->  [' + @destino + ']';

  /* 1) ¿existe el archivo? */
  SET @existeArchivo = 0;
  BEGIN TRY
    EXEC master.dbo.xp_fileexist @ruta, @existeArchivo OUTPUT;
  END TRY
  BEGIN CATCH
    SET @existeArchivo = 1;   -- si no se puede comprobar, se intenta igual
  END CATCH

  IF @existeArchivo = 0
  BEGIN
    PRINT '    ERROR: no existe el archivo ' + @ruta;
    PRINT '    Copiarlo a esa carpeta (o cambiar @CarpetaBak). Se omite.';
    FETCH NEXT FROM cur INTO @archivo, @destino, @logicoDatos, @logicoLog;
    CONTINUE;
  END

  /* 2) ¿ya existe la base destino? */
  SET @existeBase = CASE WHEN DB_ID(@destino) IS NULL THEN 0 ELSE 1 END;
  IF @existeBase = 1 AND @Recrear = 0
  BEGIN
    PRINT '    la base [' + @destino + '] YA existe y @Recrear = 0. Se omite.';
    SELECT CAST(d.name AS varchar(20)) AS base, CAST(mf.type_desc AS varchar(5)) AS tipo,
           CAST(mf.size * 8.0 / 1024 AS decimal(18,1)) AS mb
    FROM sys.databases d JOIN sys.master_files mf ON mf.database_id = d.database_id
    WHERE d.name = @destino;
    FETCH NEXT FROM cur INTO @archivo, @destino, @logicoDatos, @logicoLog;
    CONTINUE;
  END

  /* 3) espacio y contenido del respaldo */
  SELECT @mbLibre = CAST(available_bytes / 1048576.0 AS decimal(18,1))
  FROM sys.dm_os_volume_stats(DB_ID(), 1);
  PRINT '    espacio libre: ' + CONVERT(varchar(20), @mbLibre) + ' MB';
  PRINT '    contenido del respaldo (nombre lógico / tipo / MB):';
  SET @sql = N'RESTORE FILELISTONLY FROM DISK = ' + QUOTENAME(@ruta, '''') + N';';
  EXEC sp_executesql @sql;

  SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@destino) + N' FROM DISK = ' +
             QUOTENAME(@ruta, '''') +
             N' WITH MOVE ' + QUOTENAME(@logicoDatos, '''') + N' TO ' +
             QUOTENAME(@dir + @destino + N'.mdf', '''') + N', MOVE ' + QUOTENAME(@logicoLog, '''') +
             N' TO ' + QUOTENAME(@dir + @destino + N'_log.ldf', '''') + N', RECOVERY, STATS = 10;';

  IF @Simulacro = 1
  BEGIN
    PRINT '    (simulacro) ' + @sql;
  END
  ELSE
  BEGIN
    IF @existeBase = 1
    BEGIN
      PRINT '    borrando la demo anterior ...';
      SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;' +
                 N' DROP DATABASE ' + QUOTENAME(@destino) + N';';
      EXEC sp_executesql @sql;
    END

    PRINT '    restaurando ...';
    SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@destino) + N' FROM DISK = ' +
               QUOTENAME(@ruta, '''') +
               N' WITH MOVE ' + QUOTENAME(@logicoDatos, '''') + N' TO ' +
               QUOTENAME(@dir + @destino + N'.mdf', '''') + N', MOVE ' + QUOTENAME(@logicoLog, '''') +
               N' TO ' + QUOTENAME(@dir + @destino + N'_log.ldf', '''') + N', RECOVERY, STATS = 10;';
    EXEC sp_executesql @sql;

    SET @sql = N'ALTER DATABASE ' + QUOTENAME(@destino) + N' SET RECOVERY SIMPLE WITH NO_WAIT;';
    EXEC sp_executesql @sql;

    SET @sql = N'USE ' + QUOTENAME(@destino) + N'; CHECKPOINT;';
    EXEC sp_executesql @sql;

    IF @CompactarDatos = 1
    BEGIN TRY
      SET @sql = N'DBCC SHRINKDATABASE (' + QUOTENAME(@destino, '''') + N', 10) WITH NO_INFOMSGS;';
      EXEC sp_executesql @sql;
      PRINT '    datos compactados.';
    END TRY
    BEGIN CATCH
      PRINT '    AVISO: no se pudieron compactar los datos: ' + ERROR_MESSAGE();
    END CATCH

    IF @LoginApp IS NOT NULL
    BEGIN
      IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginApp)
      BEGIN
        SET @sql = N'USE ' + QUOTENAME(@destino) + N';
                     IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = ''' + @LoginApp + N''')
                       CREATE USER ' + QUOTENAME(@LoginApp) + N' FOR LOGIN ' + QUOTENAME(@LoginApp) + N';';
        BEGIN TRY EXEC sp_executesql @sql; PRINT '    login SQL habilitado en la base.'; END TRY
        BEGIN CATCH PRINT '    AVISO con el login SQL: ' + ERROR_MESSAGE(); END CATCH
      END
      ELSE
        PRINT '    AVISO: el login SQL [' + @LoginApp + '] no existe en este servidor.';
    END

    PRINT '    OK: [' + @destino + '] restaurada.';
  END

  FETCH NEXT FROM cur INTO @archivo, @destino, @logicoDatos, @logicoLog;
END

CLOSE cur;
DEALLOCATE cur;

/* ------------------------------- VERIFICACIÓN ------------------------------------------ */
PRINT '';
PRINT '=== ESTADO DE LAS DEMO ===';
SELECT CAST(d.name AS varchar(20)) AS base, CAST(d.recovery_model_desc AS varchar(8)) AS modelo,
       CAST(mf.type_desc AS varchar(5)) AS tipo, CAST(mf.size * 8.0 / 1024 AS decimal(18,1)) AS mb
FROM sys.databases d JOIN sys.master_files mf ON mf.database_id = d.database_id
WHERE d.name IN (N'SIRGA_DB_DEMO', N'SINDICATO_DEMO') ORDER BY d.name, mf.type_desc DESC;

IF DB_ID(N'SIRGA_DB_DEMO') IS NOT NULL
BEGIN
  SET @sql = N'SELECT ''SIRGA_DB_DEMO'' AS base,
      (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Companies) AS empresas,
      (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Employees) AS empleados,
      (SELECT COUNT(*) FROM SIRGA_DB_DEMO.dbo.Declarations) AS ddjj;';
  EXEC sp_executesql @sql;
END

IF DB_ID(N'SINDICATO_DEMO') IS NOT NULL
BEGIN
  SET @sql = N'SELECT ''SINDICATO_DEMO'' AS base,
      (SELECT COUNT(*) FROM SINDICATO_DEMO.fotos_.maesoc) AS socios,
      (SELECT COUNT(*) FROM SINDICATO_DEMO.dbo.ddjjt) AS ddjjt;';
  EXEC sp_executesql @sql;
END

PRINT '';
PRINT 'Siguiente paso: copiar los publish a C:\Sitios\... y crear los sitios en IIS.';
PRINT '==============================================================';
