-- SIRGA: desactiva las ESCALAS DUPLICADAS sin borrar nada.
--
-- QUE PASA: hay 42 pares (periodo, categoria) con DOS escalas cada uno (84 filas de 1.339). Se verifico
-- que son COPIAS EXACTAS (mismos BasicAmount, AntiguedadPercentage, AgreementNR1 y AgreementNR2), todas
-- ACTIVAS, con ids consecutivos: parecen de una importacion repetida. Como las dos estan activas, "la
-- escala de esa categoria en ese periodo" es AMBIGUA: la consulta devuelve dos filas y usa cualquiera.
-- Hoy no cambia el resultado (los valores son iguales), pero impide poner un indice unico y es una
-- trampa esperando.
--
-- QUE HACE: **desactiva** la copia sobrante de cada par y conserva la de MENOR Id. No borra: la regla
-- del proyecto es desactivar o respaldar, nunca borrar datos.
--
-- OJO: para que esto SIRVA, las consultas tienen que mirar `IsActive` (antes lo ignoraban, asi que
-- desactivar una escala no hacia nada). Eso se arreglo en el codigo en el mismo cambio.
--
-- IDEMPOTENTE: la segunda corrida desactiva 0 filas.
-- RESPALDO: deja las filas afectadas en dbo._BackupSalaryScalesDuplicadas (y lo conserva si ya existe).
SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Obligatorio: SalaryScales tiene un indice FILTRADO y SQL Server no deja hacer UPDATE sobre una tabla
-- con indice filtrado si QUOTED_IDENTIFIER esta en OFF (sqlcmd lo trae OFF por defecto).
SET QUOTED_IDENTIFIER ON;

-- ---------------------------------------------------------------------------
-- RESPALDO: solo las filas que se van a desactivar
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo._BackupSalaryScalesDuplicadas') IS NULL
BEGIN
    SELECT
        s.Id                    AS SalaryScaleId,
        s.Period,
        s.EmployeeCategoryId,
        s.BasicAmount,
        s.AntiguedadPercentage,
        s.AgreementNR1,
        s.AgreementNR2,
        s.IsActive              AS EstadoAnterior,
        GETDATE()               AS FechaRespaldo
    INTO dbo._BackupSalaryScalesDuplicadas
    FROM dbo.SalaryScales s
    WHERE s.IsActive = 1
      AND EXISTS (SELECT 1 FROM dbo.SalaryScales otro
                  WHERE otro.Period = s.Period
                    AND otro.EmployeeCategoryId = s.EmployeeCategoryId
                    AND otro.IsActive = 1
                    AND otro.Id < s.Id);

    SELECT 'RESPALDO -> filas a desactivar = ' + CAST(COUNT(*) AS varchar(9))
    FROM dbo._BackupSalaryScalesDuplicadas;
END
ELSE
    SELECT 'RESPALDO -> ya existia, se conserva (filas = ' + CAST(COUNT(*) AS varchar(9)) + ')'
    FROM dbo._BackupSalaryScalesDuplicadas;

-- ---------------------------------------------------------------------------
-- ANTES
-- ---------------------------------------------------------------------------
SELECT 'ANTES -> escalas activas = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.SalaryScales WHERE IsActive = 1;

SELECT 'ANTES -> pares (periodo, categoria) con MAS DE UNA activa = ' + CAST(COUNT(*) AS varchar(9))
FROM (SELECT Period, EmployeeCategoryId FROM dbo.SalaryScales
      WHERE IsActive = 1 GROUP BY Period, EmployeeCategoryId HAVING COUNT(*) > 1) x;

-- ---------------------------------------------------------------------------
-- DESACTIVAR LAS COPIAS
-- ---------------------------------------------------------------------------
BEGIN TRAN;

-- Se conserva la de menor Id de cada (periodo, categoria) y se desactiva el resto.
UPDATE s
SET s.IsActive = 0
FROM dbo.SalaryScales s
WHERE s.IsActive = 1
  AND EXISTS (SELECT 1 FROM dbo.SalaryScales otro
              WHERE otro.Period = s.Period
                AND otro.EmployeeCategoryId = s.EmployeeCategoryId
                AND otro.IsActive = 1
                AND otro.Id < s.Id);

SELECT 'FILAS DESACTIVADAS = ' + CAST(@@ROWCOUNT AS varchar(9));

COMMIT;

-- ---------------------------------------------------------------------------
-- DESPUES
-- ---------------------------------------------------------------------------
SELECT 'DESPUES -> escalas activas = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.SalaryScales WHERE IsActive = 1;
SELECT 'DESPUES -> escalas inactivas = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.SalaryScales WHERE IsActive = 0;
SELECT 'DESPUES -> filas totales (no se borro ninguna) = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.SalaryScales;

SELECT 'DESPUES -> pares con mas de una activa (tiene que dar 0) = ' + CAST(COUNT(*) AS varchar(9))
FROM (SELECT Period, EmployeeCategoryId FROM dbo.SalaryScales
      WHERE IsActive = 1 GROUP BY Period, EmployeeCategoryId HAVING COUNT(*) > 1) x;

-- Control de regresion: la escala de MAESTRANZA B (categoria 13) del 2026-03 tiene que seguir activa y
-- con el mismo importe (es la que usa la DDJJ de ABDALA LLAMIL).
SELECT 'CONTROL -> escala de MAESTRANZA B 2026-03',
       CONVERT(varchar(10), s.Period, 23) AS periodo,
       c.Code AS categoria,
       s.BasicAmount,
       s.IsActive
FROM dbo.SalaryScales s
INNER JOIN dbo.EmployeeCategories c ON c.Id = s.EmployeeCategoryId
WHERE c.Code = '13' AND s.Period = '20260301';
