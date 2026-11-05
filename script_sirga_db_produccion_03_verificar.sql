/* ==========================================================================================
   03 — VERIFICA QUE EL ESQUEMA DE PRODUCCIÓN QUEDÓ COMPLETO
   ------------------------------------------------------------------------------------------
   Comprueba, sin modificar nada:
     - que la base exista y su modelo de recuperación;
     - cuántas tablas hay y que estén TODAS las que necesita la aplicación;
     - las tablas y columnas de las últimas versiones (bitácora de empleado, actividad,
       obra social, propuesta de escala);
     - que el historial de migraciones de EF esté completo.

   USO
     sqlcmd -S <servidor> -E -b -i script_sirga_db_produccion_03_verificar.sql
   ========================================================================================== */

SET NOCOUNT ON;

DECLARE @Base sysname = N'SIRGA_DB';

PRINT '==============================================================';
PRINT '  VERIFICACIÓN DEL ESQUEMA EN [' + @Base + ']';
PRINT '==============================================================';

IF DB_ID(@Base) IS NULL
BEGIN
  PRINT '  ERROR: la base [' + @Base + '] no existe.';
  RETURN;
END

/* ---------------------------- 1) estado general ---------------------------------------- */
SELECT CAST(name AS varchar(20)) AS base, CAST(recovery_model_desc AS varchar(10)) AS modelo,
       CAST(state_desc AS varchar(10)) AS estado, CAST(compatibility_level AS varchar(10)) AS nivel_compat
FROM sys.databases WHERE name = @Base;

/* ---------------------------- 2) conteo de tablas -------------------------------------- */
DECLARE @sql nvarchar(max), @tablas int, @migraciones int, @faltantes nvarchar(max);

SET @sql = N'SELECT @n = COUNT(*) FROM ' + QUOTENAME(@Base) + N'.sys.tables;';
EXEC sp_executesql @sql, N'@n int OUTPUT', @tablas OUTPUT;
PRINT '  tablas creadas: ' + CAST(@tablas AS varchar(10));

SET @sql = N'SELECT @n = COUNT(*) FROM ' + QUOTENAME(@Base) + N'.dbo.__EFMigrationsHistory;';
BEGIN TRY
  EXEC sp_executesql @sql, N'@n int OUTPUT', @migraciones OUTPUT;
  PRINT '  migraciones registradas: ' + CAST(@migraciones AS varchar(10));
END TRY
BEGIN CATCH
  PRINT '  ADVERTENCIA: no existe __EFMigrationsHistory (¿se corrió el script 02?).';
END CATCH

/* ---------------------------- 3) tablas imprescindibles -------------------------------- */
DECLARE @esperadas TABLE (Nombre sysname);
INSERT INTO @esperadas (Nombre) VALUES
  (N'Companies'), (N'Employees'), (N'Assignments'), (N'Declarations'), (N'DeclarationDetails'),
  (N'DeclarationRegistries'), (N'Unions'), (N'Ramas'), (N'EmployeeCategories'), (N'SalaryScales'),
  (N'InterestRates'), (N'Banks'), (N'Activities'), (N'HealthInsurances'), (N'EmployeeChangeLogs'),
  (N'AspNetUsers'), (N'AspNetRoles'), (N'Payments'), (N'AuditLogs');

SELECT CAST(e.Nombre AS varchar(30)) AS tabla_esperada,
       CASE WHEN t.object_id IS NULL THEN 'FALTA' ELSE 'OK' END AS estado
FROM @esperadas e
LEFT JOIN (SELECT name, object_id FROM SIRGA_DB.sys.tables) t ON t.name = e.Nombre
ORDER BY CASE WHEN t.object_id IS NULL THEN 0 ELSE 1 END, e.Nombre;

-- (El bloque de arriba usa SIRGA_DB literal; si cambiás @Base, ajustá esa línea.)

/* ---------------------------- 4) columnas de las últimas versiones --------------------- */
DECLARE @columnas TABLE (Tabla sysname, Columna sysname);
INSERT INTO @columnas (Tabla, Columna) VALUES
  (N'Employees',           N'LastName'),
  (N'Employees',           N'FirstName'),
  (N'Employees',           N'Mobile'),
  (N'Employees',           N'HealthInsuranceId'),
  (N'Assignments',         N'ActivityId'),
  (N'Assignments',         N'FileNumber'),
  (N'DeclarationDetails',  N'UsaEscala'),
  (N'DeclarationDetails',  N'ScaleSalary'),
  (N'DeclarationDetails',  N'EnteredSalary'),
  (N'DeclarationDetails',  N'Difference'),
  (N'EmployeeChangeLogs',  N'ValorAnterior'),
  (N'EmployeeChangeLogs',  N'Origen');

SELECT CAST(c.Tabla AS varchar(28)) AS tabla, CAST(c.Columna AS varchar(28)) AS columna,
       CASE WHEN col.object_id IS NULL THEN 'FALTA' ELSE 'OK' END AS estado
FROM @columnas c
LEFT JOIN SIRGA_DB.sys.columns col
       ON col.object_id = OBJECT_ID('SIRGA_DB.dbo.' + c.Tabla)
      AND col.name = c.Columna
ORDER BY CASE WHEN col.object_id IS NULL THEN 0 ELSE 1 END, c.Tabla, c.Columna;

/* ---------------------------- 5) resumen ----------------------------------------------- */
IF @tablas = 0
  PRINT '  RESULTADO: la base está vacía. Falta correr el script 02.';
ELSE IF @tablas >= 55
  PRINT '  RESULTADO: el esquema parece completo (' + CAST(@tablas AS varchar(10)) + ' tablas).';
ELSE
  PRINT '  RESULTADO: hay ' + CAST(@tablas AS varchar(10)) + ' tablas; puede faltar parte del esquema.';

PRINT '';
PRINT '  Las filas que digan "FALTA" hay que resolverlas antes de levantar la aplicación.';
PRINT '==============================================================';
