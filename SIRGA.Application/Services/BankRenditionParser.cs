namespace SIRGA.Application.Services;

/// <summary>
/// Lee el **archivo de rendición** que el banco manda por correo (el que después concilia SIRGA).
///
/// FORMATO (decodificado sobre un archivo real, `tools/Rendicion del Banco/BSELB20261006.txt`):
///   - líneas de DETALLE (empiezan con `1`), de 218 caracteres: encabezado + **el código de barras al
///     final** (54 dígitos);
///   - una línea de TOTALES (empieza con `2`), que trae la cantidad de registros y el importe total.
///
/// El código de barras **identifica el pago por sí solo**: contiene el período (MMAA) y el CUIT.
///
/// DECISIONES DE DISEÑO (y por qué):
/// - **No se rechaza una línea por el dígito verificador.** El del formato de 54 posiciones responde a un
///   algoritmo distinto del de 58 (ver `PLAN_CONCILIACION_PAGOS.md`), así que rechazar por eso tiraría
///   pagos reales. Se calcula y se informa, pero no se descarta.
/// - **Nada se saltea en silencio**: cada línea que no se entiende se devuelve en `Problemas` con el número
///   de línea. Es la falla que ya nos costó caro una vez.
/// - El formato (54 o 58) **se detecta**, porque cada sindicato está en una etapa distinta: La Banda usa 54
///   y Santiago 58.
/// </summary>
public class BankRenditionParser
{
  /// <summary>Códigos de ente conocidos. Si el que viene no está acá, se avisa.</summary>
  private static readonly Dictionary<string, string> Entes = new()
  {
    ["0134"] = "S.E.C. Santiago del Estero",
    ["0135"] = "S.E.C. La Banda",
    ["0136"] = "S.E.C. de Frías",
    ["0137"] = "S.E.C. Añatuya"
  };

  /// <summary>Una línea de detalle ya interpretada.</summary>
  public sealed record LineaRendicion(
      int NumeroRegistro,
      string FechaProceso,
      string CodigoBarras,
      int Formato,
      string Ente,
      string EnteDescripcion,
      string Periodo,
      string Cuit,
      decimal ImportePrimero,
      DateTime VencimientoPrimero,
      decimal ImporteSegundo,
      DateTime VencimientoSegundo,
      int DigitoVerificador,
      int DigitoCalculado);

  /// <summary>Todo lo que trae el archivo.</summary>
  public sealed record Rendicion(
      IReadOnlyList<LineaRendicion> Lineas,
      int? CantidadDeclarada,
      decimal? TotalDeclarado,
      decimal TotalLeido,
      IReadOnlyList<string> Problemas)
  {
    /// <summary>El total del archivo coincide con la suma de las líneas (control de lectura).</summary>
    public bool TotalCoincide => TotalDeclarado is null || Math.Abs(TotalDeclarado.Value - TotalLeido) < 0.01m;

    public bool CantidadCoincide => CantidadDeclarada is null || CantidadDeclarada.Value == Lineas.Count;
  }

  public Rendicion Parsear(IEnumerable<string> lineas)
  {
    var detalle = new List<LineaRendicion>();
    var problemas = new List<string>();
    int? cantidad = null;
    decimal? totalDeclarado = null;
    var numeroDeLinea = 0;

    foreach (var cruda in lineas)
    {
      numeroDeLinea++;
      var linea = cruda.TrimEnd('\r', '\n', ' ');
      if (linea.Length == 0) continue;

      if (linea.StartsWith('1'))
      {
        var (ok, resultado, problema) = ParsearDetalle(linea);
        if (ok && resultado is not null) detalle.Add(resultado);
        else problemas.Add($"Linea {numeroDeLinea}: {problema}");
      }
      else if (linea.StartsWith('2'))
      {
        var (ok, cant, total, problema) = ParsearTotales(linea);
        if (ok) { cantidad = cant; totalDeclarado = total; }
        else problemas.Add($"Linea {numeroDeLinea}: {problema}");
      }
      else
      {
        problemas.Add($"Linea {numeroDeLinea}: empieza con '{linea[0]}' y no se sabe que es " +
                      "(se esperaba 1 = detalle o 2 = totales).");
      }
    }

    var totalLeido = detalle.Sum(l => l.ImportePrimero);
    var rendicion = new Rendicion(detalle, cantidad, totalDeclarado, totalLeido, problemas);

    // Controles de integridad: si el archivo dice una cosa y las lineas otra, se avisa. Puede ser que el
    // banco mande un archivo incompleto, y eso NO se puede detectar mirando una linea sola.
    if (cantidad is not null && cantidad != detalle.Count)
      problemas.Add($"El archivo declara {cantidad} registros y se leyeron {detalle.Count}.");

    if (totalDeclarado is not null && Math.Abs(totalDeclarado.Value - totalLeido) >= 0.01m)
      problemas.Add($"El archivo declara un total de {totalDeclarado:N2} y las lineas suman {totalLeido:N2}.");

    return rendicion;
  }

