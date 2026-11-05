/* ==========================================================================================
   02 — PERMISOS PARA LAS APLICACIONES EN EL SERVIDOR
   ------------------------------------------------------------------------------------------
   Las aplicaciones de IIS se ejecutan con la identidad del App Pool. Esa identidad tiene que poder
   entrar a SQL Server y leer/escribir en las bases de la demo.

   QUÉ HACE
     - Crea los logins de las tres identidades de App Pool (si no existen).
     - Les da permiso en SIRGA_DB_DEMO y SINDICATO_DEMO.
     - Si además se usa un login SQL (por ejemplo sqladmin), lo da de alta como usuario.

   ¿POR QUÉ db_owner?
     Es una DEMO y SIRGA/SIGESWEB pueden aplicar migraciones al arrancar (crear/alterar tablas).
     En producción se ajustan permisos finos; acá lo importante es que nada falle durante la
     presentación.

   USO
     sqlcmd -S <servidor> -E -b -I -i 02_permisos.sql
   ========================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

/* ---------------------------------- PARÁMETROS ------------------------------------------ */
DECLARE @Simulacro bit = 1;                       -- 1 = informa | 0 = aplica
DECLARE @LoginSQL  nvarchar(128) = NULL;          -- opcional: login SQL que use la app
/* --------------------------------------------------------------------------------------- */

PRINT '==============================================================';
PRINT CASE WHEN @Simulacro = 1 THEN '  MODO SIMULACRO: no se cambia ningún permiso'
                                   ELSE '  MODO REAL: se crean logins y permisos' END;
PRINT '==============================================================';

/* --------------------------- identidades de los App Pools ------------------------------ */
DECLARE @logins TABLE (Orden int, Nombre nvarchar(128));
INSERT INTO @logins VALUES
    (1, N'IIS APPPOOL\SIRGA_DEMO'),
    (2, N'IIS APPPOOL\SIGESWEB_DEMO'),
    (3, N'IIS APPPOOL\SIGESWEB_DEMO_CARNET');

IF @LoginSQL IS NOT NULL
    INSERT INTO @logins VALUES (4, @LoginSQL);

DECLARE @nombre nvarchar(128), @sql nvarchar(max), @existe int;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @logins ORDER BY Orden;
OPEN c;
FETCH NEXT FROM c INTO @nombre;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @existe = COUNT(*) FROM sys.server_principals WHERE name = @nombre;
    IF @existe = 0
    BEGIN
        IF @Simulacro = 1
            PRINT '  (simulacro) CREATE LOGIN [' + @nombre + '] FROM WINDOWS';
        ELSE
        BEGIN
            BEGIN TRY
                SET @sql = N'CREATE LOGIN ' + QUOTENAME(@nombre) + N' FROM WINDOWS;';
                EXEC sp_executesql @sql;
                PRINT '  login creado: ' + @nombre;
            END TRY
            BEGIN CATCH
                PRINT '  AVISO con el login ' + @nombre + ': ' + ERROR_MESSAGE();
            END CATCH
        END
    END
    ELSE
        PRINT '  login ya existe: ' + @nombre;
    FETCH NEXT FROM c INTO @nombre;
END
CLOSE c;
DEALLOCATE c;

/* ------------------------------- permiso en cada base ---------------------------------- */
DECLARE @bases TABLE (Orden int, Base sysname);
INSERT INTO @bases VALUES (1, N'SIRGA_DB_DEMO'), (2, N'SINDICATO_DEMO');

DECLARE @base sysname;

DECLARE cb CURSOR LOCAL FAST_FORWARD FOR SELECT Base FROM @bases ORDER BY Orden;
OPEN cb;
FETCH NEXT FROM cb INTO @base;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF DB_ID(@base) IS NULL
        PRINT '  AVISO: no existe la base [' + @base + '] (correr primero 01_bases_demo.sql).';
    ELSE
    BEGIN
        PRINT '';
        PRINT '  --- ' + @base;
        DECLARE cl CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @logins ORDER BY Orden;
        OPEN cl;
        FETCH NEXT FROM cl INTO @nombre;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @Simulacro = 1
                PRINT '     (simulacro) ' + @nombre + ' -> db_owner en ' + @base;
            ELSE
            BEGIN
                BEGIN TRY
                    SET @sql = N'USE ' + QUOTENAME(@base) + N';
                        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N' + QUOTENAME(@nombre, '''') + N')
                            CREATE USER ' + QUOTENAME(@nombre) + N' FOR LOGIN ' + QUOTENAME(@nombre) + N';
                        ALTER ROLE db_owner ADD MEMBER ' + QUOTENAME(@nombre) + N';';
                    EXEC sp_executesql @sql;
                    PRINT '     ' + @nombre + ' -> db_owner';
                END TRY
                BEGIN CATCH
                    PRINT '     AVISO con ' + @nombre + ': ' + ERROR_MESSAGE();
                END CATCH
            END
            FETCH NEXT FROM cl INTO @nombre;
        END
        CLOSE cl;
        DEALLOCATE cl;
    END
    FETCH NEXT FROM cb INTO @base;
END
CLOSE cb;
DEALLOCATE cb;

PRINT '';
PRINT '=== VERIFICACIÓN ===';
DECLARE cv CURSOR LOCAL FAST_FORWARD FOR SELECT Base FROM @bases ORDER BY Orden;
DECLARE @bv sysname, @cuenta int, @sqlv nvarchar(max);
OPEN cv;
FETCH NEXT FROM cv INTO @bv;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF DB_ID(@bv) IS NOT NULL
    BEGIN
        SET @sqlv = N'SELECT @c = COUNT(*) FROM ' + QUOTENAME(@bv) + N'.sys.database_principals
                      WHERE name LIKE ''IIS APPPOOL%'' AND type = ''U'';';
        EXEC sp_executesql @sqlv, N'@c int OUTPUT', @cuenta OUTPUT;
        PRINT '  ' + @bv + ': ' + CAST(@cuenta AS varchar(5)) + ' usuario(s) de App Pool';
    END
    FETCH NEXT FROM cv INTO @bv;
END
CLOSE cv;
DEALLOCATE cv;

PRINT 'Siguiente paso: 03_instalar_iis.ps1';
PRINT '==============================================================';
