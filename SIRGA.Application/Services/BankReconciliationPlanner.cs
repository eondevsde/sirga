namespace SIRGA.Application.Services;

/// <summary>
/// Decide qué hacer con cada línea del archivo de rendición del banco, **sin escribir nada**: devuelve un
/// plan que después se ejecuta (o se muestra como simulacro).
///
/// LA LLAVE ES EL CÓDIGO DE BARRAS de la línea, que trae **período (MMAA) + CUIT**: identifica la DDJJ por
/// sí solo, sin depender del encabezado del archivo ni del importe.
///
/// POR QUE EL IMPORTE NO SE COMPARA POR IGUALDAD:
/// - la boleta puede haberse pagado **vencida**, y entonces el importe incluye el **interés resarcitorio**
///   ⇒ pagado &gt; capital es un pago COMPLETO, no un error;
/// - el legado guarda los importes en `float` (se midió 1234.5599999999999), así que hace falta una
///   tolerancia mínima.
///
/// NADA SE SALTEa: cada línea termina en un estado, incluida la que no tiene DDJJ en SIRGA. Un pago sin
/// DDJJ **no se pierde**: se informa para que alguien decida (puede ser un período del sistema anterior).
/// </summary>
public class BankReconciliationPlanner
{
  /// <summary>Tolerancia por los decimales del `float` del legado.</summary>
  private const decimal Tolerancia = 0.05m;

  public enum Concordancia
  {
    /// <summary>El importe coincide con el total de la DDJJ.</summary>
    Exacta,
    /// <summary>Se pagó más que el capital: incluye interés resarcitorio. Es un pago COMPLETO.</summary>
    ConInteres,
    /// <summary>Se pagó menos que el capital: pago parcial, la DDJJ no queda saldada.</summary>
    Parcial,
    /// <summary>No hay DDJJ en SIRGA para ese período y CUIT.</summary>
    SinDdjjEnSirga,
    /// <summary>La DDJJ ya estaba registrada como pagada (reimportación del archivo).</summary>
    YaPagada
  }

  /// <summary>Una DDJJ de SIRGA que puede corresponder a una línea del archivo.</summary>
  public sealed record DdjjCandidata(int Id, string Periodo, string Cuit, decimal TotalAmount, bool YaPagada);

  public sealed record LineaConciliada(
      BankRenditionParser.LineaRendicion Linea,
      Concordancia Estado,
      DdjjCandidata? Ddjj,
      decimal Diferencia,
      string Explicacion);

  public sealed record Plan(IReadOnlyList<LineaConciliada> Lineas)
  {
    public int Exactas => Lineas.Count(l => l.Estado == Concordancia.Exacta);
    public int ConInteres => Lineas.Count(l => l.Estado == Concordancia.ConInteres);
    public int Parciales => Lineas.Count(l => l.Estado == Concordancia.Parcial);
    public int SinDdjj => Lineas.Count(l => l.Estado == Concordancia.SinDdjjEnSirga);
    public int YaPagadas => Lineas.Count(l => l.Estado == Concordancia.YaPagada);

    /// <summary>Lo que SIRGA registraría como pago si se ejecuta el plan.</summary>
    public decimal TotalARegistrar => Lineas
        .Where(l => l.Estado is Concordancia.Exacta or Concordancia.ConInteres)
        .Sum(l => l.Linea.ImportePrimero);

    /// <summary>Lo que NO se puede registrar y necesita que alguien lo mire.</summary>
    public bool RequiereRevision => SinDdjj > 0 || Parciales > 0;
  }

  /// <summary>
  /// Arma el plan. `candidatas` son las DDJJ de SIRGA que el llamador ya buscó (por CUIT y período): así
  /// esta clase no depende de la base y se puede probar sola.
  /// </summary>
  public Plan Planificar(
      BankRenditionParser.Rendicion rendicion,
      IReadOnlyCollection<DdjjCandidata> candidatas)
  {
    var plan = new List<LineaConciliada>();

    foreach (var linea in rendicion.Lineas)
    {
      // Se busca por CUIT + periodo. Si hay mas de una (por ejemplo una rectificativa), se toma la de
      // mayor importe: es la version vigente con la que hay que comparar el pago.
      var candidata = candidatas
          .Where(c => c.Cuit == linea.Cuit && c.Periodo == linea.Periodo)
          .OrderByDescending(c => c.TotalAmount)
          .FirstOrDefault();

      if (candidata is null)
      {
        plan.Add(new LineaConciliada(linea, Concordancia.SinDdjjEnSirga, null, 0,
            $"No hay DDJJ en SIRGA para el CUIT {linea.Cuit} del periodo {linea.Periodo}. El pago NO se " +
            "pierde: queda para revisar (puede ser un periodo del sistema anterior)."));
        continue;
      }

      if (candidata.YaPagada)
      {
        plan.Add(new LineaConciliada(linea, Concordancia.YaPagada, candidata, 0,
            $"La DDJJ {candidata.Id} ya figura pagada: no se registra de nuevo (idempotencia)."));
        continue;
      }

      var diferencia = linea.ImportePrimero - candidata.TotalAmount;

      if (Math.Abs(diferencia) <= Tolerancia)
      {
        plan.Add(new LineaConciliada(linea, Concordancia.Exacta, candidata, 0,
            $"Coincide exacto con la DDJJ {candidata.Id} ({candidata.TotalAmount:N2})."));
      }
      else if (diferencia > 0)
      {
        plan.Add(new LineaConciliada(linea, Concordancia.ConInteres, candidata, diferencia,
            $"Se pago de mas que el capital de la DDJJ {candidata.Id}: la diferencia de {diferencia:N2} es " +
            "interes resarcitorio. El pago es COMPLETO."));
      }
      else
      {
        plan.Add(new LineaConciliada(linea, Concordancia.Parcial, candidata, -diferencia,
            $"Se pago MENOS que el capital de la DDJJ {candidata.Id}: faltan {-diferencia:N2}. Pago parcial: " +
            "la DDJJ NO queda saldada."));
      }
    }

    return new Plan(plan);
  }
}
