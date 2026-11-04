-- =====================================================================
-- SIRGA — Completar los datos del empleado desde el sistema anterior
-- ---------------------------------------------------------------------
-- QUÉ HACE
--   Llena las columnas nuevas de Employees (apellido, nombre, nacimiento,
--   sexo, documento, contacto, domicilio, afiliado y carnet) con lo que
--   ya está cargado en la copia local del sistema anterior:
--   Sindicato.dbo.fotos_.maesoc (49 columnas por socio).
--
-- CÓMO CRUZA
--   Employees.Dni es el CUIL (en el sistema anterior MAESOC_CUIL_STR).
--   Se compara por NÚMERO (TRY_CONVERT bigint) y no por texto: las dos
--   bases tienen cotejaciones distintas y comparar texto entre ellas falla.
--
-- SEGURIDAD
--   * SIMULACRO POR DEFECTO: con @Simulacro = 1 no escribe nada, sólo informa.
--   * IDEMPOTENTE: sólo escribe donde el campo está NULL o vacío.
--   * RESPALDO: guarda el estado anterior en dbo._BackupEmployeesDatosMaesoc
--     (se crea si no existe) y deja el script para volver atrás.
--   * NO BORRA NADA: no hay DELETE ni UPDATE de columnas que ya existían.
--
-- CÓMO SE CORRE (el modo se pasa SIEMPRE por parámetro; no hay valor escondido en el archivo)
--   Simulacro (no escribe):   sqlcmd -S localhost -d SIRGA_DB -E -v Simulacro=1 -i script_sirga_db_empleados_desde_maesoc.sql
--   Aplicar de verdad:        sqlcmd -S localhost -d SIRGA_DB -E -v Simulacro=0 -i script_sirga_db_empleados_desde_maesoc.sql
-- =====================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;   -- obligatorio para escribir en tablas con índice filtrado
SET XACT_ABORT ON;

DECLARE @Simulacro bit = $(Simulacro);   -- 1 = informa (no escribe) | 0 = escribe

-- ---------------------------------------------------------------------
-- 1. Padrón cruzado (sólo filas que tienen CUIL en las dos bases)
-- ---------------------------------------------------------------------
IF OBJECT_ID('tempdb..#cruce') IS NOT NULL DROP TABLE #cruce;

SELECT
    e.Id                                                    AS EmployeeId,
    NULLIF(LTRIM(RTRIM(m.MAESOC_APELLIDO)), '')             AS LastName,
    NULLIF(LTRIM(RTRIM(m.MAESOC_NOMBRE)), '')               AS FirstName,
    CASE WHEN m.MAESOC_FECHANAC >= '19000101' THEN m.MAESOC_FECHANAC END AS BirthDate,
    NULLIF(UPPER(LTRIM(RTRIM(m.MAESOC_SEXO))), '')          AS Sex,
    NULLIF(CONVERT(varchar(3), m.MAESOC_TIPODOC), '')       AS DocumentTypeCode,
    NULLIF(LTRIM(RTRIM(m.MAESOC_NRODOC)), '')               AS DocumentNumber,
    NULLIF(LTRIM(RTRIM(m.MAESOC_TEL)), '')                  AS Phone,
    NULLIF(LTRIM(RTRIM(m.MAESOC_TELCEL)), '')               AS Mobile,
    NULLIF(LTRIM(RTRIM(m.MAESOC_EMAIL)), '')                AS Email,
    NULLIF(LTRIM(RTRIM(m.MAESOC_EMAILLAB)), '')             AS WorkEmail,
    NULLIF(LTRIM(RTRIM(m.MAESOC_CALLE)), '')                AS Address,
    NULLIF(LTRIM(RTRIM(m.MAESOC_NROCALLE)), '')             AS AddressNumber,
    NULLIF(LTRIM(RTRIM(m.MAESOC_PISO)), '')                 AS Floor,
    NULLIF(LTRIM(RTRIM(m.MAESOC_DTO)), '')                  AS Apartment,
    NULLIF(LTRIM(RTRIM(m.MAESOC_BARRIO)), '')               AS Neighborhood,
    NULLIF(LTRIM(RTRIM(m.MAESOC_CODPOS)), '')               AS ZipCode,
    NULLIF(LTRIM(RTRIM(m.MAESOC_NROAFIL)), '')              AS AffiliationNumber,
    NULLIF(LTRIM(RTRIM(m.MAESOC_NROCARNET)), '')            AS CardNumber
