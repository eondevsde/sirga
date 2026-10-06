using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

/// <summary>
/// Tasas de interés de ARCA. Antes sólo existían como endpoints anónimos en
/// InterestRateController; se movió acá para que la pantalla de tasas pueda usarlas sin pasar por
/// HTTP, porque el HttpClient de los componentes Blazor no lleva la cookie de sesión.
///
/// IMPORTANTE: **no hay borrado**. La escala de tasas es histórica: una tasa que no corresponde se
/// DESACTIVA y queda el rastro de que existió. Borrarla dejaría un hueco en la escala sin forma de
/// saber qué había, y además los cálculos de mora y del recargo de la boleta saldrían mal sin aviso.
/// </summary>
public class InterestRateService(IUnitOfWork unitOfWork)
{
  public async Task<IEnumerable<InterestRate>> GetAllAsync() => await unitOfWork.InterestRates.GetAllAsync();

  public async Task<InterestRate> CreateAsync(InterestRate rate)
  {
    await unitOfWork.InterestRates.AddAsync(rate);
    await unitOfWork.CompleteAsync();
    return rate;
  }

  /// <summary>
  /// Corrige una tasa existente. Es lo que hay que usar cuando una tasa quedó mal cargada, en lugar de
  /// desactivarla y crear otra: la escala queda con una sola fila por fecha y serie, como corresponde.
  /// </summary>
  public async Task<(bool Success, string? Error)> UpdateAsync(InterestRate rate)
  {
    var existente = await unitOfWork.InterestRates.GetByIdAsync(rate.Id);
    if (existente is null) return (false, "La tasa no existe.");

    existente.EffectiveDate = rate.EffectiveDate;
    existente.MonthlyRate = rate.MonthlyRate;
    existente.DailyRate = rate.DailyRate;
    existente.ValidTo = rate.ValidTo;
    existente.TypeOfInterest = rate.TypeOfInterest;
    existente.Norm = rate.Norm;
    existente.Description = rate.Description;

    // Mismo UnitOfWork que CompleteAsync: los repositorios comparten contexto y semáforo.
    await unitOfWork.InterestRates.UpdateAsync(existente);
    var ok = await unitOfWork.CompleteAsync() > 0;
    return (ok, ok ? null : "No se pudo guardar la tasa.");
  }

  /// <summary>
  /// Activa o desactiva una tasa. Los cálculos (mora de la DDJJ y recargo de la boleta) sólo miran las
  /// activas. El índice único por (fecha, serie) está filtrado por activas, así que desactivar una tasa
  /// deja cargar la que la reemplaza para la misma fecha.
  /// </summary>
  public async Task<(bool Success, string? Error)> SetActiveAsync(int id, bool activa)
  {
    var tasa = await unitOfWork.InterestRates.GetByIdAsync(id);
    if (tasa is null) return (false, "La tasa no existe.");
    if (tasa.IsActive == activa) return (true, null);

    tasa.IsActive = activa;
    await unitOfWork.InterestRates.UpdateAsync(tasa);
    var ok = await unitOfWork.CompleteAsync() > 0;
    return (ok, ok ? null : "No se pudo cambiar el estado de la tasa.");
  }
}
