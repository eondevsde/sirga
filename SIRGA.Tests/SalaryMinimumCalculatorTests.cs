using SIRGA.Application.Services;
using SIRGA.Domain.Enums;

namespace SIRGA.Tests;

/// <summary>
/// Pruebas del MÍNIMO DE ESCALA: el sueldo de referencia que se compara contra el recibo de sueldo.
///
/// Los importes de los casos "reales" salen de la escala vigente 2026-03 del
/// SINDICATO EMPLEADOS DE COMERCIO SANTIAGO DEL ESTERO (UnionId 19) medidos contra SIRGA_DB, para
/// que el cálculo quede anclado a datos concretos y no a números inventados.
/// </summary>
public class SalaryMinimumCalculatorTests
{
  // ======================================================================
  // ANTIGÜEDAD: AÑOS CUMPLIDOS DESDE EL INGRESO A LA EMPRESA
  // ======================================================================

  [Theory]
  // Ingresó el 13/11/2018 y se declara marzo 2026: todavía no llegó el aniversario -> 7 años.
  [InlineData("2018-11-13", "2026-03-01", 7)]
  // Ingresó el 01/11/2019 y se declara marzo 2026 -> 6 años.
  [InlineData("2019-11-01", "2026-03-01", 6)]
  // El mismo día y mes del aniversario YA cuenta como año cumplido.
  [InlineData("2018-03-01", "2026-03-01", 8)]
  // Un día antes del aniversario, no.
  [InlineData("2018-03-02", "2026-03-01", 7)]
  // Ingreso posterior al período: no puede dar antigüedad negativa.
  [InlineData("2027-01-01", "2026-03-01", 0)]
  public void Antiguedad_CuentaAniosCumplidos(string ingreso, string corte, int esperado)
  {
    // Se parsea con cultura invariante a propósito: con la cultura local (es-AR) el formato de las
    // fechas de los InlineData podría interpretarse al revés (mes/día).
    var anios = SalaryMinimumCalculator.AniosAntiguedad(
        DateTime.Parse(ingreso, System.Globalization.CultureInfo.InvariantCulture),
        DateTime.Parse(corte, System.Globalization.CultureInfo.InvariantCulture));

    Assert.Equal(esperado, anios);
  }

  [Fact]
  public void Antiguedad_SinFechaIngreso_EsCero()
  {
    // Assignments.CreatedAt sin dato: no se inventa antigüedad.
    Assert.Equal(0, SalaryMinimumCalculator.AniosAntiguedad(default, new DateTime(2026, 3, 1)));
  }

  // ======================================================================
  // MÍNIMO DE ESCALA: BÁSICO + ANTIGÜEDAD + NR, PRORRATEADO POR JORNADA
  // ======================================================================

  /// <summary>
  /// Caso real: JIMENEZ IRMA BEATRIZ, MAESTRANZA B (código 13), empresa 29656 "ABDALA LLAMIL",
  /// ingresó a la empresa el 13/11/2018. Escala 2026-03 del sindicato 19.
  /// </summary>
  [Fact]
  public void Minimo_MaestranzaB_Comercio_Marzo2026_JornadaCompleta()
  {
    var minimo = SalaryMinimumCalculator.Minimo(
        basico: 1_212_885.36m,
        antiguedadPorcentaje: 1m,
        nr1: 60_644.27m,
        nr2: 120_000.00m,
        fechaIngreso: new DateTime(2018, 11, 13),
        fechaCorte: new DateTime(2026, 3, 1),
        jornada: JornadaType.Completa);

    // Básico 1.212.885,36 + antigüedad 7 años (84.901,98) + NR 180.644,27
    Assert.Equal(1_478_431.61m, minimo);
  }

  /// <summary>
  /// Caso real: BUSTAMANTE MARIA ESTELA, ADMINISTRATIVO B (código 2), misma empresa,
  /// ingresó a la empresa el 01/11/2019 -> 6 años.
  /// </summary>
  [Fact]
  public void Minimo_AdministrativoB_Comercio_Marzo2026_JornadaCompleta()
  {
    var minimo = SalaryMinimumCalculator.Minimo(
        basico: 1_199_026.94m,
        antiguedadPorcentaje: 1m,
        nr1: 59_951.35m,
        nr2: 120_000.00m,
        fechaIngreso: new DateTime(2019, 11, 1),
        fechaCorte: new DateTime(2026, 3, 1),
        jornada: JornadaType.Completa);

    Assert.Equal(1_450_919.91m, minimo);
  }

