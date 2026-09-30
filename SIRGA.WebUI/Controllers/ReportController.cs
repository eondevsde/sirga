using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController(EmployeeService employeeService, IReportService reportService, AuditService auditService, DeclarationService declarationService, PdfReportService pdfService, InspectionService inspectionService) : ControllerBase
{
  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  [HttpGet("inspection/{id}")]
  public async Task<IActionResult> DownloadInspectionAct(int id)
  {
    var act = await inspectionService.GetActDetailAsync(id);
    if (act == null) return NotFound();

    var pdfBytes = pdfService.GenerateInspectionActPdf(act);
    return File(pdfBytes, "application/pdf", $"Acta_Inspeccion_{act.ActNumber}.pdf");
  }

  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  [HttpGet("payment-plan/{id}")]
  public async Task<IActionResult> DownloadPaymentPlan(int id)
  {
    var plan = await inspectionService.GetPaymentPlanDetailAsync(id);
    if (plan == null) return NotFound();

    var pdfBytes = pdfService.GeneratePaymentPlanPdf(plan);
    return File(pdfBytes, "application/pdf", $"Plan_Pago_{plan.ActNumber}.pdf");
  }

  [Authorize(Roles = "EstudioContable,Sindicato,Auditor,Admin")]
  [HttpGet("boleta/{id}")]
  public async Task<IActionResult> DownloadBoleta(int id)
  {
    var declaration = await declarationService.GetResponseByIdAsync(id);
    if (declaration == null) return NotFound();

    var pdfBytes = pdfService.GeneratePaymentBoleta(declaration);
    return File(pdfBytes, "application/pdf", $"Boleta_SIRGA_{declaration.Period}_{declaration.CompanyCuit}.pdf");
  }

  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  [HttpGet("visit/{id}")]
  public async Task<IActionResult> DownloadVisitNotice(int id)
  {
    var pdfBytes = await inspectionService.GenerateVisitPdfAsync(id);
    return File(pdfBytes, "application/pdf", $"Constancia_Visita_{id}.pdf");
  }

  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  [HttpGet("padron")]
  public async Task<IActionResult> DownloadPadron([FromQuery] int? companyId, [FromQuery] string? city)
  {
    // El filtro se resuelve en SQL, junto con la empresa de cada empleado.
    var employees = await employeeService.GetEmployeesForPadronAsync(companyId, city);

    var title = "Padrón de Beneficiarios SIRGA - " + DateTime.Now.ToString("dd/MM/yyyy");
    var fileBytes = await reportService.ExportEmployeesToExcelAsync(employees, title, companyId);

    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Padron_SIRGA_{DateTime.Now:yyyyMMdd}.xlsx");
  }

  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  [HttpGet("debtors-excel")]
  public async Task<IActionResult> DownloadDebtorsExcel()
  {
    var stats = await auditService.GetStatsAsync();
    var excelBytes = await reportService.ExportDebtorsToExcelAsync(stats.Debtors, "Reporte de Deudores y Proyección de Intereses");
    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Deudores_{DateTime.Now:yyyyMMdd}.xlsx");
  }

  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  [HttpGet("collections-excel")]
  public async Task<IActionResult> DownloadCollectionsExcel()
  {
    var stats = await auditService.GetStatsAsync();
    var excelBytes = await reportService.ExportCollectionsToExcelAsync(stats.MonthlyCollections, "Resumen de Recaudación Mensual");
    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Resumen_Recaudacion_{DateTime.Now:yyyyMMdd}.xlsx");
  }
}



