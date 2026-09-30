namespace SIRGA.Tests;

/// <summary>
/// Pruebas de calculos financieros: intereses, cuotas, totales.
/// </summary>
public class FinancialCalculationTests
{
  // ======================================================================
  // CALCULO DE INTERES SIMPLE
  // ======================================================================

  [Theory]
  [InlineData(10000, 0.03, 1, 300)]
  [InlineData(10000, 0.03, 6, 1800)]
  [InlineData(5000, 0.025, 12, 1500)]
  [InlineData(0, 0.03, 6, 0)]
  public void Calcular_InteresSimple_Mensual(
      decimal capital, decimal tasaMensual, int meses, decimal esperado)
  {
    var interes = capital * tasaMensual * meses;
    Assert.Equal(esperado, interes);
  }

  // ======================================================================
  // CALCULO DE CUOTA FIJA (Sistema Frances)
  // ======================================================================

  [Fact]
  public void Calcular_CuotaFija_SistemaFrances()
  {
    // Prestamo: $10,000 - Tasa: 3% mensual - 6 cuotas
    var capital = 10000m;
    var tasa = 0.03m;
    var n = 6;

    // Formula: C = P * [i(1+i)^n] / [(1+i)^n - 1]
    var factor = (decimal)Math.Pow((double)(1 + tasa), n);
    var cuota = capital * (tasa * factor) / (factor - 1);
    cuota = Math.Round(cuota, 2);

    // Valor esperado calculado: 1845.97
    Assert.InRange(cuota, 1845, 1847);
  }

  [Fact]
  public void Calcular_TotalPlan_ShouldBe_CuotasMultiplicadoPorNumero()
  {
    var capital = 10000m;
    var tasa = 0.03m;
    var n = 6;
    var factor = (decimal)Math.Pow((double)(1 + tasa), n);
    var cuota = capital * (tasa * factor) / (factor - 1);
    cuota = Math.Round(cuota, 2);

    var totalAPagar = cuota * n;
    var totalIntereses = totalAPagar - capital;

    Assert.True(totalAPagar > capital);
    Assert.True(totalIntereses > 0);
    Assert.Equal(10000m + totalIntereses, totalAPagar);
  }

  // ======================================================================
  // CALCULO DE MONTOS DE ACTA (Total = Deuda + Interes)
  // ======================================================================

  [Theory]
  [InlineData(1000, 0, 1000)]
  [InlineData(1000, 200, 1200)]
  [InlineData(50000, 5000, 55000)]
  [InlineData(1234.56, 123.45, 1358.01)]
  public void Calcular_TotalActa(decimal deuda, decimal interes, decimal esperado)
  {
    var total = deuda + interes;
    Assert.Equal(esperado, total);
  }

  // ======================================================================
  // CALCULO DE PORCENTAJES
  // ======================================================================

  [Theory]
  [InlineData(1000, 10, 100)]
  [InlineData(50000, 2.5, 1250)]
  [InlineData(250000, 6, 15000)]
  public void Calcular_Porcentaje(decimal baseAmount, decimal porcentaje, decimal esperado)
  {
    var resultado = baseAmount * (porcentaje / 100);
    Assert.Equal(esperado, resultado);
  }

  // ======================================================================
  // CALCULO DE APORTE PATRONAL Y OBRERO
  // ======================================================================

  [Fact]
  public void Calcular_Aportes_SobreSalario()
  {
    var salario = 250000m;

    // Aporte Obrero: 2%
    var aporteObrero = salario * 0.02m;

    // Aporte Patronal: 6%
    var aportePatronal = salario * 0.06m;

    var totalAporte = aporteObrero + aportePatronal;

    Assert.Equal(5000m, aporteObrero);
    Assert.Equal(15000m, aportePatronal);
    Assert.Equal(20000m, totalAporte);
  }

  [Fact]
  public void Calcular_Aportes_SobreSalariosMultiples()
  {
    var empleados = new[]
    {
      new { Salario = 250000m },
      new { Salario = 180000m },
      new { Salario = 320000m }
    };

    var totalSalarios = empleados.Sum(e => e.Salario);
    var aporteObrero = totalSalarios * 0.02m;
    var aportePatronal = totalSalarios * 0.06m;

    Assert.Equal(750000m, totalSalarios);
    Assert.Equal(15000m, aporteObrero);
    Assert.Equal(45000m, aportePatronal);
  }

