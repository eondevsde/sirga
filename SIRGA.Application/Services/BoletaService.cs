using Microsoft.EntityFrameworkCore;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;

namespace SIRGA.Application.Services;

/// <summary>
/// Arma la BOLETA BANCARIA de una DDJJ: junta los datos de la declaración, de la empresa, del
/// sindicato y de su configuración bancaria, calcula los dos vencimientos con sus importes y genera el
/// código de barras de 58 dígitos del banco.
///
/// Es el paso que faltaba entre el PDF ya escrito (<c>BoletaPdfService</c>, en WebUI) y los datos: el
/// PDF sabía dibujar la boleta, pero nadie le pasaba los datos armados.
///
/// RECARGO DEL 2° VENCIMIENTO: sale de la única tabla de tasas del sistema (<c>InterestRates</c>),
/// prorrateando la tasa MENSUAL por los días configurados entre vencimientos (días / 30).
/// **Ojo: hoy esa tabla tiene una sola fila, que es el 3% por defecto de SIRGA y NO la escala de AFIP.**
/// Por eso el recargo que sale es provisorio y se devuelve una advertencia que la pantalla muestra.
/// </summary>
public class BoletaService(IUnitOfWork unitOfWork)
{
  /// <summary>Base de prorrateo de la tasa mensual. Documentado para poder revisarlo: ver pendiente del recargo en CONTEXTO_SIRGA.md.</summary>
  public const int DiasDelMesParaProrrateo = 30;

  /// <summary>Tasa mensual que usa el sistema cuando la tabla de tasas no tiene ninguna fila vigente.</summary>
  public const decimal TasaMensualPorDefecto = 0.03m;

  public async Task<BoletaArmadoResult> ArmarAsync(int declarationId)
  {
    // Los subtotales se calculan EN SQL. Si se sumaran en memoria sobre Details sin cargar la
    // colección darían 0: es un bug que ya apareció con las 248.151 filas de la grilla de auditoría.
    var cabecera = await unitOfWork.Declarations.RunQueryAsync(q => q
        .Where(d => d.Id == declarationId)
        .Select(d => new
        {
          d.Id,
          d.Period,
          d.UnionId,
          d.TotalAmount,
          AporteLey = d.Details.Sum(x => x.LawContribution),
          CuotaSocio = d.Details.Sum(x => x.MemberContribution),
          RazonSocial = d.Company.BusinessName,
          Cuit = d.Company.Cuit,
          Sindicato = d.Union.Name
        })
        .FirstOrDefaultAsync());

    if (cabecera is null)
      return new BoletaArmadoResult(null, "La declaración no existe.", null);

    var configuracion = (await unitOfWork.UnionBankConfigs
        .FindAsync(c => c.UnionId == cabecera.UnionId && c.IsActive)).FirstOrDefault();

    if (configuracion is null)
      return new BoletaArmadoResult(null,
          $"El sindicato \"{cabecera.Sindicato}\" no tiene una configuración bancaria activa, así que no se puede emitir la boleta. Cargala en Bancos y boletas.",
          null);

    var banco = await unitOfWork.Banks.GetByIdAsync(configuracion.BankId);
    if (banco is null)
      return new BoletaArmadoResult(null, "El banco configurado para el sindicato no existe.", null);

    DateTime primerVencimiento;
    try
    {
      primerVencimiento = BoletaBse.PrimerVencimiento(cabecera.Period, configuracion.FirstDueDay);
    }
    catch (ArgumentException ex)
    {
      // Sin período válido no hay vencimientos ni código de barras: se dice, no se emite una boleta rota.
      return new BoletaArmadoResult(null, $"La declaración tiene un período inválido (\"{cabecera.Period}\"): {ex.Message}", null);
    }

    var segundoVencimiento = BoletaBse.SegundoVencimiento(primerVencimiento, configuracion.DaysToSecondDue);

    // El importe del primer vencimiento es el TOTAL de la declaración: la boleta es una por DDJJ, por
    // período, por el total. NO es el saldo (que puede venir con crédito aplicado o con intereses).
    decimal importePrimero = cabecera.TotalAmount;

    var (tasaMensual, esTasaPorDefecto) = await ObtenerTasaMensualAsync();
    var (recargo, importeSegundo, porcentajeRecargo) =
        CalcularRecargo(importePrimero, tasaMensual, configuracion.DaysToSecondDue);

    var codigo = BoletaBse.GenerarConConfiguracion(
        configuracion, cabecera.Period, cabecera.Cuit ?? string.Empty, importePrimero, importeSegundo);

    var datos = new BoletaDatosDto
    {
      Sindicato = cabecera.Sindicato ?? string.Empty,
      Banco = banco.Name,
      Sucursal = banco.Branch ?? string.Empty,
      NumeroCliente = configuracion.ClientNumber,
      RazonSocial = cabecera.RazonSocial ?? string.Empty,
      Cuit = cabecera.Cuit ?? string.Empty,
      Periodo = cabecera.Period,
      AporteLey = cabecera.AporteLey,
      CuotaSindical = cabecera.CuotaSocio,
      Recargo = recargo,
      RecargoPorcentaje = Math.Round(porcentajeRecargo * 100m, 4),
      DiasRecargo = configuracion.DaysToSecondDue,
      PrimerVencimiento = primerVencimiento,
      ImportePrimerVencimiento = importePrimero,
      SegundoVencimiento = segundoVencimiento,
      ImporteSegundoVencimiento = importeSegundo,
      CodigoBarras = codigo
    };

    string? advertencia = esTasaPorDefecto
        ? $"El recargo del 2° vencimiento ({datos.RecargoPorcentaje:0.####}% por {datos.DiasRecargo} días) " +
          $"se calculó con la tasa por defecto de SIRGA ({tasaMensual * 100m:0.##}% mensual). " +
          "La escala de tasas de AFIP todavía no está cargada."
        : null;

    return new BoletaArmadoResult(datos, null, advertencia);
  }

