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
    ILogger<DeclarationService> logger)
{
  // Estos porcentajes ahora se obtienen dinámicamente del Sindicato
  // private const decimal LawContributionRate = 0.02m;       
  // private const decimal SocioContributionRate = 0.02m;     

  public async Task<DeclarationResponseDto> CreateDeclarationAsync(DeclarationRequestDto request)
  {
    // 1. Validar Periodo, Empresa y Sindicato
    var branch = await unitOfWork.Companies.GetByIdAsync(request.CompanyId)
        ?? throw new Exception("La sucursal no existe.");

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
    var scales = await unitOfWork.SalaryScales.FindAsync(s =>
        s.IsActive && s.Period <= targetDate && idsCategorias.Contains(s.EmployeeCategoryId));
    if (!scales.Any())
    {
      throw new Exception(
          $"No se encontró una escala salarial cargada para el periodo {request.Period} de la rama " +
          $"{union.Rama?.Name ?? ramaId.ToString()} (sindicato {union.Name}).");
    }
    var latestScalePeriod = scales.Max(s => s.Period);
    scales = scales.Where(s => s.Period == latestScalePeriod).ToList();

    // 3. Crear Cabecera de DDJJ
    var existingDeclarations = await unitOfWork.Declarations.FindAsync(d =>
        d.CompanyId == request.CompanyId && d.Period == request.Period);

    // Si ya hay un borrador de esta empresa y período (por ejemplo, porque el paso final de un intento
    // anterior no se completó), se REANUDA ese mismo borrador en lugar de tirar un error: el mensaje
    // decía "debe finalizarla antes de crear otra" y la aplicación no tiene ninguna pantalla para
    // finalizar ni borrar un borrador, así que era un callejón sin salida.
    // Sólo se reanuda un BORRADOR: nunca una DDJJ presentada, aprobada, rechazada o pagada.
    var existingDraft = existingDeclarations.FirstOrDefault(d => d.Status == DeclarationStatus.Draft);

    // Si ya hubo una presentada/aprobada/pagada del mismo periodo, es una rectificativa
    var lastPresented = existingDeclarations
        .Where(d => d.Status == DeclarationStatus.Submitted
                 || d.Status == DeclarationStatus.Approved
                 || d.Status == DeclarationStatus.Paid)
        .OrderByDescending(d => d.SubmissionDate)
        .FirstOrDefault();
    bool isRectification = lastPresented != null;

    var declaration = existingDraft ?? new DeclarationRegistry();

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
    declaration.Status = DeclarationStatus.Draft;
    declaration.SubmissionDate = DateTime.Now;
    declaration.IsRectification = isRectification;
    declaration.PreviousAmount = lastPresented?.TotalAmount ?? 0;
    declaration.PresencialReference = $"DDJJ-{request.CompanyId}-{DateTime.Now:ssfff}";

    decimal totalAmount = 0;
    decimal totalAporteLey = 0;
    decimal totalCuotaSocio = 0;

    // 4. Calcular Detalles por cada Empleado Activo de la Sucursal para el Sindicato seleccionado
    var assignments = await unitOfWork.Assignments.FindAsync(a =>
        a.CompanyId == request.CompanyId && a.UnionId == request.UnionId && a.IsActive);

    var enteredSalaries = request.Salaries.GroupBy(s => s.EmployeeId).ToDictionary(g => g.Key, g => g.Last().BaseSalary);

    foreach (var assign in assignments)
    {
      // Buscar remuneración básica en la escala para la categoría del empleado
      var categoryId = assign.CategoryCode != null && categories.TryGetValue(assign.CategoryCode, out var category) ? category.Id : 0;
      var scale = scales.FirstOrDefault(s => s.EmployeeCategoryId == categoryId);
      if (scale == null) continue;

      decimal remunerativeTotal;
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
      }

      // Cálculos dinámicos basados en la configuración del Sindicato. Se redondea a 2 decimales
      // (igual que la pantalla y que las columnas decimal(18,2) de la base): sin esto el importe
      // que muestra la pantalla y el que queda guardado podían diferir en centavos.
      decimal lawContribution = Redondear(remunerativeTotal * (union.LawContributionPercentage / 100));
      decimal socioContribution = assign.IsVoluntaryMember
          ? Redondear(remunerativeTotal * (union.UnionMemberPercentage / 100))
          : 0;

      var detail = new DeclarationDetail
      {
        AssignmentId = assign.Id,
        BaseSalary = remunerativeTotal,
        LawContribution = lawContribution,
        MemberContribution = socioContribution,
        TotalWithheld = lawContribution + socioContribution // En SIRGA "TotalWithheld" es el pago al sindicato
      };

      declaration.Details.Add(detail);
      totalAmount += detail.TotalWithheld;
      totalAporteLey += lawContribution;
      totalCuotaSocio += socioContribution;
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
    if (totalAmount == 0 && assignments.Any())
    {
      throw new Exception(
          $"No se guardó la DDJJ: ninguno de los {assignments.Count()} empleados de la empresa tiene " +
          $"categoría con escala salarial para el período {request.Period}. Revisá los códigos de " +
          "categoría de los empleados contra las categorías cargadas del sindicato.");
    }

    // Si se reanudó un borrador existente, la cabecera ya está en la base: se actualiza. Si es nueva,
    // se inserta. Con AddAsync sobre una entidad ya existente, EF intentaría insertar el mismo Id.
    if (existingDraft != null)
      await unitOfWork.Declarations.UpdateAsync(declaration);
    else
      await unitOfWork.Declarations.AddAsync(declaration);
    await unitOfWork.CompleteAsync();

    // 6. Auditoría de la acción
    await auditService.LogActionAsync(
        entityName: "DeclarationRegistry",
        entityId: declaration.Id,
        action: declaration.IsRectification ? "Rectificativa" : "Presentación",
        userId: "0", // TODO: Obtener GUID del contexto de autenticación
        oldValues: declaration.IsRectification ? new { lastPresented?.TotalAmount, lastPresented?.Period } : null,
        newValues: new { declaration.TotalAmount, declaration.Period, detailCount = declaration.Details.Count }
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
      Period = declaration.Period,
      TotalAmount = declaration.TotalAmount,
      Status = declaration.Status.ToString(),
      CompanyName = branch.BusinessName,
      CompanyCuit = branch.Cuit,
      UnionName = union?.Name ?? "Sindicato no asignado",
      SubmissionDate = declaration.SubmissionDate,
      ExpirationDate = declaration.SubmissionDate.AddDays(15),
      IsRectification = declaration.IsRectification,
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
      Period = d.Period,
      TotalAmount = d.TotalAmount,
      Status = d.Status.ToString(),
      CompanyName = company?.BusinessName ?? "N/A",
      CompanyCuit = company?.Cuit ?? "N/A",
      UnionName = union?.Name ?? "N/A",
      SubmissionDate = d.SubmissionDate,
      ExpirationDate = d.SubmissionDate.AddDays(15),
      IsRectification = d.IsRectification,
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
      Period = d.Period,
      TotalAmount = d.TotalAmount,
      Status = d.Status.ToString(),
      CompanyName = d.Company.BusinessName ?? "N/A",
      CompanyCuit = d.Company.Cuit ?? "N/A",
      UnionName = d.Union.Name ?? "N/A",
      SubmissionDate = d.SubmissionDate,
      ExpirationDate = d.SubmissionDate.AddDays(15),
      IsRectification = d.IsRectification,
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
          IsSocio = dt.Assignment.IsVoluntaryMember
        })
        .ToListAsync());
  }

  public async Task ApproveDeclarationAsync(int id, string observation)
  {
    var declaration = await unitOfWork.Declarations.GetByIdAsync(id);
    if (declaration == null) throw new Exception("Declaración no encontrada");

    declaration.Status = DeclarationStatus.Approved;
    await unitOfWork.CompleteAsync();
  }

  public async Task RejectDeclarationAsync(int id, string observation)
  {
    var declaration = await unitOfWork.Declarations.GetByIdAsync(id);
    if (declaration == null) throw new Exception("Declaración no encontrada");

    declaration.Status = DeclarationStatus.Rejected;
    await unitOfWork.CompleteAsync();
  }
}