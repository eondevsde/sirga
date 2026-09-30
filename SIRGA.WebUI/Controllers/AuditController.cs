using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

// Antes era anónimo: cualquiera con la URL podía leer el dashboard financiero completo
// (recaudación y 4.352 deudores) y, peor, disparar el envío masivo de avisos por email.
// Las pantallas que lo usan ahora llaman a los servicios directamente.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Sindicato,Auditor,Admin")]
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
