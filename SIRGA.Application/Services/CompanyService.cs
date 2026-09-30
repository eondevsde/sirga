using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

public class CompanyService
{
  private readonly IUnitOfWork _unitOfWork;

  public CompanyService(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
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
}