INTO #cruce
FROM dbo.Employees e
JOIN Sindicato.fotos_.maesoc m
  ON TRY_CONVERT(bigint, LTRIM(RTRIM(m.MAESOC_CUIL_STR))) = TRY_CONVERT(bigint, e.Dni);

-- ---------------------------------------------------------------------
-- 2. Informe previo: qué se va a completar
-- ---------------------------------------------------------------------
PRINT '=== DATOS QUE HAY EN EL SISTEMA ANTERIOR (de los cruzados) ===';
SELECT
    (SELECT COUNT(*) FROM #cruce)                                    AS empleados_cruzados,
    (SELECT COUNT(*) FROM #cruce WHERE LastName IS NOT NULL)         AS con_apellido,
    (SELECT COUNT(*) FROM #cruce WHERE FirstName IS NOT NULL)        AS con_nombre,
    (SELECT COUNT(*) FROM #cruce WHERE BirthDate IS NOT NULL)        AS con_nacimiento,
    (SELECT COUNT(*) FROM #cruce WHERE Sex IS NOT NULL)              AS con_sexo,
    (SELECT COUNT(*) FROM #cruce WHERE DocumentNumber IS NOT NULL)   AS con_documento,
    (SELECT COUNT(*) FROM #cruce WHERE Mobile IS NOT NULL)           AS con_celular,
    (SELECT COUNT(*) FROM #cruce WHERE Email IS NOT NULL)            AS con_email,
    (SELECT COUNT(*) FROM #cruce WHERE Address IS NOT NULL)          AS con_domicilio,
    (SELECT COUNT(*) FROM #cruce WHERE AffiliationNumber IS NOT NULL) AS con_nro_afiliado;

PRINT '=== TIPOS DE DOCUMENTO (para saber qué códigos hay) ===';
SELECT DocumentTypeCode, COUNT(*) AS filas
FROM #cruce
GROUP BY DocumentTypeCode
ORDER BY filas DESC;

PRINT '=== SEXO ===';
SELECT Sex, COUNT(*) AS filas FROM #cruce GROUP BY Sex;

-- Cuántos empleados de SIRGA NO cruzan con el sistema anterior (se informan, no se tocan)
PRINT '=== EMPLEADOS DE SIRGA SIN DATOS EN EL SISTEMA ANTERIOR ===';
SELECT COUNT(*) AS sin_cruce
FROM dbo.Employees e
WHERE NOT EXISTS (SELECT 1 FROM #cruce c WHERE c.EmployeeId = e.Id);

-- ---------------------------------------------------------------------
-- 3. Respaldo del estado anterior (sólo cuando se va a escribir)
-- ---------------------------------------------------------------------
IF @Simulacro = 0 AND OBJECT_ID('dbo._BackupEmployeesDatosMaesoc') IS NULL
BEGIN
    SELECT e.Id, e.Dni, e.LastName, e.FirstName, e.BirthDate, e.Sex, e.DocumentTypeCode,
           e.DocumentNumber, e.Phone, e.Mobile, e.Email, e.WorkEmail, e.Address,
           e.AddressNumber, e.Floor, e.Apartment, e.Neighborhood, e.ZipCode,
           e.AffiliationNumber, e.CardNumber, GETDATE() AS RespaldoFecha
    INTO dbo._BackupEmployeesDatosMaesoc
    FROM dbo.Employees e
    WHERE EXISTS (SELECT 1 FROM #cruce c WHERE c.EmployeeId = e.Id);
    PRINT 'Respaldo creado: dbo._BackupEmployeesDatosMaesoc';
END
ELSE IF @Simulacro = 0
    PRINT 'Respaldo ya existía: dbo._BackupEmployeesDatosMaesoc (no se toca)';

-- ---------------------------------------------------------------------
-- 4. Escritura (o simulacro)
-- ---------------------------------------------------------------------
IF @Simulacro = 1
BEGIN
    PRINT '=== SIMULACRO: NO SE ESCRIBIÓ NADA. Para aplicar, poner @Simulacro = 0 ===';
    SELECT TOP 10 e.Id, e.Dni, c.LastName, c.FirstName, CONVERT(varchar(10), c.BirthDate, 103) AS Nacimiento,
           c.Sex, c.Mobile, c.Email, c.Address + ' ' + ISNULL(c.AddressNumber, '') AS Domicilio
    FROM dbo.Employees e
    JOIN #cruce c ON c.EmployeeId = e.Id
    ORDER BY e.Id;
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    UPDATE e SET
        LastName          = COALESCE(NULLIF(e.LastName, ''),          c.LastName),
        FirstName         = COALESCE(NULLIF(e.FirstName, ''),         c.FirstName),
        BirthDate         = COALESCE(e.BirthDate,                      c.BirthDate),
        Sex               = COALESCE(NULLIF(e.Sex, ''),               c.Sex),
        DocumentTypeCode  = COALESCE(NULLIF(e.DocumentTypeCode, ''),  c.DocumentTypeCode),
        DocumentNumber    = COALESCE(NULLIF(e.DocumentNumber, ''),    c.DocumentNumber),
        Phone             = COALESCE(NULLIF(e.Phone, ''),             c.Phone),
        Mobile            = COALESCE(NULLIF(e.Mobile, ''),            c.Mobile),
        Email             = COALESCE(NULLIF(e.Email, ''),             c.Email),
        WorkEmail         = COALESCE(NULLIF(e.WorkEmail, ''),         c.WorkEmail),
        Address           = COALESCE(NULLIF(e.Address, ''),           c.Address),
        AddressNumber     = COALESCE(NULLIF(e.AddressNumber, ''),     c.AddressNumber),
        Floor             = COALESCE(NULLIF(e.Floor, ''),             c.Floor),
        Apartment         = COALESCE(NULLIF(e.Apartment, ''),         c.Apartment),
        Neighborhood      = COALESCE(NULLIF(e.Neighborhood, ''),      c.Neighborhood),
        ZipCode           = COALESCE(NULLIF(e.ZipCode, ''),           c.ZipCode),
        AffiliationNumber = COALESCE(NULLIF(e.AffiliationNumber, ''), c.AffiliationNumber),
        CardNumber        = COALESCE(NULLIF(e.CardNumber, ''),        c.CardNumber)
    FROM dbo.Employees e
    JOIN #cruce c ON c.EmployeeId = e.Id;

    DECLARE @filas int = @@ROWCOUNT;
    COMMIT TRANSACTION;
    PRINT CONCAT('Filas actualizadas: ', @filas);
END

-- ---------------------------------------------------------------------
-- 5. Verificación: cómo quedó la tabla
-- ---------------------------------------------------------------------
PRINT '=== ESTADO FINAL DE Employees ===';
SELECT
    COUNT(*)                                          AS empleados,
    SUM(CASE WHEN LastName IS NOT NULL THEN 1 ELSE 0 END)          AS con_apellido,
    SUM(CASE WHEN FirstName IS NOT NULL THEN 1 ELSE 0 END)         AS con_nombre,
    SUM(CASE WHEN BirthDate IS NOT NULL THEN 1 ELSE 0 END)         AS con_nacimiento,
    SUM(CASE WHEN Sex IS NOT NULL THEN 1 ELSE 0 END)               AS con_sexo,
    SUM(CASE WHEN DocumentNumber IS NOT NULL THEN 1 ELSE 0 END)    AS con_documento,
    SUM(CASE WHEN Mobile IS NOT NULL THEN 1 ELSE 0 END)            AS con_celular,
    SUM(CASE WHEN Address IS NOT NULL THEN 1 ELSE 0 END)           AS con_domicilio,
    SUM(CASE WHEN AffiliationNumber IS NOT NULL THEN 1 ELSE 0 END) AS con_nro_afiliado
FROM dbo.Employees;

-- ---------------------------------------------------------------------
-- VOLVER ATRÁS (si hiciera falta)
--   UPDATE e SET LastName = b.LastName, FirstName = b.FirstName, BirthDate = b.BirthDate,
--          Sex = b.Sex, DocumentTypeCode = b.DocumentTypeCode, DocumentNumber = b.DocumentNumber,
--          Phone = b.Phone, Mobile = b.Mobile, Email = b.Email, WorkEmail = b.WorkEmail,
--          Address = b.Address, AddressNumber = b.AddressNumber, Floor = b.Floor,
--          Apartment = b.Apartment, Neighborhood = b.Neighborhood, ZipCode = b.ZipCode,
--          AffiliationNumber = b.AffiliationNumber, CardNumber = b.CardNumber
--   FROM dbo.Employees e
--   JOIN dbo._BackupEmployeesDatosMaesoc b ON b.Id = e.Id;
-- ---------------------------------------------------------------------
