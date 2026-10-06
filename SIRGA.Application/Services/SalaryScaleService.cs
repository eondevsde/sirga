using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

public class SalaryScaleService(IUnitOfWork unitOfWork)
{
  public async Task<IEnumerable<SalaryScaleDto>> GetScalesByPeriodAsync(DateTime period)
  {
    var scales = await unitOfWork.SalaryScales.FindAsync(s => s.Period.Year == period.Year && s.Period.Month == period.Month);
    var categoryIds = scales.Select(s => s.EmployeeCategoryId).Distinct();
    var categories = (await unitOfWork.EmployeeCategories.FindAsync(c => categoryIds.Contains(c.Id))).ToDictionary(c => c.Id);

    return scales.Select(s => new SalaryScaleDto
    {
      Id = s.Id,
      RamaId = categories.TryGetValue(s.EmployeeCategoryId, out var cat) ? cat.RamaId : 0,
      Period = s.Period,
      EmployeeCategoryId = s.EmployeeCategoryId,
      CategoryCode = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Code : "N/A",
      CategoryName = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Name : "N/A",
      BasicAmount = s.BasicAmount,
      AgreementNR1 = s.AgreementNR1,
      AgreementNR2 = s.AgreementNR2,
      AntiguedadPercentage = s.AntiguedadPercentage,
      IsActive = s.IsActive
    });
  }

  public async Task<IEnumerable<SalaryScaleDto>> GetAllAsync()
  {
    var scales = await unitOfWork.SalaryScales.GetAllAsync();
    var categories = (await unitOfWork.EmployeeCategories.GetAllAsync()).ToDictionary(c => c.Id);

    return scales.Select(s => new SalaryScaleDto
    {
      Id = s.Id,
      RamaId = categories.TryGetValue(s.EmployeeCategoryId, out var cat) ? cat.RamaId : 0,
      Period = s.Period,
      EmployeeCategoryId = s.EmployeeCategoryId,
      CategoryCode = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Code : "N/A",
      CategoryName = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Name : "N/A",
      BasicAmount = s.BasicAmount,
      AgreementNR1 = s.AgreementNR1,
      AgreementNR2 = s.AgreementNR2,
      AntiguedadPercentage = s.AntiguedadPercentage,
      IsActive = s.IsActive
    }).OrderByDescending(s => s.Period).ThenBy(s => s.CategoryCode);
  }

  /// <summary>
  /// Escalas de una RAMA de actividad: las escalas son nacionales por rama, así que ya no se piden por
  /// sindicato. Las categorías se resuelven por la rama y las escalas por esas categorías.
  /// </summary>
  /// <param name="incluirInactivas">
  /// Por defecto trae sólo las ACTIVAS, que son las que se usan para liquidar. La pantalla de administración
  /// pasa <c>true</c> para poder ver y reactivar las desactivadas.
  /// </param>
  public async Task<List<SalaryScaleDto>> GetByRamaAsync(int ramaId, bool incluirInactivas = false)
  {
    var categorias = (await unitOfWork.EmployeeCategories.FindAsync(c => c.RamaId == ramaId)).ToDictionary(c => c.Id);
    if (categorias.Count == 0) return new();

    var ids = categorias.Keys.ToList();
    var scales = await unitOfWork.SalaryScales.FindAsync(s =>
        ids.Contains(s.EmployeeCategoryId) && (incluirInactivas || s.IsActive));

    return scales.Select(s => new SalaryScaleDto
    {
      Id = s.Id,
      RamaId = ramaId,
      Period = s.Period,
      EmployeeCategoryId = s.EmployeeCategoryId,
      CategoryCode = categorias.TryGetValue(s.EmployeeCategoryId, out var c) ? c.Code : "N/A",
      CategoryName = categorias.TryGetValue(s.EmployeeCategoryId, out var c2) ? c2.Name : "N/A",
      BasicAmount = s.BasicAmount,
      AgreementNR1 = s.AgreementNR1,
      AgreementNR2 = s.AgreementNR2,
      AntiguedadPercentage = s.AntiguedadPercentage,
      IsActive = s.IsActive
    }).OrderByDescending(s => s.Period).ToList();
  }

  public async Task CreateAsync(SalaryScaleDto dto)
  {
    var entity = new SalaryScale
    {
      Period = dto.Period,
      EmployeeCategoryId = dto.EmployeeCategoryId,
      BasicAmount = dto.BasicAmount,
      AntiguedadPercentage = dto.AntiguedadPercentage,
      AgreementNR1 = dto.AgreementNR1,
      AgreementNR2 = dto.AgreementNR2,
      IsActive = dto.IsActive
    };

    await unitOfWork.SalaryScales.AddAsync(entity);
    await unitOfWork.CompleteAsync();
  }

  public async Task UpdateAsync(SalaryScaleDto dto)
  {
    var entity = await unitOfWork.SalaryScales.GetByIdAsync(dto.Id);
    if (entity != null)
    {
      entity.BasicAmount = dto.BasicAmount;
      entity.AgreementNR1 = dto.AgreementNR1;
      entity.AgreementNR2 = dto.AgreementNR2;
      entity.AntiguedadPercentage = dto.AntiguedadPercentage;
      entity.IsActive = dto.IsActive;
      entity.Period = dto.Period;
      entity.EmployeeCategoryId = dto.EmployeeCategoryId;
      await unitOfWork.CompleteAsync();
    }
  }

  /// <summary>
  /// Activa o desactiva una escala. **NO se borra**: la escala es histórica y una que no corresponde se
  /// desactiva dejando rastro. Las consultas que liquidan (la DDJJ) sólo miran las activas, y el índice
  /// único por (período, categoría) está filtrado por activas, así que desactivar una deja cargar la que
  /// la reemplaza.
  /// </summary>
  public async Task<(bool Success, string? Error)> SetActiveAsync(int id, bool activa)
  {
    var entity = await unitOfWork.SalaryScales.GetByIdAsync(id);
    if (entity is null) return (false, "La escala no existe.");
    if (entity.IsActive == activa) return (true, null);

    entity.IsActive = activa;
    await unitOfWork.SalaryScales.UpdateAsync(entity);
    var ok = await unitOfWork.CompleteAsync() > 0;
    return (ok, ok ? null : "No se pudo cambiar el estado de la escala.");
  }

  public async Task CreateScaleAsync(SalaryScaleDto dto)
  {
    var entity = new SalaryScale
    {
      Period = dto.Period,
      EmployeeCategoryId = dto.EmployeeCategoryId,
      BasicAmount = dto.BasicAmount,
      AntiguedadPercentage = dto.AntiguedadPercentage,
      AgreementNR1 = dto.AgreementNR1,
      AgreementNR2 = dto.AgreementNR2,
      IsActive = true
    };

    await unitOfWork.SalaryScales.AddAsync(entity);
    await unitOfWork.CompleteAsync();
  }

  public async Task UpdateScaleAsync(SalaryScaleDto dto)
  {
    var entity = await unitOfWork.SalaryScales.GetByIdAsync(dto.Id);
    if (entity != null)
    {
      entity.BasicAmount = dto.BasicAmount;
      entity.AgreementNR1 = dto.AgreementNR1;
      entity.AgreementNR2 = dto.AgreementNR2;
      entity.IsActive = dto.IsActive;
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task<bool> SeedScalesFromSqlInfoAsync()
  {
    // Este método se podría usar para una carga inicial controlada
    // Por ahora, el usuario puede ejecutar el .sql directamente en la base
    return true;
  }
}
