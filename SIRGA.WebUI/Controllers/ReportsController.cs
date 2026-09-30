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

    [HttpGet("employees-excel")]
    public async Task<IActionResult> ExportEmployeesExcel([FromQuery] int? companyId)
    {
      var employees = await _employeeService.GetAllAsync();

      if (companyId.HasValue)
      {
        // Filtrar empleados que tengan una asignación activa en esa empresa
        employees = employees
            .Where(e => e.Assignments.Any(a => a.CompanyId == companyId.Value))
            .ToList();
      }

      var sampleEmp = employees.FirstOrDefault();
      string companyName = sampleEmp?.Assignments?.FirstOrDefault(a => a.CompanyId == companyId)?.Company?.BusinessName ?? "Empresa";
      string title = companyId.HasValue ? $"Padrón de Afiliados - {companyName}" : "Padrón General de Afiliados";

      var content = await _reportService.ExportEmployeesToExcelAsync(employees, title);

      return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          $"Padron_{DateTime.Now:yyyyMMdd}.xlsx");
    }
  }
}