  private static (bool Ok, LineaRendicion? Linea, string? Problema) ParsearDetalle(string linea)
  {
    if (linea.Length < 60)
      return (false, null, $"tiene {linea.Length} caracteres, es demasiado corta.");

    // El codigo esta al final. Se prueba 58 (formato propuesto) y despues 54 (formato actual).
    foreach (var largoCodigo in new[] { 58, 54 })
    {
      if (linea.Length < largoCodigo) continue;
      var codigo = linea[^largoCodigo..];
      if (!codigo.All(char.IsDigit)) continue;

      var ente = codigo[..4];
      if (!Entes.ContainsKey(ente)) continue;

      // Datos generales: periodo MMAA + relleno + CUIT. La posicion del CUIT cambia con el largo del
      // importe: en los dos formatos arranca en la posicion 15 (base 1) del codigo.
      var datos = codigo.Substring(5, 20);
      var periodo = datos[..4];
      var cuit = datos[9..];

      var mes = int.Parse(periodo[..2]);
      if (mes is < 1 or > 12)
        return (false, null, $"el periodo '{periodo}' del codigo no es valido (mes {mes}).");
      if (cuit.Length != 11)
        return (false, null, $"el CUIT '{cuit}' del codigo no tiene 11 digitos.");

      var esFormato58 = largoCodigo == 58;
      var anchoImporte = esFormato58 ? 10 : 8;
      var desdeVenc1 = 26;
      var desdeImp1 = desdeVenc1 + 5;
      var desdeVenc2 = desdeImp1 + anchoImporte;
      var desdeImp2 = desdeVenc2 + 5;

      var numeroRegistro = linea.Length >= 5 && int.TryParse(linea[1..5], out var n) ? n : 0;
      var fechaProceso = linea.Length >= 13 ? linea[5..13] : "";

      var lineaRendicion = new LineaRendicion(
          numeroRegistro,
          fechaProceso,
          codigo,
          esFormato58 ? 58 : 54,
          ente,
          Entes[ente],
          // OJO: el codigo trae MMAA (mes primero). Si se invierte, el periodo queda como "26-09" y NO
          // coincide NUNCA con Declarations.Period de SIRGA ("2026-09"): la conciliacion no encontraria
          // ninguna DDJJ y diria "sin DDJJ" para todo. Lo detecto una prueba.
          $"20{periodo[2..4]}-{periodo[..2]}",
          cuit,
          LeerImporte(codigo, desdeImp1, anchoImporte),
          LeerFechaJuliana(codigo, desdeVenc1),
          LeerImporte(codigo, desdeImp2, anchoImporte),
          LeerFechaJuliana(codigo, desdeVenc2),
          codigo[^1] - '0',
          BoletaBse.DigitoVerificador(codigo[..^1]));

      return (true, lineaRendicion, null);
    }

    return (false, null,
        $"no se pudo leer el codigo de barras de los ultimos caracteres ('{linea[^Math.Min(58, linea.Length)..]}').");
  }

  private static (bool Ok, int? Cantidad, decimal? Total, string? Problema) ParsearTotales(string linea)
  {
    if (linea.Length < 13)
      return (false, null, null, $"la linea de totales tiene {linea.Length} caracteres, es demasiado corta.");

    var cantidad = int.TryParse(linea[1..5], out var c) ? c : (int?)null;
    var total = decimal.TryParse(linea[^8..], out var t) ? t / 100m : (decimal?)null;

    if (cantidad is null || total is null)
      return (false, null, null, "no se pudieron leer la cantidad o el total.");

    return (true, cantidad, total, null);
  }

  /// <summary>Importe en centavos: 8 dígitos = 6 enteros + 2 decimales; 10 dígitos = 8 + 2.</summary>
  private static decimal LeerImporte(string codigo, int desde, int ancho) =>
      int.Parse(codigo.Substring(desde, ancho)) / 100m;

  /// <summary>Fecha juliana `aaddd`: año de 2 dígitos + día del año.</summary>
  private static DateTime LeerFechaJuliana(string codigo, int desde)
  {
    var valor = codigo.Substring(desde, 5);
    var anio = 2000 + int.Parse(valor[..2]);
    var dia = int.Parse(valor[2..]);
    return new DateTime(anio, 1, 1).AddDays(dia - 1);
  }
}
