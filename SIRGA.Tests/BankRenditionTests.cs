using SIRGA.Application.Services;
using Xunit;

namespace SIRGA.Tests;

/// <summary>
/// Pruebas del lector del archivo de rendición del banco y del planificador de la conciliación.
///
/// OJO: los datos de estas pruebas son **INVENTADOS a propósito**. Los archivos reales del banco tienen
/// CUITs e importes de empresas reales y **no van al repositorio**.
/// </summary>
public class BankRenditionTests
{
  private const string CuitFicticio = "20123456789";

  /// <summary>Arma una linea de detalle valida: 164 de encabezado + el codigo de barras.</summary>
  private static string LineaDetalle(string periodo = "0926", string imp1 = "02061036", string imp2 = "02079320")
  {
    var codigo = "0134" + "4" + periodo + "00000" + CuitFicticio + "1" + "26282" + imp1 + "26292" + imp2 + "0" + "7";
    var encabezado = "1" + "0001" + "06102026" + new string('0', 151);
    return encabezado + codigo;
  }

  private static string LineaTotales(string importe8 = "02061036")
  {
    return "2" + "0001" + "07102026" + new string('0', 67) + importe8;
  }

  [Fact]
  public void El_codigo_de_barras_esta_al_final_y_se_lee_completo()
  {
    var rendicion = new BankRenditionParser().Parsear([LineaDetalle()]);

    var linea = Assert.Single(rendicion.Lineas);
    Assert.Empty(rendicion.Problemas);
    Assert.Equal(54, linea.Formato);
    Assert.Equal("0134", linea.Ente);
    Assert.Equal("2026-09", linea.Periodo);
    Assert.Equal(CuitFicticio, linea.Cuit);
    Assert.Equal(20610.36m, linea.ImportePrimero);
    Assert.Equal(20793.20m, linea.ImporteSegundo);
    // El juliano 26282 es el dia 282 de 2026.
    Assert.Equal(new DateTime(2026, 10, 9), linea.VencimientoPrimero);
  }

  [Fact]
  public void Una_linea_que_no_se_entiende_no_se_saltea_en_silencio()
  {
    var rendicion = new BankRenditionParser().Parsear([LineaDetalle(), "1esto no es una linea valida"]);

    Assert.Single(rendicion.Lineas);
    Assert.Single(rendicion.Problemas);
  }

  [Fact]
  public void Una_linea_de_largo_imposible_se_informa()
  {
    var rendicion = new BankRenditionParser().Parsear(["1" + new string('0', 40), LineaDetalle()]);

    Assert.Single(rendicion.Lineas);
    Assert.Contains(rendicion.Problemas, p => p.Contains("demasiado corta"));
  }

  [Fact]
  public void Detecta_el_formato_de_54_aunque_pruebe_el_de_58_primero()
  {
    // Es el caso real de La Banda: si tomara 58 caracteres, el ente seria parte del encabezado.
    var rendicion = new BankRenditionParser().Parsear([LineaDetalle()]);

    Assert.Equal(54, Assert.Single(rendicion.Lineas).Formato);
  }

  [Fact]
  public void Controla_que_el_total_del_archivo_coincida_con_las_lineas()
  {
    var rendicion = new BankRenditionParser().Parsear([LineaDetalle(), LineaTotales()]);

    Assert.Equal(1, rendicion.CantidadDeclarada);
    Assert.Equal(20610.36m, rendicion.TotalDeclarado);
    Assert.Equal(20610.36m, rendicion.TotalLeido);
    Assert.True(rendicion.TotalCoincide);
    Assert.True(rendicion.CantidadCoincide);
    Assert.Empty(rendicion.Problemas);
  }

  [Fact]
  public void Un_total_que_no_cierra_se_informa_pero_no_es_un_error()
  {
    // Se probaron DOS archivos reales y el importe del renglon de totales NO es el mismo campo en los dos
    // formatos: compararlo daba una alarma falsa en cada archivo de Santiago. El dato queda disponible
    // (TotalCoincide en false) pero NO se agrega como problema: lo que se controla es la CANTIDAD.
    var rendicion = new BankRenditionParser().Parsear([LineaDetalle(), LineaTotales("99999999")]);

    Assert.False(rendicion.TotalCoincide);
    Assert.Empty(rendicion.Problemas);
  }
}
