using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

/// <summary>
/// CRUD de bancos y de la configuración bancaria por sindicato: número de cliente en el banco, código
/// de ente del código de barras y reglas de vencimiento.
///
/// Un sindicato puede operar con más de un banco, y cada banco tiene su propia configuración.
/// Los bancos NO se eliminan: se desactivan, porque las boletas ya emitidas los referencian y hay que
/// poder reconstruir con qué banco se cobró cada una.
/// </summary>
public class BankService(
    IRepository<Bank> bancos,
    IRepository<UnionBankConfig> configuraciones,
    IRepository<Union> sindicatos,
    IUnitOfWork unitOfWork)
{
  // ---------- Bancos ----------

  public async Task<IReadOnlyList<Bank>> GetBancosAsync()
      => (await bancos.GetAllAsync()).OrderBy(b => b.Name).ToList();

  public async Task<Bank?> GetBancoAsync(int id) => await bancos.GetByIdAsync(id);

  public async Task<(bool Ok, string? Error)> GuardarBancoAsync(Bank banco)
  {
    if (string.IsNullOrWhiteSpace(banco.Name))
      return (false, "El nombre del banco es obligatorio.");

    banco.Name = banco.Name.Trim();
    banco.Branch = banco.Branch?.Trim();

    var repetidos = await bancos.FindAsync(b => b.Name == banco.Name && b.Id != banco.Id);
    if (repetidos.Any())
      return (false, $"Ya hay un banco cargado con el nombre \"{banco.Name}\".");

    if (banco.Id == 0)
      await bancos.AddAsync(banco);
    else
      await bancos.UpdateAsync(banco);

    await unitOfWork.CompleteAsync();
    return (true, null);
  }

  /// <summary>Activa o desactiva un banco. No se borra: las boletas emitidas lo referencian.</summary>
  public async Task AlternarBancoAsync(int id)
  {
    var banco = await bancos.GetByIdAsync(id);
    if (banco == null) return;

    banco.IsActive = !banco.IsActive;
    await bancos.UpdateAsync(banco);
    await unitOfWork.CompleteAsync();
  }

  // ---------- Configuración por sindicato ----------

  public async Task<IReadOnlyList<Union>> GetSindicatosAsync()
      => (await sindicatos.GetAllAsync()).OrderBy(u => u.Name).ToList();

  public async Task<IReadOnlyList<UnionBankConfig>> GetConfiguracionesAsync()
      => (await configuraciones.GetAllAsync()).OrderBy(c => c.UnionId).ToList();

  /// <summary>Configuración activa de un sindicato: la que se usa para emitir la boleta.</summary>
  public async Task<UnionBankConfig?> GetConfiguracionActivaAsync(int unionId)
      => (await configuraciones.FindAsync(c => c.UnionId == unionId && c.IsActive)).FirstOrDefault();

  public async Task<(bool Ok, string? Error)> GuardarConfiguracionAsync(UnionBankConfig config)
  {
    if (config.UnionId <= 0) return (false, "Elegí el sindicato.");
    if (config.BankId <= 0) return (false, "Elegí el banco.");
    if (string.IsNullOrWhiteSpace(config.ClientNumber))
      return (false, "El número de cliente en el banco es obligatorio.");
    if (string.IsNullOrWhiteSpace(config.EntityCode))
      return (false, "El código de ente es obligatorio.");

    config.ClientNumber = config.ClientNumber.Trim();
    config.EntityCode = new string(config.EntityCode.Where(char.IsDigit).ToArray()).PadLeft(4, '0');

    if (config.EntityCode.Length > 4) return (false, "El código de ente tiene 4 posiciones.");
    if (config.DesignFormat <= 0) return (false, "El formato de diseño tiene que ser mayor que cero.");
    if (config.FirstDueDay < 1 || config.FirstDueDay > 28)
      return (false, "El día del primer vencimiento tiene que estar entre 1 y 28.");
    if (config.DaysToSecondDue < 0 || config.DaysToSecondDue > 60)
      return (false, "Los días hasta el segundo vencimiento tienen que estar entre 0 y 60.");

    var existente = config.Id > 0
        ? await configuraciones.GetByIdAsync(config.Id)
        : (await configuraciones.FindAsync(c => c.UnionId == config.UnionId && c.BankId == config.BankId)).FirstOrDefault();

    // Un sindicato no puede tener dos configuraciones distintas del mismo banco.
    var otras = await configuraciones.FindAsync(c =>
        c.UnionId == config.UnionId && c.BankId == config.BankId && c.Id != config.Id);
    if (otras.Any())
      return (false, "Ese sindicato ya tiene configurado ese banco.");

    if (existente != null)
    {
      existente.ClientNumber = config.ClientNumber;
      existente.EntityCode = config.EntityCode;
      existente.DesignFormat = config.DesignFormat;
      existente.FirstDueDay = config.FirstDueDay;
      existente.DaysToSecondDue = config.DaysToSecondDue;
      existente.IsActive = config.IsActive;
      await configuraciones.UpdateAsync(existente);
    }
    else
    {
      await configuraciones.AddAsync(config);
    }

    await unitOfWork.CompleteAsync();
    return (true, null);
  }
}
