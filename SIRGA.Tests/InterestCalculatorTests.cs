using SIRGA.Application.Services;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Tests;

/// <summary>
/// Pruebas del MOTOR DE INTERESES RESARCITORIOS (ARCA), según la especificación del usuario:
/// mora desde el día siguiente al vencimiento hasta la fecha de pago, fragmentada por tramos de tasa,
/// con tasa diaria = mensual / 30 SIEMPRE (sin importar los días del mes real).
///
/// Los tramos y las tasas son los REALES de la escala migrada (coinciden con la matriz de ARCA que pasó
/// el usuario): 01/02/2025-28/02/2025 = 7,26%; 01/03/2025-30/06/2025 = 4,00%; 01/07/2025 en adelante =
/// 2,75%.
/// </summary>
public class InterestCalculatorTests
{
  private static InterestRate Tasa(string desde, string? hasta, decimal porcentajeMensual, string norma = "R (ME) 3/2024") => new()
  {
    EffectiveDate = DateTime.Parse(desde, System.Globalization.CultureInfo.InvariantCulture),
    ValidTo = hasta is null ? null : DateTime.Parse(hasta, System.Globalization.CultureInfo.InvariantCulture),
    MonthlyRate = porcentajeMensual / 100m,   // SIRGA guarda la FRACCIÓN (2,75% -> 0,0275)
    TypeOfInterest = InterestType.Resarcitorio,
    Norm = norma,
    IsActive = true
  };

  private static List<InterestRate> EscalaReal() =>
  [
    Tasa("2025-02-01", "2025-02-28", 7.26m),
    Tasa("2025-03-01", "2025-06-30", 4.00m),
    Tasa("2025-07-01", null, 2.75m, "R (MEC) 823/2025")
  ];

  // ======================================================================
  // 1. VALIDACIÓN INICIAL: EN TÉRMINO NO HAY INTERÉS
  // ======================================================================

  [Theory]
  [InlineData("2026-09-15", "2026-09-15")]   // pagó el mismo día del vencimiento
  [InlineData("2026-09-15", "2026-09-10")]   // pagó antes
  public void SiPagaEnTermino_OAntes_ElInteresEsCero(string vencimiento, string pago)
  {
    var r = InterestCalculator.Calcular(
        100_000m,
        DateTime.Parse(vencimiento, System.Globalization.CultureInfo.InvariantCulture),
        DateTime.Parse(pago, System.Globalization.CultureInfo.InvariantCulture),
        EscalaReal());

    Assert.Equal(0m, r.Interes);
    Assert.Equal(0, r.DiasMora);
    Assert.Empty(r.Tramos);
  }

  // ======================================================================
  // 2. DÍAS DE MORA: DESDE EL DÍA SIGUIENTE AL VENCIMIENTO
  // ======================================================================

  [Fact]
  public void LaMoraEmpieza_ElDiaSiguienteAlVencimiento()
  {
    // Vence el 15/09 y paga el 25/09: los días de mora son 16..25 = 10 (no 9, no 11).
    var r = InterestCalculator.Calcular(
        51_000m,
        new DateTime(2026, 9, 15),
        new DateTime(2026, 9, 25),
        EscalaReal());

    Assert.Equal(10, r.DiasMora);
    Assert.Equal(10, r.Tramos.Sum(t => t.Dias));
  }

  // ======================================================================
  // 3. TASA DIARIA = MENSUAL / 30, SIEMPRE
  // ======================================================================

  [Fact]
  public void TasaDiaria_EsLaMensualSobre30_SinImportarElMes()
  {
    // 2,75% mensual -> 0,00091667 diaria, y se aplica igual en agosto (31 días) que en septiembre (30).
    var r = InterestCalculator.Calcular(
        72_703.76m,
        new DateTime(2025, 8, 12),
        new DateTime(2025, 8, 22),
        EscalaReal());

    Assert.Equal(0.0275m / InterestCalculator.DiasBaseMes, r.Tramos.Single().TasaDiaria);
    Assert.Equal(666.45m, r.Interes);   // 72.703,76 x 0,00091667 x 10
  }

  // ======================================================================
  // 4. FRAGMENTACIÓN POR TRAMOS: LA DEUDA ATRAVIESA UN CAMBIO DE TASA
  // ======================================================================

