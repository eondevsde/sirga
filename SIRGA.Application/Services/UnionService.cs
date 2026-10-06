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

  /// <summary>Auditores asignados a un sindicato, para la pantalla de asignación.</summary>
  public async Task<IEnumerable<UnionAuditorDto>> GetAuditorsAsync(int unionId)
  {
    var asignaciones = await _unitOfWork.UnionAuditors.FindAsync(a => a.UnionId == unionId);
    return asignaciones
        .OrderBy(a => a.Username)
        .Select(a => new UnionAuditorDto(a.Id, a.UnionId, a.Username, a.AssignedAt, a.AssignedByUsername));
  }

  /// <summary>
  /// Asigna un auditor a un sindicato. Es idempotente a propósito: si ya estaba asignado no duplica
  /// la fila ni devuelve error, así el botón se puede apretar dos veces sin ensuciar la tabla.
  /// </summary>
  public async Task<(bool Ok, string? Error)> AssignAuditorAsync(int unionId, string username, string? assignedBy)
  {
    if (string.IsNullOrWhiteSpace(username)) return (false, "Falta el usuario.");

    var yaEstaba = await _unitOfWork.UnionAuditors.FindAsync(
        a => a.UnionId == unionId && a.Username == username);
    if (yaEstaba.Any()) return (true, null);

    await _unitOfWork.UnionAuditors.AddAsync(new UnionAuditor
    {
      UnionId = unionId,
      Username = username.Trim(),
      AssignedAt = DateTime.UtcNow,
      AssignedByUsername = assignedBy
    });

    var ok = await _unitOfWork.CompleteAsync() > 0;
    return (ok, ok ? null : "No se pudo guardar la asignación.");
  }

  /// <summary>Quita la asignación de un auditor a un sindicato.</summary>
  public async Task<(bool Ok, string? Error)> RemoveAuditorAsync(int unionId, string username)
  {
    var asignaciones = await _unitOfWork.UnionAuditors.FindAsync(
        a => a.UnionId == unionId && a.Username == username);

    foreach (var asignacion in asignaciones)
      await _unitOfWork.UnionAuditors.RemoveAsync(asignacion);

    var ok = await _unitOfWork.CompleteAsync() > 0;
    return (ok, ok ? null : "No se pudo quitar la asignación.");
  }

  /// <summary>Un auditor asignado a un sindicato.</summary>
  public record UnionAuditorDto(int Id, int UnionId, string Username, DateTime AssignedAt, string? AssignedByUsername);

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
      IsActive = u.IsActive,
      LawContributionPercentage = u.LawContributionPercentage,
      UnionMemberPercentage = u.UnionMemberPercentage
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
      IsActive = union.IsActive,
      LawContributionPercentage = union.LawContributionPercentage,
      UnionMemberPercentage = union.UnionMemberPercentage
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
      IsActive = union.IsActive,
      LawContributionPercentage = union.LawContributionPercentage,
      UnionMemberPercentage = union.UnionMemberPercentage
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
