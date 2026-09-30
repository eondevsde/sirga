using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SIRGA.Application.Services;

public class AccountingStudioService(IUnitOfWork unitOfWork)
{
  public async Task<IEnumerable<AccountingStudioDto>> GetAllAsync()
  {
    var studios = await unitOfWork.AccountingStudios.GetAllAsync();
    return studios.Select(s => new AccountingStudioDto
    {
      Id = s.Id,
      Name = s.Name,
      ResponsibleUsername = s.ResponsibleUsername
    });
  }

  public async Task<IEnumerable<CompanyDashboardDto>> GetCompaniesForStudioAsync(int studioId)
  {
    // Cargar empresas de forma asíncrona primero, filtrando solo las que no son sucursales (ParentCompanyId == null)
    var companies = await unitOfWork.Companies.FindAsync(c => c.AccountingStudioId == studioId && c.ParentCompanyId == null);

    // Última DDJJ de cada empresa en UNA sola consulta, proyectando solo 3 columnas y sin
    // rastrear entidades. Antes se hacía una consulta por empresa que traía TODAS sus DDJJ
    // completas (y rastreadas) para quedarse con la última: mismo volumen de datos, pero en N
    // round-trips y con el change tracker cargado. Medido: 57 ms para 54 empresas.
    var historial = await unitOfWork.Declarations.RunQueryAsync(q =>
        q.Where(d => d.Company.AccountingStudioId == studioId && d.Company.ParentCompanyId == null)
         .Select(d => new { d.CompanyId, d.Period, d.TotalAmount })
         .ToListAsync());

    var ultimaPorEmpresa = historial
        .GroupBy(h => h.CompanyId)
        .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.Period).First());

    return companies.Select(company =>
    {
      ultimaPorEmpresa.TryGetValue(company.Id, out var ultima);

      return new CompanyDashboardDto
      {
        Id = company.Id,
        BusinessName = company.BusinessName,
        Cuit = company.Cuit,
        LastPeriodDeclared = ultima?.Period ?? "N/A",
        TotalLastDeclaration = ultima?.TotalAmount ?? 0,
        Status = company.Status.ToString()
      };
    }).ToList();
  }

  public async Task<int> RegisterStudioAsync(string name, string username, string email,
      string? contactPhone = null, string? contactCell = null, string? address = null, int? cityId = null,
      string? referencePerson = null, string? referencePhone = null)
  {
    var studio = new AccountingStudio
    {
      Name = name,
      ResponsibleUsername = username,
      Email = email,
      ContactPhone = contactPhone,
      ContactCellPhone = contactCell,
      Address = address,
      CityId = cityId,
      ReferencePerson = referencePerson,
      ReferencePhone = referencePhone,
      IsApproved = false,
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };

    await unitOfWork.AccountingStudios.AddAsync(studio);
    await unitOfWork.CompleteAsync();
    return studio.Id;
  }

  public async Task<AccountingStudio?> GetByUsernameAsync(string username)
  {
    var studios = await unitOfWork.AccountingStudios.FindAsync(s => s.ResponsibleUsername == username);
    return studios.FirstOrDefault();
  }

  public async Task ApproveStudioAsync(int studioId)
  {
    var studio = await unitOfWork.AccountingStudios.GetByIdAsync(studioId);
    if (studio != null)
    {
      studio.IsApproved = true;
      studio.ApprovedAt = DateTime.UtcNow;
      studio.RejectionReason = null; // Limpiar si fue rechazado antes
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task RejectStudioAsync(int studioId, string reason)
  {
    var studio = await unitOfWork.AccountingStudios.GetByIdAsync(studioId);
    if (studio != null)
    {
      studio.IsApproved = false;
      studio.RejectionReason = reason;
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task<IEnumerable<AccountingStudio>> GetPendingApprovalsAsync()
  {
    return await unitOfWork.AccountingStudios.FindAsync(s => !s.IsApproved);
  }
}