  [Fact]
  public void SiElPeriodoCruzaUnCambioDeTasa_SeCalculaPorTramos()
  {
    // Vence el 15/02/2025 y paga el 15/03/2025: 13 días al 7,26% + 15 días al 4,00% = 28 días de mora.
    var r = InterestCalculator.Calcular(100_000m, new DateTime(2025, 2, 15), new DateTime(2025, 3, 15), EscalaReal());

    Assert.Equal(28, r.DiasMora);
    Assert.Equal(2, r.Tramos.Count);

    var primero = r.Tramos[0];
    Assert.Equal(new DateTime(2025, 2, 16), primero.Desde);
    Assert.Equal(new DateTime(2025, 2, 28), primero.Hasta);
    Assert.Equal(13, primero.Dias);
    Assert.Equal(3_146.00m, primero.Interes);   // 100.000 x 0,00242 x 13

    var segundo = r.Tramos[1];
    Assert.Equal(new DateTime(2025, 3, 1), segundo.Desde);
    Assert.Equal(new DateTime(2025, 3, 15), segundo.Hasta);
    Assert.Equal(15, segundo.Dias);
    Assert.Equal(2_000.00m, segundo.Interes);   // 100.000 x 0,00133333 x 15

    Assert.Equal(5_146.00m, r.Interes);         // suma de los tramos
    Assert.False(r.FaltaTasa);
  }

  [Fact]
  public void SiElPeriodoCruzaTresTramos_SeSumanLosTres()
  {
    // Vence el 15/02/2025 y paga el 15/08/2025: 13 días al 7,26% + 122 al 4,00% + 46 al 2,75% = 181.
    var r = InterestCalculator.Calcular(100_000m, new DateTime(2025, 2, 15), new DateTime(2025, 8, 15), EscalaReal());

    Assert.Equal(3, r.Tramos.Count);
    Assert.Equal(13 + 122 + 46, r.DiasMora);
    Assert.Equal(181, r.DiasMora);
    Assert.Equal(r.Tramos.Sum(t => t.Interes), r.Interes);
  }

  // ======================================================================
  // 5. CONTROL: SI FALTARA LA TASA, HAY QUE VERLO
  // ======================================================================

  [Fact]
  public void SiNoHayTasaParaElPeriodo_LoReporta()
  {
    // Sin tasas cargadas: hay mora pero ningún tramo la cubre. No se puede inventar el interés.
    var r = InterestCalculator.Calcular(10_000m, new DateTime(2026, 8, 15), new DateTime(2026, 9, 15), []);

    Assert.Equal(0m, r.Interes);
    Assert.Empty(r.Tramos);
    Assert.Equal(31, r.DiasMora);
    Assert.True(r.FaltaTasa);
    Assert.Equal(31, r.DiasSinTasa);
  }

  [Fact]
  public void SiLaTasaCubreSoloParteDelPeriodo_LoReporta()
  {
    // La escala termina el 28/02 y la mora sigue hasta el 15/03: 15 días quedan sin tasa.
    var soloFebrero = new List<InterestRate> { Tasa("2025-02-01", "2025-02-28", 7.26m) };

    var r = InterestCalculator.Calcular(100_000m, new DateTime(2025, 2, 15), new DateTime(2025, 3, 15), soloFebrero);

    Assert.Equal(13, r.Tramos.Sum(t => t.Dias));
    Assert.Equal(15, r.DiasSinTasa);
    Assert.True(r.FaltaTasa);
  }

  // ======================================================================
  // 6. EL CASO REAL DE LA BOLETA DEL BANCO
  // ======================================================================

  /// <summary>
  /// OJO: acá queda anotada una DIFERENCIA CON LA BOLETA REAL que hay que resolver con el usuario.
  ///
  /// La boleta de GAMMAROTA (período 07/2025, 1er venc. 12/08/2025, 2do venc. 22/08/2025 sobre
  /// 72.703,76) tiene impreso un recargo de **644,96**. Con la fórmula de ARCA —tasa diaria = mensual/30—
  /// los 10 días de mora dan **666,45**: la diferencia es 21,49.
  ///
  /// Con la convención anterior (prorratear la tasa mensual por los 31 días de agosto) daba 644,95, que
  /// es el número impreso. O sea: la boleta real parece haber usado días del mes y no /30. Es UNA sola
  /// boleta, así que hay que contrastar con más antes de decidir cuál manda.
  /// </summary>
  [Fact]
  public void BoletaRealGammarota_ConLaFormulaDeArca_DaDistintoQueLaBoletaImpresaLo()
  {
    var r = InterestCalculator.Calcular(72_703.76m, new DateTime(2025, 8, 12), new DateTime(2025, 8, 22), EscalaReal());

    Assert.Equal(666.45m, r.Interes);
    Assert.NotEqual(644.96m, r.Interes);
    Assert.Equal(21.49m, r.Interes - 644.96m);
  }
}
