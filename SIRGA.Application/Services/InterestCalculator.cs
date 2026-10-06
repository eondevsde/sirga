using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

/// <summary>Un tramo de tasa que cubre parte del período de mora.</summary>
public record TramoInteres(
    DateTime Desde,
    DateTime Hasta,
    int Dias,
    decimal TasaMensual,
    decimal TasaDiaria,
    string? Norma,
    decimal Interes);

/// <summary>Resultado del cálculo: el interés total, el detalle por tramo y los días de mora.</summary>
public record ResultadoInteres(decimal Interes, IReadOnlyList<TramoInteres> Tramos, int DiasMora)
{
  /// <summary>Días de mora que NO quedaron cubiertos por ninguna tasa cargada. Debería ser 0.</summary>
  public int DiasSinTasa => DiasMora - Tramos.Sum(t => t.Dias);

  public bool FaltaTasa => DiasMora > 0 && DiasSinTasa > 0;
}

/// <summary>
/// Motor de cálculo de INTERESES RESARCITORIOS (ARCA), según la especificación del usuario:
///
///   1. Si <c>fechaPago &lt;= fechaVencimiento</c>, el interés es 0 (pagó en término).
///   2. El período de mora va desde <b>el día siguiente al vencimiento</b> hasta la fecha de pago,
///      ambos inclusive.
///   3. La deuda se <b>fragmenta por tramos de tasa</b>: si el período de mora atraviesa un cambio de
///      tasa (ARCA las cambia bimestralmente), cada parte se calcula con la tasa que le corresponde.
///   4. La <b>tasa diaria es la mensual dividida por 30 SIEMPRE</b>, sin importar si el mes real tiene
///      28, 29, 30 o 31 días. (Esto reemplaza la convención anterior, que prorrateaba por los días del
///      mes; ver la nota al final.)
///   5. Interés del tramo = capital × tasa diaria × días del tramo. El total es la suma de los tramos.
///
/// Es una función pura y estática a propósito: es la cuenta que define cuánto se cobra, así que tiene
/// que poder probarse sin base de datos.
/// </summary>
public static class InterestCalculator
{
  /// <summary>Base de días para pasar la tasa mensual a diaria: 30 SIEMPRE (especificación de ARCA).</summary>
  public const int DiasBaseMes = 30;

  public static ResultadoInteres Calcular(
      decimal capitalAdeudado,
      DateTime fechaVencimiento,
      DateTime fechaPago,
      IEnumerable<InterestRate> tasas)
  {
    // 1) Validación inicial: en término o antes -> no hay interés.
    if (fechaPago.Date <= fechaVencimiento.Date)
      return new ResultadoInteres(0m, Array.Empty<TramoInteres>(), 0);

    // 2) Rango de mora: del día siguiente al vencimiento hasta la fecha de pago, inclusive.
    var inicioMora = fechaVencimiento.Date.AddDays(1);
    var finMora = fechaPago.Date;
    int diasMora = (finMora - inicioMora).Days + 1;

    // 3) Fragmentación por tramos: se recorre cada tasa y se toma la parte que cae dentro de la mora.
    var tramos = new List<TramoInteres>();
    foreach (var tasa in tasas.OrderBy(t => t.EffectiveDate))
    {
      var desdeTramo = tasa.EffectiveDate.Date;
      var hastaTramo = tasa.ValidTo?.Date ?? DateTime.MaxValue;

      var desde = desdeTramo > inicioMora ? desdeTramo : inicioMora;
      var hasta = hastaTramo < finMora ? hastaTramo : finMora;
      if (hasta < desde) continue;   // el tramo no toca el período de mora

      // 4) Días corridos del tramo y tasa diaria (mensual / 30).
      int dias = (hasta - desde).Days + 1;
      decimal tasaDiaria = tasa.MonthlyRate / DiasBaseMes;

      // 5) Acumulación del tramo. Se redondea CADA tramo a 2 decimales (es un importe) y el total es la
      // suma: si se sumara sin redondear, el desglose no cerraría con el total. Pasa de verdad porque la
      // tasa diaria suele ser un decimal periódico (0,04 / 30 = 0,00133333...): un tramo de 15 días da
      // 1.999,9999999999999999999999950 en lugar de 2.000,00.
      tramos.Add(new TramoInteres(
          desde, hasta, dias, tasa.MonthlyRate, tasaDiaria, tasa.Norm,
          Redondear(capitalAdeudado * tasaDiaria * dias)));
    }

    return new ResultadoInteres(tramos.Sum(t => t.Interes), tramos, diasMora);
  }

  /// <summary>Redondeo de dinero a 2 decimales hacia afuera, igual que el resto del sistema.</summary>
  private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
