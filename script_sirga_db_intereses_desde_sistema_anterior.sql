-- SIRGA: migra las TASAS DE INTERES del sistema anterior a dbo.InterestRates.
--
-- ORIGEN: base Sindicato, tablas dbo.Intereses (70 filas) y dbo.Intereses_ (52 filas).
--   OJO: Intereses_ NO es un subconjunto de Intereses. Tiene 6 filas que la principal no tiene, y 4 de
--   ellas TAPAN HUECOS de la escala (2004-06-01 tipo 1, 2004-09-01 tipos 1 y 2, 2006-07-01 tipo 2).
--   Las otras 2 (2019-08-01) ya estan cubiertas por el rango estirado 2019-07-01..2019-09-30 de la
--   principal, con la MISMA tasa, asi que agregarlas crearia un solapamiento: no se agregan.
--   Verificado: las dos tablas coinciden en la tasa donde se solapan, y el conjunto final queda con
--   74 filas, 0 huecos y 0 solapamientos (37 por cada serie).
--
-- MAPEO (y por que):
--   Desde        -> EffectiveDate
--   Mensual/100  -> MonthlyRate   *** en SIRGA la tasa es FRACCION (0,0275 = 2,75%) y el sistema
--                                     anterior la guarda en PORCENTAJE (2,75). Sin el /100 las tasas
--                                     quedan 100 veces mas grandes. ***
--   Diario/100   -> DailyRate     (se guarda el dato tal cual venia; NO se usa para el recargo)
--   Hasta        -> ValidTo       (2999-12-31 significa "sin fin" -> NULL)
--   TipoDeInteres-> TypeOfInterest (1 = resarcitorio = la mas baja, 2 = punitorio = la mas alta)
--   Norma        -> Norm
--
-- IDEMPOTENTE: solo inserta las combinaciones (EffectiveDate, TypeOfInterest) que no esten ya, asi se
-- puede correr las veces que haga falta: la segunda corrida no cambia nada.
-- RESPALDO: deja el estado anterior en dbo._BackupInterestRates (y lo conserva si ya existe).
--
-- OJO CON LAS FECHAS: la sesion viene con dateformat=dmy, asi que NO se usan literales 'yyyy-mm-dd'
-- contra columnas datetime (se malinterpretan). El "sin fin" se detecta con YEAR(Hasta) >= 2999.
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ---------------------------------------------------------------------------
-- Origen consolidado: la principal + las filas de la copia que tapan huecos
-- ---------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#InteresesLegacy') IS NOT NULL DROP TABLE #InteresesLegacy;

SELECT
    Desde, Hasta, TipoDeInteres, Mensual, Diario, Norma
INTO #InteresesLegacy
FROM
(
    SELECT Desde, Hasta, TipoDeInteres, Mensual, Diario, Norma FROM Sindicato.dbo.Intereses
    UNION ALL
    SELECT b.Desde, b.Hasta, b.TipoDeInteres, b.Mensual, b.Diario, b.Norma
    FROM Sindicato.dbo.Intereses_ b
    WHERE NOT EXISTS (
        SELECT 1 FROM Sindicato.dbo.Intereses i
        WHERE i.TipoDeInteres = b.TipoDeInteres AND b.Desde BETWEEN i.Desde AND i.Hasta)
) x;

SELECT 'ORIGEN -> filas a migrar = ' + CAST(COUNT(*) AS varchar(9)) FROM #InteresesLegacy;
SELECT 'ORIGEN -> por serie', TipoDeInteres, COUNT(*) AS filas,
       CAST(MIN(Mensual) AS decimal(18,2)) AS mensual_min, CAST(MAX(Mensual) AS decimal(18,2)) AS mensual_max
FROM #InteresesLegacy GROUP BY TipoDeInteres ORDER BY TipoDeInteres;

-- Control del origen: en las 74 filas la tasa diaria tiene que ser la mensual / 30.
SELECT 'ORIGEN -> filas donde Diario <> Mensual/30 = ' + CAST(COUNT(*) AS varchar(9))
FROM #InteresesLegacy WHERE ABS(Diario - Mensual / 30.0) > 0.000001;

-- ---------------------------------------------------------------------------
-- RESPALDO (se conserva si ya existe: recrearlo en una segunda corrida lo dejaria vacio)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo._BackupInterestRates') IS NULL
BEGIN
    SELECT Id, EffectiveDate, MonthlyRate, DailyRate, ValidTo, TypeOfInterest, Norm, Description,
           GETDATE() AS FechaRespaldo
    INTO dbo._BackupInterestRates
    FROM dbo.InterestRates;

    SELECT 'RESPALDO -> creado con filas = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo._BackupInterestRates;
END
ELSE
    SELECT 'RESPALDO -> ya existia, se conserva (filas = ' + CAST(COUNT(*) AS varchar(9)) + ')'
    FROM dbo._BackupInterestRates;

SELECT 'ANTES -> filas en InterestRates = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.InterestRates;

-- ---------------------------------------------------------------------------
-- MIGRACION
-- ---------------------------------------------------------------------------
BEGIN TRAN;