  [Fact]
  public void Minimo_JornadaParcial_EsLaMitadDelMinimoCompleto_TambienLosNR()
  {
    // Definido con el usuario: parcial = 50% del mínimo COMPLETO, incluidos los acuerdos no
    // remunerativos. 1.478.431,61 / 2 = 739.215,805 -> se redondea hacia afuera.
    var minimo = SalaryMinimumCalculator.Minimo(
        basico: 1_212_885.36m,
        antiguedadPorcentaje: 1m,
        nr1: 60_644.27m,
        nr2: 120_000.00m,
        fechaIngreso: new DateTime(2018, 11, 13),
        fechaCorte: new DateTime(2026, 3, 1),
        jornada: JornadaType.Parcial);

    Assert.Equal(739_215.81m, minimo);
  }

  [Fact]
  public void Minimo_SinAntiguedadCargada_NoSumaAntiguedad()
  {
    // Es el estado actual de las 1.339 escalas: AntiguedadPercentage = 0. La antigüedad suma $0,
    // así que el mínimo queda por debajo del real. La pantalla lo avisa con un chip.
    var conCero = SalaryMinimumCalculator.Minimo(
        basico: 1_000_000m, antiguedadPorcentaje: 0m, nr1: 0m, nr2: 0m,
        fechaIngreso: new DateTime(2018, 1, 1), fechaCorte: new DateTime(2026, 3, 1),
        jornada: JornadaType.Completa);

    Assert.Equal(1_000_000m, conCero);
    Assert.Equal(0m, SalaryMinimumCalculator.Antiguedad(1_000_000m, 0m, 8));
  }

  [Fact]
  public void Minimo_IngresoIgualAlPeriodo_EsSoloElBasicoMasNR()
  {
    var minimo = SalaryMinimumCalculator.Minimo(
        basico: 500_000m, antiguedadPorcentaje: 1m, nr1: 10_000m, nr2: 5_000m,
        fechaIngreso: new DateTime(2026, 3, 1), fechaCorte: new DateTime(2026, 3, 1),
        jornada: JornadaType.Completa);

    Assert.Equal(515_000m, minimo);
  }

  // ======================================================================
  // COMPARACIÓN CONTRA LA ESCALA: DEBAJO / IGUAL / SUPERIOR / SIN DATOS
  // ======================================================================

  [Theory]
  [InlineData(1_400_000, 1_478_431.61, EscalaComparacion.Debajo)]
  [InlineData(1_478_431.61, 1_478_431.61, EscalaComparacion.Igual)]
  // Dentro de la tolerancia de $1 se considera igual: exigir el centavo exacto haría que "igual"
  // no apareciera nunca en la práctica.
  [InlineData(1_478_432.00, 1_478_431.61, EscalaComparacion.Igual)]
  [InlineData(1_600_000, 1_478_431.61, EscalaComparacion.Superior)]
  public void Comparar_ContraMinimoDeEscala(decimal remuneracion, decimal minimo, EscalaComparacion esperado)
  {
    Assert.Equal(esperado, SalaryMinimumCalculator.Comparar(remuneracion, minimo));
  }

  [Fact]
  public void Comparar_SinMinimo_DevuelveSinDatos_NoSuperior()
  {
    // Un mínimo en cero es "falta la escala", NO "el sueldo está por encima". Si se leyera como
    // Superior, el aviso saldría al revés de lo que pasa.
    Assert.Equal(EscalaComparacion.SinDatos, SalaryMinimumCalculator.Comparar(500_000m, 0m));
  }

  // ======================================================================
  // REDONDEO DE DINERO
  // ======================================================================

  [Fact]
  public void Minimo_Redondea2Decimales_HaciaAfuera()
  {
    // 333.333,33 * 1% * 1 año = 3.333,3333 -> 3.333,33 (no redondeo bancario)
    Assert.Equal(3_333.33m, SalaryMinimumCalculator.Antiguedad(333_333.33m, 1m, 1));

    // 100,005 con redondeo bancario daría 100,00; hacia afuera da 100,01.
    var minimo = SalaryMinimumCalculator.Minimo(
        basico: 100.005m, antiguedadPorcentaje: 0m, nr1: 0m, nr2: 0m,
        fechaIngreso: default, fechaCorte: new DateTime(2026, 3, 1), jornada: JornadaType.Completa);
    Assert.Equal(100.01m, minimo);
  }
}
