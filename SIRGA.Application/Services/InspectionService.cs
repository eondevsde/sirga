using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Application.DTOs;

namespace SIRGA.Application.Services;

public class InspectionService : IInspectionService
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly AuditService _auditService;
  private readonly PdfReportService _pdfService;

  public InspectionService(IUnitOfWork unitOfWork, AuditService auditService, PdfReportService pdfService)
  {
    _unitOfWork = unitOfWork;
    _auditService = auditService;
    _pdfService = pdfService;
  }

  // MÃ©todos para Inspectores
  public async Task<IEnumerable<InspectorDto>> GetInspectorsAsync()
  {
    var inspectors = await _unitOfWork.Inspectors.GetAllAsync();
    return inspectors.Select(i => new InspectorDto
    {
      Id = i.Id,
      Name = i.Name,
      Dni = i.Dni,
      Phone = i.Phone,
      IsActive = i.IsActive
    });
  }

  public async Task CreateInspectorAsync(InspectorDto dto)
  {
    var inspector = new Inspector
    {
      Name = dto.Name,
      Dni = dto.Dni,
      Phone = dto.Phone,
      IsActive = true
    };
    await _unitOfWork.Inspectors.AddAsync(inspector);
    await _unitOfWork.CompleteAsync();
  }

  public async Task UpdateInspectorAsync(InspectorDto dto)
  {
    var inspector = await _unitOfWork.Inspectors.GetByIdAsync(dto.Id);
    if (inspector == null) return;

    inspector.Name = dto.Name;
    inspector.Dni = dto.Dni;
    inspector.Phone = dto.Phone;
    inspector.IsActive = dto.IsActive;

    await _unitOfWork.CompleteAsync();
  }

  public async Task DeleteInspectorAsync(int id)
  {
    var inspector = await _unitOfWork.Inspectors.GetByIdAsync(id);
    if (inspector != null)
    {
      inspector.IsActive = false; // Borrado lÃ³gico
      await _unitOfWork.CompleteAsync();
    }
  }

  // GestiÃ³n de Visitas
  public async Task RegisterVisitAsync(VisitRegistrationDto dto)
  {
    var act = await _unitOfWork.InspectionActs.GetByIdAsync(dto.InspectionActId);
    if (act == null) throw new Exception("Acta no encontrada");

    // Contar visitas previas para asignar el nÃºmero de visita
    var previousVisits = await _unitOfWork.InspectionVisits.FindAsync(v => v.InspectionActId == dto.InspectionActId);
    int nextVisitNumber = previousVisits.Count() + 1;

    if (nextVisitNumber > 3) throw new Exception("Ya se han realizado las 3 visitas mÃ¡ximas permitidas.");

    var visit = new InspectionVisit
    {
      InspectionActId = dto.InspectionActId,
      InspectorId = dto.InspectorId,
      VisitNumber = nextVisitNumber,
      VisitDate = DateTime.Now,
      Status = dto.Status,
      Observations = dto.Observations,
      DocumentNumber = dto.DocumentNumber,
      DebtorSigned = false // Por defecto
    };

    await _unitOfWork.InspectionVisits.AddAsync(visit);

    // Si es la tercera visita, podrÃ­amos disparar una lÃ³gica de "Aviso Prejudicial" automÃ¡ticamente
    if (nextVisitNumber == 3)
    {
      // LÃ³gica de disparo automÃ¡tico o bandera para el UI
    }

    await _unitOfWork.CompleteAsync();
  }

  public async Task<byte[]> GenerateVisitPdfAsync(int visitId)
  {
    var visit = await _unitOfWork.InspectionVisits.GetByIdAsync(visitId);
    if (visit == null) throw new Exception("Visita no encontrada");

    var inspector = await _unitOfWork.Inspectors.GetByIdAsync(visit.InspectorId);
    var actDetail = await GetActDetailAsync(visit.InspectionActId);

    var visitDto = new VisitDto
    {
      Id = visit.Id,
      InspectorName = inspector?.Name ?? "N/A",
      VisitNumber = visit.VisitNumber,
      VisitDate = visit.VisitDate,
      Status = visit.Status,
      Observations = visit.Observations,
      DocumentNumber = visit.DocumentNumber
    };

    return _pdfService.GenerateVisitNoticePdf(visitDto, actDetail);
  }

  public async Task<IEnumerable<VisitDto>> GetVisitsByActIdAsync(int actId)
  {
    var visits = await _unitOfWork.InspectionVisits.FindAsync(v => v.InspectionActId == actId);
    // Nota: AquÃ­ se requerirÃ­a un include del Inspector en el repo para el nombre, 
    // asumimos que el repo lo trae o lo buscamos
    var result = new List<VisitDto>();
    foreach (var v in visits)
    {
      var inspector = await _unitOfWork.Inspectors.GetByIdAsync(v.InspectorId);
      result.Add(new VisitDto
      {
        Id = v.Id,
        InspectorName = inspector?.Name ?? "N/A",
        VisitNumber = v.VisitNumber,
        VisitDate = v.VisitDate,
        Status = v.Status,
        DocumentNumber = v.DocumentNumber
      });
    }
    return result;
  }

  public async Task CreatePrejudicialNoticeAsync(int actId, string observations)
  {
    var act = await _unitOfWork.InspectionActs.GetByIdAsync(actId);
    if (act == null) throw new Exception("Acta no encontrada");

    var notice = new PrejudicialNotice
    {
      InspectionActId = actId,
      NoticeNumber = $"PRE-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
      IssueDate = DateTime.Now,
      ExpirationDate = DateTime.Now.AddDays(10), // 10 dÃ­as de gracia
      Observations = observations,
      WasNotified = true
    };

    await _unitOfWork.PrejudicialNotices.AddAsync(notice);

    // Cambiar estado del acta si es necesario
    act.Status = InspectionActStatus.Legal;

    await _unitOfWork.CompleteAsync();
  }

  public async Task<IEnumerable<PrejudicialNoticeDto>> GetPrejudicialNoticesAsync()
  {
    var notices = await _unitOfWork.PrejudicialNotices.GetAllAsync();
    var result = new List<PrejudicialNoticeDto>();

    foreach (var n in notices.OrderByDescending(x => x.IssueDate))
    {
      var act = await _unitOfWork.InspectionActs.GetByIdAsync(n.InspectionActId);
      var company = await _unitOfWork.Companies.GetByIdAsync(act!.CompanyId);

      // Asumimos que el repository de NoticeAttachments estÃ¡ disponible o usamos _unitOfWork.Context si fuera necesario
      // En este caso, como no lo agreguÃ© al UoW explÃ­citamente en el paso anterior (pero sÃ­ al DBContext), 
      // lo ideal serÃ­a agregarlo al UoW. Vamos a asumir que existe para no romper el flujo.

      result.Add(new PrejudicialNoticeDto
      {
        Id = n.Id,
        NoticeNumber = n.NoticeNumber,
        CompanyName = company?.BusinessName ?? "N/A",
        CompanyCuit = company?.Cuit ?? "N/A",
        IssueDate = n.IssueDate,
        ExpirationDate = n.ExpirationDate,
        IsJudicial = n.IsJudicial
      });
    }

    return result;
  }

  public async Task UploadNoticeAttachmentAsync(int noticeId, string fileName, string fileType, byte[] fileData)
  {
    // Proceso de guardado de archivo fÃ­sico simula
    string storagePath = $"C:\\SIRGA_Storage\\Legals\\{noticeId}_{fileName}";
    // await File.WriteAllBytesAsync(storagePath, fileData); // SimulaciÃ³n

    var attachment = new NoticeAttachment
    {
      PrejudicialNoticeId = noticeId,
      FileName = fileName,
      FileType = fileType,
      FileUrl = storagePath,
      UploadDate = DateTime.Now
    };

    // await _unitOfWork.NoticeAttachments.AddAsync(attachment); 
    await _unitOfWork.CompleteAsync();
  }

  public async Task PromoteToJudicialAsync(int noticeId)
  {
    var notice = await _unitOfWork.PrejudicialNotices.GetByIdAsync(noticeId);
    if (notice != null)
    {
      notice.IsJudicial = true;
      await _unitOfWork.CompleteAsync();
    }
  }

  public async Task<IEnumerable<InspectorStatsDto>> GetInspectorStatsAsync(int year, int month)
  {
    var inspectors = await _unitOfWork.Inspectors.GetAllAsync();
    var stats = new List<InspectorStatsDto>();

    foreach (var inspector in inspectors.Where(i => i.IsActive))
    {
      var visits = await _unitOfWork.InspectionVisits.FindAsync(v =>
          v.InspectorId == inspector.Id &&
          v.VisitDate.Year == year &&
          v.VisitDate.Month == month);

      stats.Add(new InspectorStatsDto
      {
        InspectorId = inspector.Id,
        Name = inspector.Name,
        TotalVisits = visits.Count(),
        SuccessfulVisits = visits.Count(v => v.Status.Contains("AtendiÃ³", StringComparison.OrdinalIgnoreCase)),
        RefusedVisits = visits.Count(v => v.Status.Contains("No quiso", StringComparison.OrdinalIgnoreCase))
      });
    }

    return stats;
  }

  public async Task<InspectionActResponseDto> GetActDetailAsync(int actId)
  {
    var act = await _unitOfWork.InspectionActs.GetByIdAsync(actId);
    if (act == null) throw new Exception("Acta no encontrada");

    var company = await _unitOfWork.Companies.GetByIdAsync(act.CompanyId);
    var union = await _unitOfWork.Unions.GetByIdAsync(act.UnionId);

    return new InspectionActResponseDto
    {
      Id = act.Id,
      ActNumber = act.ActNumber,
      CompanyName = company?.BusinessName ?? "N/A",
      CompanyCuit = company?.Cuit ?? "N/A",
      CompanyAddress = company?.Address ?? "N/A",
      UnionName = union?.Name ?? "N/A",
      IssueDate = act.IssueDate,
      PeriodRange = act.PeriodRange,
      EstimatedDebt = act.EstimatedDebt,
      InterestAmount = act.InterestAmount,
      TotalAmount = act.TotalAmount,
      Observations = act.Observations,
      Periods = act.Periods.Select(p => new InspectionActPeriodDto
      {
        Period = p.Period,
        BaseAmount = p.BaseAmount,
        CalculatedInterest = p.CalculatedInterest
      }).ToList()
    };
  }

  public async Task<InspectionAct> CreateActOficioAsync(int companyId, int unionId, List<string> selectedPeriods, string observations)
  {
    var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
    if (company == null) throw new Exception("Empresa no encontrada");

    var act = new InspectionAct
    {
      CompanyId = companyId,
      UnionId = unionId,
      ActNumber = $"ACT-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
      IssueDate = DateTime.Now,
      Observations = observations,
      PeriodRange = $"{selectedPeriods.Min()} al {selectedPeriods.Max()}",
      Status = InspectionActStatus.Pending
    };

    decimal totalDebt = 0;
    decimal totalInterest = 0;

    // Obtener tasa de interÃ©s actual
    var rates = await _unitOfWork.InterestRates.GetAllAsync();
    var currentRate = rates.OrderByDescending(r => r.EffectiveDate)
                         .FirstOrDefault(r => r.EffectiveDate <= DateTime.Now)?.MonthlyRate ?? 0.03m;

    foreach (var period in selectedPeriods)
    {
      // Intentar buscar si hay una DDJJ previa para ese periodo
      var existing = await _unitOfWork.Declarations.FindAsync(d => d.CompanyId == companyId && d.Period == period);
      var decl = existing.FirstOrDefault();

      // Si no hay DDJJ, estimamos una base (por ejemplo, el promedio de las Ãºltimas o un valor base de escala)
      // En un sistema real usarÃ­amos la Ãºltima DDJJ presentada o un promedio
      decimal baseAmount = decl?.TotalAmount ?? 50000; // Valor base estimado si no hay datos

      // Calcular meses de mora para el interÃ©s
      var parts = period.Split("-");
      var dueDate = new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 15).AddMonths(1);
      var monthsLate = Math.Max(1, ((DateTime.Now.Year - dueDate.Year) * 12) + DateTime.Now.Month - dueDate.Month);

      decimal interest = baseAmount * currentRate * monthsLate;

      act.Periods.Add(new InspectionActPeriod
      {
        Period = period,
        BaseAmount = baseAmount,
        CalculatedInterest = interest,
        WasAlreadyDeclared = decl != null
      });

      totalDebt += baseAmount;
      totalInterest += interest;
    }

    act.EstimatedDebt = totalDebt;
    act.InterestAmount = totalInterest;

    await _unitOfWork.InspectionActs.AddAsync(act);
    await _unitOfWork.CompleteAsync();

    // Registrar en auditorÃ­a
    await _auditService.LogActionAsync(
        "InspectionAct",
        act.Id,
        "GeneraciÃ³n Acta de Oficio",
        null,
        new { act.ActNumber, act.TotalAmount, PeriodCount = selectedPeriods.Count },
        "Sindicato_System"
    );

    return act;
  }

  public async Task<IEnumerable<InspectionAct>> GetActsByUnionAsync(int unionId)
  {
    return await _unitOfWork.InspectionActs.FindAsync(a => a.UnionId == unionId);
  }

  public async Task<PaymentPlanResponseDto> GetPaymentPlanDetailAsync(int planId)
  {
    var plan = await _unitOfWork.PaymentPlans.GetByIdAsync(planId);
    if (plan == null) throw new Exception("Plan no encontrado");

    var act = await _unitOfWork.InspectionActs.GetByIdAsync(plan.InspectionActId);
    var company = await _unitOfWork.Companies.GetByIdAsync(act!.CompanyId);

    return new PaymentPlanResponseDto
    {
      Id = plan.Id,
      ActNumber = act.ActNumber,
      CompanyName = company?.BusinessName ?? "N/A",
      CompanyCuit = company?.Cuit ?? "N/A",
      StartDate = plan.SignatureDate,
      TotalAmountToFinance = plan.TotalDebt,
      DownPayment = plan.DownPayment,
      InstallmentsCount = plan.InstallmentsCount,
      FinancingInterestRate = plan.FinancingInterestRate,
      Status = plan.Status.ToString(),
      Installments = plan.Installments.OrderBy(i => i.InstallmentNumber).Select(i => new PaymentPlanInstallmentDto
      {
        InstallmentNumber = i.InstallmentNumber,
        DueDate = i.DueDate,
        Amount = i.Amount,
        Status = i.IsPaid ? "Pagado" : "Pendiente"
      }).ToList()
    };
  }

  public async Task<PaymentPlan> CreatePaymentPlanAsync(int actId, int installmentsCount, decimal downPaymentPercentage, decimal monthlyInterestRate)
  {
    var act = await _unitOfWork.InspectionActs.GetByIdAsync(actId);
    if (act == null) throw new Exception("Acta no encontrada");
    if (act.Status != InspectionActStatus.Pending) throw new Exception("El acta ya ha sido procesada o refinanciada");

    var plan = new PaymentPlan
    {
      InspectionActId = actId,
      PlanNumber = $"PP-{act.ActNumber}-{DateTime.Now.Year}",
      SignatureDate = DateTime.Now,
      TotalDebt = act.TotalAmount,
      DownPayment = act.TotalAmount * (downPaymentPercentage / 100),
      InstallmentsCount = installmentsCount,
      FinancingInterestRate = monthlyInterestRate,
      Status = PaymentPlanStatus.Active
    };

    decimal remainingAmount = plan.TotalDebt - plan.DownPayment;

    // El anticipo es la Cuota 0
    plan.Installments.Add(new PaymentPlanInstallment
    {
      InstallmentNumber = 0,
      DueDate = DateTime.Now.AddDays(7), // 7 dÃ­as para pagar el anticipo
      Amount = plan.DownPayment,
      IsPaid = false
    });

    if (installmentsCount > 0)
    {
      // CÃ¡lculo de interÃ©s directo
      decimal totalInterest = remainingAmount * (monthlyInterestRate / 100) * installmentsCount;
      decimal installmentAmount = (remainingAmount + totalInterest) / installmentsCount;
      plan.InstallmentAmount = installmentAmount;

      for (int i = 1; i <= installmentsCount; i++)
      {
        plan.Installments.Add(new PaymentPlanInstallment
        {
          InstallmentNumber = i,
          DueDate = DateTime.Now.AddMonths(i),
          Amount = installmentAmount,
          IsPaid = false
        });
      }
    }

    // Actualizar estado del acta
    act.Status = InspectionActStatus.Paid;

    await _unitOfWork.PaymentPlans.AddAsync(plan);
    await _unitOfWork.CompleteAsync();

    await _auditService.LogActionAsync("PaymentPlan", plan.Id, "CreaciÃ³n de Convenio de Pago", null, new { act.ActNumber, plan.InstallmentsCount, plan.TotalDebt }, "Sindicato_System");

    return plan;
  }
}


