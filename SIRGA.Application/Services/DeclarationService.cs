using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;

namespace SIRGA.Application.Services;

public class DeclarationService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    PdfReportService pdfService,
    BalanceService balanceService,
    AuditService auditService)
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

    // 2. Buscar Escala Salarial para el periodo (Formato YYYY-MM)
    var periodParts = request.Period.Split("-");
    int year = int.Parse(periodParts[0]);
    int month = int.Parse(periodParts[1]);
    var targetDate = new DateTime(year, month, 1);

    // Buscar escala para el periodo; si no hay exacta, congelar la más reciente anterior (fallback)
    var scales = await unitOfWork.SalaryScales.FindAsync(s => s.Period <= targetDate && s.UnionId == request.UnionId);
    if (!scales.Any())
    {
      throw new Exception($"No se encontró una escala salarial cargada para el periodo {request.Period} del sindicato {union.Name}.");
    }
    var latestScalePeriod = scales.Max(s => s.Period);
    scales = scales.Where(s => s.Period == latestScalePeriod).ToList();

    var categories = (await unitOfWork.EmployeeCategories.FindAsync(c => c.UnionId == request.UnionId)).ToDictionary(c => c.Code);

    // 3. Crear Cabecera de DDJJ
    var existingDeclarations = await unitOfWork.Declarations.FindAsync(d =>
        d.CompanyId == request.CompanyId && d.Period == request.Period);

    // Bloquear duplicados en borrador: debe finalizarse la existente.
    // OJO: hoy esto es un callejón sin salida, porque la aplicación no tiene ninguna pantalla para
    // finalizar ni para borrar un borrador. Para descartarlo hace falta un repositorio de
    // DeclarationDetail en IUnitOfWork (no existe) y borrar los detalles ANTES que la cabecera,
    // porque la clave foránea está en Restrict y no arrastra.
    var existingDraft = existingDeclarations.FirstOrDefault(d => d.Status == DeclarationStatus.Draft);
    if (existingDraft != null)
    {
      throw new Exception($"Ya existe una DDJJ en borrador para la empresa y el periodo {request.Period}. Debe finalizarla antes de crear otra.");
    }

    // Si ya hubo una presentada/aprobada/pagada del mismo periodo, es una rectificativa
    var lastPresented = existingDeclarations
        .Where(d => d.Status == DeclarationStatus.Submitted
                 || d.Status == DeclarationStatus.Approved
                 || d.Status == DeclarationStatus.Paid)
        .OrderByDescending(d => d.SubmissionDate)
        .FirstOrDefault();
    bool isRectification = lastPresented != null;

    var declaration = new DeclarationRegistry
    {
      CompanyId = request.CompanyId,
      UnionId = request.UnionId,
      Period = request.Period,
      Status = DeclarationStatus.Draft,
      SubmissionDate = DateTime.Now,
      IsRectification = isRectification,
      PreviousAmount = lastPresented?.TotalAmount ?? 0,
      PresencialReference = $"DDJJ-{request.CompanyId}-{DateTime.Now:ssfff}"
    };

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
      // Si el estudio cargó un salario bruto para este empleado, se usa ese como base de cálculo;
      // si no (salario 0 o ausente), se calcula por escala (básico + antigüedad).
      if (enteredSalaries.TryGetValue(assign.EmployeeId, out var enteredBase) && enteredBase > 0)
      {
        remunerativeTotal = enteredBase;
      }
      else
      {
        // Cálculo de Antigüedad
        var employee = await unitOfWork.Employees.GetByIdAsync(assign.EmployeeId);
        int internalAntiguedad = 0;
        if (employee != null)
        {
          internalAntiguedad = targetDate.Year - employee.HireDate.Year;
          if (targetDate.Month < employee.HireDate.Month) internalAntiguedad--;
          if (internalAntiguedad < 0) internalAntiguedad = 0;
        }

        decimal basicSalary = scale.BasicAmount;
        decimal antiguedadAmount = basicSalary * (scale.AntiguedadPercentage / 100) * internalAntiguedad;
        remunerativeTotal = basicSalary + antiguedadAmount;
      }

      // Cálculos dinámicos basados en la configuración del Sindicato
      decimal lawContribution = remunerativeTotal * (union.LawContributionPercentage / 100);
      decimal socioContribution = assign.IsVoluntaryMember ? remunerativeTotal * (union.UnionMemberPercentage / 100) : 0;

      // Acuerdos No Remunerativos (suman directo)
      decimal nonRemunerativeTotal = scale.AgreementNR1 + scale.AgreementNR2;

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
    if (DateTime.Now > dueDate && declaration.BalanceAmount > 0)
    {
      var rates = await unitOfWork.InterestRates.GetAllAsync();
      var currentRate = rates.OrderByDescending(r => r.EffectiveDate)
                           .FirstOrDefault(r => r.EffectiveDate <= DateTime.Now)?.MonthlyRate ?? 0.03m;

      var monthsLate = ((DateTime.Now.Year - dueDate.Year) * 12) + DateTime.Now.Month - dueDate.Month;
      if (monthsLate < 1) monthsLate = 1; // Mínimo un mes de interés si ya venció

      interestAmount = declaration.BalanceAmount * currentRate * monthsLate;
      declaration.BalanceAmount += interestAmount;
    }

    // Generar Código de Barras ficticio para la boleta (Basado en CUIT + Periodo + Saldo/Total)
    decimal amountToPay = declaration.BalanceAmount > 0 ? declaration.BalanceAmount : 0;
    declaration.PresencialBarCode = $"001{branch.Cuit}{request.Period.Replace("-", "")}{((int)(amountToPay * 100)):D10}";

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
    catch { /* Log error but don't fail declaration creation */ }

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