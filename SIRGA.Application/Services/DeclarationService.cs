using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;

namespace SIRGA.Application.Services;

public class DeclarationService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    PdfReportService pdfService,
    BalanceService balanceService,
    AuditService auditService,
    ILegacyDeclarationWriter legacyWriter,
    ILogger<DeclarationService> logger,
    EmployeeService? employeeService = null)
{
  // Estos porcentajes ahora se obtienen dinámicamente del Sindicato
  // private const decimal LawContributionRate = 0.02m;       
  // private const decimal SocioContributionRate = 0.02m;     

  public async Task<DeclarationResponseDto> CreateDeclarationAsync(DeclarationRequestDto request)
  {
    // El cálculo de los renglones vive en UN SOLO lugar (CalcularAsync), que es el mismo que usa la
    // previsualización de una rectificativa: así la comparación que ve el estudio no puede diferir de lo
    // que se guarda.
    var calculo = await CalcularAsync(request);
    var branch = calculo.Sucursal;
    var union = calculo.Sindicato;

    // 3. Crear Cabecera de DDJJ
    var existingDeclarations = (await unitOfWork.Declarations.FindAsync(d =>
        d.CompanyId == request.CompanyId && d.Period == request.Period)).ToList();

    // Si ya hay un borrador de esta empresa y período (por ejemplo, porque el paso final de un intento
    // anterior no se completó), se REANUDA ese mismo borrador en lugar de tirar un error: el mensaje
    // decía "debe finalizarla antes de crear otra" y la aplicación no tiene ninguna pantalla para
    // finalizar ni borrar un borrador, así que era un callejón sin salida.
    var existingDraft = existingDeclarations.FirstOrDefault(d => d.Status == DeclarationStatus.Draft);

    // La versión VIGENTE es la última que no sea un borrador ni esté ya reemplazada. Presentar una DDJJ
    // de un período que ya tiene una vigente ES una rectificativa: es el mismo criterio del sistema
    // anterior, donde la columna `rect` era el número de versión (0 la original, 1.. la rectificativa) y
    // TODAS las versiones se conservaban. Una rechazada también cuenta como DDJJ presentada.
    var vigente = existingDeclarations
        .Where(d => d.Status != DeclarationStatus.Draft && d.Status != DeclarationStatus.Rectificada)
        .OrderByDescending(d => d.VersionNumber)
        .FirstOrDefault();

    bool esRectificativa = vigente is not null;

    var declaration = existingDraft ?? new DeclarationRegistry();

    // La versión 2 es la primera rectificativa, la 3 la segunda, y así.
    if (existingDraft is null)
    {
      declaration.VersionNumber = (existingDeclarations.Max(d => (int?)d.VersionNumber) ?? 0) + 1;
    }

    // Si se REANUDA un borrador hay que borrar sus detalles antes de volver a generarlos. Sin esto, cada
    // presentación del mismo borrador AGREGA los detalles de nuevo: quedaron 12 detalles para 4 empleados
    // después de 3 intentos, y la cabecera decía 112.622,46 mientras los detalles sumaban 337.867,38.
    // Sólo se reanudan BORRADORES: nunca una DDJJ presentada, aprobada, pagada o rechazada.
    if (existingDraft is not null)
    {
      var detallesPrevios = await unitOfWork.DeclarationDetails.FindAsync(d => d.RegistryId == existingDraft.Id);
      foreach (var previo in detallesPrevios)
      {
        await unitOfWork.DeclarationDetails.RemoveAsync(previo);
      }
      await unitOfWork.CompleteAsync();
    }

    declaration.CompanyId = request.CompanyId;
    declaration.UnionId = request.UnionId;
    declaration.Period = request.Period;

    // Se PRESENTA: la DDJJ queda en estado Presentada (antes quedaba como Borrador aunque la pantalla
    // dijera "presentada con éxito", y ninguna parte del código ponía el estado Presentada nunca).
    declaration.Status = DeclarationStatus.Submitted;
    declaration.SubmissionDate = DateTime.Now;
    declaration.IsRectification = esRectificativa;

    // La base de una rectificativa es lo que se PAGÓ de la versión anterior, NO su total declarado: si la
    // anterior no se pagó, se debe el total nuevo completo. Restar el total declarado cobraba de menos
    // (caso real medido en el sistema anterior: una DDJJ presentada por 418,95 y rectificada a 20.947,42
    // nunca pagada; restar 418,95 cobraba 20.528,47 en lugar de 20.947,42).
    declaration.PreviousAmount = esRectificativa ? await MontoPagadoAsync(vigente!) : 0;
    declaration.PresencialReference = $"DDJJ-{request.CompanyId}-{DateTime.Now:ssfff}";

    decimal totalAmount = calculo.Total;
    decimal totalAporteLey = calculo.TotalAporteLey;
    decimal totalCuotaSocio = calculo.TotalCuotaSocio;

    // 4. Los renglones ya vienen calculados por CalcularAsync (mismo cálculo que la previsualización).
    foreach (var fila in calculo.Filas)
    {
      declaration.Details.Add(new DeclarationDetail
      {
        AssignmentId = fila.AssignmentId,
        BaseSalary = fila.BaseSalary,
        LawContribution = fila.LawContribution,
        MemberContribution = fila.MemberContribution,
        TotalWithheld = fila.TotalWithheld, // En SIRGA "TotalWithheld" es el pago al sindicato
        // Se guarda el ORIGEN del importe: es lo que permite pre-cargar una rectificativa copiando sólo los
        // que se cargaron a mano, sin congelar los mínimos viejos.
        UsaEscala = fila.UsoEscala,
        // Los dos importes y la diferencia quedan guardados CON la DDJJ: la escala cambia y después nadie
        // puede reconstruir qué mínimo regía cuando se presentó.
        ScaleSalary = fila.SalarioEscala,
        EnteredSalary = fila.SalarioIngresado,
        // La diferencia sólo tiene sentido si el estudio cargó un importe: si se liquidó por escala, no hay
        // nada con qué comparar (mostrar -1.600.000 haría parecer que se declaró de menos).
        Difference = fila.UsoEscala ? null : fila.SalarioIngresado - fila.SalarioEscala
      });
    }

    declaration.TotalAmount = totalAmount;
    if (declaration.IsRectification)
    {
      declaration.BalanceAmount = totalAmount - declaration.PreviousAmount;
      // En una rectificativa, el monto a pagar es solo la diferencia si es positiva
      // Si el saldo es negativo (saldo a favor), el sistema lo registra pero la boleta sale por $0 o el saldo.
    }
    else
    {
      declaration.BalanceAmount = totalAmount;

      // USAR CRÉDITO DISPONIBLE para nuevas DDJJ
      var availableCredit = await balanceService.GetAvailableCreditAsync(request.CompanyId);
      if (availableCredit > 0 && declaration.BalanceAmount > 0)
      {
        // Reservamos el crédito. El uso real se confirma al guardar.
        // Aquí solo visualizamos el descuento en la boleta preliminar
        decimal creditToUse = Math.Min(availableCredit, declaration.BalanceAmount);
        declaration.BalanceAmount -= creditToUse;
        // Guardaremos este movimiento después del CompleteAsync para tener el Id de la DDJJ
      }
    }

    // 5. Calcular Intereses si la presentación es fuera de término (Vencimiento sugerido: día 15 del mes siguiente)
    var parts = declaration.Period.Split("-");
    var nextMonth = new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 1).AddMonths(1);
    var dueDate = new DateTime(nextMonth.Year, nextMonth.Month, 15);

    decimal interestAmount = 0;
    if (DateTime.Now.Date > dueDate.Date && declaration.BalanceAmount > 0)
    {
      // Interés resarcitorio por DÍAS y por TRAMOS de tasa (especificación de ARCA): desde el día
      // siguiente al vencimiento hasta hoy, fragmentando el período si atraviesa un cambio de tasa.
      // ANTES se calculaba por MESES ENTEROS con un mínimo de 1, así que pagar un día tarde costaba un
      // mes completo de interés; y la tasa diaria era la mensual sobre los días del mes real.
      var tasas = (await unitOfWork.InterestRates.GetAllAsync())
          .Where(r => r.IsActive && r.TypeOfInterest == InterestType.Resarcitorio);

      var resultado = InterestCalculator.Calcular(declaration.BalanceAmount, dueDate, DateTime.Now, tasas);
      interestAmount = resultado.Interes;
      declaration.BalanceAmount += interestAmount;

      if (resultado.FaltaTasa)
      {
        // No debería pasar (la escala llega hasta hoy), pero si pasa hay que verlo: son días de mora sin
        // tasa cargada, o sea interés cobrado de menos.
        logger.LogError(
            "La mora de la DDJJ {DeclarationId} tiene {Dias} día(s) sin tasa cargada: el interés puede estar " +
            "incompleto. Revisar la escala de InterestRates.",
            declaration.Id, resultado.DiasSinTasa);
      }
    }

    // Generar Código de Barras ficticio para la boleta (Basado en CUIT + Periodo + Saldo/Total)
    decimal amountToPay = declaration.BalanceAmount > 0 ? declaration.BalanceAmount : 0;
    declaration.PresencialBarCode = $"001{branch.Cuit}{request.Period.Replace("-", "")}{((int)(amountToPay * 100)):D10}";

    // Ninguna DDJJ se guarda en cero SIN JUSTIFICACIÓN. Si no se calculó ningún importe y hay
    // empleados asignados, es un problema de datos (el código de categoría del empleado no existe
    // entre las categorías del sindicato, o no hay escala para esa categoría) y hay que verlo: una
    // DDJJ presentada en cero la rechaza el sindicato, y guardarla como si estuviera bien es peor que
    // fallar. El único cero legítimo es el de un empleado con licencia médica, que se declara aparte.
    if (totalAmount == 0 && calculo.Asignaciones > 0)
    {
      throw new Exception(
          $"No se guardó la DDJJ: ninguno de los {calculo.Asignaciones} empleados de la empresa tiene " +
          $"categoría con escala salarial para el período {request.Period}. Revisá los códigos de " +
          "categoría de los empleados contra las categorías cargadas del sindicato.");
    }

    // Si se reanudó un borrador existente, la cabecera ya está en la base: se actualiza. Si es nueva,
    // se inserta. Con AddAsync sobre una entidad ya existente, EF intentaría insertar el mismo Id.
    if (existingDraft != null)
      await unitOfWork.Declarations.UpdateAsync(declaration);
    else
      await unitOfWork.Declarations.AddAsync(declaration);
        // VOLCADO AL LEGADO: recién presentada, la DDJJ se escribe en `ddjj`/`ddjjt`, que es de donde SIGES
    // WEB lee para saber si la empresa debe.
    // VA ANTES DE GUARDAR A PROPOSITO: si el legado falla, la presentación FALLA y la DDJJ NO queda
    // presentada en SIRGA. Al revés quedaría presentada acá e invisible para SIGES, y como la boleta sólo
    // se emite si está presentada, se podría imprimir una boleta que el banco no puede cobrar.
    var idsAsignaciones = calculo.Filas.Select(f => f.AssignmentId).ToList();
    var asignaciones = await unitOfWork.Assignments.RunQueryAsync(q =>
        q.Include(a => a.Employee).Where(a => idsAsignaciones.Contains(a.Id)).ToListAsync());
    var empresaDelLegado = await unitOfWork.Companies.GetByIdAsync(request.CompanyId);

    // POLÍTICA (2026-11-04): una vez que el socio existe en SIGES, el TELÉFONO y el DOMICILIO son de SIGES.
    // Antes de volcar, se traen del padrón para que SIRGA quede igual (y queda en la bitácora con origen SIGES).
    if (employeeService is not null)
    {
      try
      {
        await employeeService.TraerContactoDeSigesAsync(
            asignaciones.Select(a => a.EmployeeId).Distinct().ToList(), "SIGES");
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "No se pudo traer el contacto del padrón al presentar la DDJJ {Ddjj}", declaration.Id);
      }
    }

    var detallesLegado = calculo.Filas
        .Select(fila => new
        {
            Fila = fila,
            Dni = asignaciones.FirstOrDefault(a => a.Id == fila.AssignmentId)?.Employee?.Dni
        })
        .Where(x => !string.IsNullOrWhiteSpace(x.Dni))
        .Select(x =>
        {
          var asignacion = asignaciones.FirstOrDefault(a => a.Id == x.Fila.AssignmentId);
          var empleado = asignacion?.Employee;

          // Los datos del padrón (maesoc) salen del empleado; la categoría, la actividad y la fecha de
          // ingreso, de la asignación. Lo que no tenemos va vacío: no se inventan datos en el padrón.
          return new DetalleLegado(x.Dni!, x.Fila.BaseSalary, x.Fila.MemberContribution > 0)
          {
            Apellido = empleado?.LastName ?? string.Empty,
            Nombre = empleado?.FirstName ?? string.Empty,
            Documento = empleado?.DocumentNumber,
            TipoDocumento = empleado?.DocumentTypeCode,
            FechaNacimiento = empleado?.BirthDate,
            Sexo = empleado?.Sex,
            Telefono = empleado?.Phone,
            Celular = empleado?.Mobile,
            Email = empleado?.Email,
            EmailLaboral = empleado?.WorkEmail,
            Calle = empleado?.Address,
            Numero = empleado?.AddressNumber,
            Piso = empleado?.Floor,
            Depto = empleado?.Apartment,
            Barrio = empleado?.Neighborhood,
            CodigoPostal = empleado?.ZipCode,
            Categoria = asignacion?.CategoryCode,
            Actividad = asignacion?.ActivityCode,
            ObraSocial = empleado?.HealthInsuranceCode,
            Jubilado = empleado?.IsRetired ?? false,
            CargaFamiliar = empleado?.HasFamilyCharge ?? false,
            FechaIngreso = asignacion?.CreatedAt,
            Legajo = asignacion?.FileNumber
          };
        })
        .ToList();

    try
    {
      await legacyWriter.EscribirAsync(new DeclaracionLegado(
          empresaDelLegado?.Cuit ?? string.Empty,
          request.Period,
          declaration.VersionNumber > 0 ? declaration.VersionNumber - 1 : 0,
          totalAporteLey,
          totalCuotaSocio,
          detallesLegado,
          "SIRGA"));
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
          $"La DDJJ NO se presentó: no se pudo volcar al legado ({ex.Message}). SIGES WEB no la vería, así " +
          "que se prefiere que no quede presentada acá antes que emitir una boleta que el banco no cobra.", ex);
    }
