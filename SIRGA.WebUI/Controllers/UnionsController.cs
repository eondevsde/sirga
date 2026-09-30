using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnionsController : ControllerBase
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly UnionSubscriptionService _subscriptionService;

  public UnionsController(IUnitOfWork unitOfWork, UnionSubscriptionService subscriptionService)
  {
    _unitOfWork = unitOfWork;
    _subscriptionService = subscriptionService;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var unions = await _unitOfWork.Unions.GetAllAsync();
    return Ok(unions);
  }

  /// <summary>
  /// Crea el usuario de Identity del sindicato. Antes era anónimo: cualquiera podía crear
  /// usuarios con rol Sindicato y contraseña fija 12345 desde afuera. Ahora exige el mismo rol
  /// que la pantalla que lo usa (/unions es Admin,Auditor) y la respuesta ya no expone la
  /// contraseña.
  /// </summary>
  [HttpPost("create-user")]
  [Authorize(Roles = "Admin,Auditor")]
  public async Task<IActionResult> CreateUnionUser([FromBody] UnionDto dto)
  {
    var (_, yaExistia, error) = await _subscriptionService.EnsureUnionUserAsync(dto.AdminEmail);

    if (error != null) return BadRequest(new { message = error });
    if (yaExistia) return Ok(new { message = "El usuario ya existe." });

    return Ok(new { message = "Usuario de sindicato creado con éxito." });
  }
}
