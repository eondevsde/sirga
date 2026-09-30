using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

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
    var result = new List<CompanyDashboardDto>();

    // Procesar cada empresa de forma secuencial para evitar conflictos de hilos en el DbContext
    foreach (var company in companies)
    {
      var declarations = await unitOfWork.Declarations.FindAsync(d => d.CompanyId == company.Id);
      var lastDec = declarations
                    .OrderByDescending(d => d.Period)
                    .FirstOrDefault();

      result.Add(new CompanyDashboardDto
      {
        Id = company.Id,
        BusinessName = company.BusinessName,
        Cuit = company.Cuit,
        LastPeriodDeclared = lastDec?.Period ?? "N/A",
        TotalLastDeclaration = lastDec?.TotalAmount ?? 0,
        Status = company.Status.ToString()
      });
    }
    return result;
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
