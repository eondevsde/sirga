using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SIRGA.Application.DTOs;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnionsController : ControllerBase
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly UserManager<IdentityUser> _userManager;
  private readonly RoleManager<IdentityRole> _roleManager;

  public UnionsController(
    IUnitOfWork unitOfWork,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager)
  {
    _unitOfWork = unitOfWork;
    _userManager = userManager;
    _roleManager = roleManager;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var unions = await _unitOfWork.Unions.GetAllAsync();
    return Ok(unions);
  }

  [HttpPost("create-user")]
  public async Task<IActionResult> CreateUnionUser([FromBody] UnionDto dto)
  {
    if (string.IsNullOrEmpty(dto.AdminEmail))
    {
      return BadRequest("El email del administrador es requerido.");
    }

    var existingUser = await _userManager.FindByEmailAsync(dto.AdminEmail);
    if (existingUser != null)
    {
      return Ok(new { message = "El usuario ya existe." });
    }

    var user = new IdentityUser { UserName = dto.AdminEmail, Email = dto.AdminEmail, EmailConfirmed = true };
    var result = await _userManager.CreateAsync(user, "12345");

    if (result.Succeeded)
    {
      if (!await _roleManager.RoleExistsAsync("Sindicato"))
      {
        await _roleManager.CreateAsync(new IdentityRole("Sindicato"));
      }
      await _userManager.AddToRoleAsync(user, "Sindicato");
      return Ok(new { message = "Usuario de sindicato creado con éxito (pass: 12345)." });
    }

    return BadRequest(new { message = "Error al crear usuario", errors = result.Errors });
  }
}
