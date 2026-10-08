using Microsoft.EntityFrameworkCore;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Enums;

namespace SIRGA.Application.Services;

/// <summary>
/// Arma la BOLETA BANCARIA de una DDJJ: junta los datos de la declaración, de la empresa, del sindicato y
/// de su configuración bancaria, y genera el código de barras de 58 dígitos del banco.
///
/// **UNA SOLA FECHA DE VENCIMIENTO** (ya no hay 2° vencimiento):
///   - Si la DDJJ **todavía no venció**, la boleta sale por el total con la fecha de vencimiento del banco
///     para ese período y **sin interés**.
///   - Si **ya venció**, el cliente declara cuándo va a pagar (<paramref name="fechaPago"/>, que la
///     pantalla pide en un modal) y **esa pasa a ser la fecha de vencimiento de la boleta**, con el
///     INTERÉS RESARCITORIO calculado desde el día siguiente al vencimiento original hasta esa fecha.
///     O sea: desde el primer día de mora, la boleta ya sale con el interés correspondiente.
///
/// El interés lo calcula <see cref="InterestCalculator"/> (tasa diaria = mensual / 30 y fragmentado por
/// tramos si el período cruza un cambio de tasa), el mismo motor que usa la mora de la DDJJ.
/// </summary>
public class BoletaService(IUnitOfWork unitOfWork)
{
  /// <summary>
  /// Arma la boleta. <paramref name="fechaPago"/> es la fecha en que el cliente va a pagar: si no se
  /// pasa, se usa hoy. Si es posterior al vencimiento, manda ella y se cobra el interés hasta ahí.
  /// </summary>
  public async Task<BoletaArmadoResult> ArmarAsync(int declarationId, DateTime? fechaPago = null)
  {
    var cabecera = await CargarAsync(declarationId);
    if (cabecera.Error is not null) return new BoletaArmadoResult(null, cabecera.Error, null);

    var declaracion = cabecera.Declaracion!;
    var configuracion = cabecera.Configuracion!;
    var banco = cabecera.Banco!;

    // CONTROL PEDIDO POR EL USUARIO (2026-10-07): "ninguna boleta puede imprimirse si no está presentada
    // antes". Un BORRADOR no está presentado: emitir su boleta sería cobrar una DDJJ que nunca se presentó
    // (y que SIGES WEB todavía no ve, porque el volcado al legado ocurre en la presentación).
    // Va acá, en el servicio, y no sólo en la pantalla: si no, se saltea por API.
    if (declaracion.Status == DeclarationStatus.Draft)
    {
      return new BoletaArmadoResult(null,
          $"La DDJJ {declaracion.Id} (período {declaracion.Period}) todavía es un BORRADOR: no está " +
          "presentada, así que no se emite su boleta. Presentala primero.", null);
    }

    // Una DDJJ reemplazada por una rectificativa NO emite boleta: la deuda es la de la versión vigente, y
    // emitir la de la versión vieja sería cobrar dos veces la misma DDJJ. Se bloquea acá (servicio) y no
    // sólo en la pantalla: si no, quedaría evitable por API.
    if (declaracion.Status == DeclarationStatus.Rectificada)
    {
      return new BoletaArmadoResult(null,
          $"La DDJJ {declaracion.Id} (período {declaracion.Period}) fue reemplazada por una rectificativa: " +
          "no se emite su boleta. Emití la de la versión vigente.", null);
    }

    DateTime vencimiento;
    try
    {
      vencimiento = BoletaBse.PrimerVencimiento(declaracion.Period, configuracion.FirstDueDay);
    }
    catch (ArgumentException ex)
    {
      // Sin período válido no hay vencimiento ni código de barras: se dice, no se emite una boleta rota.
      return new BoletaArmadoResult(null,
          $"La declaración tiene un período inválido (\"{declaracion.Period}\"): {ex.Message}", null);
    }

    // El capital es el TOTAL de la declaración: la boleta es una por DDJJ, por período, por el total. NO
    // es el saldo (que puede venir con crédito aplicado o con la mora ya sumada al presentar).
    decimal capital = declaracion.TotalAmount;
    DateTime fecha = (fechaPago ?? DateTime.Now).Date;

    // El interés se calcula SIEMPRE con el motor: si la fecha de pago es el vencimiento o anterior, el
    // motor devuelve 0 y no hay nada que hacer.
    var tasas = (await unitOfWork.InterestRates.GetAllAsync())
        .Where(r => r.IsActive && r.TypeOfInterest == InterestType.Resarcitorio);

    var interes = InterestCalculator.Calcular(capital, vencimiento, fecha, tasas);

    if (interes.FaltaTasa)
    {
      // Hay días de mora sin tasa cargada: no se puede saber cuánto cobrar, y no se inventa.
      return new BoletaArmadoResult(null,
          $"No hay tasa de interés cargada para {interes.DiasSinTasa} día(s) del período de mora " +
          $"({vencimiento:dd/MM/yyyy} en adelante). Cargala en Tasas de interés antes de emitir la boleta.",
          null);
    }

    // Si ya venció, la fecha de pago declarada ES el vencimiento que lleva impreso la boleta.
    DateTime vencimientoBoleta = fecha > vencimiento ? fecha : vencimiento;
    decimal importeTotal = capital + interes.Interes;

    var codigo = BoletaBse.GenerarConConfiguracion(
        configuracion, declaracion.Period, declaracion.Cuit ?? string.Empty, vencimientoBoleta, importeTotal);

    // Si el período de mora cruzó más de un tramo, la boleta puede tener más de una norma: se listan todas.
    string? norma = interes.Tramos
        .Select(t => t.Norma)
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Distinct()
        .Aggregate((string?)null, (a, b) => a is null ? b : $"{a} / {b}");

    var datos = new BoletaDatosDto
    {
      Sindicato = declaracion.Sindicato ?? string.Empty,
      Jurisdiccion = declaracion.Jurisdiccion ?? string.Empty,
      Banco = banco!.Name,
      Sucursal = banco.Branch ?? string.Empty,
      NumeroCliente = configuracion!.ClientNumber,
      RazonSocial = declaracion.RazonSocial ?? string.Empty,
      Cuit = declaracion.Cuit ?? string.Empty,
      Periodo = declaracion.Period,
      AporteLey = declaracion.AporteLey,
      CuotaSindical = declaracion.CuotaSocio,
      AporteLeyPorcentaje = declaracion.AporteLeyPorcentaje,
      CuotaSindicalPorcentaje = declaracion.CuotaSindicalPorcentaje,
      Vencimiento = vencimientoBoleta,
      Capital = capital,
      Interes = interes.Interes,
      DiasInteres = interes.DiasMora,
      InteresPorcentaje = capital > 0 ? Math.Round(interes.Interes / capital * 100m, 4) : 0m,
      InteresNorma = norma,
      ImporteTotal = importeTotal,
      CodigoBarras = codigo
    };

    // Si el período de mora cruzó un cambio de tasa, se dice: el interés está sumado por tramos y conviene
    // que el estudio lo vea.
    string? advertencia = interes.Tramos.Count > 1
        ? "El interés se calculó en más de un tramo de tasa: " +
          string.Join(" + ", interes.Tramos.Select(t => $"{t.Dias} días al {t.TasaMensual * 100m:0.####}%")) +
          "."
        : null;

    return new BoletaArmadoResult(datos, null, advertencia);
  }

