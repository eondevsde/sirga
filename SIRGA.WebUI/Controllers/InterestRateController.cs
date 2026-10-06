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

  [HttpPut("{id}")]
  public async Task<IActionResult> Update(int id, InterestRate rate)
  {
    if (id != rate.Id) return BadRequest("El id de la ruta no coincide con el de la tasa.");

    var (ok, error) = await interestRateService.UpdateAsync(rate);
    if (ok) return NoContent();
    return error == "La tasa no existe." ? NotFound() : BadRequest(error);
  }

  /// <summary>
  /// Activa o desactiva una tasa. Reemplaza al DELETE que había: la escala es histórica, así que una tasa
  /// que no corresponde se desactiva y queda el rastro, en lugar de borrarse.
  /// </summary>
  [HttpPost("{id}/estado")]
  public async Task<IActionResult> SetActive(int id, [FromQuery] bool activa)
  {
    var (ok, error) = await interestRateService.SetActiveAsync(id, activa);
    if (ok) return NoContent();
    return error == "La tasa no existe." ? NotFound() : BadRequest(error);
  }
}
