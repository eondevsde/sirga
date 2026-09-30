using Microsoft.EntityFrameworkCore;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Application.Services;

public class CompanyService
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly CompanyValidationService _validation;
  private readonly AssociationService _associationService;

  public CompanyService(IUnitOfWork unitOfWork, CompanyValidationService validation, AssociationService associationService)
  {
    _unitOfWork = unitOfWork;
    _validation = validation;
    _associationService = associationService;
  }

  public async Task<IEnumerable<Company>> GetAllAsync() => await _unitOfWork.Companies.GetAllAsync();

  /// <summary>Todas las empresas ordenadas por razón social (como las devolvía GET /api/companies).</summary>
  public async Task<IEnumerable<Company>> GetAllOrderedAsync()
  {
    var companies = await _unitOfWork.Companies.GetAllAsync();
    return companies.OrderBy(c => c.BusinessName, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Empresas del estudio contable dueño de ese usuario. Si el usuario no corresponde a ningún
  /// estudio devuelve vacío, para no mostrarle empresas de otros.
  /// </summary>
  public async Task<IEnumerable<Company>> GetByStudioUsernameAsync(string username)
  {
    var studio = (await _unitOfWork.AccountingStudios.FindAsync(s => s.ResponsibleUsername == username))
        .FirstOrDefault();
    if (studio == null) return Enumerable.Empty<Company>();

    var companies = await _unitOfWork.Companies.FindAsync(c => c.AccountingStudioId == studio.Id);
    return companies.OrderBy(c => c.BusinessName, StringComparer.OrdinalIgnoreCase);
  }

  public async Task<Company?> GetByIdAsync(int id) => await _unitOfWork.Companies.GetByIdAsync(id);

  public async Task AddAsync(Company company)
  {
    await _unitOfWork.Companies.AddAsync(company);
    await _unitOfWork.CompleteAsync();
  }

  public async Task RegisterCompanyAsync(string email, string businessName, string cuit, string address, string? phone, string? refPerson, string? refPhone, int? cityId)
  {
    var company = new Company
    {
      ContactEmail = email,
      BusinessName = businessName,
      Cuit = cuit,
      Address = address,
      Phone = phone,
      ReferencePerson = refPerson,
      ReferencePhone = refPhone,
      CityId = cityId,
      Status = SIRGA.Domain.Enums.CompanyStatus.PendingAuthorization,
      BranchName = "Sede Central"
    };

    await _unitOfWork.Companies.AddAsync(company);
    await _unitOfWork.CompleteAsync();
  }

  /// <summary>
  /// Alta de empresa. Es la misma lógica que estaba en CompaniesController (con los mismos
  /// resultados, incluidos los códigos de respuesta), movida acá para que las pantallas puedan
  /// dar de alta sin pasar por HTTP.
  /// </summary>
  public async Task<CompanySaveResult> CreateCompanyAsync(Company company, int? studioId, int? unionId)
  {
    try
    {
      // Validar CUIT (formato + dígito verificador) para casas centrales
      if (!company.ParentCompanyId.HasValue)
      {
        var (ok, error) = CompanyValidationService.ValidateCuit(company.Cuit);
        if (!ok) return CompanySaveResult.BadRequest(error);
      }

      // Alta por estudio: pendiente de autorización + solicitud de asociación
      if (studioId.HasValue && unionId.HasValue)
      {
        var studio = await _unitOfWork.AccountingStudios.GetByIdAsync(studioId.Value);
        if (studio == null) return CompanySaveResult.BadRequest("El estudio contable no existe.");
        if (!studio.IsActive) return CompanySaveResult.BadRequest("El estudio contable está desactivado.");
        if (!studio.IsApproved) return CompanySaveResult.BadRequest("El estudio aún no fue aprobado para operar.");

        var existing = await _validation.FindParentByCuitAsync(company.Cuit);

        // La empresa ya existe: no duplicamos, solo controlamos pertenencia/solicitud
        if (existing != null)
        {
          var owner = existing.AccountingStudioId is { } oid
              ? await _unitOfWork.AccountingStudios.GetByIdAsync(oid)
              : null;
          var ownedByActive = owner is { IsActive: true, IsApproved: true };

          if (ownedByActive)
            return CompanySaveResult.Conflict($"La empresa con CUIT {CompanyValidationService.FormatCuit(existing.Cuit)} ya pertenece al estudio contable '{owner!.Name}'.");

          if (existing.AccountingStudioId == studioId)
            return CompanySaveResult.Conflict("Ya tienes esta empresa a cargo.");

          var (created, error) = await _associationService.CreateRequestAsync(studioId.Value, existing.Id, unionId.Value);
          if (!created) return CompanySaveResult.Conflict(error);

          return CompanySaveResult.Created(existing);
        }

        company.Status = CompanyStatus.PendingAuthorization;
        company.AccountingStudioId = null;

        var dups = await _validation.GetDuplicatesAsync(company);
        if (dups.BusinessNameDuplicate != null)
          return CompanySaveResult.Conflict($"Ya existe una empresa registrada con la razón social '{company.BusinessName}'.");
        if (dups.AddressDuplicate != null)
          return CompanySaveResult.Conflict($"Ya existe una empresa registrada en la misma dirección: '{company.Address}'.");
        if (dups.PhoneDuplicate != null)
          return CompanySaveResult.Conflict($"Ya existe una empresa registrada con el teléfono {company.Phone}.");

        await _unitOfWork.Companies.AddAsync(company);
        await _unitOfWork.CompleteAsync();

        var (created2, error2) = await _associationService.CreateRequestAsync(studioId.Value, company.Id, unionId.Value);
        if (!created2) return CompanySaveResult.Conflict(error2);

        return CompanySaveResult.Created(company);
      }

      // Alta directa (Admin) - las sucursales comparten CUIT y razón social con su central, no se deduplican
      if (!company.ParentCompanyId.HasValue)
      {
        var directDups = await _validation.GetDuplicatesAsync(company);
        if (directDups.CuitDuplicate != null || directDups.BusinessNameDuplicate != null)
          return CompanySaveResult.Conflict("Ya existe una empresa con el mismo CUIT o razón social.");
        if (directDups.AddressDuplicate != null)
          return CompanySaveResult.Conflict("Ya existe una empresa registrada en la misma dirección.");
        if (directDups.PhoneDuplicate != null)
          return CompanySaveResult.Conflict("Ya existe una empresa registrada con el mismo teléfono.");
      }

      company.Status = CompanyStatus.Active;
      await _unitOfWork.Companies.AddAsync(company);
      await _unitOfWork.CompleteAsync();
      return CompanySaveResult.Created(company);
    }
    catch (DbUpdateException)
    {
      // Índice único: posible carrera de doble alta
      return CompanySaveResult.Conflict("Ya existe una empresa con el mismo CUIT y nombre de sede.");
    }
    catch (Exception ex)
    {
      return CompanySaveResult.InternalError($"Error interno: {ex.Message}");
    }
  }

  /// <summary>
  /// Edición de empresa. Misma lógica que estaba en CompaniesController: si llega un username, la
  /// empresa tiene que pertenecer al estudio de ese usuario.
  /// </summary>
  public async Task<CompanySaveResult> UpdateCompanyAsync(int id, Company company, string? username)
  {
    try
    {
      if (id != company.Id) return CompanySaveResult.BadRequest();

      var existing = await _unitOfWork.Companies.GetByIdAsync(id);
      if (existing == null) return CompanySaveResult.NotFound();

      // Control de pertenencia: un estudio solo edita sus propias empresas
      if (!string.IsNullOrEmpty(username))
      {
        var studio = (await _unitOfWork.AccountingStudios.FindAsync(s => s.ResponsibleUsername == username)).FirstOrDefault();
        if (studio == null) return CompanySaveResult.Forbidden();
        if (existing.AccountingStudioId != studio.Id) return CompanySaveResult.Forbidden();
      }

      if (!company.ParentCompanyId.HasValue)
      {
        var (ok, error) = CompanyValidationService.ValidateCuit(company.Cuit);
        if (!ok) return CompanySaveResult.BadRequest(error);
      }

      var dups = await _validation.GetDuplicatesAsync(company, ignoreId: id);
      if (dups.CuitDuplicate != null)
        return CompanySaveResult.Conflict($"El CUIT {CompanyValidationService.FormatCuit(company.Cuit)} ya pertenece a otra empresa.");
      if (dups.BusinessNameDuplicate != null)
        return CompanySaveResult.Conflict($"Ya existe otra empresa registrada con la razón social '{company.BusinessName}'.");
      if (dups.AddressDuplicate != null)
        return CompanySaveResult.Conflict("Ya existe otra empresa registrada en la misma dirección.");
      if (dups.PhoneDuplicate != null)
        return CompanySaveResult.Conflict("Ya existe otra empresa registrada con el mismo teléfono.");

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
      return CompanySaveResult.Ok();
    }
    catch (DbUpdateException)
    {
      return CompanySaveResult.Conflict("Ya existe una empresa con el mismo CUIT y nombre de sede.");
    }
    catch (Exception ex)
    {
      return CompanySaveResult.InternalError($"Error interno: {ex.Message}");
    }
  }
}
