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
