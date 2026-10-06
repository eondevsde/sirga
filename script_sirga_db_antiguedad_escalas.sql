-- SIRGA: carga el 1% de antiguedad en las escalas salariales.
--
-- POR QUE: SalaryScales.AntiguedadPercentage estaba en 0 en las 1.339 escalas, asi que la antiguedad
-- sumaba $0 y el minimo de escala quedaba por debajo del real. La resolucion oficial dice
-- "ANTIGUEDAD: ES EL 1% POR AÑO TRABAJADO" y el propio campo del modelo lo comenta.
-- El campo queda configurable por escala: esto solo llena las que estan en 0.
--
-- IDEMPOTENTE: solo toca las escalas que estan en 0. Se puede correr las veces que haga falta: la
-- segunda corrida no cambia nada.
-- RESPALDO: guarda en dbo._BackupSalaryScalesAntiguedad las filas que modifica, con el valor
-- anterior, para poder revertir.
--
-- Para revertir:
--   UPDATE s SET s.AntiguedadPercentage = b.PorcentajeAnterior
--   FROM dbo.SalaryScales s
--   INNER JOIN dbo._BackupSalaryScalesAntiguedad b ON b.SalaryScaleId = s.Id;
SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Obligatorio: SalaryScales tiene un indice FILTRADO y SQL Server no deja hacer UPDATE sobre una tabla
-- con indice filtrado si QUOTED_IDENTIFIER esta en OFF (sqlcmd lo trae OFF por defecto).
SET QUOTED_IDENTIFIER ON;

DECLARE @porcentaje decimal(18,2) = 1.00;

-- ---------------------------------------------------------------------------
-- RESPALDO: solo de las filas que se van a modificar.
-- Si el respaldo YA existe se CONSERVA: recrearlo en una segunda corrida lo dejaria vacio (ya no
-- quedan filas en 0) y se perderia la posibilidad de revertir. Eso seria pisar el respaldo.
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo._BackupSalaryScalesAntiguedad') IS NULL
BEGIN
    SELECT
        s.Id                                            AS SalaryScaleId,
        ec.RamaId,
        s.Period,
        s.AntiguedadPercentage                          AS PorcentajeAnterior,
        GETDATE()                                       AS FechaRespaldo
    INTO dbo._BackupSalaryScalesAntiguedad
    FROM dbo.SalaryScales s
    INNER JOIN dbo.EmployeeCategories ec ON ec.Id = s.EmployeeCategoryId
    WHERE s.AntiguedadPercentage = 0;

    SELECT 'RESPALDO -> creado con filas = ' + CAST(COUNT(*) AS varchar(9))
    FROM dbo._BackupSalaryScalesAntiguedad;
END
ELSE
BEGIN
    SELECT 'RESPALDO -> ya existia, se conserva (filas = ' + CAST(COUNT(*) AS varchar(9)) + ')'
    FROM dbo._BackupSalaryScalesAntiguedad;
END

SELECT 'ANTES -> escalas con antiguedad cargada = ' + CAST(COUNT(*) AS varchar(9))
FROM dbo.SalaryScales WHERE AntiguedadPercentage <> 0;

-- ---------------------------------------------------------------------------
-- UPDATE
-- ---------------------------------------------------------------------------
BEGIN TRAN;

UPDATE dbo.SalaryScales
SET AntiguedadPercentage = @porcentaje
WHERE AntiguedadPercentage = 0;

SELECT 'FILAS ACTUALIZADAS = ' + CAST(@@ROWCOUNT AS varchar(9));

COMMIT;

-- ---------------------------------------------------------------------------
-- VERIFICACION
-- ---------------------------------------------------------------------------
SELECT 'DESPUES -> escalas con antiguedad cargada = ' + CAST(COUNT(*) AS varchar(9))
FROM dbo.SalaryScales WHERE AntiguedadPercentage <> 0;

SELECT 'DESPUES -> escalas que siguen en 0 = ' + CAST(COUNT(*) AS varchar(9))
FROM dbo.SalaryScales WHERE AntiguedadPercentage = 0;

PRINT '=== LA PRUEBA: ABDALA LLAMIL (empresa 29656) — minimo de escala por periodo vigente ===';
-- OJO: desde el modelo de ramas, las categorias y las escalas cuelgan de la RAMA del sindicato, no del
-- sindicato (`SalaryScales` ya no tiene `UnionId`). Se llega por: asignacion -> sindicato -> rama ->
-- categoria (por codigo) -> escala del periodo.
;WITH ultimo AS (
    SELECT ec.RamaId, MAX(s.Period) AS Periodo
    FROM dbo.SalaryScales s
    INNER JOIN dbo.EmployeeCategories ec ON ec.Id = s.EmployeeCategoryId
    WHERE s.IsActive = 1
    GROUP BY ec.RamaId
),
base AS (
    SELECT
        e.FullName,
        ec.Name                                       AS categoria,
        s.BasicAmount                                 AS basico,
        s.AntiguedadPercentage                        AS pct,
        s.AgreementNR1                                AS nr1,
        s.AgreementNR2                                AS nr2,
        a.Jornada,
        a.CreatedAt                                   AS ingreso,
        u.Periodo,
        CASE
            WHEN (MONTH(u.Periodo) < MONTH(a.CreatedAt))
              OR (MONTH(u.Periodo) = MONTH(a.CreatedAt) AND DAY(u.Periodo) < DAY(a.CreatedAt))
            THEN DATEDIFF(year, a.CreatedAt, u.Periodo) - 1
            ELSE DATEDIFF(year, a.CreatedAt, u.Periodo)
        END                                           AS anios,
        CASE WHEN a.Jornada = 2 THEN 0.5 ELSE 1.0 END AS factor_jornada
    FROM dbo.Assignments a
    INNER JOIN dbo.Employees e           ON e.Id = a.EmployeeId
    INNER JOIN dbo.Unions un             ON un.Id = a.UnionId
    INNER JOIN ultimo u                  ON u.RamaId = un.RamaId
    INNER JOIN dbo.EmployeeCategories ec ON ec.RamaId = un.RamaId AND ec.Code = a.CategoryCode
    INNER JOIN dbo.SalaryScales s        ON s.EmployeeCategoryId = ec.Id AND s.Period = u.Periodo AND s.IsActive = 1
    WHERE a.CompanyId = 29656 AND a.IsActive = 1
)
SELECT
    FullName                                   AS empleado,
    categoria,
    CONVERT(varchar(10), ingreso, 23)          AS ingreso,
    anios                                      AS anios_antiguedad,
    pct                                        AS pct_antiguedad,
    basico                                     AS basico,
    CAST(ROUND(basico * (pct/100) * anios, 2) AS decimal(18,2)) AS antiguedad,
    CAST(ROUND((basico + ROUND(basico * (pct/100) * anios, 2) + nr1 + nr2) * factor_jornada, 2) AS decimal(18,2)) AS minimo_escala
FROM base
ORDER BY FullName;
