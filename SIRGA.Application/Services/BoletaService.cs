using Microsoft.EntityFrameworkCore;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Enums;

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

    // RECARGO DEL 2° VENCIMIENTO = el interés resarcitorio por los días que van del 1° al 2° vencimiento:
    // desde el día siguiente al 1° hasta el 2°, con la MISMA fórmula que la mora de la DDJJ (tasa diaria =
    // mensual / 30 y fragmentado por tramos si el período cruza un cambio de tasa).
    // Antes se prorrateaba por los días del mes; la especificación de ARCA dice /30 SIEMPRE.
    var tasas = (await unitOfWork.InterestRates.GetAllAsync())
        .Where(r => r.IsActive && r.TypeOfInterest == InterestType.Resarcitorio);

    var resultadoRecargo = InterestCalculator.Calcular(importePrimero, primerVencimiento, segundoVencimiento, tasas);

    if (resultadoRecargo.FaltaTasa)
    {
      // Sin tasa para todo el período no se puede saber cuánto cobrar el 2° vencimiento: se dice, no se
      // emite una boleta con un recargo inventado.
      return new BoletaArmadoResult(null,
          $"No hay tasa de interés cargada para todo el período entre el 1° y el 2° vencimiento " +
          $"({resultadoRecargo.DiasSinTasa} día(s) sin tasa). Cargala en Tasas de interés antes de emitir la boleta.",
          null);
    }

    decimal recargo = resultadoRecargo.Interes;
    decimal importeSegundo = importePrimero + recargo;
    decimal porcentajeRecargo = importePrimero > 0 ? recargo / importePrimero : 0m;

    // Si el período cruza más de un tramo, la boleta puede tener más de una norma: se listan todas.
    string? normaTasa = resultadoRecargo.Tramos
        .Select(t => t.Norma)
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Distinct()
        .Aggregate((string?)null, (a, b) => a is null ? b : $"{a} / {b}");

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
      DiasRecargo = resultadoRecargo.DiasMora,
      RecargoNorma = normaTasa,
      RecargoSerie = nameof(InterestType.Resarcitorio),
      PrimerVencimiento = primerVencimiento,
      ImportePrimerVencimiento = importePrimero,
      SegundoVencimiento = segundoVencimiento,
      ImporteSegundoVencimiento = importeSegundo,
      CodigoBarras = codigo
    };

    // Si el período entre vencimientos cruzó un cambio de tasa, se dice: el recargo está sumado por
    // tramos y conviene que el estudio lo vea.
    string? advertencia = resultadoRecargo.Tramos.Count > 1
        ? "El período entre el 1° y el 2° vencimiento cruza más de un tramo de tasa: " +
          string.Join(" + ", resultadoRecargo.Tramos.Select(t => $"{t.Dias} días al {t.TasaMensual * 100m:0.####}%")) +
          "."
        : null;

    return new BoletaArmadoResult(datos, null, advertencia);
  }

  /// <summary>
  /// Tasa mensual RESARCITORIA vigente a una fecha, la norma que la fija, y si hubo que caer en la tasa
  /// por defecto de SIRGA.
  ///
  /// Se filtra por <see cref="InterestType.Resarcitorio"/> porque la tabla trae DOS series por período
  /// (resarcitoria y punitoria): sin el filtro, "la tasa vigente" devolvería dos filas para la misma
  /// fecha y elegiría una al azar. Y se usa el rango de vigencia completo (EffectiveDate..ValidTo) en
  /// lugar de "la última fecha anterior", que ignoraba el fin de vigencia.
  /// </summary>
  private async Task<(decimal TasaMensual, string? Norma, bool EsPorDefecto)> ObtenerTasaResarcitoriaAsync(DateTime fecha)
  {
    var tasas = (await unitOfWork.InterestRates.GetAllAsync())
        .Where(r => r.IsActive && r.TypeOfInterest == InterestType.Resarcitorio)
        .ToList();

    var vigente = tasas
        .Where(r => r.EffectiveDate <= fecha && (r.ValidTo is null || r.ValidTo >= fecha))
        .OrderByDescending(r => r.EffectiveDate)
        .FirstOrDefault();

    if (vigente is null)
      return (TasaMensualPorDefecto, null, true);

    // La fila de 1900 es el "default SIRGA" que se cargó a mano, no una tasa real de AFIP.
    bool esPorDefecto = vigente.EffectiveDate.Year <= 1900;
    return (vigente.MonthlyRate, vigente.Norm, esPorDefecto);
  }
}
