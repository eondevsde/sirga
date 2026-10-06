-- SIRGA: trae la lista de categorias del sistema anterior (categorias_empleado) a EmployeeCategories.
-- Idempotente: completa lo que falta y actualiza nombres, sin duplicar.
--   - Sindicato 19 (Santiago del Estero): ya tiene las 21; se corrigen los nombres si difieren.
--   - Sindicato 20 (La Banda): NO tiene ninguna; se crean las 21.
--   - El codigo 0 = 'TODAS' del legacy NO se migra: es una opcion de filtro, no una categoria, y
--     ningun empleado lo usa (verificado: 0 socios con MAESOC_CODCAT = 0).
-- Respaldo del estado anterior en dbo._BackupEmployeeCategories.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('dbo._BackupEmployeeCategories') IS NOT NULL DROP TABLE dbo._BackupEmployeeCategories;
SELECT Id, UnionId, Code, Name, IsActive INTO dbo._BackupEmployeeCategories FROM dbo.EmployeeCategories;

SELECT 'ANTES: sindicato 19 = ' + CAST(COUNT(*) AS varchar(4)) FROM dbo.EmployeeCategories WHERE UnionId = 19;
SELECT 'ANTES: sindicato 20 = ' + CAST(COUNT(*) AS varchar(4)) FROM dbo.EmployeeCategories WHERE UnionId = 20;

BEGIN TRAN;

UPDATE e SET e.Name = l.MAECAT_NOMCAT
FROM dbo.EmployeeCategories e
INNER JOIN Sindicato.dbo.categorias_empleado l ON TRY_CAST(e.Code AS int) = l.MAECAT_CODCAT
WHERE e.UnionId = 19 AND l.MAECAT_CODCAT >= 1
  AND e.Name COLLATE DATABASE_DEFAULT <> l.MAECAT_NOMCAT COLLATE DATABASE_DEFAULT;

SELECT 'NOMBRES CORREGIDOS (sindicato 19) = ' + CAST(@@ROWCOUNT AS varchar(4));

INSERT INTO dbo.EmployeeCategories (Code, Name, UnionId, IsActive)
SELECT CAST(l.MAECAT_CODCAT AS nvarchar(50)), l.MAECAT_NOMCAT COLLATE DATABASE_DEFAULT, 20, 1
FROM Sindicato.dbo.categorias_empleado l
WHERE l.MAECAT_CODCAT >= 1
  AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeCategories e
                  WHERE e.UnionId = 20 AND TRY_CAST(e.Code AS int) = l.MAECAT_CODCAT);

SELECT 'CATEGORIAS CREADAS (sindicato 20) = ' + CAST(@@ROWCOUNT AS varchar(4));

COMMIT;

SELECT 'DESPUES: sindicato 19 = ' + CAST(COUNT(*) AS varchar(4)) FROM dbo.EmployeeCategories WHERE UnionId = 19;
SELECT 'DESPUES: sindicato 20 = ' + CAST(COUNT(*) AS varchar(4)) FROM dbo.EmployeeCategories WHERE UnionId = 20;

SELECT 'LA BANDA: empleados asignados (¿le hacía falta?) = ' + CAST(COUNT(*) AS varchar(6))
FROM dbo.Assignments WHERE UnionId = 20;
SELECT 'LA BANDA: empresas distintas = ' + CAST(COUNT(DISTINCT CompanyId) AS varchar(6))
FROM dbo.Assignments WHERE UnionId = 20;
