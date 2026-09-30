using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;
using SIRGA.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly AssociationService _associationService;
  private readonly CompanyValidationService _validation;
  private readonly CompanyService _companyService;

  public CompaniesController(IUnitOfWork unitOfWork, AssociationService associationService, CompanyValidationService validation, CompanyService companyService)
  {
    _unitOfWork = unitOfWork;
    _associationService = associationService;
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

  [HttpPost]
  public async Task<IActionResult> Create(Company company, [FromQuery] int? studioId, [FromQuery] int? unionId)
  {
    try
    {
      // Validar CUIT (formato + dígito verificador) para casas centrales
      if (!company.ParentCompanyId.HasValue)
      {
        var (ok, error) = CompanyValidationService.ValidateCuit(company.Cuit);
        if (!ok) return BadRequest(error);
      }

      // Alta por estudio: pendiente de autorización + solicitud de asociación
      if (studioId.HasValue && unionId.HasValue)
      {
        var studio = await _unitOfWork.AccountingStudios.GetByIdAsync(studioId.Value);
        if (studio == null) return BadRequest("El estudio contable no existe.");
        if (!studio.IsActive) return BadRequest("El estudio contable está desactivado.");
        if (!studio.IsApproved) return BadRequest("El estudio aún no fue aprobado para operar.");

        var existing = await _validation.FindParentByCuitAsync(company.Cuit);

        // La empresa ya existe: no duplicamos, solo controlamos pertenencia/solicitud
        if (existing != null)
        {
          var owner = existing.AccountingStudioId is { } oid
              ? await _unitOfWork.AccountingStudios.GetByIdAsync(oid)
              : null;
          var ownedByActive = owner is { IsActive: true, IsApproved: true };

          if (ownedByActive)
            return Conflict($"La empresa con CUIT {CompanyValidationService.FormatCuit(existing.Cuit)} ya pertenece al estudio contable '{owner!.Name}'.");

          if (existing.AccountingStudioId == studioId)
            return Conflict("Ya tienes esta empresa a cargo.");

          var (created, error) = await _associationService.CreateRequestAsync(studioId.Value, existing.Id, unionId.Value);
          if (!created) return Conflict(error);

          return CreatedAtAction(nameof(GetById), new { id = existing.Id }, existing);
        }

        company.Status = CompanyStatus.PendingAuthorization;
        company.AccountingStudioId = null;

        var dups = await _validation.GetDuplicatesAsync(company);
        if (dups.BusinessNameDuplicate != null)
          return Conflict($"Ya existe una empresa registrada con la razón social '{company.BusinessName}'.");
        if (dups.AddressDuplicate != null)
          return Conflict($"Ya existe una empresa registrada en la misma dirección: '{company.Address}'.");
        if (dups.PhoneDuplicate != null)
          return Conflict($"Ya existe una empresa registrada con el teléfono {company.Phone}.");

        await _unitOfWork.Companies.AddAsync(company);
        await _unitOfWork.CompleteAsync();

        var (created2, error2) = await _associationService.CreateRequestAsync(studioId.Value, company.Id, unionId.Value);
        if (!created2) return Conflict(error2);

        return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
      }

      // Alta directa (Admin) - las sucursales comparten CUIT y razón social con su central, no se deduplican
      if (!company.ParentCompanyId.HasValue)
      {
        var directDups = await _validation.GetDuplicatesAsync(company);
        if (directDups.CuitDuplicate != null || directDups.BusinessNameDuplicate != null)
          return Conflict("Ya existe una empresa con el mismo CUIT o razón social.");
        if (directDups.AddressDuplicate != null)
          return Conflict("Ya existe una empresa registrada en la misma dirección.");
        if (directDups.PhoneDuplicate != null)
          return Conflict("Ya existe una empresa registrada con el mismo teléfono.");
      }

      company.Status = CompanyStatus.Active;
      await _unitOfWork.Companies.AddAsync(company);
      await _unitOfWork.CompleteAsync();
      return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
    }
    catch (DbUpdateException)
    {
      // Índice único: posible carrera de doble alta
      return Conflict("Ya existe una empresa con el mismo CUIT y nombre de sede.");
    }
    catch (Exception ex)
    {
      return StatusCode(500, $"Error interno: {ex.Message}");
    }
  }

  [HttpPut("{id}")]
  public async Task<IActionResult> Update(int id, Company company, [FromQuery] string? username)
  {
    try
    {
      if (id != company.Id) return BadRequest();

      var existing = await _unitOfWork.Companies.GetByIdAsync(id);
      if (existing == null) return NotFound();

      // Control de pertenencia: un estudio solo edita sus propias empresas
      if (!string.IsNullOrEmpty(username))
      {
        var studio = (await _unitOfWork.AccountingStudios.FindAsync(s => s.ResponsibleUsername == username)).FirstOrDefault();
        if (studio == null) return Forbid();
        if (existing.AccountingStudioId != studio.Id) return Forbid();
      }

      if (!company.ParentCompanyId.HasValue)
      {
        var (ok, error) = CompanyValidationService.ValidateCuit(company.Cuit);
        if (!ok) return BadRequest(error);
      }

      var dups = await _validation.GetDuplicatesAsync(company, ignoreId: id);
      if (dups.CuitDuplicate != null)
        return Conflict($"El CUIT {CompanyValidationService.FormatCuit(company.Cuit)} ya pertenece a otra empresa.");
      if (dups.BusinessNameDuplicate != null)
        return Conflict($"Ya existe otra empresa registrada con la razón social '{company.BusinessName}'.");
      if (dups.AddressDuplicate != null)
        return Conflict("Ya existe otra empresa registrada en la misma dirección.");
      if (dups.PhoneDuplicate != null)
        return Conflict("Ya existe otra empresa registrada con el mismo teléfono.");

      existing.BusinessName = company.BusinessName;
      existing.Cuit = company.Cuit;
      existing.Address = company.Address;
      existing.Phone = company.Phone;
      existing.BranchName = company.BranchName;
      existing.CityId = company.CityId;
      existing.ContactEmail = company.ContactEmail;
      existing.ParentCompanyId = company.ParentCompanyId;
      existing.ReferencePerson = company.ReferencePerson;
      existing.ReferencePhone = company.ReferencePhone;

      await _unitOfWork.CompleteAsync();
      return NoContent();
    }
    catch (DbUpdateException)
    {
      return Conflict("Ya existe una empresa con el mismo CUIT y nombre de sede.");
    }
    catch (Exception ex)
    {
      return StatusCode(500, $"Error interno: {ex.Message}");
    }
  }
}