  /// <summary>
  /// Tasa mensual vigente y si es la de por defecto. La tabla trae una sola fila (3%, fecha 1900) que es
  /// el default de SIRGA: si nunca se cargó una tasa con fecha real, el recargo es provisorio.
  /// </summary>
  private async Task<(decimal TasaMensual, bool EsPorDefecto)> ObtenerTasaMensualAsync()
  {
    var tasas = (await unitOfWork.InterestRates.GetAllAsync()).ToList();

    var vigente = tasas
        .OrderByDescending(r => r.EffectiveDate)
        .FirstOrDefault(r => r.EffectiveDate <= DateTime.Today);

    if (vigente is null)
      return (TasaMensualPorDefecto, true);

    bool esPorDefecto = tasas.Count == 1 && vigente.EffectiveDate.Year <= 1900;
    return (vigente.MonthlyRate, esPorDefecto);
  }

  /// <summary>
  /// Recargo del segundo vencimiento: prorratea la tasa MENSUAL por los días entre vencimientos
  /// (días / 30) y lo aplica sobre el importe del primero. Devuelve también el porcentaje aplicado,
  /// para poder mostrarlo y para poder auditarlo.
  ///
  /// Es público y estático para poder probarse: es la cuenta que define cuánto cobra el banco.
  /// </summary>
  public static (decimal Recargo, decimal ImporteSegundo, decimal Porcentaje) CalcularRecargo(
      decimal importePrimero, decimal tasaMensual, int dias)
  {
    decimal porcentaje = tasaMensual * (dias / (decimal)DiasDelMesParaProrrateo);
    decimal recargo = Redondear(importePrimero * porcentaje);
    return (recargo, importePrimero + recargo, porcentaje);
  }

  /// <summary>Redondeo de dinero a 2 decimales hacia afuera, igual que el resto del sistema.</summary>
  private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
