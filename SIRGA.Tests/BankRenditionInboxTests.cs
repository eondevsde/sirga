using SIRGA.Application.Services;
using Xunit;

namespace SIRGA.Tests;

/// <summary>
/// Pruebas de la bandeja de entrada de las rendiciones del banco.
///
/// Se trabaja en una carpeta dentro del directorio de salida de las pruebas (`AppContext.BaseDirectory`):
/// el entorno no deja escribir en `%TEMP%`, y así la prueba no depende de rutas del sistema ni deja basura.
/// Los datos son inventados.
/// </summary>
public class BankRenditionInboxTests : IDisposable
{
  private readonly string _base;
  private readonly string _entrada;

  public BankRenditionInboxTests()
  {
    _base = Path.Combine(AppContext.BaseDirectory, "bandeja-" + Guid.NewGuid().ToString("N")[..8]);
    _entrada = Path.Combine(_base, "entrada");
    Directory.CreateDirectory(_entrada);
  }

  public void Dispose()
  {
    if (Directory.Exists(_base)) Directory.Delete(_base, recursive: true);
    GC.SuppressFinalize(this);
  }

  /// <summary>Una linea de detalle valida (218 caracteres: encabezado + codigo de barras de 54).</summary>
  private static string LineaValida()
  {
    var codigo = "0134" + "4" + "0926" + "00000" + "20123456789" + "1" + "26282" + "02061036"
               + "26292" + "02079320" + "0" + "7";
    return "1" + "0001" + "06102026" + new string('0', 151) + codigo;
  }

  private string Escribir(string nombre, string contenido)
  {
    var ruta = Path.Combine(_entrada, nombre);
    File.WriteAllText(ruta, contenido);
    return ruta;
  }

  [Fact]
  public void Un_archivo_valido_se_procesa_y_se_archiva_en_Procesados()
  {
    Escribir("rendicion.txt", LineaValida());

    var resultado = new BankRenditionInbox().Barrer(_entrada, r => $"{r.Lineas.Count} lineas");

    var archivo = Assert.Single(resultado.Archivos);
    Assert.True(archivo.Ok);
    Assert.Equal(1, resultado.Procesados);
    Assert.False(resultado.HayErrores);
    // La carpeta de entrada queda vacia y el archivo NO se borro: se movio.
    Assert.Empty(Directory.GetFiles(_entrada));
    Assert.True(File.Exists(Path.Combine(_entrada, BankRenditionInbox.CarpetaProcesados, "rendicion.txt")));
  }

  [Fact]
  public void Un_archivo_ilegible_va_a_Errores_con_el_motivo_y_no_se_borra()
  {
    Escribir("roto.txt", "esto no es una rendicion");

    var resultado = new BankRenditionInbox().Barrer(_entrada, _ => "no deberia llamarse");

    var archivo = Assert.Single(resultado.Archivos);
    Assert.False(archivo.Ok);
    Assert.Equal(1, resultado.ConError);
    Assert.True(resultado.HayErrores);
    Assert.Contains("no se sabe que es", archivo.Detalle);
    Assert.True(File.Exists(Path.Combine(_entrada, BankRenditionInbox.CarpetaErrores, "roto.txt")));
  }

  [Fact]
  public void Un_archivo_ya_procesado_no_se_vuelve_a_procesar()
  {
    Escribir("rendicion.txt", LineaValida());
    var bandeja = new BankRenditionInbox();

    bandeja.Barrer(_entrada, _ => "primera");
    var segunda = bandeja.Barrer(_entrada, _ => "segunda");

    // Reimportar el mismo archivo no puede duplicar nada.
    Assert.Empty(segunda.Archivos);
    Assert.Equal(0, segunda.Procesados);
  }

  [Fact]
  public void Con_moverArchivos_en_false_procesa_pero_deja_el_archivo_en_la_entrada()
  {
    Escribir("rendicion.txt", LineaValida());
    var llamado = false;

    var resultado = new BankRenditionInbox().Barrer(_entrada,
        r => { llamado = true; return $"{r.Lineas.Count} lineas"; }, moverArchivos: false);

    Assert.True(llamado);
    Assert.Single(resultado.Archivos);
    Assert.Null(resultado.Archivos[0].Destino);
    Assert.True(File.Exists(Path.Combine(_entrada, "rendicion.txt")));
    Assert.False(Directory.Exists(Path.Combine(_entrada, BankRenditionInbox.CarpetaProcesados)));
  }

  [Fact]
  public void Si_ya_hay_un_archivo_con_el_mismo_nombre_no_se_pisa()
  {
    var bandeja = new BankRenditionInbox();
    Escribir("rendicion.txt", LineaValida());
    bandeja.Barrer(_entrada, _ => "primera");
    Escribir("rendicion.txt", LineaValida());
    bandeja.Barrer(_entrada, _ => "segunda");

    var archivados = Directory.GetFiles(Path.Combine(_entrada, BankRenditionInbox.CarpetaProcesados));
    // Los dos tienen que estar: el segundo con la hora agregada, no encima del primero.
    Assert.Equal(2, archivados.Length);
  }

  [Fact]
  public void Sin_carpeta_de_entrada_avisa_en_vez_de_no_hacer_nada()
  {
    Assert.Throws<ArgumentException>(() => new BankRenditionInbox().Barrer("", _ => "x"));
  }
}
