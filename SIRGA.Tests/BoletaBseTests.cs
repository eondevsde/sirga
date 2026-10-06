using SIRGA.Application.Services;

namespace SIRGA.Tests;

/// <summary>
/// La prueba que manda: el código de barras de una boleta REAL del sistema que se reemplaza tiene que
/// salir exactamente igual. Valida de una sola vez el formato de 58 posiciones, las fechas julianas,
/// los importes, el relleno de "datos generales" y el dígito verificador.
///
/// Boleta real aportada por el usuario: S.E.C. Santiago del Estero, período 07/2025,
/// CUIT 27-12102232-5, 1er vencimiento 12/08/2025 por $72.703,76 y 2do 22/08/2025 por $73.348,72.
/// </summary>
public class BoletaBseTests
{
  private const string CodigoDeLaBoletaReal =
      "0134407250000027121022325125224000727037625234000733487202";

  [Fact]
  public void Reproduce_Exactamente_El_Codigo_De_La_Boleta_Real()
  {
    var codigo = BoletaBse.GenerarSantiagoDelEstero(
        periodo: "2025-07",
        cuit: "27-12102232-5",
        primerVencimiento: new DateTime(2025, 8, 12),
        importePrimerVencimiento: 72703.76m,
        importeSegundoVencimiento: 73348.72m);

    Assert.Equal(CodigoDeLaBoletaReal, codigo);
  }

  [Fact]
  public void Fechas_Julianas_Coinciden_Con_La_Boleta_Y_Con_El_Instructivo()
  {
    Assert.Equal("25224", BoletaBse.FechaJuliana(new DateTime(2025, 8, 12)));
    Assert.Equal("25234", BoletaBse.FechaJuliana(new DateTime(2025, 8, 22)));
    // El ejemplo del propio instructivo del banco: 05/02/2011 = 11036
    Assert.Equal("11036", BoletaBse.FechaJuliana(new DateTime(2011, 2, 5)));
  }

  [Fact]
  public void Periodo_E_Importes_Se_Formatean_Como_El_Banco()
  {
    Assert.Equal("0725", BoletaBse.PeriodoParaCodigo("2025-07"));
    Assert.Equal("0926", BoletaBse.PeriodoParaCodigo("2026-09"));

    Assert.Equal("0007270376", BoletaBse.Importe(72703.76m));
    Assert.Equal("0000000001", BoletaBse.Importe(0.01m));
    Assert.Equal("0000000000", BoletaBse.Importe(0m));
    Assert.Equal("1000000000", BoletaBse.Importe(10000000m));   // tope del formato
  }

  [Fact]
  public void Datos_Generales_Llevan_Periodo_Y_Cuit_En_20_Posiciones()
  {
    Assert.Equal("07250000027121022325", BoletaBse.DatosGenerales("2025-07", "27-12102232-5"));
    Assert.Equal(20, BoletaBse.DatosGenerales("2025-07", "27-12102232-5").Length);
    Assert.Equal(20, BoletaBse.DatosGenerales("2026-09", "30-71157059-0").Length);
  }

  [Fact]
  public void El_Codigo_Completo_Tiene_58_Posiciones_Numericas_Y_El_Ente_Adelante()
  {
    var codigo = BoletaBse.GenerarSantiagoDelEstero(
        "2026-09", "30-71157059-0", new DateTime(2026, 10, 15), 1234567.89m, 1260000m);

    Assert.Equal(BoletaBse.LargoCodigo, codigo.Length);
    Assert.All(codigo, c => Assert.True(char.IsDigit(c)));
    Assert.Equal("0134", codigo[..4]);
    Assert.Equal('4', codigo[4]);
    Assert.Equal('1', codigo[25]);   // código de moneda: pesos
  }
}
