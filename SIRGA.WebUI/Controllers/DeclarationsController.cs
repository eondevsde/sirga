using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeclarationsController : ControllerBase
{
  private readonly DeclarationService _declarationService;

  public DeclarationsController(DeclarationService declarationService)
  {
    _declarationService = declarationService;
  }

  /// <summary>
  /// Crea una nueva Declaración Jurada (DDJJ) en estado Borrador.
  /// </summary>
  [HttpPost]
  [Authorize(Roles = "EstudioContable,Auditor,Admin")]
  [ProducesResponseType(typeof(DeclarationResponseDto), StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> Create(DeclarationRequestDto request)
  {
    try
    {
      var result = await _declarationService.CreateDeclarationAsync(request);
      return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }
    catch (Exception ex)
    {
      return BadRequest(new { message = ex.Message });
    }
  }

  /// <summary>
  /// Obtiene todas las DDJJ para auditoría del Sindicato.
  /// </summary>
  [HttpGet]
  [ProducesResponseType(typeof(IEnumerable<DeclarationResponseDto>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetAll()
  {
    var result = await _declarationService.GetAllDeclarationsAsync();
    return Ok(result);
  }

  [HttpGet("by-union/{unionId}")]
  [ProducesResponseType(typeof(IEnumerable<DeclarationResponseDto>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetByUnion(int unionId)
  {
    var result = await _declarationService.GetDeclarationsByUnionAsync(unionId);
    return Ok(result);
  }

  [HttpGet("by-union/{unionId}/paged")]
  [ProducesResponseType(typeof(PagedDeclarationsDto), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetByUnionPaged(int unionId, int page = 1, int pageSize = 10)
  {
    var result = await _declarationService.GetDeclarationsByUnionPagedAsync(unionId, page, pageSize);
    return Ok(result);
  }

  [HttpGet("{id}")]
  [ProducesResponseType(typeof(DeclarationResponseDto), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetById(int id)
  {
    var result = await _declarationService.GetResponseByIdAsync(id);
    if (result == null) return NotFound();
    return Ok(result);
  }

  [HttpGet("history/{companyId}")]
  [ProducesResponseType(typeof(IEnumerable<DeclarationResponseDto>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetHistory(int companyId)
  {
    var result = await _declarationService.GetDeclarationsByCompanyAsync(companyId);
    return Ok(result);
  }

  [HttpGet("{id}/details")]
  [ProducesResponseType(typeof(IEnumerable<DeclarationDetailDto>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetDetails(int id)
  {
    var result = await _declarationService.GetDeclarationDetailsAsync(id);
    return Ok(result);
  }

  [HttpPost("approve")]
  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  public async Task<IActionResult> Approve(AuditActionRequestDto request)
  {
    try
    {
      await _declarationService.ApproveDeclarationAsync(request.DeclarationId, request.Observation);
      return Ok();
    }
    catch (Exception ex)
    {
      return BadRequest(ex.Message);
    }
  }

  [HttpPost("reject")]
  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  public async Task<IActionResult> Reject(AuditActionRequestDto request)
  {
    try
    {
      await _declarationService.RejectDeclarationAsync(request.DeclarationId, request.Observation);
      return Ok();
    }
    catch (Exception ex)
    {
      return BadRequest(ex.Message);
    }
  }
}
