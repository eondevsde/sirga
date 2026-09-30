using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
  private readonly CompanyValidationService _validation;
  private readonly CompanyService _companyService;

  public CompaniesController(CompanyValidationService validation, CompanyService companyService)
  {
    _validation = validation;
    _companyService = companyService;
  }

  [Authorize(Roles = "EstudioContable,Sindicato,Auditor,Admin")]
  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] string? username)
  {
    // La composición se movió a CompanyService para que las pantallas puedan pedirla sin HTTP.
    if (!string.IsNullOrEmpty(username))
      return Ok(await _companyService.GetByStudioUsernameAsync(username));

    return Ok(await _companyService.GetAllOrderedAsync());
  }

  [Authorize(Roles = "EstudioContable,Sindicato,Auditor,Admin")]
  [HttpGet("{id}")]
  public async Task<IActionResult> GetById(int id)
  {
    var company = await _companyService.GetByIdAsync(id);
    if (company == null) return NotFound();
    return Ok(company);
  }

  [Authorize(Roles = "EstudioContable,Sindicato,Auditor,Admin")]
  [HttpGet("lookup")]
  public async Task<IActionResult> Lookup([FromQuery] string? cuit, [FromQuery] int? studioId)
  {
    // La composición se movió a CompanyValidationService para que el diálogo de alta la use
    // directamente, sin pasar por HTTP.
    return Ok(await _validation.LookupAsync(cuit, studioId));
  }

  /// <summary>
  /// Alta de empresa. La lógica (validación de CUIT, deduplicación, solicitud de vinculación y
  /// alta directa) vive en <see cref="CompanyService.CreateCompanyAsync"/>: acá sólo se traduce el
  /// resultado al código HTTP que corresponde. Exige los roles de las pantallas que la usan
  /// (el alta de empresas está en /companies y en el diálogo de sucursales).
  /// </summary>
  [Authorize(Roles = "EstudioContable,Sindicato,Auditor,Admin")]
  [HttpPost]
  public async Task<IActionResult> Create(Company company, [FromQuery] int? studioId, [FromQuery] int? unionId)
      => MapSaveResult(await _companyService.CreateCompanyAsync(company, studioId, unionId));

  /// <summary>
  /// Edición de empresa. Igual que el alta, la lógica está en
  /// <see cref="CompanyService.UpdateCompanyAsync"/>, que además valida la pertenencia cuando
  /// llega un username.
  /// </summary>
  [Authorize(Roles = "EstudioContable,Sindicato,Auditor,Admin")]
  [HttpPut("{id}")]
  public async Task<IActionResult> Update(int id, Company company, [FromQuery] string? username)
      => MapSaveResult(await _companyService.UpdateCompanyAsync(id, company, username));

  /// <summary>
  /// Traduce el resultado del servicio al mismo HTTP que devolvía el controlador cuando tenía la
  /// lógica adentro. El detalle que importa: BadRequest y NotFound <b>sin</b> mensaje se responden
  /// con ProblemDetails (como BadRequest() / NotFound() sin argumentos) y con mensaje se responden
  /// como texto plano.
  /// </summary>
  private IActionResult MapSaveResult(CompanySaveResult result) => result.Status switch
  {
    CompanySaveStatus.Created => CreatedAtAction(nameof(GetById), new { id = result.Company!.Id }, result.Company),
    CompanySaveStatus.Ok => NoContent(),
    CompanySaveStatus.Conflict => Conflict(result.Message),
    CompanySaveStatus.Forbidden => Forbid(),
    CompanySaveStatus.NotFound => result.Message == null ? NotFound() : NotFound(result.Message),
    CompanySaveStatus.InternalError => StatusCode(500, result.Message),
    _ => result.Message == null ? BadRequest() : BadRequest(result.Message)
  };
}
