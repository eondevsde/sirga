namespace SIRGA.Application.Services;

/// <summary>
/// Lee los archivos de rendición del banco que caen en una **carpeta de entrada**: es el punto donde
/// SIRGA procesa lo que hoy llega por correo (la descarga automática del correo se conecta acá).
///
/// REGLAS (por qué está hecho así):
/// - **Nunca borra un archivo.** Lo mueve: lo que se procesó bien va a `Procesados/`, lo que no se pudo
///   leer va a `Errores/`. Un archivo de rendición es un comprobante: no se tira.
/// - **La carpeta de entrada es la cola.** Un archivo que ya se movió no se vuelve a procesar, así que
///   reimportar el mismo día no duplica nada, sin necesidad de una tabla de control.
/// - **Nada se saltea en silencio**: si un archivo no se puede leer, queda en `Errores/` y se informa.
///   Si se descartara sin avisar, un día de recaudación podría desaparecer sin que nadie lo note.
/// - Si el archivo ya tiene problemas de lectura pero trae líneas utilizables, se procesa igual y se
///   informa el problema: perder un pago entero por una línea rara sería peor.
/// </summary>
public class BankRenditionInbox
{
  public const string CarpetaProcesados = "Procesados";
  public const string CarpetaErrores = "Errores";

  public sealed record ResultadoArchivo(string Archivo, bool Ok, string Detalle, string? Destino);

  public sealed record ResultadoBarrido(IReadOnlyList<ResultadoArchivo> Archivos)
  {
    public int Procesados => Archivos.Count(a => a.Ok);
    public int ConError => Archivos.Count(a => !a.Ok);
    public bool HayErrores => ConError > 0;
  }

  /// <summary>
  /// Recorre la carpeta de entrada y procesa cada `.txt`. `procesar` recibe la rendición ya leída y
  /// devuelve el detalle que se informa (ahí se engancha la conciliación, con o sin base de datos).
  ///
  /// `moverArchivos: false` sirve para una prueba o un simulacro: procesa e informa **sin mover nada**.
  /// </summary>
  public ResultadoBarrido Barrer(
      string carpetaEntrada,
      Func<BankRenditionParser.Rendicion, string> procesar,
      bool moverArchivos = true)
  {
    if (string.IsNullOrWhiteSpace(carpetaEntrada))
      throw new ArgumentException("Falta la carpeta de entrada de las rendiciones.", nameof(carpetaEntrada));

    Directory.CreateDirectory(carpetaEntrada);

    var parser = new BankRenditionParser();
    var resultados = new List<ResultadoArchivo>();

    foreach (var archivo in Directory.GetFiles(carpetaEntrada, "*.txt").OrderBy(a => a))
    {
      var nombre = Path.GetFileName(archivo);
      try
      {
        var rendicion = parser.Parsear(File.ReadAllLines(archivo));

        // Sin lineas utilizables no hay nada que conciliar: el archivo va a Errores.
        if (rendicion.Lineas.Count == 0)
        {
          resultados.Add(Mover(archivo, nombre, false,
              "No se pudo leer ninguna linea utilizable" +
              (rendicion.Problemas.Count > 0 ? $": {string.Join(" | ", rendicion.Problemas)}" : "."),
              carpetaEntrada, moverArchivos));
          continue;
        }

        var detalle = procesar(rendicion);
        if (rendicion.Problemas.Count > 0)
          detalle += $" ATENCION: {rendicion.Problemas.Count} problema(s) de lectura: " +
                     string.Join(" | ", rendicion.Problemas);

        resultados.Add(Mover(archivo, nombre, true, detalle, carpetaEntrada, moverArchivos));
      }
      catch (Exception ex)
      {
        // Un archivo que rompe no puede frenar el resto ni desaparecer.
        resultados.Add(Mover(archivo, nombre, false, $"Error al procesar: {ex.Message}", carpetaEntrada, moverArchivos));
      }
    }

    return new ResultadoBarrido(resultados);
  }

  private static ResultadoArchivo Mover(
      string archivo, string nombre, bool ok, string detalle, string carpetaEntrada, bool mover)
  {
    if (!mover) return new ResultadoArchivo(nombre, ok, detalle, null);

    var carpetaDestino = Path.Combine(carpetaEntrada, ok ? CarpetaProcesados : CarpetaErrores);
    Directory.CreateDirectory(carpetaDestino);

    var destino = Path.Combine(carpetaDestino, nombre);
    // Si ya hay uno con el mismo nombre (dos archivos iguales el mismo dia), no se pisa: se agrega la hora.
    if (File.Exists(destino))
      destino = Path.Combine(carpetaDestino, $"{Path.GetFileNameWithoutExtension(nombre)}_" +
                                             $"{DateTime.Now:HHmmss}{Path.GetExtension(nombre)}");

    File.Move(archivo, destino);
    return new ResultadoArchivo(nombre, ok, detalle, destino);
  }
}