INSERT INTO dbo.InterestRates (EffectiveDate, MonthlyRate, DailyRate, ValidTo, TypeOfInterest, Norm, Description)
SELECT
    l.Desde,
    CAST(l.Mensual / 100.0 AS decimal(18,6)),
    CAST(l.Diario  / 100.0 AS decimal(18,8)),
    CASE WHEN YEAR(l.Hasta) >= 2999 THEN NULL ELSE l.Hasta END,
    l.TipoDeInteres,
    LEFT(l.Norma, 100),
    'Migrada del sistema anterior'
FROM #InteresesLegacy l
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.InterestRates r
    WHERE r.EffectiveDate = l.Desde AND r.TypeOfInterest = l.TipoDeInteres);

SELECT 'FILAS INSERTADAS = ' + CAST(@@ROWCOUNT AS varchar(9));

COMMIT;

-- ---------------------------------------------------------------------------
-- NORMALIZACION DE VIGENCIAS ABIERTAS
--
-- La fila por defecto que ya existia (1900-01-01, sin ValidTo) queda abierta "para siempre" y se
-- SOLAPA con todas las tasas reales posteriores: al buscar la tasa de una fecha por su rango de
-- vigencia aparecen DOS filas (la de 1900 y la real), que es exactamente la ambiguedad que hay que
-- evitar. Se cierra cualquier tasa abierta que tenga otra posterior de la misma serie, el dia antes.
-- Es idempotente: despues de la primera corrida ya no queda ninguna en esa situacion.
-- ---------------------------------------------------------------------------
UPDATE r
SET ValidTo = DATEADD(day, -1, n.ProximoDesde)
FROM dbo.InterestRates r
CROSS APPLY (
    SELECT MIN(x.EffectiveDate) AS ProximoDesde
    FROM dbo.InterestRates x
    WHERE x.TypeOfInterest = r.TypeOfInterest AND x.EffectiveDate > r.EffectiveDate
) n
WHERE r.ValidTo IS NULL AND n.ProximoDesde IS NOT NULL;

SELECT 'VIGENCIAS ABIERTAS CERRADAS = ' + CAST(@@ROWCOUNT AS varchar(9));

-- ---------------------------------------------------------------------------
-- VERIFICACION
-- ---------------------------------------------------------------------------
SELECT 'DESPUES -> filas en InterestRates = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo.InterestRates;

SELECT 'DESPUES -> por serie', TypeOfInterest, COUNT(*) AS filas
FROM dbo.InterestRates GROUP BY TypeOfInterest ORDER BY TypeOfInterest;

-- La escala tiene que quedar sin huecos y sin solapamientos, por serie.
;WITH g AS (
    SELECT TypeOfInterest, EffectiveDate, ValidTo,
           LAG(ISNULL(ValidTo, CONVERT(datetime, '29991231'))) OVER (PARTITION BY TypeOfInterest ORDER BY EffectiveDate) AS VigenciaPrevia
    FROM dbo.InterestRates
)
SELECT 'CONTROL -> huecos en la escala = ' + CAST(COUNT(*) AS varchar(9))
FROM g WHERE VigenciaPrevia IS NOT NULL AND DATEDIFF(day, VigenciaPrevia, EffectiveDate) > 1;

SELECT 'CONTROL -> solapamientos = ' + CAST(COUNT(*) AS varchar(9))
FROM dbo.InterestRates a
JOIN dbo.InterestRates b
  ON a.TypeOfInterest = b.TypeOfInterest AND a.EffectiveDate < b.EffectiveDate
 AND a.EffectiveDate < ISNULL(b.ValidTo, CONVERT(datetime, '29991231'))
 AND b.EffectiveDate < ISNULL(a.ValidTo, CONVERT(datetime, '29991231'));

-- La prueba real: el recargo de GAMMAROTA (644,96 sobre 72.703,76 en los 10 dias entre vencimientos).
-- La tasa que corresponde es la resarcitoria (la mas baja) vigente al 12/08/2025, y el prorrateo es por
-- los DIAS DEL MES del vencimiento (agosto = 31), NO por 30.
DECLARE @importe decimal(18,2) = 72703.76, @real decimal(18,2) = 644.96, @dias int = 10;
SELECT
    'PRUEBA GAMMAROTA' AS caso,
    r.MonthlyRate AS tasa_mensual_fraccion,
    CAST(r.MonthlyRate * 100 AS decimal(18,4)) AS tasa_mensual_pct,
    r.Norm AS norma,
    CAST(ROUND(@importe * r.MonthlyRate * @dias / 31.0, 2) AS decimal(18,2)) AS recargo_calculado,
    @real AS recargo_real,
    CAST(@real - ROUND(@importe * r.MonthlyRate * @dias / 31.0, 2) AS decimal(18,2)) AS diferencia
FROM dbo.InterestRates r
WHERE r.TypeOfInterest = 1
  AND r.EffectiveDate <= CONVERT(datetime, '20250812')
  AND (r.ValidTo IS NULL OR r.ValidTo >= CONVERT(datetime, '20250812'));

DROP TABLE #InteresesLegacy;