await unitOfWork.CompleteAsync();

    // La versión anterior queda reemplazada: se marca como Rectificada y NO se borra (es el antecedente).
    // Marcarla es lo que impide que se pueda aprobar o cobrar dos veces la misma DDJJ. Se hace DESPUÉS de
    // guardar la nueva, para que un fallo no deje el período sin ninguna versión vigente.
    if (vigente is not null)
    {
      vigente.Status = DeclarationStatus.Rectificada;
      await unitOfWork.Declarations.UpdateAsync(vigente);
      await unitOfWork.CompleteAsync();
    }

    // 6. Auditoría de la acción
    await auditService.LogActionAsync(
        entityName: "DeclarationRegistry",
        entityId: declaration.Id,
        action: declaration.IsRectification ? "Rectificativa" : "Presentación",
        userId: "0", // TODO: Obtener GUID del contexto de autenticación
        oldValues: declaration.IsRectification
            ? new { vigente?.TotalAmount, vigente?.Period, pagadoAntes = declaration.PreviousAmount, versionAnterior = vigente?.VersionNumber }
            : null,
        newValues: new { declaration.TotalAmount, declaration.Period, declaration.VersionNumber, detailCount = declaration.Details.Count }
    );

    // PROCESAR BALANCE: Si fue rectificativa negativa (genera crédito) 
    // o si fue DDJJ común que usó crédito (lo descuenta oficialmente)
    if (declaration.IsRectification && declaration.BalanceAmount < 0)
    {
      await balanceService.ProcessDeclarationBalanceAsync(declaration.Id);
    }
    else if (!declaration.IsRectification)
    {
      // Re-calcular cuánto crédito se usó realmente basándose en el total original
      var availableCredit = await balanceService.GetAvailableCreditAsync(request.CompanyId);
      if (availableCredit > 0)
      {
        // totalAmount es el valor calculado antes de restar crédito
        decimal needed = totalAmount + interestAmount;
        await balanceService.UseCreditAsync(request.CompanyId, needed, declaration.Id);
      }
    }

    // Notificación Automática (Opcional)
    try
    {
      var responseDto = MapToResponse(declaration, branch, union, totalAporteLey, totalCuotaSocio);
      var pdfBytes = pdfService.GeneratePaymentBoleta(responseDto);

      // Buscar el estudio contable de la empresa
      var studio = await unitOfWork.AccountingStudios.GetByIdAsync(branch.AccountingStudioId ?? 0);
      if (studio != null && !string.IsNullOrEmpty(studio.Email))
      {
        await emailService.SendEmailAsync(
            studio.Email,
            $"Boleta de Pago SIRGA - Periodo {request.Period} - {branch.BusinessName}",
            $"Se adjunta la boleta de pago correspondiente a la declaración jurada presentada para el periodo {request.Period}.",
            pdfBytes,
            $"Boleta_{branch.Cuit}_{request.Period}.pdf");
      }
    }
    catch (Exception ex)
    {
      // La DDJJ ya quedó guardada y no se hace fallar por el mail: eso está bien. Lo que NO está bien
      // es que el error quede invisible, que es lo que pasaba con el catch vacío que había acá: si la
      // boleta no se generaba (por ejemplo por la carpeta temporal de QuestPDF) o el mail no salía,
      // nadie se enteraba. Queda en el log con el id y el período para poder rastrearlo.
      logger.LogError(ex,
          "La DDJJ {DeclarationId} (empresa {CompanyId}, período {Period}) se guardó, pero falló la " +
          "generación o el envío de la boleta al estudio. La declaración NO se vio afectada.",
          declaration.Id, declaration.CompanyId, declaration.Period);
    }

    return MapToResponse(declaration, branch, union, totalAporteLey, totalCuotaSocio);
  }

  public async Task<List<EmployeeSalaryDto>> ProcessExcelDeclarationAsync(Stream fileStream)
  {
    using var workbook = new XLWorkbook(fileStream);
    var worksheet = workbook.Worksheet(1);
    var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>(); // Saltar encabezado

    var resultList = new List<EmployeeSalaryDto>();
    foreach (var row in rows)
    {
      var dni = row.Cell(1).GetString();
      if (string.IsNullOrEmpty(dni)) continue;

      var employee = await unitOfWork.Employees.FindAsync(e => e.Dni == dni);
      var emp = employee.FirstOrDefault();

      if (emp != null)
      {
        resultList.Add(new EmployeeSalaryDto
        {
          EmployeeId = emp.Id,
          BaseSalary = row.Cell(3).GetValue<decimal>()
        });
      }
    }
    return resultList;
  }

  /// <summary>Redondeo de dinero a 2 decimales "hacia afuera" (criterio contable), no el bancario de .NET.</summary>
  private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

  private DeclarationResponseDto MapToResponse(DeclarationRegistry declaration, Company branch, Union? union, decimal totalAporteLey, decimal totalCuotaSocio)
  {
    return new DeclarationResponseDto
    {
      Id = declaration.Id,
      CompanyId = declaration.CompanyId,
      UnionId = declaration.UnionId,
      Period = declaration.Period,
      TotalAmount = declaration.TotalAmount,
      Status = declaration.Status.ToString(),
      CompanyName = branch.BusinessName,
      CompanyCuit = branch.Cuit,
      UnionName = union?.Name ?? "Sindicato no asignado",
      SubmissionDate = declaration.SubmissionDate,
      ExpirationDate = declaration.SubmissionDate.AddDays(15),
      IsRectification = declaration.IsRectification,
      VersionNumber = declaration.VersionNumber,
      PreviousAmount = declaration.PreviousAmount,
      BalanceAmount = declaration.BalanceAmount,
      PresencialBarCode = declaration.PresencialBarCode,
      PresencialReference = declaration.PresencialReference,
      AporteLey = totalAporteLey,
      CuotaSocio = totalCuotaSocio
    };
  }

  public async Task<IEnumerable<DeclarationResponseDto>> GetDeclarationsByUnionAsync(int unionId)
  {
    var page = await GetDeclarationsByUnionPagedAsync(unionId, 1, int.MaxValue);
    return page.Items;
  }

  // Versión paginada y eficiente (proyección única, sin N+1)
  public async Task<PagedDeclarationsDto> GetDeclarationsByUnionPagedAsync(int unionId, int page, int pageSize)
  {
    return await unitOfWork.Declarations.RunQueryAsync(async q =>
    {
      var baseQuery = q.Where(d => d.UnionId == unionId);

      var totalCount = await baseQuery.CountAsync();
      var totalAmount = await baseQuery.SumAsync(d => (decimal?)d.TotalAmount) ?? 0m;

      var items = await baseQuery
          .OrderByDescending(d => d.SubmissionDate)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .Select(ProjectDeclaration())
          .ToListAsync();

      return new PagedDeclarationsDto { TotalCount = totalCount, TotalAmount = totalAmount, Items = items };
    });
  }

  /// <summary>
  /// Versión paginada y filtrable para las grillas de auditoría, usando la proyección
  /// eficiente (joins a Empresa/Sindicato y subtotales agregados en SQL).
  /// Reemplaza el patrón de traer todas las DDJJ a la UI: medido, GET /api/declarations
  /// tardaba 40,5 s y devolvía 115,68 MB de JSON, y además calculaba los subtotales sobre
  /// <c>Details</c> sin cargar la colección, por lo que AporteLey y CuotaSocio daban 0 en
  /// las 248.151 filas.
  /// </summary>
  public async Task<PagedDeclarationsDto> GetDeclarationsPagedAsync(
      int page, int pageSize, string? status = null, int? unionId = null, int? companyId = null, string? search = null)
  {
    return await unitOfWork.Declarations.RunQueryAsync(async q =>
    {
      var baseQuery = q.AsQueryable();

      if (unionId.HasValue) baseQuery = baseQuery.Where(d => d.UnionId == unionId.Value);
      if (companyId.HasValue) baseQuery = baseQuery.Where(d => d.CompanyId == companyId.Value);

      if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DeclarationStatus>(status, true, out var estado))
      {
        baseQuery = baseQuery.Where(d => d.Status == estado);
      }

      if (!string.IsNullOrWhiteSpace(search))
      {
        var texto = search.Trim();
        baseQuery = baseQuery.Where(d => d.Company.BusinessName.Contains(texto) || d.Company.Cuit.Contains(texto));
      }

      var totalCount = await baseQuery.CountAsync();
      var totalAmount = await baseQuery.SumAsync(d => (decimal?)d.TotalAmount) ?? 0m;

      var items = await baseQuery
          .OrderByDescending(d => d.SubmissionDate)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .Select(ProjectDeclaration())
          .ToListAsync();

      return new PagedDeclarationsDto { TotalCount = totalCount, TotalAmount = totalAmount, Items = items };
    });
  }

  public async Task<DeclarationResponseDto?> GetResponseByIdAsync(int id)
  {
    var d = await unitOfWork.Declarations.GetByIdAsync(id);
    if (d == null) return null;

    var company = await unitOfWork.Companies.GetByIdAsync(d.CompanyId);
    var union = await unitOfWork.Unions.GetByIdAsync(d.UnionId);

    var totalAporteLey = d.Details.Sum(x => x.LawContribution);
    var totalCuotaSocio = d.Details.Sum(x => x.MemberContribution);
    var totalSepelio = d.Details.Sum(x => x.TotalWithheld) - totalAporteLey - totalCuotaSocio;

    return new DeclarationResponseDto
    {
      Id = d.Id,
      CompanyId = d.CompanyId,
      UnionId = d.UnionId,
      Period = d.Period,
      TotalAmount = d.TotalAmount,
      Status = d.Status.ToString(),
      CompanyName = company?.BusinessName ?? "N/A",
      CompanyCuit = company?.Cuit ?? "N/A",
      UnionName = union?.Name ?? "N/A",
      SubmissionDate = d.SubmissionDate,
      ExpirationDate = d.SubmissionDate.AddDays(15),
      IsRectification = d.IsRectification,
      VersionNumber = d.VersionNumber,
      PresencialBarCode = d.PresencialBarCode,
      PresencialReference = d.PresencialReference,
      AporteLey = totalAporteLey,
      SeguroSepelio = totalSepelio,
      CuotaSocio = totalCuotaSocio
    };
  }

  public async Task<DeclarationRegistry?> GetByIdAsync(int id)
  {
    return await unitOfWork.Declarations.GetByIdAsync(id);
  }

  public async Task<IEnumerable<DeclarationResponseDto>> GetDeclarationsByCompanyAsync(int companyId)
  {
    return await unitOfWork.Declarations.RunQueryAsync(q =>
        q.Where(d => d.CompanyId == companyId)
        .OrderByDescending(d => d.Period)
        .Select(ProjectDeclaration())
        .ToListAsync());
  }

  // Proyección única y eficiente de DeclarationRegistry a DeclarationResponseDto
  private static System.Linq.Expressions.Expression<Func<DeclarationRegistry, DeclarationResponseDto>> ProjectDeclaration()
  {
    return d => new DeclarationResponseDto
    {
      Id = d.Id,
      CompanyId = d.CompanyId,
      UnionId = d.UnionId,
      Period = d.Period,
      TotalAmount = d.TotalAmount,
      Status = d.Status.ToString(),
      CompanyName = d.Company.BusinessName ?? "N/A",
      CompanyCuit = d.Company.Cuit ?? "N/A",
      UnionName = d.Union.Name ?? "N/A",
      SubmissionDate = d.SubmissionDate,
      ExpirationDate = d.SubmissionDate.AddDays(15),
      IsRectification = d.IsRectification,
      VersionNumber = d.VersionNumber,
      PreviousAmount = d.PreviousAmount,
      BalanceAmount = d.BalanceAmount,
      MercadoPagoPaymentId = d.MercadoPagoPaymentId,
      PaymentDate = d.PaymentDate,
      PresencialBarCode = d.PresencialBarCode,
      PresencialReference = d.PresencialReference,
      AporteLey = d.Details.Sum(x => x.LawContribution),
      CuotaSocio = d.Details.Sum(x => x.MemberContribution),
      SeguroSepelio = d.Details.Sum(x => x.TotalWithheld) - d.Details.Sum(x => x.LawContribution) - d.Details.Sum(x => x.MemberContribution)
    };
  }

  public async Task<List<DeclarationDetailDto>> GetDeclarationDetailsAsync(int declarationId)
  {
    return await unitOfWork.Declarations.RunQueryAsync(q =>
        q.Where(d => d.Id == declarationId)
        .SelectMany(d => d.Details)
        .Select(dt => new DeclarationDetailDto
        {
          EmployeeId = dt.Assignment.Employee.Id,
          EmployeeName = dt.Assignment.Employee.FullName ?? "N/A",
          Dni = dt.Assignment.Employee.Dni ?? "N/A",
          BaseSalary = dt.BaseSalary,
          LawContribution = dt.LawContribution,
          MemberContribution = dt.MemberContribution,
          TotalWithheld = dt.TotalWithheld,
          IsSocio = dt.Assignment.IsVoluntaryMember,
          ScaleSalary = dt.ScaleSalary,
          EnteredSalary = dt.EnteredSalary,
          Difference = dt.Difference,
          UsaEscala = dt.UsaEscala
        })
        .ToListAsync());
  }

  /// <summary>
  /// La DDJJ VIGENTE de una empresa y período (la que quedaría reemplazada si se presenta otra), o
  /// <c>null</c> si no hay ninguna. La pantalla la usa para avisar que presentar va a ser una
  /// rectificativa: que la rectificativa sea automática no quiere decir que tenga que ser invisible.
  /// </summary>
  public async Task<DeclarationAnteriorDto?> ObtenerVigenteAsync(int companyId, string period)
  {
    var vigente = (await unitOfWork.Declarations.FindAsync(d =>
            d.CompanyId == companyId && d.Period == period
            && d.Status != DeclarationStatus.Draft && d.Status != DeclarationStatus.Rectificada))
        .OrderByDescending(d => d.VersionNumber)
        .FirstOrDefault();

    if (vigente is null) return null;

    return new DeclarationAnteriorDto(
        vigente.Id,
        vigente.VersionNumber,
        vigente.TotalAmount,
        await MontoPagadoAsync(vigente),
        vigente.Status.ToString(),
        vigente.SubmissionDate);
  }

  /// <summary>
  /// Calcula los renglones de una DDJJ (uno por empleado activo) <b>sin guardar nada</b>.
  ///
  /// Es el ÚNICO lugar donde se calcula: lo usan tanto <see cref="CreateDeclarationAsync"/> como
  /// <see cref="PrevisualizarRectificativaAsync"/>. Existe una sola implementación a propósito: si la
  /// previsualización calculara por su lado, podría mostrar un importe distinto del que se termina
  /// guardando, que es justo lo que no puede pasar.
  /// </summary>
  /// <summary>
  /// Una empresa INACTIVA no puede presentar DDJJ. Se miran las DOS banderas que usa la pantalla
  /// (`IsActive` y `Status`) porque quedaron desincronizadas: la grilla mostraba el chip "Inactiva"
  /// y la DDJJ se podía presentar igual. Vive acá, aparte, para poder cubrirlo con una prueba.
  /// </summary>
  public static void ValidarEmpresaActiva(Company empresa)
  {
    if (!empresa.IsActive || empresa.Status != CompanyStatus.Active)
      throw new Exception(
          $"La empresa {empresa.BusinessName} está inactiva: no se puede presentar la DDJJ. " +
          "Activala primero desde Empresas.");
  }
  private async Task<CalculoDeclaracion> CalcularAsync(DeclarationRequestDto request)
  {
    // 1. Validar Periodo, Empresa y Sindicato
    var branch = await unitOfWork.Companies.GetByIdAsync(request.CompanyId)
        ?? throw new Exception("La sucursal no existe.");

    // Una empresa inactiva no puede declarar (ver ValidarEmpresaActiva).
    ValidarEmpresaActiva(branch);

    var union = await unitOfWork.Unions.GetByIdAsync(request.UnionId)
        ?? throw new Exception("El sindicato no existe.");

    // Las escalas y las categorías son NACIONALES por RAMA (el convenio), no por sindicato: se resuelven
    // por la rama del sindicato. Si el sindicato no tiene rama asignada no se puede saber qué escala
    // aplicarle, y hay que decirlo en vez de liquidar cualquier cosa.
    int ramaId = union.RamaId
        ?? throw new Exception(
            $"El sindicato {union.Name} no tiene una rama de actividad asignada, así que no se puede " +
            "saber qué escala salarial aplicarle. Asignásela en la pantalla de Sindicatos.");

    // 2. Buscar Escala Salarial para el periodo (Formato YYYY-MM)
    var periodParts = request.Period.Split("-");
    int year = int.Parse(periodParts[0]);
    int month = int.Parse(periodParts[1]);
    var targetDate = new DateTime(year, month, 1);

    var categories = (await unitOfWork.EmployeeCategories.FindAsync(c => c.RamaId == ramaId))
        .ToDictionary(c => c.Code);

    // Buscar escala para el periodo; si no hay exacta, congelar la más reciente anterior (fallback).
    // Sólo las ACTIVAS: una escala desactivada no se usa (antes se ignoraba IsActive, así que
    // desactivar una escala no hacía nada y podía usarse una dada de baja).
    var idsCategorias = categories.Values.Select(c => c.Id).ToList();
    var scales = (await unitOfWork.SalaryScales.FindAsync(s =>
        s.IsActive && s.Period <= targetDate && idsCategorias.Contains(s.EmployeeCategoryId))).ToList();
    if (!scales.Any())
    {
      throw new Exception(
          $"No se encontró una escala salarial cargada para el periodo {request.Period} de la rama " +
          $"{union.Rama?.Name ?? ramaId.ToString()} (sindicato {union.Name}).");
    }
    var latestScalePeriod = scales.Max(s => s.Period);
    scales = scales.Where(s => s.Period == latestScalePeriod).ToList();

    // 4. Calcular Detalles por cada Empleado Activo de la Sucursal para el Sindicato seleccionado
    var assignments = (await unitOfWork.Assignments.FindAsync(a =>
        a.CompanyId == request.CompanyId && a.UnionId == request.UnionId && a.IsActive)).ToList();

    var enteredSalaries = request.Salaries.GroupBy(s => s.EmployeeId).ToDictionary(g => g.Key, g => g.Last().BaseSalary);

    // Los empleados se traen aparte: la asignación guarda el EmployeeId, pero la navegación a Employee no
    // viene cargada, y sin el nombre la comparación no le sirve a nadie.
    var idsEmpleados = assignments.Select(a => a.EmployeeId).Distinct().ToList();
    var empleados = (await unitOfWork.Employees.FindAsync(e => idsEmpleados.Contains(e.Id)))
        .ToDictionary(e => e.Id);

    var filas = new List<FilaCalculada>();
    decimal total = 0, totalAporteLey = 0, totalCuotaSocio = 0;

    foreach (var assign in assignments)
    {
      // Buscar remuneración básica en la escala para la categoría del empleado
      var categoria = assign.CategoryCode != null && categories.TryGetValue(assign.CategoryCode, out var c) ? c : null;
      var scale = categoria is null ? null : scales.FirstOrDefault(s => s.EmployeeCategoryId == categoria.Id);
      if (scale == null) continue;

      decimal remunerativeTotal;
      bool usoEscala;
      // Antigüedad contada desde el ingreso del empleado A ESTA EMPRESA (Assignments.CreatedAt,
      // origen SOCEMP_FECHAING). Antes se contaba desde Employees.HireDate, que es la fecha de alta
      // al sindicato: daba una antigüedad distinta de la real. La fórmula vive en un solo lugar
      // (SalaryMinimumCalculator) para que la pantalla y este cálculo no puedan divergir.
      int anios = SalaryMinimumCalculator.AniosAntiguedad(assign.CreatedAt, targetDate);
      decimal factorJornada = SalaryMinimumCalculator.FactorJornada(assign.Jornada);

      // Si el estudio cargó un salario bruto para este empleado, se usa ese como base de cálculo
      // (es el importe que figura en el recibo de sueldo); si no (salario 0 o ausente), se usa el
      // mínimo REMUNERATIVO de escala: básico + antigüedad, prorrateado por jornada.
      if (enteredSalaries.TryGetValue(assign.EmployeeId, out var enteredBase) && enteredBase > 0)
      {
        remunerativeTotal = enteredBase;
        usoEscala = false;
      }
      else
      {
        // Los acuerdos no remunerativos (AgreementNR1/AgreementNR2) NO entran en la base de aportes:
        // son no remunerativos por definición. Por eso acá va sólo básico + antigüedad, aunque el
        // mínimo de escala que se muestra en pantalla (que sí los incluye) sea más alto.
        decimal antiguedadImporte = SalaryMinimumCalculator.Antiguedad(
            scale.BasicAmount, scale.AntiguedadPercentage, anios);
        remunerativeTotal = Math.Round(
            (scale.BasicAmount + antiguedadImporte) * factorJornada, 2, MidpointRounding.AwayFromZero);
        usoEscala = true;
      }

      // Cálculos dinámicos basados en la configuración del Sindicato. Se redondea a 2 decimales
      // (igual que la pantalla y que las columnas decimal(18,2) de la base): sin esto el importe
      // que muestra la pantalla y el que queda guardado podían diferir en centavos.
      decimal lawContribution = Redondear(remunerativeTotal * (union.LawContributionPercentage / 100));
      decimal socioContribution = assign.IsVoluntaryMember
          ? Redondear(remunerativeTotal * (union.UnionMemberPercentage / 100))
          : 0;

      decimal totalWithheld = lawContribution + socioContribution;
      empleados.TryGetValue(assign.EmployeeId, out var empleado);

      // Los dos importes que hay que dejar guardados para la auditoría: lo que proponía la escala (con los
      // no remunerativos incluidos, igual que la pantalla) y lo que cargó el estudio (0 si no cargó nada).
      decimal salarioEscala = SalaryMinimumCalculator.Minimo(scale, assign.CreatedAt, targetDate, assign.Jornada);
      decimal salarioIngresado = remunerativeTotal;
      if (usoEscala) salarioIngresado = 0;

      filas.Add(new FilaCalculada(
          assign.Id,
          assign.EmployeeId,
          empleado?.FullName,
          empleado?.Dni,
          assign.CategoryCode,
          categoria?.Name,
          remunerativeTotal,
          lawContribution,
          socioContribution,
          totalWithheld,
          assign.IsVoluntaryMember,
          usoEscala,
          salarioEscala,
          salarioIngresado));

      total += totalWithheld;
      totalAporteLey += lawContribution;
      totalCuotaSocio += socioContribution;
    }

    return new CalculoDeclaracion(branch, union, targetDate, latestScalePeriod, filas, assignments.Count,
        total, totalAporteLey, totalCuotaSocio);
  }

  /// <summary>
  /// Comparación EMPLEADO POR EMPLEADO entre la versión vigente de una DDJJ y lo que daría presentarla de
  /// nuevo, sin guardar nada. La pantalla la muestra antes de confirmar una rectificativa: comparando sólo
  /// los totales, si un empleado sube y otro baja la diferencia lo esconde.
  ///
  /// Devuelve <c>null</c> si no hay una versión vigente (o sea, si no sería una rectificativa).
  /// </summary>
  public async Task<ComparacionRectificativaDto?> PrevisualizarRectificativaAsync(DeclarationRequestDto request)
  {
    var anterior = await ObtenerVigenteAsync(request.CompanyId, request.Period);
    if (anterior is null) return null;

    var calculo = await CalcularAsync(request);

    var anteriores = (await GetDeclarationDetailsAsync(anterior.Id))
        .GroupBy(d => d.EmployeeId)
        .ToDictionary(g => g.Key, g => g.First());

    var filas = new List<ComparacionEmpleadoDto>();
    var vistos = new HashSet<int>();

    foreach (var fila in calculo.Filas)
    {
      vistos.Add(fila.EmployeeId);
      anteriores.TryGetValue(fila.EmployeeId, out var previo);

      decimal importeAnterior = previo?.TotalWithheld ?? 0;
      decimal baseAnterior = previo?.BaseSalary ?? 0;

      filas.Add(new ComparacionEmpleadoDto(
          fila.Empleado ?? $"Empleado {fila.EmployeeId}",
          fila.Dni,
          fila.CategoriaNombre ?? fila.CategoriaCodigo ?? "Sin categoría",
          baseAnterior, fila.BaseSalary,
          importeAnterior, fila.TotalWithheld,
          EstadoDeComparacion(previo is null, importeAnterior, fila.TotalWithheld),
          fila.UsoEscala));
    }

    // Empleados que estaban en la versión anterior y ya no están (baja de la empresa, desvinculación...).
    foreach (var previo in anteriores.Values.Where(p => !vistos.Contains(p.EmployeeId)))
    {
      filas.Add(new ComparacionEmpleadoDto(
          previo.EmployeeName, previo.Dni, "—",
          previo.BaseSalary, 0,
          previo.TotalWithheld, 0,
          "Sale", false));
    }

    return new ComparacionRectificativaDto(
        anterior.VersionNumber,
        anterior.TotalAmount,
        anterior.Pagado,
        calculo.Total,
        calculo.Total - anterior.TotalAmount,
        filas.OrderBy(f => f.Empleado, StringComparer.CurrentCultureIgnoreCase).ToList());
  }

  /// <summary>Cómo cambió el importe de un empleado entre las dos versiones.</summary>
  private static string EstadoDeComparacion(bool esNuevo, decimal anterior, decimal nuevo)
  {
    if (esNuevo) return "Nuevo";
    if (nuevo > anterior) return "Sube";
    if (nuevo < anterior) return "Baja";
    return "Igual";
  }

  /// <summary>Renglón calculado de un empleado (lo que después se guarda como <see cref="DeclarationDetail"/>).</summary>
  private record FilaCalculada(
      int AssignmentId,
      int EmployeeId,
      string? Empleado,
      string? Dni,
      string? CategoriaCodigo,
      string? CategoriaNombre,
      decimal BaseSalary,
      decimal LawContribution,
      decimal MemberContribution,
      decimal TotalWithheld,
      bool EsSocio,
      bool UsoEscala,
      decimal SalarioEscala,
      decimal SalarioIngresado);

  /// <summary>Resultado del cálculo de una DDJJ, antes de guardarla.</summary>
  private record CalculoDeclaracion(
      Company Sucursal,
      Union Sindicato,
      DateTime FechaPeriodo,
      DateTime PeriodoEscala,
      IReadOnlyList<FilaCalculada> Filas,
      int Asignaciones,
      decimal Total,
      decimal TotalAporteLey,
      decimal TotalCuotaSocio);

  /// <summary>
  /// La DDJJ que sirve de BASE para lo que se está por presentar en ese período: el <b>borrador</b> si
  /// existe (porque se reanuda y sus importes tienen que volver a la pantalla), o la versión <b>vigente</b>
  /// si no hay borrador (que es la que se rectifica). Devuelve <c>null</c> si no hay ninguna.
  ///
  /// Es distinto de <see cref="ObtenerVigenteAsync"/> a propósito: una DDJJ en <b>borrador NO es una
  /// rectificativa</b> (nunca se presentó, así que presentarla es su primera presentación), pero sus
  /// importes SÍ tienen que precargarse: si no, continuar un borrador perdería lo que el estudio ya había
  /// cargado, porque al presentar se recalculan los renglones.
  /// </summary>
  public async Task<DeclarationAnteriorDto?> ObtenerBaseAsync(int companyId, string period)
  {
    var delPeriodo = (await unitOfWork.Declarations.FindAsync(d =>
        d.CompanyId == companyId && d.Period == period)).ToList();

    var baseDdjj = delPeriodo
        .Where(d => d.Status == DeclarationStatus.Draft)
        .OrderByDescending(d => d.VersionNumber)
        .FirstOrDefault()
        ?? delPeriodo
            .Where(d => d.Status != DeclarationStatus.Rectificada)
            .OrderByDescending(d => d.VersionNumber)
            .FirstOrDefault();

    if (baseDdjj is null) return null;

    return new DeclarationAnteriorDto(
        baseDdjj.Id,
        baseDdjj.VersionNumber,
        baseDdjj.TotalAmount,
        await MontoPagadoAsync(baseDdjj),
        baseDdjj.Status.ToString(),
        baseDdjj.SubmissionDate);
  }

  public async Task ApproveDeclarationAsync(int id, string observation)
  {
    var declaration = await unitOfWork.Declarations.GetByIdAsync(id);
    if (declaration == null) throw new Exception("Declaración no encontrada");

    // Una DDJJ reemplazada por una rectificativa NO se aprueba: quedaría como aprobada una versión vieja y
    // se podría terminar cobrando dos veces la misma DDJJ.
    if (declaration.Status == DeclarationStatus.Rectificada)
    {
      throw new Exception(
          $"La DDJJ {id} (período {declaration.Period}, versión {declaration.VersionNumber}) fue reemplazada " +
          "por una rectificativa: no se puede aprobar. Aprobá la versión vigente.");
    }

    declaration.Status = DeclarationStatus.Approved;
    await unitOfWork.CompleteAsync();
  }

  /// <summary>
  /// Lo que se PAGÓ de una DDJJ: la suma de sus transacciones de pago. Si no tiene ninguna pero está
  /// marcada como Pagada, se toma su total.
  ///
  /// El fallback es necesario y no es un capricho: las DDJJ que vinieron del sistema anterior están en
  /// estado Pagada pero **sin transacciones** (la tabla `PaymentTransactions` se migró vacía). Sin esto,
  /// rectificar una DDJJ migrada cobraría el total completo otra vez, como si nunca se hubiera pagado.
  /// </summary>
  private async Task<decimal> MontoPagadoAsync(DeclarationRegistry declaration)
  {
    var pagos = (await unitOfWork.PaymentTransactions
        .FindAsync(p => p.DeclarationId == declaration.Id)).ToList();

    if (pagos.Count > 0) return pagos.Sum(p => p.Amount);

    return declaration.Status == DeclarationStatus.Paid ? declaration.TotalAmount : 0m;
  }

  public async Task RejectDeclarationAsync(int id, string observation)
  {
    var declaration = await unitOfWork.Declarations.GetByIdAsync(id);
    if (declaration == null) throw new Exception("Declaración no encontrada");

    declaration.Status = DeclarationStatus.Rejected;
    await unitOfWork.CompleteAsync();
  }
}