using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

public class UnionService
{
  private readonly IUnitOfWork _unitOfWork;

  public UnionService(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  /// <summary>
  /// Sindicato al que le asignaron este auditor. Devuelve null si todavía no se lo asignaron, que es
  /// el caso en el que el auditor NO debe ver datos de ningún sindicato (antes veía todo).
  /// Si estuviera asignado a más de uno, se toma el primero por fecha de asignación.
  /// </summary>
  public async Task<int?> GetUnionIdForAuditorAsync(string username)
  {
    if (string.IsNullOrWhiteSpace(username)) return null;

    var asignaciones = await _unitOfWork.UnionAuditors.FindAsync(a => a.Username == username);
    return asignaciones.OrderBy(a => a.AssignedAt).Select(a => (int?)a.UnionId).FirstOrDefault();
  }

  public async Task<IEnumerable<UnionDto>> GetAllAsync()
  {
    var unions = await _unitOfWork.Unions.GetAllAsync();
    return unions.Select(u => new UnionDto
    {
      Id = u.Id,
      Name = u.Name,
      JurisdictionProvince = u.JurisdictionProvince,
      JurisdictionCity = u.JurisdictionCity,
      CctCode = u.CctCode,
      AdminEmail = u.AdminEmail,
      IsActive = u.IsActive
    });
  }

  public async Task<UnionDto?> GetByIdAsync(int id)
  {
    var union = await _unitOfWork.Unions.GetByIdAsync(id);
    if (union == null) return null;
    return new UnionDto
    {
      Id = union.Id,
      Name = union.Name,
      JurisdictionProvince = union.JurisdictionProvince,
      JurisdictionCity = union.JurisdictionCity,
      CctCode = union.CctCode,
      AdminEmail = union.AdminEmail,
      IsActive = union.IsActive
    };
  }

  public async Task<UnionDto?> GetByUsernameAsync(string username)
  {
    var unions = await _unitOfWork.Unions.FindAsync(u => u.AdminEmail == username);
    var union = unions.FirstOrDefault();
    if (union == null) return null;
    return new UnionDto
    {
      Id = union.Id,
      Name = union.Name,
      JurisdictionProvince = union.JurisdictionProvince,
      JurisdictionCity = union.JurisdictionCity,
      CctCode = union.CctCode,
      AdminEmail = union.AdminEmail,
      IsActive = union.IsActive
    };
  }

  public async Task<List<Province>> GetProvincesAsync()
  {
    var provinces = await _unitOfWork.Provinces.GetAllAsync();
    return provinces.OrderBy(p => p.Name).ToList();
  }

  public async Task<List<City>> GetCitiesByProvinceAsync(int provinceId)
  {
    var cities = await _unitOfWork.Cities.FindAsync(c => c.ProvinceId == provinceId);
    return cities.OrderBy(c => c.Name).ToList();
  }

  public async Task CreateAsync(UnionDto dto)
  {
    var union = new Union
    {
      Name = dto.Name,
      JurisdictionProvince = dto.JurisdictionProvince,
      JurisdictionCity = dto.JurisdictionCity,
      CctCode = dto.CctCode,
      AdminEmail = dto.AdminEmail,
      IsActive = dto.IsActive
    };

    await _unitOfWork.Unions.AddAsync(union);
    await _unitOfWork.CompleteAsync();
  }

  public async Task UpdateAsync(UnionDto dto)
  {
    var union = await _unitOfWork.Unions.GetByIdAsync(dto.Id);
    if (union == null) return;

    union.Name = dto.Name;
    union.JurisdictionProvince = dto.JurisdictionProvince;
    union.JurisdictionCity = dto.JurisdictionCity;
    union.CctCode = dto.CctCode;
    union.AdminEmail = dto.AdminEmail;
    union.IsActive = dto.IsActive;

    await _unitOfWork.Unions.UpdateAsync(union);
    await _unitOfWork.CompleteAsync();
  }

  public async Task DeleteAsync(int id)
  {
    var union = await _unitOfWork.Unions.GetByIdAsync(id);
    if (union != null)
    {
      await _unitOfWork.Unions.RemoveAsync(union);
      await _unitOfWork.CompleteAsync();
    }
  }

  public async Task<List<City>> GetCitiesAsync() => (await _unitOfWork.Cities.GetAllAsync()).ToList();
}
