using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Enums;

namespace SIRGA.Infrastructure.Legacy;

/// <summary>
/// Hace "la magia" completa de la conciliación, en UNA pasada y en el orden correcto:
///
///   1. registra el pago en **SIRGA** (`PaymentTransactions` + la DDJJ como pagada);
///   2. y recién después lo vuelca al **legado** (`ddjjt.fpago` e `impban1`), que es lo que lee SIGES WEB.
///
/// ESTE ORDEN IMPORTA: si se escribiera primero en el legado y fallara SIRGA, SIGES WEB daría la DDJJ por
/// pagada y SIRGA no tendría el pago registrado. Al revés, lo peor que puede pasar es que SIRGA tenga el
/// pago y el legado todavía no: se vuelve a correr y se completa (ambos lados son idempotentes).
///
/// Está en la capa de Infrastructure **a propósito**: es la única que puede ver las dos bases. La lógica de
/// cada lado vive en su servicio (Application e Infrastructure), no acá: acá sólo se orquesta.
///
/// **POR DEFECTO ES SIMULACRO**: no escribe en ninguna de las dos bases. Escribir hay que pedirlo.
/// </summary>
public class BankRenditionProcessor
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly LegacySindicatoRepository _legado;

  public BankRenditionProcessor(IUnitOfWork unitOfWork, LegacySindicatoRepository legado)
  {
    _unitOfWork = unitOfWork;
    _legado = legado;
  }

  public sealed record ResultadoProceso(
      int Lineas,
      int Conciliadas,
      int VolcadasAlLegado,
      int SinDdjj,
      int Parciales,
      bool Simulacro,
      IReadOnlyList<string> Detalles);

  public async Task<ResultadoProceso> ProcesarAsync(
      BankRenditionParser.Rendicion rendicion,
      bool soloSimulacro = true,
      string usuario = "conciliacion-banco",
      CancellationToken ct = default)
  {
    var detalles = new List<string>();

    // ---- 1) SIRGA -------------------------------------------------------------------------------
    var registro = new BankPaymentRegistrationService(_unitOfWork);
    var enSirga = await registro.RegistrarAsync(rendicion, soloSimulacro, ct);
    detalles.Add($"SIRGA: {enSirga.Conciliados} pago(s) {(soloSimulacro ? "para registrar" : "registrados")} " +
                 $"por {enSirga.TotalRegistrado:N2}.");
    detalles.AddRange(enSirga.Detalles);

    // ---- 2) Legado ------------------------------------------------------------------------------
    // Se vuelve a planificar para saber EXACTAMENTE qué líneas quedaron conciliables y con qué DDJJ.
    // (Es una función pura y barata; se recalcula para no acoplar los dos servicios por un estado interno.)
    var plan = await PlanificarAsync(rendicion, ct);
    var volcadas = 0;

    foreach (var linea in plan.Lineas.Where(l =>
                 l.Estado is BankReconciliationPlanner.Concordancia.Exacta
                          or BankReconciliationPlanner.Concordancia.ConInteres))
    {
      var periodo = DateTime.ParseExact(linea.Linea.Periodo + "-01", "yyyy-MM-dd",
          System.Globalization.CultureInfo.InvariantCulture);
      var fechaPago = FechaDeProceso(linea.Linea.FechaProceso);

      if (soloSimulacro)
      {
        var simulacro = await _legado.SimularPagoAsync(linea.Linea.Cuit, periodo, linea.Linea.ImportePrimero,
            fechaPago, ct);
        detalles.Add($"Legado (simulacro) {linea.Linea.Cuit} {linea.Linea.Periodo}: {simulacro.QueSeCambiaria}");
        continue;
      }

      var resultado = await _legado.MarcarPagadaAsync(linea.Linea.Cuit, periodo, linea.Linea.ImportePrimero,
          fechaPago, usuario, ct);
      if (resultado.Actualizado) volcadas++;
      detalles.Add($"Legado {linea.Linea.Cuit} {linea.Linea.Periodo}: {resultado.Detalle}");
    }

    return new ResultadoProceso(rendicion.Lineas.Count, enSirga.Conciliados, volcadas,
        enSirga.SinDdjj, enSirga.Parciales, soloSimulacro, detalles);
  }

  /// <summary>Las DDJJ candidatas, igual que las busca el registro: por CUIT y período.</summary>
  private async Task<BankReconciliationPlanner.Plan> PlanificarAsync(
      BankRenditionParser.Rendicion rendicion, CancellationToken ct)
  {
    var cuits = rendicion.Lineas.Select(l => l.Cuit).Distinct().ToList();
    var periodos = rendicion.Lineas.Select(l => l.Periodo).Distinct().ToList();

    var empresas = (await _unitOfWork.Companies.FindAsync(c => cuits.Contains(c.Cuit))).ToList();
    var ids = empresas.Select(e => e.Id).ToList();
    var declaraciones = ids.Count == 0
        ? []
        : (await _unitOfWork.Declarations.FindAsync(
            d => ids.Contains(d.CompanyId) && periodos.Contains(d.Period))).ToList();

    var candidatas = declaraciones
        .Join(empresas, d => d.CompanyId, e => e.Id,
            (d, e) => new BankReconciliationPlanner.DdjjCandidata(
                d.Id, d.Period, e.Cuit, d.TotalAmount, d.Status == DeclarationStatus.Paid))
        .ToList();

    return new BankReconciliationPlanner().Planificar(rendicion, candidatas);
  }

  /// <summary>La rendición trae la fecha de proceso como `DDMMAAAA`.</summary>
  private static DateTime FechaDeProceso(string fecha)
  {
    if (fecha.Length == 8
        && int.TryParse(fecha[..2], out var dia)
        && int.TryParse(fecha[2..4], out var mes)
        && int.TryParse(fecha[4..], out var anio)
        && mes is >= 1 and <= 12 && dia is >= 1 and <= 31)
    {
      return new DateTime(anio, mes, dia);
    }

    return DateTime.Today;
  }
}
