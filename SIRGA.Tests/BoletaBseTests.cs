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

  [Fact]
  public void El_Vencimiento_Es_El_Dia_Configurado_Del_Mes_Siguiente()
  {
    // Acordado con el sindicato: el período 09/2026 vence el 15/10/2026. Es la ÚNICA fecha de
    // vencimiento: la boleta ya no tiene 2° vencimiento, y desde el día siguiente a esta fecha corre el
    // interés resarcitorio.
    Assert.Equal(new DateTime(2026, 10, 15), BoletaBse.PrimerVencimiento("2026-09", 15));

    // Y el período de diciembre vence en enero del año siguiente.
    Assert.Equal(new DateTime(2027, 1, 15), BoletaBse.PrimerVencimiento("2026-12", 15));
  }

  [Fact]
  public void La_Boleta_Es_Triplicada_Cliente_Banco_Y_Sindicato()
  {
    Assert.Equal(3, BoletaBse.Talones.Length);
    Assert.Contains("Talón para el CLIENTE", BoletaBse.Talones);
    Assert.Contains("Talón para el BANCO", BoletaBse.Talones);
    Assert.Contains("Talón para el SINDICATO", BoletaBse.Talones);
  }

  [Fact]
  public void Genera_Con_La_Configuracion_Del_Sindicato()
  {
    var configuracion = new SIRGA.Domain.Entities.UnionBankConfig
    {
      UnionId = 19,
      BankId = 1,
      ClientNumber = "18588639",
      EntityCode = "0134",
      DesignFormat = 4,
      FirstDueDay = 15,
      DaysToSecondDue = 10,
      IsActive = true
    };

    var codigo = BoletaBse.GenerarConConfiguracion(
        configuracion, "2026-09", "30-71157059-0", new DateTime(2026, 10, 15), 1234567.89m);

    Assert.Equal(BoletaBse.LargoCodigo, codigo.Length);
    Assert.Equal("0134", codigo[..4]);

    // 15/10/2026 = día 288 de 2026 -> 26288. La boleta tiene UNA sola fecha de vencimiento, así que los
    // DOS juegos de fecha+importe que pide el formato del banco van con el mismo valor.
    Assert.Equal("26288", codigo[26..31]);   // 1ª fecha
    Assert.Equal("26288", codigo[41..46]);   // 2ª fecha: la misma
    Assert.Equal("0123456789", codigo[31..41]);   // 1.234.567,89
    Assert.Equal("0123456789", codigo[46..56]);   // el mismo importe
  }

  [Fact]
  public void Un_Periodo_Invalido_No_Pasa_Silenciosamente()
  {
    Assert.Throws<ArgumentException>(() => BoletaBse.PrimerVencimiento("2026", 15));
    Assert.Throws<ArgumentException>(() => BoletaBse.PrimerVencimiento("", 15));
  }
}
