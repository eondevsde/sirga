/* ==========================================================================================
   02 — RESTAURA EL ESQUEMA DE SIRGA EN UNA BASE NUEVA (copia sólo-esquema, sin datos)
   ------------------------------------------------------------------------------------------
   POR QUÉ EXISTE
     La cadena de migraciones de EF NO puede crear una base de SIRGA desde cero: hay 8 columnas
     agregadas dos veces por migraciones distintas y migraciones escritas a mano que asumen un estado
     previo. Por eso, para una base NUEVA se parte de este respaldo, que tiene:
        - las 59 tablas y todas las columnas del modelo actual,
        - el historial de migraciones COMPLETO (57 filas), así EF no intenta aplicar nada,
        - las tablas VACÍAS (sólo el historial tiene filas).

   QUÉ HACE
     1. Verifica que el respaldo exista.
     2. Si la base destino ya existe, no la toca (salvo @Recrear = 1).
     3. Restaura con MOVE a la carpeta de datos de la instancia, en SIMPLE.
     4. Verifica: cantidad de tablas, filas (tiene que ser 0) y migraciones registradas.

   SEGURIDAD
     - Arranca con @Simulacro = 1: informa y no restaura nada.
     - NO toca ninguna base existente salvo @Recrear = 1 (y lo dice).

   USO
     sqlcmd -S <servidor> -E -b -I -i script_sirga_db_produccion_02_restaurar_esquema.sql
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

/* ---------------------------------- PARÁMETROS ------------------------------------------ */
DECLARE @Simulacro     bit = 1;                                    -- 1 = sólo informa | 0 = restaura
DECLARE @Recrear       bit = 0;                                    -- 1 = borra la destino si existe
DECLARE @ArchivoBak    nvarchar(400) = N'C:\Demo\bak\SIRGA_DB_ESQUEMA_VACIA.bak';
DECLARE @Destino       sysname = N'SIRGA_DB';
DECLARE @CarpetaDatos  nvarchar(400) = NULL;                       -- NULL = carpeta por defecto
DECLARE @LogicoDatos   sysname = N'SIRGA_DB';                      -- nombres lógicos del respaldo
DECLARE @LogicoLog     sysname = N'SIRGA_DB_log';
/* --------------------------------------------------------------------------------------- */

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se restaura nada'
                                   ELSE '  MODO REAL: se restaura el esquema vacío' END;
PRINT '  respaldo: ' + @ArchivoBak;
PRINT '  destino : [' + @Destino + ']';
PRINT '==============================================================';

DECLARE @existeArchivo int = 0, @dir nvarchar(400), @sql nvarchar(max);

BEGIN TRY
    EXEC master.dbo.xp_fileexist @ArchivoBak, @existeArchivo OUTPUT;
END TRY
BEGIN CATCH
    SET @existeArchivo = 1;
END CATCH

IF @existeArchivo = 0
BEGIN
    PRINT '  ERROR: no existe el respaldo. Copiarlo a esa ruta o cambiar @ArchivoBak.';
    RETURN;
END

IF DB_ID(@Destino) IS NOT NULL
BEGIN
    IF @Recrear = 0
    BEGIN
        PRINT '  La base [' + @Destino + '] YA existe. No se toca.';
        PRINT '  (Si querés reemplazarla, @Recrear = 1.)';
        RETURN;
    END
    PRINT '  La base [' + @Destino + '] existe y @Recrear = 1: se va a BORRAR y recrear.';
END

IF @CarpetaDatos IS NULL
    SELECT @dir = CONVERT(nvarchar(400), SERVERPROPERTY('InstanceDefaultDataPath'));
ELSE
    SET @dir = @CarpetaDatos;
IF RIGHT(@dir, 1) <> '\' SET @dir = @dir + '\';

PRINT '  archivo de datos: ' + @dir + @Destino + N'.mdf';
PRINT '  archivo de log  : ' + @dir + @Destino + N'_log.ldf';

SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@Destino) + N' FROM DISK = ' + QUOTENAME(@ArchivoBak, '''') +
           N' WITH MOVE ' + QUOTENAME(@LogicoDatos, '''') + N' TO ' + QUOTENAME(@dir + @Destino + N'.mdf', '''') +
           N', MOVE ' + QUOTENAME(@LogicoLog, '''') + N' TO ' + QUOTENAME(@dir + @Destino + N'_log.ldf', '''') +
           N', RECOVERY, STATS = 25;';

IF @Simulacro = 1
BEGIN
    PRINT '';
    PRINT '  (simulacro) ' + @sql;
    PRINT '  (simulacro) ALTER DATABASE [' + @Destino + '] SET RECOVERY SIMPLE;';
END
ELSE
BEGIN
    IF DB_ID(@Destino) IS NOT NULL
    BEGIN
        SET @sql = N'ALTER DATABASE ' + QUOTENAME(@Destino) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;' +
                   N' DROP DATABASE ' + QUOTENAME(@Destino) + N';';
        EXEC sp_executesql @sql;
        PRINT '  base anterior borrada.';
    END

    PRINT '  restaurando el esquema ...';
    SET @sql = N'RESTORE DATABASE ' + QUOTENAME(@Destino) + N' FROM DISK = ' + QUOTENAME(@ArchivoBak, '''') +
               N' WITH MOVE ' + QUOTENAME(@LogicoDatos, '''') + N' TO ' + QUOTENAME(@dir + @Destino + N'.mdf', '''') +
               N', MOVE ' + QUOTENAME(@LogicoLog, '''') + N' TO ' + QUOTENAME(@dir + @Destino + N'_log.ldf', '''') +
               N', RECOVERY, STATS = 25;';
    EXEC sp_executesql @sql;

    SET @sql = N'ALTER DATABASE ' + QUOTENAME(@Destino) + N' SET RECOVERY SIMPLE WITH NO_WAIT;';
    EXEC sp_executesql @sql;
    PRINT '  restaurada en SIMPLE.';
END

/* ------------------------------- verificación ------------------------------------------ */
PRINT '';
PRINT '=== VERIFICACIÓN ===';

IF DB_ID(@Destino) IS NOT NULL
BEGIN
    SET @sql = N'SELECT ''' + @Destino + ''' AS base,
        (SELECT COUNT(*) FROM ' + QUOTENAME(@Destino) + N'.sys.tables) AS tablas,
        (SELECT COUNT(*) FROM ' + QUOTENAME(@Destino) + N'.dbo.__EFMigrationsHistory) AS migraciones,
        (SELECT ISNULL(SUM(p.rows), 0) FROM ' + QUOTENAME(@Destino) + N'.sys.tables t
           JOIN ' + QUOTENAME(@Destino) + N'.sys.partitions p
             ON p.object_id = t.object_id AND p.index_id IN (0,1)
          WHERE t.name <> ''__EFMigrationsHistory'') AS filas_de_datos;';
    EXEC sp_executesql @sql;

    SELECT CAST(name AS varchar(20)) AS base, CAST(recovery_model_desc AS varchar(10)) AS modelo,
           CAST(state_desc AS varchar(10)) AS estado
    FROM sys.databases WHERE name = @Destino;

    PRINT 'Esperado: 59 tablas, 57 migraciones y 0 filas de datos.';
    PRINT 'Siguiente paso: 03 — verificar, y después el ETL para cargar los datos.';
END
ELSE
    PRINT '  La base [' + @Destino + '] no existe (fue un simulacro).';

PRINT '==============================================================';
