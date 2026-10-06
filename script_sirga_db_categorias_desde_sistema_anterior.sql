-- SIRGA: trae el codigo de categoria de los empleados desde el sistema anterior (tabla fotos_.maesoc).
-- Idempotente: solo completa las asignaciones que estan en NULL, asi se puede correr las veces que haga falta.
-- Respalda el estado anterior en dbo._BackupAssignmentsCategoryCode (para poder revertir con un UPDATE a NULL).
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('dbo._BackupAssignmentsCategoryCode') IS NOT NULL
    DROP TABLE dbo._BackupAssignmentsCategoryCode;

SELECT Id AS AssignmentId, CategoryCode AS CategoriaAnterior, GETDATE() AS FechaRespaldo
INTO dbo._BackupAssignmentsCategoryCode
FROM dbo.Assignments;

SELECT 'ANTES  -> con categoria = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.Assignments WHERE CategoryCode IS NOT NULL AND LTRIM(RTRIM(CategoryCode)) <> '';

BEGIN TRAN;

UPDATE a
SET a.CategoryCode = CAST(m.MAESOC_CODCAT AS varchar(100))
FROM dbo.Assignments a
INNER JOIN dbo.Employees e ON e.Id = a.EmployeeId
INNER JOIN Sindicato.fotos_.maesoc m ON CAST(m.MAESOC_CUIL AS bigint) =
     TRY_CAST(REPLACE(REPLACE(REPLACE(CAST(e.Dni AS varchar(20)),'-',''),'.',''),' ','') AS bigint)
WHERE a.IsActive = 1
  AND (a.CategoryCode IS NULL OR LTRIM(RTRIM(a.CategoryCode)) = '')
  AND m.MAESOC_CODCAT BETWEEN 1 AND 21;

SELECT 'FILAS ACTUALIZADAS = ' + CAST(@@ROWCOUNT AS varchar(9));

COMMIT;

SELECT 'DESPUES -> con categoria = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.Assignments WHERE CategoryCode IS NOT NULL AND LTRIM(RTRIM(CategoryCode)) <> '';
SELECT 'DESPUES -> sin categoria (activas) = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.Assignments WHERE IsActive = 1 AND (CategoryCode IS NULL OR LTRIM(RTRIM(CategoryCode)) = '');

SELECT 'LA PRUEBA: ABDALA LLAMIL' AS titulo;
SELECT e.Dni, e.FullName, a.CategoryCode FROM dbo.Assignments a
INNER JOIN dbo.Employees e ON e.Id = a.EmployeeId
WHERE a.CompanyId = 29656 AND a.IsActive = 1;

SELECT 'LOS QUE QUEDAN SIN RESOLVER' AS titulo;
SELECT e.Dni, e.FullName, a.CompanyId, a.UnionId FROM dbo.Assignments a
INNER JOIN dbo.Employees e ON e.Id = a.EmployeeId
WHERE a.IsActive = 1 AND (a.CategoryCode IS NULL OR LTRIM(RTRIM(a.CategoryCode)) = '');
