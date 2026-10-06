-- SIRGA: crea la rama de Comercio y vincula los sindicatos que le corresponden.
-- Paso 2 del modelo de ramas (paso 1 fue crear la tabla `Ramas`).
--
-- POR QUE: las escalas salariales son NACIONALES y dependen de la RAMA (el convenio), no del sindicato
-- ni de la ciudad. Hoy la misma escala nacional hay que copiarla por sindicato, que es el error que este
-- modelo corrige. Este paso deja el VINCULO; los pasos siguientes mueven `EmployeeCategories` y
-- `SalaryScales` para que cuelguen de la rama, y recien despues las consultas.
--
-- IDEMPOTENTE: se puede correr las veces que haga falta.
--
-- OJO CON LA BANDA: el sindicato 20 tiene el `CctCode` en **'PENDIENTE'** (dato sin completar). Se
-- vincula igual a Comercio porque su nombre es "SINDICATO EMPLEADO DE COMERCIO DE LA CIUDAD DE LA
-- BANDA" y porque la documentacion de la entidad `Rama` dice que Santiago del Estero y La Banda
-- comparten Comercio 130/75. **Queda pendiente completarle el CctCode.**
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ---------------------------------------------------------------------------
-- 1) La rama
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Ramas WHERE Code = '130/75')
BEGIN
    INSERT INTO dbo.Ramas (Code, Name, Notes, IsActive)
    VALUES ('130/75', 'Comercio', 'Empleados de Comercio. Escalas nacionales de la rama.', 1);

    SELECT 'RAMA -> creada: Comercio (130/75)';
END
ELSE
    SELECT 'RAMA -> ya existia (130/75): se conserva';

-- ---------------------------------------------------------------------------
-- 2) El vinculo (solo los que no tienen rama: asi no pisa una asignacion hecha a mano)
-- ---------------------------------------------------------------------------
DECLARE @ramaId int = (SELECT Id FROM dbo.Ramas WHERE Code = '130/75');

UPDATE u
SET u.RamaId = @ramaId
FROM dbo.Unions u
WHERE u.RamaId IS NULL
  -- UPPER() para no depender de si la cotejacion es sensible a mayusculas.
  AND (u.CctCode = '130/75' OR UPPER(u.Name) LIKE '%COMERCIO%');

SELECT 'SINDICATOS VINCULADOS = ' + CAST(@@ROWCOUNT AS varchar(9));

-- ---------------------------------------------------------------------------
-- 3) Verificacion
-- ---------------------------------------------------------------------------
SELECT 'RESULTADO', u.Id AS sindicato, LEFT(u.Name, 45) AS nombre, u.CctCode, u.RamaId, r.Name AS rama
FROM dbo.Unions u
LEFT JOIN dbo.Ramas r ON r.Id = u.RamaId
ORDER BY u.Id;

SELECT 'SIN RAMA = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.Unions WHERE RamaId IS NULL;
