using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalanceController(BalanceService balanceService) : ControllerBase
{
  [HttpGet("{companyId}")]
  public async Task<ActionResult<CompanyBalanceDto>> GetStatement(int companyId)
  {
    var result = await balanceService.GetFullStatementAsync(companyId);
    if (result == null) return NotFound();
    return Ok(result);
  }
}
