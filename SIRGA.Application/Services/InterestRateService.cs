using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

/// <summary>
/// Tasas de interés de ARCA. Antes sólo existían como endpoints anónimos en
/// InterestRateController; se movió acá para que la pantalla de tasas pueda usarlas sin pasar por
/// HTTP, porque el HttpClient de los componentes Blazor no lleva la cookie de sesión.
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

  /// <summary>Elimina la tasa indicada. Devuelve false si no existe.</summary>
  public async Task<bool> DeleteAsync(int id)
  {
    var rate = await unitOfWork.InterestRates.GetByIdAsync(id);
    if (rate == null) return false;

    await unitOfWork.InterestRates.RemoveAsync(rate);
    await unitOfWork.CompleteAsync();
    return true;
  }
}
