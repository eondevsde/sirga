using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
  private readonly EmployeeService _employeeService;

  public EmployeesController(EmployeeService employeeService)
  {
    _employeeService = employeeService;
  }

  /// <summary>
  /// Empleados paginados y ordenados por nombre. Antes devolvía los 27.934 completos
  /// (4,76 MB de JSON) en un endpoint anónimo. Para los de una empresa usar
  /// <c>/api/employees/by-company/{companyId}</c>.
  /// </summary>
  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 200)
  {
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 500) pageSize = 200;

    return Ok(await _employeeService.GetPagedAsync(page, pageSize));
  }

  [HttpGet("by-company/{companyId}")]
  public async Task<IActionResult> GetByCompany(int companyId)
  {
    return Ok(await _employeeService.GetEmployeesByCompanyAsync(companyId));
  }

  [HttpGet("by-assignment/{companyId}/{unionId}")]
  public async Task<IActionResult> GetByAssignment(int companyId, int unionId)
  {
    return Ok(await _employeeService.GetEmployeesByAssignmentAsync(companyId, unionId));
  }

  [HttpPost]
  public async Task<IActionResult> Create(Employee employee)
  {
    try
    {
      await _employeeService.AddAsync(employee);
      return Ok(employee);
    }
    catch (Exception ex)
    {
      return BadRequest(new { message = ex.Message });
    }
  }

  [HttpPatch("transfer/{employeeId}")]
  public async Task<IActionResult> Transfer(int employeeId, [FromQuery] int fromCompanyId, [FromQuery] int toCompanyId)
  {
    try
    {
      await _employeeService.TransferEmployeeAsync(employeeId, fromCompanyId, toCompanyId);
      return Ok(new { message = "Empleado transferido exitosamente" });
    }
    catch (Exception ex)
    {
      return BadRequest(new { message = ex.Message });
    }
  }

  [Authorize(Roles = "EstudioContable,Admin")]
  [HttpPost("membership/resign")]
  public async Task<IActionResult> RequestMembershipResignation(MembershipResignationRequestDto dto)
  {
    var (success, error) = await _employeeService.RequestMembershipResignationAsync(dto.AssignmentId, dto.Reason);
    if (!success) return BadRequest(new { message = error });
    return Ok(new { message = "Solicitud de renuncia registrada. El sindicato debe aprobarla." });
  }

  [Authorize(Roles = "Sindicato,Admin")]
  [HttpGet("membership/requests")]
  public async Task<IActionResult> GetMembershipResignationRequests([FromQuery] int unionId)
  {
    return Ok(await _employeeService.GetMembershipResignationRequestsAsync(unionId));
  }

  [Authorize(Roles = "Sindicato,Admin")]
  [HttpPost("membership/resolve")]
  public async Task<IActionResult> ResolveMembershipResignation(MembershipResignationResolveDto dto)
  {
    var (success, error) = await _employeeService.ResolveMembershipResignationAsync(dto.AssignmentId, dto.Approve, dto.Comment);
    if (!success) return BadRequest(new { message = error });
    return Ok(new { message = dto.Approve ? "Renuncia aprobada. El empleado dejó de ser socio." : "Solicitud de renuncia rechazada. El empleado continúa siendo socio." });
  }
}
