using System.Globalization;

namespace SIRGA.Application.Services;

/// <summary>
/// Interpreta un importe escrito por el usuario aceptando **coma o punto** como separador decimal.
///
/// POR QUÉ EXISTE: en Argentina el separador decimal es la coma, pero mucha gente escribe con el
/// punto (teclado numérico). Los campos numéricos de MudBlazor, con la cultura del sistema, rechazan
/// el punto. Con esto, "1758785,23" y "1758785.23" valen lo mismo, y "1.500.000" se entiende como
/// un millón quinientos mil (punto de miles).
/// </summary>
public static class ImporteParser
{
  /// <summary>
  /// Convierte el texto en un número. Devuelve <c>null</c> si no hay ningún número interpretable.
  /// </summary>
  public static decimal? Parse(string? texto)
  {
    if (string.IsNullOrWhiteSpace(texto)) return 0m;

    var limpio = new string(texto.Where(c => char.IsDigit(c) || c is ',' or '.' or '-').ToArray());
    if (limpio.Length == 0 || limpio == "-") return 0m;

    var negativo = limpio.StartsWith('-');
    limpio = limpio.TrimStart('-');

    var ultimaComa = limpio.LastIndexOf(',');
    var ultimoPunto = limpio.LastIndexOf('.');
    string numero;

    if (ultimaComa >= 0 && ultimoPunto >= 0)
    {
      // Hay de los dos: el que está más a la derecha es el decimal y el otro es de miles.
      numero = ultimaComa > ultimoPunto
          ? limpio.Replace(".", "").Replace(',', '.')
          : limpio.Replace(",", "");
    }
    else if (ultimaComa >= 0)
    {
      numero = limpio.Replace(',', '.');
    }
    else if (ultimoPunto >= 0)
    {
      // Sólo puntos: uno solo con 1 o 2 dígitos detrás es decimal ("1758785.23");
      // si tiene 3, o hay varios, son separadores de miles ("1.500.000", "1.500").
      var partes = limpio.Split('.');
      numero = partes.Length == 2 && partes[1].Length is 1 or 2
          ? limpio
          : limpio.Replace(".", "");
    }
    else
    {
      numero = limpio;
    }

    if (!decimal.TryParse(numero, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
      return null;

    return negativo ? -valor : valor;
  }
}