  /// <summary>
  /// Vencimiento de una DDJJ (el que le corresponde al período según la configuración bancaria del
  /// sindicato) y si ya pasó, para que la pantalla sepa si tiene que preguntarle al cliente cuándo va a
  /// pagar antes de imprimir. Devuelve <c>null</c> si falta la declaración o la configuración bancaria.
  /// </summary>
  public async Task<VencimientoBoletaDto?> ObtenerVencimientoAsync(int declarationId)
  {
    var cabecera = await CargarAsync(declarationId);
    if (cabecera.Error is not null) return null;

    var declaracion = cabecera.Declaracion!;
    var configuracion = cabecera.Configuracion!;

    try
    {
      var vencimiento = BoletaBse.PrimerVencimiento(declaracion.Period, configuracion.FirstDueDay);
      return new VencimientoBoletaDto(
          vencimiento,
          DateTime.Now.Date > vencimiento.Date,
          declaracion.TotalAmount);
    }
    catch (ArgumentException)
    {
      return null;
    }
  }

  /// <summary>
  /// Trae de una sola vez lo que necesita el armado: la cabecera de la declaración con sus subtotales
  /// (sumados EN SQL, porque sumarlos en memoria sobre una colección sin cargar daría 0 — es un bug que ya
  /// apareció con las 248.151 filas de la grilla de auditoría), la configuración bancaria y el banco.
  /// </summary>
  private async Task<CabeceraBoleta> CargarAsync(int declarationId)
  {
    var declaracion = await unitOfWork.Declarations.RunQueryAsync(q => q
        .Where(d => d.Id == declarationId)
        .Select(d => new DeclaracionBoleta(
            d.Id,
            d.Period,
            d.UnionId,
            d.TotalAmount,
            d.Details.Sum(x => x.LawContribution),
            d.Details.Sum(x => x.MemberContribution),
            d.Company.BusinessName,
            d.Company.Cuit,
            d.Union.Name,
            d.Union.JurisdictionProvince,
            d.Union.LawContributionPercentage,
            d.Union.UnionMemberPercentage,
            d.Status))
        .FirstOrDefaultAsync());

    if (declaracion is null)
      return new CabeceraBoleta(null, null, null, "La declaración no existe.");

    var configuracion = (await unitOfWork.UnionBankConfigs
        .FindAsync(c => c.UnionId == declaracion.UnionId && c.IsActive)).FirstOrDefault();

    if (configuracion is null)
      return new CabeceraBoleta(declaracion, null, null,
          $"El sindicato \"{declaracion.Sindicato}\" no tiene una configuración bancaria activa, así que no se puede emitir la boleta. Cargala en Bancos y boletas.");

    var banco = await unitOfWork.Banks.GetByIdAsync(configuracion.BankId);
    if (banco is null)
      return new CabeceraBoleta(declaracion, configuracion, null, "El banco configurado para el sindicato no existe.");

    return new CabeceraBoleta(declaracion, configuracion, banco, null);
  }

  /// <summary>Lo que el armado necesita, o el motivo por el que no se puede armar.</summary>
  private record CabeceraBoleta(
      DeclaracionBoleta? Declaracion,
      Domain.Entities.UnionBankConfig? Configuracion,
      Domain.Entities.Bank? Banco,
      string? Error);

  /// <summary>Cabecera de la DDJJ con lo que la boleta necesita, ya sumado en SQL.</summary>
  private record DeclaracionBoleta(
      int Id,
      string Period,
      int UnionId,
      decimal TotalAmount,
      decimal AporteLey,
      decimal CuotaSocio,
      string? RazonSocial,
      string? Cuit,
      string? Sindicato,
      string? Jurisdiccion,
      decimal AporteLeyPorcentaje,
      decimal CuotaSindicalPorcentaje,
      DeclarationStatus Status);
}
