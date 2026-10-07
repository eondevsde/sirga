-- SIRGA: quitar la provincia del NOMBRE del sindicato (la provincia tiene su propia columna).
--
-- POR QUE: el sindicato 19 se llamaba "SINDICATO EMPLEADOS DE COMERCIO SANTIAGO DEL ESTERO" y ademas tiene
-- JurisdictionProvince = "SANTIAGO DEL ESTERO". En la boleta eso salia DUPLICADO: el nombre ya la incluia y
-- ademas se imprime la jurisdiccion en su propia linea. El usuario pidio que la boleta muestre:
--     SINDICATO EMPLEADOS DE COMERCIO
--     SANTIAGO DEL ESTERO
-- asi que la provincia sale del nombre y queda solo en su columna.
--
-- IDEMPOTENTE: solo actualiza los sindicatos cuyo nombre TERMINA con su provincia; la segunda corrida
-- no cambia nada. RESPALDO: deja el valor anterior en dbo._BackupUnionName (y lo conserva si ya existe).
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

-- ---------------------------------------------------------------------------
-- SIMULACRO: que se va a cambiar
-- ---------------------------------------------------------------------------
SELECT 'SIMULACRO -> se renombran' AS paso,
       u.Id,
       u.Name AS nombre_actual,
       RTRIM(LEFT(u.Name, LEN(u.Name) - LEN(u.JurisdictionProvince))) AS nombre_nuevo,
       u.JurisdictionProvince AS provincia
FROM dbo.Unions u
WHERE u.JurisdictionProvince IS NOT NULL
  AND LEN(u.JurisdictionProvince) > 0
  AND u.Name LIKE '%' + u.JurisdictionProvince;

-- ---------------------------------------------------------------------------
-- RESPALDO (se conserva si ya existe)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo._BackupUnionName') IS NULL
BEGIN
    SELECT
        u.Id            AS UnionId,
        u.Name          AS NombreAnterior,
        GETDATE()       AS FechaRespaldo
    INTO dbo._BackupUnionName
    FROM dbo.Unions u
    WHERE u.JurisdictionProvince IS NOT NULL
      AND LEN(u.JurisdictionProvince) > 0
      AND u.Name LIKE '%' + u.JurisdictionProvince;

    SELECT 'RESPALDO -> filas = ' + CAST(COUNT(*) AS varchar(9)) FROM dbo._BackupUnionName;
END
ELSE
    SELECT 'RESPALDO -> ya existia, se conserva (filas = ' + CAST(COUNT(*) AS varchar(9)) + ')'
    FROM dbo._BackupUnionName;

-- ---------------------------------------------------------------------------
-- RENOMBRAR
-- ---------------------------------------------------------------------------
BEGIN TRAN;

UPDATE u
SET u.Name = RTRIM(LEFT(u.Name, LEN(u.Name) - LEN(u.JurisdictionProvince)))
FROM dbo.Unions u
WHERE u.JurisdictionProvince IS NOT NULL
  AND LEN(u.JurisdictionProvince) > 0
  AND u.Name LIKE '%' + u.JurisdictionProvince;

SELECT 'SINDICATOS RENOMBRADOS = ' + CAST(@@ROWCOUNT AS varchar(9));

COMMIT;

-- ---------------------------------------------------------------------------
-- DESPUES
-- ---------------------------------------------------------------------------
SELECT 'DESPUES' AS paso, Id, Name, JurisdictionCity AS ciudad, JurisdictionProvince AS provincia
FROM dbo.Unions ORDER BY Id;
