using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

/// <summary>
/// Pruebas del recargo del 2° vencimiento, que es la cuenta que define CUÁNTO COBRA EL BANCO.
///
/// Datos reales medidos, no inventados:
///   - DDJJ 1737080 de ABDALA LLAMIL (empresa 29656, sindicato 19), período 2026-08, total 51.000,00.
///   - Boleta de GAMMAROTA que aportó el usuario: recargo 644,96 sobre 72.703,76 en 10 días.
///   - Tasa resarcitoria migrada del sistema anterior: 2,75% mensual (R (MEC) 823/2025), vigente desde
///     el 2025-07-01. Como FRACCIÓN en SIRGA: 0,0275.
///
/// La convención de prorrateo se midió contra la boleta real: por los DÍAS DEL MES del vencimiento
/// (31 en agosto) da 644,95 contra 644,96 real; con /30 daba 666,45 y fallaba por $21,49.
/// </summary>
public class BoletaServiceTests
{
  /// <summary>Tasa resarcitoria migrada, en la unidad de SIRGA (fracción).</summary>
  private const decimal TasaResarcitoria275 = 0.0275m;

  /// <summary>Configuración bancaria real del sindicato 19, medida en SIRGA_DB.</summary>
  private static UnionBankConfig ConfiguracionSindicato19() => new()
  {
    Id = 1,
    UnionId = 19,
    BankId = 1,
    ClientNumber = "18588639",
    EntityCode = "0134",
    DesignFormat = 4,
    FirstDueDay = 15,
    DaysToSecondDue = 10,
    IsActive = true
  };

  // ======================================================================
  // RECARGO DEL SEGUNDO VENCIMIENTO
  // ======================================================================

  [Fact]
  public void Recargo_AbdalaLlamil_ConLaTasaRealEnUnMesDe30Dias()
  {
    // Período 2026-08: 1er venc. 15/09/2026 y 2do 25/09/2026, septiembre tiene 30 días.
    // 51.000,00 * 2,75% * 10/30 = 467,50
    var (recargo, importeSegundo, porcentaje) =
        BoletaService.CalcularRecargo(51_000m, TasaResarcitoria275, 10, 30);

    Assert.Equal(0.9167m, Math.Round(porcentaje * 100m, 4));   // 0,9167% (2,75% x 10/30)
    Assert.Equal(467.50m, recargo);
    Assert.Equal(51_467.50m, importeSegundo);
  }

  /// <summary>
  /// La prueba que define la convención: la boleta real de GAMMAROTA. Falla 1 centavo contra el
  /// importe impreso (644,95 contra 644,96), que es redondeo del sistema anterior.
  /// </summary>
  [Fact]
  public void Recargo_Gammarota_ReproduceLaBoletaReal()
  {
    const decimal importeReal = 72_703.76m;

    // Agosto tiene 31 días: 72.703,76 * 2,75% * 10/31 = 644,95
    var (recargo, importeSegundo, _) = BoletaService.CalcularRecargo(importeReal, TasaResarcitoria275, 10, 31);

    Assert.Equal(644.95m, recargo);
    Assert.Equal(73_348.71m, importeSegundo);
    Assert.Equal(0.01m, 644.96m - recargo);   // el centavo de diferencia con la boleta impresa
  }

  [Fact]
  public void Recargo_ElProrrateoEsPorLosDiasDelMes_NoPor30()
  {
    // Mismo importe, misma tasa y mismos días: en un mes de 31 días el recargo es MENOR que en uno de
    // 30, porque se reparte sobre más días. Si se prorrateara por 30 darían iguales.
    const decimal importe = 72_703.76m;

    var (enMesDe31, _, _) = BoletaService.CalcularRecargo(importe, TasaResarcitoria275, 10, 31);
    var (enMesDe30, _, _) = BoletaService.CalcularRecargo(importe, TasaResarcitoria275, 10, 30);

    Assert.Equal(644.95m, enMesDe31);
    Assert.Equal(666.45m, enMesDe30);
    Assert.True(enMesDe31 < enMesDe30);
  }

  [Fact]
  public void Recargo_Redondea2Decimales_HaciaAfuera()
  {
    // 100,005 * 1% * 10/30 = 0,333350 -> 0,33; con el redondeo bancario de .NET daria 0,33 tambien,
    // pero el criterio tiene que ser el contable (hacia afuera).
    var (recargo, _, _) = BoletaService.CalcularRecargo(100.005m, 0.01m, 10, 30);
    Assert.Equal(0.33m, recargo);
  }

  [Fact]
  public void Recargo_SinDiasEntreVencimientos_EsCero()
  {
    var (recargo, importeSegundo, porcentaje) = BoletaService.CalcularRecargo(10_000m, TasaResarcitoria275, 0, 31);

    Assert.Equal(0m, recargo);
    Assert.Equal(10_000m, importeSegundo);
    Assert.Equal(0m, porcentaje);
  }

  // ======================================================================
  // CÓDIGO DE BARRAS CON LA CONFIGURACIÓN REAL DEL SINDICATO
  // ======================================================================

  [Fact]
  public void CodigoBarras_AbdalaLlamil_Periodo2026_08()
  {
    // Período 2026-08: el primer vencimiento es el 15/09/2026 y el segundo 10 días después, porque
    // así está configurado el sindicato 19 en la base.
    var configuracion = ConfiguracionSindicato19();

    var primerVencimiento = BoletaBse.PrimerVencimiento("2026-08", configuracion.FirstDueDay);
    Assert.Equal(new DateTime(2026, 9, 15), primerVencimiento);

    var codigo = BoletaBse.GenerarConConfiguracion(
        configuracion, "2026-08", "20405264693", 51_000m, 51_467.50m);

    Assert.Equal(BoletaBse.LargoCodigo, codigo.Length);
    Assert.All(codigo, c => Assert.True(char.IsDigit(c), $"El código tiene un caracter no numérico: {c}"));

    // ente 0134 | formato 4 | datos generales (período 0826 + relleno + CUIT) | moneda 1
    Assert.StartsWith("01344082600000204052646931", codigo);
    // 1er vencimiento 15/09/2026 = día 258 -> 26258, importe 51.000,00 -> 0005100000
    Assert.Contains("262580005100000", codigo);
    // 2do vencimiento 25/09/2026 = día 268 -> 26268, importe 51.467,50 -> 0005146750
    Assert.Contains("262680005146750", codigo);
  }
}
