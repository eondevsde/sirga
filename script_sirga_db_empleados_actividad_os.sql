-- =====================================================================
-- SIRGA — Actividad, obra social, nro. de legajo, jubilado y carga familiar
-- ---------------------------------------------------------------------
-- QUÉ HACE
--   1. Carga los dos catálogos que faltaban, copiados del sistema anterior:
--        dbo.Activities       <- Sindicato.dbo.actividad   (349 filas)
--        dbo.HealthInsurances <- Sindicato.dbo.obrasocial  (297 filas)
--   2. Completa las columnas nuevas:
--        Employees.HealthInsuranceCode  <- maesoc.MAESOC_CODOSM
--        Employees.IsRetired            <- maesoc.MAESOC_JUBIL   (0 = no, 1/2 = sí)
--        Employees.HasFamilyCharge      <- maesoc.MAESOC_CARGAFLIA (0/1)
--        Assignments.ActivityCode       <- maesoc.MAESOC_CODACT
--        Assignments.FileNumber         <- socemp.SOCEMP_NROLEG
--
-- SEGURIDAD
--   * SIMULACRO POR DEFECTO (@Simulacro = 1 no escribe nada).
--   * IDEMPOTENTE: catálogos sólo si el código no existe; columnas sólo donde están vacías.
--   * RESPALDO en dbo._BackupEmployeesActividadOs y dbo._BackupAssignmentsActividadLegajo.
--   * NO BORRA NADA.
--
-- CÓMO SE CORRE
--   Simulacro: sqlcmd -S localhost -d SIRGA_DB -E -v Simulacro=1 -i script_sirga_db_empleados_actividad_os.sql
--   Aplicar:   sqlcmd -S localhost -d SIRGA_DB -E -v Simulacro=0 -i script_sirga_db_empleados_actividad_os.sql
-- =====================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Simulacro bit = $(Simulacro);

PRINT '=== LO QUE HAY EN EL SISTEMA ANTERIOR ===';
SELECT
    (SELECT COUNT(*) FROM Sindicato.dbo.actividad) AS actividades_en_catalogo,
    (SELECT COUNT(*) FROM Sindicato.dbo.obrasocial) AS obras_sociales_en_catalogo,
    (SELECT COUNT(*) FROM Sindicato.fotos_.maesoc WHERE MAESOC_CODOSM IS NOT NULL AND MAESOC_CODOSM <> 0) AS socios_con_os,
    (SELECT COUNT(*) FROM Sindicato.fotos_.maesoc WHERE MAESOC_JUBIL > 0) AS socios_jubilados,
    (SELECT COUNT(*) FROM Sindicato.fotos_.maesoc WHERE MAESOC_CARGAFLIA > 0) AS socios_con_carga,
    (SELECT COUNT(*) FROM Sindicato.fotos_.socemp WHERE LTRIM(RTRIM(ISNULL(SOCEMP_NROLEG,''))) <> '') AS legajos;

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------
-- 1. Catálogos
-- ---------------------------------------------------------------------
INSERT INTO dbo.Activities (Code, Name, IsActive)
SELECT LTRIM(RTRIM(CONVERT(varchar(20), a.MAEACT_CODACT))) AS Code,
       ISNULL(NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(200), a.MAEACT_NOMACT))), ''), '(sin descripción)') AS Name,
       1
FROM Sindicato.dbo.actividad a
WHERE a.MAEACT_CODACT IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Activities d WHERE d.Code = LTRIM(RTRIM(CONVERT(varchar(20), a.MAEACT_CODACT))))
GROUP BY a.MAEACT_CODACT, a.MAEACT_NOMACT;

DECLARE @actividades int = @@ROWCOUNT;

INSERT INTO dbo.HealthInsurances (Code, Name, Acronym, IsActive)
SELECT LTRIM(RTRIM(CONVERT(varchar(20), o.MAEOSM_CODOSM))) AS Code,
       ISNULL(NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(250), o.MAEOSM_DESCRI))), ''), '(sin descripción)') AS Name,
       NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(30), o.MAEOSM_SIGLA))), '') AS Acronym,
       1
FROM Sindicato.dbo.obrasocial o
WHERE o.MAEOSM_CODOSM IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.HealthInsurances d WHERE d.Code = LTRIM(RTRIM(CONVERT(varchar(20), o.MAEOSM_CODOSM))))
GROUP BY o.MAEOSM_CODOSM, o.MAEOSM_DESCRI, o.MAEOSM_SIGLA;

DECLARE @obras int = @@ROWCOUNT;

-- ---------------------------------------------------------------------
-- 2. Respaldo del estado anterior (sólo al escribir)
-- ---------------------------------------------------------------------
IF @Simulacro = 0
BEGIN
    IF OBJECT_ID('dbo._BackupEmployeesActividadOs') IS NULL
    BEGIN
        SELECT Id, Dni, HealthInsuranceCode, IsRetired, HasFamilyCharge, GETDATE() AS RespaldoFecha
        INTO dbo._BackupEmployeesActividadOs
        FROM dbo.Employees;
        PRINT 'Respaldo creado: dbo._BackupEmployeesActividadOs';
    END

    IF OBJECT_ID('dbo._BackupAssignmentsActividadLegajo') IS NULL
    BEGIN
        SELECT Id, EmployeeId, CompanyId, ActivityCode, FileNumber, GETDATE() AS RespaldoFecha
        INTO dbo._BackupAssignmentsActividadLegajo
        FROM dbo.Assignments;
        PRINT 'Respaldo creado: dbo._BackupAssignmentsActividadLegajo';
    END
