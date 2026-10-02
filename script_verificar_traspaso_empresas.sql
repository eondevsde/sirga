-- Verificacion del traspaso de empresas entre estudios contables.
-- NO CAMBIA NADA: todo corre dentro de una transaccion que se revierte al final, asi que se puede
-- correr las veces que haga falta y sobre la base de produccion sin dejar rastro.
-- Comprueba contra el esquema real: que la secuencia del servicio (cerrar la vigente, abrir la del
-- estudio nuevo, cambiar el dueño) funcione, que la consulta del aviso devuelva lo que corresponde y
-- que el indice unico filtrado impida dos estudios a cargo de la misma empresa a la vez.

SET NOCOUNT ON;

BEGIN TRAN;

DECLARE @cuit varchar(20) = '30711570590';   -- VEGA METALES SRL
DECLARE @companyId int = (SELECT Id FROM Companies WHERE NormalizedCuit = @cuit AND ParentCompanyId IS NULL);
DECLARE @origen int = (SELECT AccountingStudioId FROM Companies WHERE Id = @companyId);
DECLARE @destino int = (SELECT TOP 1 c.AccountingStudioId
                        FROM Assignments a
                        INNER JOIN Companies c ON c.Id = a.CompanyId
                        WHERE a.UnionId = 19 AND c.AccountingStudioId IS NOT NULL
                          AND c.AccountingStudioId <> @origen);

IF @companyId IS NULL OR @origen IS NULL OR @destino IS NULL
BEGIN
  SELECT 'No se pudo preparar la prueba (falta empresa, origen o destino)' AS resultado;
  ROLLBACK;
END
ELSE
BEGIN
  SELECT 'PREPARACION -> empresa=' + CAST(@companyId AS varchar(8))
       + ' estudio_origen=' + CAST(@origen AS varchar(8))
       + ' estudio_destino=' + CAST(@destino AS varchar(8)) AS paso;

  -- Estado inicial: la empresa es del estudio de origen (el historial en produccion esta vacio).
  INSERT INTO CompanyStudioHistories (CompanyId, AccountingStudioId, UnionId, [From], [To], AuthorizedByUsername, Reason)
  VALUES (@companyId, @origen, 19, DATEADD(year, -1, SYSDATETIME()), NULL, NULL, 'alta inicial (simulada)');

  -- Lo mismo que hace AssociationService.TransferCompanyAsync, en el mismo orden.
  UPDATE CompanyStudioHistories SET [To] = SYSDATETIME() WHERE CompanyId = @companyId AND [To] IS NULL;
  INSERT INTO CompanyStudioHistories (CompanyId, AccountingStudioId, UnionId, [From], [To], AuthorizedByUsername, Reason)
  VALUES (@companyId, @destino, 19, SYSDATETIME(), NULL, 'sindicato@sirga.com', 'pedido del sindicato');
  UPDATE Companies SET AccountingStudioId = @destino WHERE Id = @companyId;

  SELECT 'DUENIO ACTUAL -> ' + ISNULL(e.Name, '(sin estudio)') AS paso
  FROM Companies c LEFT JOIN AccountingStudios e ON e.Id = c.AccountingStudioId
  WHERE c.Id = @companyId;

  SELECT 'FILAS VIGENTES (debe ser 1) -> ' + CAST(COUNT(*) AS varchar(3)) AS paso
  FROM CompanyStudioHistories WHERE CompanyId = @companyId AND [To] IS NULL;

  -- Es la consulta de GetCompaniesLeftAsync: lo que ve el estudio que perdio la empresa.
  SELECT 'AVISO AL ESTUDIO ANTERIOR -> ' + c.BusinessName
       + ' | desde ' + CONVERT(varchar(10), h.[From], 103)
       + ' hasta ' + CONVERT(varchar(10), h.[To], 103)
       + ' | hoy en ' + ISNULL(destino.Name, '-')
       + ' | autorizo ' + ISNULL(h.AuthorizedByUsername, '-')
       + ' | motivo: ' + ISNULL(h.Reason, '-') AS aviso
  FROM CompanyStudioHistories h
  INNER JOIN Companies c ON c.Id = h.CompanyId
  LEFT JOIN AccountingStudios destino ON destino.Id = c.AccountingStudioId
  WHERE h.AccountingStudioId = @origen AND h.[To] IS NOT NULL;

  -- El indice unico filtrado tiene que impedir que queden dos estudios vigentes a la vez.
  BEGIN TRY
    INSERT INTO CompanyStudioHistories (CompanyId, AccountingStudioId, UnionId, [From], [To], AuthorizedByUsername, Reason)
    VALUES (@companyId, @origen, 19, SYSDATETIME(), NULL, 'test', 'duplicado a proposito');
    SELECT 'CONTROL -> FALLO: la base permitio dos estudios a cargo de la misma empresa' AS paso;
  END TRY
  BEGIN CATCH
    SELECT 'CONTROL -> OK: la base lo impidio (' + ERROR_MESSAGE() + ')' AS paso;
  END CATCH
END

ROLLBACK;

-- Cierre: la tabla tiene que seguir vacia y la empresa con su dueño original.
SELECT 'DESPUES DE LA PRUEBA -> filas en el historial=' + CAST((SELECT COUNT(*) FROM CompanyStudioHistories) AS varchar(6)) AS cierre;
SELECT 'DESPUES DE LA PRUEBA -> ' + c.BusinessName + ' sigue en ' + ISNULL(e.Name, '(sin estudio)') AS cierre
FROM Companies c LEFT JOIN AccountingStudios e ON e.Id = c.AccountingStudioId
WHERE c.NormalizedCuit = @cuit AND c.ParentCompanyId IS NULL;
