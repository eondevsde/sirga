using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController(AuditService auditService, DebtNotificationService debtNotificationService) : ControllerBase
{
  [HttpGet("dashboard")]
  public async Task<ActionResult<AuditDashboardDto>> GetDashboard([FromQuery] int? unionId)
  {
    return Ok(await auditService.GetStatsAsync(unionId));
  }

  [HttpPost("notify-debtors")]
  public async Task<ActionResult<int>> NotifyDebtors()
  {
    var count = await debtNotificationService.NotifyAllDebtorsAsync();
    return Ok(count);
  }
}
