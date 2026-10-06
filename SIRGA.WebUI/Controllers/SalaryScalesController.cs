using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

/// <summary>
/// Escalas paritarias. Ninguna pantalla consume estos endpoints (SalaryScales.razor usa
/// SalaryScaleService directamente), así que se protegen sin conversión previa: la lectura
/// queda para Admin y Auditor, y la escritura sólo para Admin, igual que la pantalla.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SalaryScalesController(SalaryScaleService salaryScaleService) : ControllerBase
{
  [Authorize(Roles = "Admin,Auditor")]
  [HttpGet("{year}/{month}")]
  public async Task<ActionResult<IEnumerable<SalaryScaleDto>>> GetScales(int year, int month)
  {
    var period = new DateTime(year, month, 1);
    var scales = await salaryScaleService.GetScalesByPeriodAsync(period);
    return Ok(scales);
  }

  [Authorize(Roles = "Admin,Auditor")]
  [HttpGet]
  public async Task<ActionResult<IEnumerable<SalaryScaleDto>>> GetAll()
  {
    var scales = await salaryScaleService.GetAllAsync();
    return Ok(scales);
  }

  [Authorize(Roles = "Admin")]
  [HttpPost]
  public async Task<IActionResult> Create(SalaryScaleDto dto)
  {
    await salaryScaleService.CreateScaleAsync(dto);
    return Ok();
  }

  [Authorize(Roles = "Admin")]
  [HttpPut("{id}")]
  public async Task<IActionResult> Update(int id, SalaryScaleDto dto)
  {
    dto.Id = id;
    await salaryScaleService.UpdateScaleAsync(dto);
    return Ok();
  }

  /// <summary>
  /// Activa o desactiva una escala. Reemplaza al DELETE que había: la escala es histórica, así que una
  /// que no corresponde se desactiva y queda el rastro, en lugar de borrarse.
  /// </summary>
  [Authorize(Roles = "Admin")]
  [HttpPost("{id}/estado")]
  public async Task<IActionResult> SetActive(int id, [FromQuery] bool activa)
  {
    var (ok, error) = await salaryScaleService.SetActiveAsync(id, activa);
    if (ok) return Ok();
    return error == "La escala no existe." ? NotFound() : BadRequest(error);
  }
}