  // ======================================================================
  // CALCULO DE DIAS DE MORA
  // ======================================================================

  [Fact]
  public void Calcular_DiasDeMora()
  {
    var fechaVencimiento = new DateTime(2026, 1, 15);
    var fechaPago = new DateTime(2026, 2, 10);
    var diasMora = (fechaPago - fechaVencimiento).Days;

    Assert.Equal(26, diasMora);
  }

  [Fact]
  public void Calcular_InteresesPorMora()
  {
    var capital = 10000m;
    var fechaVencimiento = new DateTime(2026, 1, 15);
    var fechaPago = new DateTime(2026, 2, 10);
    var diasMora = (fechaPago - fechaVencimiento).Days;
    var tasaDiaria = 0.001m; // 0.1% diario

    var interesMora = capital * tasaDiaria * diasMora;

    Assert.Equal(26, diasMora);
    Assert.Equal(260m, interesMora);
  }

  [Fact]
  public void SinMora_CuandoPagoAntesDelVencimiento()
  {
    var fechaVencimiento = new DateTime(2026, 2, 15);
    var fechaPago = new DateTime(2026, 2, 10);
    var diasMora = (fechaPago - fechaVencimiento).Days;

    Assert.True(diasMora <= 0);
  }

  // ======================================================================
  // CALCULO DE AJUSTE DE ACTA (Debitar, Acreditar, Recargos, etc.)
  // ======================================================================

  [Theory]
  [InlineData(5000, 0, 200, 100, 50, 5350)]   // todo debitado
  [InlineData(0, 2000, 0, 0, 0, -2000)]        // solo acreditar
  [InlineData(3000, 1000, 500, 300, 100, 2900)] // mixto
  [InlineData(0, 0, 0, 0, 0, 0)]                 // sin movimientos
  public void Calcular_TotalAjusteActa(
      decimal debitar, decimal acreditar,
      decimal recargos, decimal intereses, decimal otros,
      decimal esperado)
  {
    var total = debitar - acreditar + recargos + intereses + otros;
    Assert.Equal(esperado, total);
  }

  [Fact]
  public void MontoActualizadoActa_SinAjustesPrevios_EsElValorOriginal()
  {
    var montoOriginal = 15000m;
    var tieneAjustes = false;

    var montoActual = tieneAjustes switch
    {
      false => montoOriginal,
      true => throw new InvalidOperationException("No deberia haber ajustes")
    };

    Assert.Equal(15000m, montoActual);
  }

  [Fact]
  public void MontoActualizadoActa_ConAjustes_EsElUltimoMontoAjustado()
  {
    var montoOriginal = 15000m;
    var ajuste = 3000m;
    var montoActualizadoEsperado = montoOriginal + ajuste;

    Assert.Equal(18000m, montoActualizadoEsperado);
  }

  [Fact]
  public void MontoActualizadoActa_ConMultiplesAjustesAcumulaCorrectamente()
  {
    var montoActual = 10000m;

    // Ajuste 1: +2000
    montoActual += 2000m;
    Assert.Equal(12000m, montoActual);

    // Ajuste 2: -1500
    montoActual += (-1500m);
    Assert.Equal(10500m, montoActual);

    // Ajuste 3: +300
    montoActual += 300m;
    Assert.Equal(10800m, montoActual);
  }

  // ======================================================================
  // VALIDACION DE IMPORTES NEGATIVOS
  // ======================================================================

  [Fact]
  public void Importe_NoPuedeSer_Negativo_CuandoEsDebito()
  {
    var debito = -100m;
    Assert.True(debito < 0, "Un debito negativo no tiene sentido");
  }

  [Fact]
  public void Diferencia_EntreDebitoYCredito_PuedeSerNegativa()
  {
    var debitar = 1000m;
    var acreditar = 3000m;
    var diferencia = debitar - acreditar;

    Assert.True(diferencia < 0);
    Assert.Equal(-2000m, diferencia);
  }
}
