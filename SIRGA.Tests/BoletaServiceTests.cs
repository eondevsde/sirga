using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

/// <summary>
/// Pruebas del recargo del 2° vencimiento, que es la cuenta que define CUÁNTO COBRA EL BANCO en la
/// boleta. El armado completo de los datos vive en <see cref="BoletaService.ArmarAsync"/> y necesita
/// base; lo que se prueba acá es la cuenta, que es lo que puede salir mal en silencio.
///
/// Los datos salen de casos reales medidos, no de números inventados:
///   - DDJJ 1737080 de ABDALA LLAMIL (empresa 29656, sindicato 19), período 2026-08, total 51.000,00.
///   - La boleta de GAMMAROTA que aportó el usuario: recargo 644,96 sobre 72.703,76 en 10 días.
/// </summary>
public class BoletaServiceTests
{
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
  public void Recargo_AbdalaLlamil_ConLaTasaPorDefecto()
  {
    // 3% mensual prorrateado a 10 días = 1,00% -> 510,00 sobre un total de 51.000,00.
    var (recargo, importeSegundo, porcentaje) = BoletaService.CalcularRecargo(51_000m, 0.03m, 10);

    Assert.Equal(0.01m, porcentaje);
    Assert.Equal(510.00m, recargo);
    Assert.Equal(51_510.00m, importeSegundo);
  }

  [Fact]
  public void Recargo_Redondea2Decimales_HaciaAfuera()
  {
    // 72.703,76 * 1% = 727,0376 -> 727,04
    var (recargo, importeSegundo, _) = BoletaService.CalcularRecargo(72_703.76m, 0.03m, 10);

    Assert.Equal(727.04m, recargo);
    Assert.Equal(73_430.80m, importeSegundo);
  }

  [Fact]
  public void Recargo_SinDiasEntreVencimientos_EsCero()
  {
    var (recargo, importeSegundo, porcentaje) = BoletaService.CalcularRecargo(10_000m, 0.03m, 0);

    Assert.Equal(0m, recargo);
    Assert.Equal(10_000m, importeSegundo);
    Assert.Equal(0m, porcentaje);
  }

  /// <summary>
  /// Deja anotado, con el caso real, QUE FALTA LA TASA: con el 3% mensual por defecto de SIRGA la
  /// fórmula da 1,00% ($727,04) y el recargo real de la boleta de GAMMAROTA fue 0,887% ($644,96).
  /// La fórmula puede expresar el caso real; lo que falta es cargar la escala de tasas de AFIP.
  /// Si algún día se carga la escala, este test tiene que cambiar y el recargo tiene que dar 644,96.
  /// </summary>
  [Fact]
  public void Recargo_ConLaTasaPorDefecto_NO_ReproduceElCasoReal_PorqueFaltaLaEscalaDeAfip()
  {
    const decimal importeReal = 72_703.76m;
    const decimal recargoReal = 644.96m;

    var (recargoConDefault, _, porcentaje) = BoletaService.CalcularRecargo(importeReal, 0.03m, 10);

    Assert.Equal(0.01m, porcentaje);                  // 1,00% con la tasa por defecto
    Assert.Equal(727.04m, recargoConDefault);
    Assert.NotEqual(recargoReal, recargoConDefault);  // 644,96 sólo con la tasa real de AFIP
  }

  [Fact]
  public void Recargo_ConLaTasaQueImplicaElCasoReal_LoReproduce()
  {
    // Verifica que la convención de prorrateo (días / 30) puede expresar el caso real: despejando la
    // tasa mensual que implica el 0,887% en 10 días, la fórmula devuelve exactamente 644,96.
    const decimal importe = 72_703.76m;
    const decimal recargoEsperado = 644.96m;

    decimal tasaMensual = (recargoEsperado / importe) * BoletaService.DiasDelMesParaProrrateo / 10m;
    var (recargo, _, _) = BoletaService.CalcularRecargo(importe, tasaMensual, 10);

    Assert.Equal(recargoEsperado, recargo);
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
        configuracion, "2026-08", "20405264693", 51_000m, 51_510m);

    Assert.Equal(BoletaBse.LargoCodigo, codigo.Length);
    Assert.All(codigo, c => Assert.True(char.IsDigit(c), $"El código tiene un caracter no numérico: {c}"));

    // ente 0134 | formato 4 | datos generales (período 0826 + relleno + CUIT) | moneda 1
    Assert.StartsWith("01344082600000204052646931", codigo);
    // 1er vencimiento 15/09/2026 = día 258 -> 26258, importe 51.000,00 -> 0005100000
    Assert.Contains("262580005100000", codigo);
    // 2do vencimiento 25/09/2026 = día 268 -> 26268, importe 51.510,00 -> 0005151000
    Assert.Contains("262680005151000", codigo);
  }
}
