using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Application.Services;

/// <summary>
/// Registra en SIRGA los pagos que vienen del archivo de rendición del banco.
///
/// QUÉ HACE: lee la rendición, busca la DDJJ de cada línea (por **CUIT + período**, que salen del código de
/// barras), decide con <see cref="BankReconciliationPlanner"/> y, si corresponde, crea el pago y marca la
/// DDJJ como pagada.
///
/// **POR DEFECTO ES SIMULACRO** (`soloSimulacro: true`): informa qué haría sin escribir nada. Escribir
/// requiere pedirlo explícitamente, porque son datos del usuario.
///
/// IDEMPOTENCIA POR DOS LADOS:
/// - si la DDJJ ya está pagada, no se registra de nuevo (lo detecta el planificador);
/// - y si ya existe un pago con el **mismo código de barras** (`ExternalReference`), tampoco. El segundo
///   control cubre el caso de reimportar un archivo viejo cuya DDJJ se pagó por otra vía.
///
/// QUÉ NO HACE (a propósito): no escribe en el legado. Eso lo hace <c>LegacySindicatoRepository</c>, con su
/// propio respaldo y su propia verificación, porque es otra base y otra responsabilidad.
/// </summary>
public class BankPaymentRegistrationService
{
  /// <summary>Forma de pago que se guarda para los cobros que llegan por rendición bancaria.</summary>
  public const string FormaDePagoRendicion = "Rendicion bancaria";

  private readonly IUnitOfWork _unitOfWork;

  public BankPaymentRegistrationService(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public sealed record Resultado(
      int Conciliados,
      int SinDdjj,
      int Parciales,
      int YaPagadas,
      decimal TotalRegistrado,
      bool Simulacro,
      IReadOnlyList<string> Detalles)
  {
    public bool RequiereRevision => SinDdjj > 0 || Parciales > 0;
  }

  public async Task<Resultado> RegistrarAsync(
      BankRenditionParser.Rendicion rendicion,
      bool soloSimulacro = true,
      CancellationToken ct = default)
  {
    var detalles = new List<string>();

    // 1) Las DDJJ candidatas: por CUIT (de las empresas) y período.
    var cuits = rendicion.Lineas.Select(l => l.Cuit).Distinct().ToList();
    var periodos = rendicion.Lineas.Select(l => l.Periodo).Distinct().ToList();

    var empresas = (await _unitOfWork.Companies.FindAsync(c => cuits.Contains(c.Cuit))).ToList();
    var idsEmpresas = empresas.Select(e => e.Id).ToList();
    var declaraciones = idsEmpresas.Count == 0
        ? []
        : (await _unitOfWork.Declarations.FindAsync(
            d => idsEmpresas.Contains(d.CompanyId) && periodos.Contains(d.Period))).ToList();

    var candidatas = declaraciones
        .Join(empresas, d => d.CompanyId, e => e.Id,
            (d, e) => new BankReconciliationPlanner.DdjjCandidata(
                d.Id, d.Period, e.Cuit, d.TotalAmount, d.Status == DeclarationStatus.Paid))
        .ToList();

    // 2) Los pagos que YA existen con esos codigos de barras (segunda red de idempotencia).
    var codigos = rendicion.Lineas.Select(l => l.CodigoBarras).Distinct().ToList();
    var yaRegistrados = (await _unitOfWork.PaymentTransactions.FindAsync(
        t => t.ExternalReference != null && codigos.Contains(t.ExternalReference)))
        .Select(t => t.ExternalReference!)
        .ToHashSet();

    // 3) El plan.
    var plan = new BankReconciliationPlanner().Planificar(rendicion, candidatas);

    var conciliados = 0;
    var totalRegistrado = 0m;

    foreach (var linea in plan.Lineas)
    {
      if (linea.Estado is not (BankReconciliationPlanner.Concordancia.Exacta
                            or BankReconciliationPlanner.Concordancia.ConInteres))
      {
        detalles.Add($"{linea.Linea.Cuit} {linea.Linea.Periodo} $ {linea.Linea.ImportePrimero:N2}: " +
                     $"{linea.Estado} - {linea.Explicacion}");
        continue;
      }

      if (yaRegistrados.Contains(linea.Linea.CodigoBarras))
      {
        detalles.Add($"{linea.Linea.Cuit} {linea.Linea.Periodo}: ya habia un pago con este codigo de barras " +
                     "(no se registra de nuevo).");
        continue;
      }

      var declaracion = declaraciones.First(d => d.Id == linea.Ddjj!.Id);

      if (!soloSimulacro)
      {
        await _unitOfWork.PaymentTransactions.AddAsync(new PaymentTransaction
        {
          DeclarationId = declaracion.Id,
          Amount = linea.Linea.ImportePrimero,
          // La rendicion trae la fecha de proceso del banco: es la fecha del cobro.
          PaymentDate = FechaDeProceso(linea.Linea.FechaProceso),
          ExternalReference = linea.Linea.CodigoBarras,
          PaymentMethod = FormaDePagoRendicion,
          Notes = linea.Estado == BankReconciliationPlanner.Concordancia.ConInteres
              ? $"Incluye interes: pago {linea.Linea.ImportePrimero:N2} contra capital {declaracion.TotalAmount:N2}"
              : null,
          CreatedAt = DateTime.Now
        });

        declaracion.Status = DeclarationStatus.Paid;
        declaracion.PaymentDate = FechaDeProceso(linea.Linea.FechaProceso);
        await _unitOfWork.Declarations.UpdateAsync(declaracion);
      }

      conciliados++;
      totalRegistrado += linea.Linea.ImportePrimero;
      detalles.Add($"{linea.Linea.Cuit} {linea.Linea.Periodo} $ {linea.Linea.ImportePrimero:N2}: " +
                   $"{(soloSimulacro ? "SE REGISTRARIA" : "registrado")} en la DDJJ {declaracion.Id} " +
                   $"({linea.Estado}).");
    }

    // Se guarda recien al final: si algo falla antes, no queda un pago suelto sin su DDJJ marcada.
    if (!soloSimulacro && conciliados > 0) await _unitOfWork.CompleteAsync();

    return new Resultado(conciliados, plan.SinDdjj, plan.Parciales, plan.YaPagadas,
        totalRegistrado, soloSimulacro, detalles);
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

    // Si la fecha viene rara no se inventa una: se usa la de hoy y queda dicho en las notas del pago.
    return DateTime.Today;
  }
}
