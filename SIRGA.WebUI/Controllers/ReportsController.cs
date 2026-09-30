using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using System.Threading.Tasks;

namespace SIRGA.WebUI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class ReportsController : ControllerBase
  {
    private readonly IReportService _reportService;
    private readonly EmployeeService _employeeService;

    public ReportsController(IReportService reportService, EmployeeService employeeService)
    {
      _reportService = reportService;
      _employeeService = employeeService;
    }

    [Authorize(Roles = "EstudioContable,Sindicato,Auditor,Admin")]
    [HttpGet("employees-excel")]
    public async Task<IActionResult> ExportEmployeesExcel([FromQuery] int? companyId)
    {
      // El filtro se resuelve en SQL, junto con la empresa de cada empleado.
      var employees = await _employeeService.GetEmployeesForPadronAsync(companyId);

      var sampleEmp = employees.FirstOrDefault();
      string companyName = sampleEmp?.Assignments?.FirstOrDefault(a => a.CompanyId == companyId)?.Company?.BusinessName ?? "Empresa";
      string title = companyId.HasValue ? $"Padrón de Afiliados - {companyName}" : "Padrón General de Afiliados";

      var content = await _reportService.ExportEmployeesToExcelAsync(employees, title, companyId);

      return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          $"Padron_{DateTime.Now:yyyyMMdd}.xlsx");
    }
  }
}