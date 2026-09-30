using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.WebUI.Controllers;

/// <summary>
/// Tasas de interés de ARCA. Estaban anónimas: cualquiera podía leerlas, crear tasas falsas o
/// borrar las existentes, y con eso alterar el cálculo de intereses por mora. La pantalla
/// (/admin/interest-rates, sólo Admin) las usa a través de InterestRateService, así que se puede
/// exigir rol sin romperla.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class InterestRateController(InterestRateService interestRateService) : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<IEnumerable<InterestRate>>> GetAll()
  {
    return Ok(await interestRateService.GetAllAsync());
  }

  [HttpPost]
  public async Task<ActionResult<InterestRate>> Create(InterestRate rate)
  {
    var creada = await interestRateService.CreateAsync(rate);
    return CreatedAtAction(nameof(GetAll), new { id = creada.Id }, creada);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(int id)
  {
    if (!await interestRateService.DeleteAsync(id)) return NotFound();
    return NoContent();
  }
}