END

-- ---------------------------------------------------------------------
-- 3. Datos del empleado
-- ---------------------------------------------------------------------
UPDATE e SET
    HealthInsuranceCode = CASE WHEN m.MAESOC_CODOSM IS NULL OR m.MAESOC_CODOSM = 0
                               THEN NULL
                               ELSE LTRIM(RTRIM(CONVERT(varchar(20), m.MAESOC_CODOSM))) END,
    IsRetired       = CASE WHEN m.MAESOC_JUBIL > 0 THEN 1 ELSE 0 END,
    HasFamilyCharge = CASE WHEN m.MAESOC_CARGAFLIA > 0 THEN 1 ELSE 0 END
FROM dbo.Employees e
JOIN Sindicato.fotos_.maesoc m
  ON TRY_CONVERT(bigint, LTRIM(RTRIM(m.MAESOC_CUIL_STR))) = TRY_CONVERT(bigint, e.Dni);

DECLARE @empleados int = @@ROWCOUNT;

-- ---------------------------------------------------------------------
-- 4. Datos por empresa: actividad y nro. de legajo
-- ---------------------------------------------------------------------
UPDATE a SET
    ActivityCode = LTRIM(RTRIM(CONVERT(varchar(20), m.MAESOC_CODACT)))
FROM dbo.Assignments a
JOIN dbo.Employees e ON e.Id = a.EmployeeId
JOIN Sindicato.fotos_.maesoc m
  ON TRY_CONVERT(bigint, LTRIM(RTRIM(m.MAESOC_CUIL_STR))) = TRY_CONVERT(bigint, e.Dni)
WHERE a.ActivityCode IS NULL AND m.MAESOC_CODACT IS NOT NULL;

DECLARE @actividades_asig int = @@ROWCOUNT;

UPDATE a SET
    FileNumber = NULLIF(LTRIM(RTRIM(s.SOCEMP_NROLEG)), '')
FROM dbo.Assignments a
JOIN dbo.Employees e ON e.Id = a.EmployeeId
JOIN dbo.Companies c ON c.Id = a.CompanyId
JOIN Sindicato.fotos_.socemp s
  ON TRY_CONVERT(bigint, LTRIM(RTRIM(s.SOCEMP_CUIL_STR))) = TRY_CONVERT(bigint, e.Dni)
 AND TRY_CONVERT(bigint, LTRIM(RTRIM(s.SOCEMP_CUITE_STR))) = TRY_CONVERT(bigint, c.Cuit)
WHERE a.FileNumber IS NULL;

DECLARE @legajos int = @@ROWCOUNT;

IF @Simulacro = 1
BEGIN
    PRINT '=== SIMULACRO: NO SE ESCRIBIÓ NADA. Para aplicar, -v Simulacro=0 ===';
    ROLLBACK TRANSACTION;
END
ELSE
BEGIN
    COMMIT TRANSACTION;
    PRINT CONCAT('Aplicado. Actividades nuevas: ', @actividades,
                 ' | Obras sociales nuevas: ', @obras,
                 ' | Empleados actualizados: ', @empleados,
                 ' | Asignaciones con actividad: ', @actividades_asig,
                 ' | Asignaciones con legajo: ', @legajos);
END

-- ---------------------------------------------------------------------
-- 5. Verificación
-- ---------------------------------------------------------------------
PRINT '=== ESTADO FINAL ===';
SELECT
    (SELECT COUNT(*) FROM dbo.Activities) AS actividades,
    (SELECT COUNT(*) FROM dbo.HealthInsurances) AS obras_sociales,
    (SELECT COUNT(*) FROM dbo.Employees WHERE HealthInsuranceCode IS NOT NULL) AS empleados_con_os,
    (SELECT COUNT(*) FROM dbo.Employees WHERE IsRetired = 1) AS jubilados,
    (SELECT COUNT(*) FROM dbo.Employees WHERE HasFamilyCharge = 1) AS con_carga,
    (SELECT COUNT(*) FROM dbo.Assignments WHERE ActivityCode IS NOT NULL) AS asignaciones_con_actividad,
    (SELECT COUNT(*) FROM dbo.Assignments WHERE FileNumber IS NOT NULL) AS asignaciones_con_legajo;

-- VOLVER ATRÁS (si hiciera falta)
--   UPDATE e SET HealthInsuranceCode = b.HealthInsuranceCode, IsRetired = b.IsRetired,
--          HasFamilyCharge = b.HasFamilyCharge
--   FROM dbo.Employees e JOIN dbo._BackupEmployeesActividadOs b ON b.Id = e.Id;
--   UPDATE a SET ActivityCode = b.ActivityCode, FileNumber = b.FileNumber
--   FROM dbo.Assignments a JOIN dbo._BackupAssignmentsActividadLegajo b ON b.Id = a.Id;
