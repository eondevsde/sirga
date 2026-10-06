-- SIRGA: revierte las categorias que se habian creado para el sindicato 20 (La Banda).
-- Motivo: la lista de categorias del sistema anterior (categorias_empleado) es del Sindicato
-- Empleados de Comercio de Santiago del Estero (sindicato 19). Asignarsela a La Banda fue una
-- inferencia, no un dato. La Banda no tiene empleados asignados, asi que no afecta nada en uso.
-- Idempotente: si no hay filas, no hace nada. Respaldo previo: dbo._BackupEmployeeCategories.
SET NOCOUNT ON;
SET XACT_ABORT ON;

SELECT 'ANTES: categorias del sindicato 20 = ' + CAST(COUNT(*) AS varchar(4)) FROM dbo.EmployeeCategories WHERE UnionId = 20;
SELECT 'ANTES: asignaciones del sindicato 20 = ' + CAST(COUNT(*) AS varchar(6)) FROM dbo.Assignments WHERE UnionId = 20;

BEGIN TRAN;

DELETE e FROM dbo.EmployeeCategories e
WHERE e.UnionId = 20
  AND NOT EXISTS (SELECT 1 FROM dbo._BackupEmployeeCategories b WHERE b.UnionId = 20 AND TRY_CAST(b.Code AS int) = TRY_CAST(e.Code AS int));

SELECT 'FILAS BORRADAS = ' + CAST(@@ROWCOUNT AS varchar(4));

COMMIT;

SELECT 'DESPUES: categorias del sindicato 20 = ' + CAST(COUNT(*) AS varchar(4)) FROM dbo.EmployeeCategories WHERE UnionId = 20;
SELECT 'DESPUES: sindicato 19 (el que importa) = ' + CAST(COUNT(*) AS varchar(4)) FROM dbo.EmployeeCategories WHERE UnionId = 19;
SELECT 'DESPUES: empleados con categoria = ' + CAST(COUNT(*) AS varchar(7)) FROM dbo.Assignments WHERE CategoryCode IS NOT NULL AND LTRIM(RTRIM(CategoryCode)) <> '';
