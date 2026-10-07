using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

/// <summary>
/// Genera el código de barras de la boleta de pago del Banco Santiago del Estero (BSE).
///
/// Formato verificado contra una boleta REAL del sistema que se reemplaza (58 posiciones):
///   0134 4 | 07250000027121022325 | 1 | 25224 | 0007270376 | 25234 | 0007334872 | 0 | 2
///   ente 4 | diseño 1 | datos generales 20 | moneda 1 | 1er venc 5 | importe1 10 | 2do venc 5 |
///   importe2 10 | sin uso 1 | dígito verificador 1
///
/// El código de ente NO es fijo: cada sindicato tiene el suyo (Santiago del Estero 0134,
/// La Banda 0135, Frías 0136, Añatuya 0137), así que se recibe por parámetro.
/// </summary>
public static class BoletaBse
{
  /// <summary>Código del ente del S.E.C. Santiago del Estero, según la estructura del banco.</summary>
  public const string EnteSantiagoDelEstero = "0134";

  /// <summary>Valor constante del campo "formato diseño" en la estructura vigente.</summary>
  public const int FormatoDiseno = 4;

  /// <summary>Código de moneda para pesos.</summary>
  public const int MonedaPesos = 1;

  /// <summary>Días entre el primer y el segundo vencimiento (confirmado por el usuario y por la boleta real).</summary>
  public const int DiasEntreVencimientos = 10;

  /// <summary>Largo total del código: 4+1+20+1+5+10+5+10+1+1.</summary>
  public const int LargoCodigo = 58;

  /// <summary>
  /// Fecha en formato juliano del banco: <c>aaddd</c> = dos dígitos del año + día del año en 3 dígitos.
  /// Ejemplo del propio instructivo: 05/02/2011 = 11036. Verificado contra la boleta real:
  /// 12/08/2025 = 25224 y 22/08/2025 = 25234.
  /// </summary>
  public static string FechaJuliana(DateTime fecha) => $"{fecha.Year % 100:D2}{fecha.DayOfYear:D3}";

  /// <summary>
  /// Convierte el período que usa SIRGA (por ejemplo "2025-07") al que va en el código: MMAA ("0725").
  /// </summary>
  public static string PeriodoParaCodigo(string periodo)
  {
    // SIRGA guarda el período como yyyy-MM.
    var partes = (periodo ?? string.Empty).Split('-');
    if (partes.Length != 2) return string.Empty;
    return $"{partes[1]}{partes[0][^2..]}";   // MM + últimos 2 del año
  }

  /// <summary>
  /// Bloque "datos generales" (20 posiciones): período MMAA + relleno con ceros + CUIT sin guiones.
  /// La boleta real trae 0725 + 00000 + 27121022325, que es el período y el CUIT del contribuyente.
  /// Es el dato con el que el sindicato identifica la boleta y con el que después se concilia el cobro.
  /// </summary>
  public static string DatosGenerales(string periodo, string cuit)
  {
    var periodoCodigo = PeriodoParaCodigo(periodo);
    var soloDigitos = new string((cuit ?? string.Empty).Where(char.IsDigit).ToArray());

    var relleno = 20 - periodoCodigo.Length - soloDigitos.Length;
    if (relleno < 0) relleno = 0;

    var bloque = periodoCodigo + new string('0', relleno) + soloDigitos;
    return bloque.Length > 20 ? bloque[..20] : bloque.PadRight(20, '0');
  }

  /// <summary>
  /// Importe en 10 posiciones: 8 enteros + 2 decimales, sin separadores.
  /// 72.703,76 -> "0007270376".
  /// </summary>
  public static string Importe(decimal importe)
  {
    var centavos = (long)Math.Round(importe * 100m, MidpointRounding.AwayFromZero);
    if (centavos < 0) centavos = 0;
    return centavos.ToString("D10");
  }

  /// <summary>
  /// Dígito verificador: módulo 10 con pesos 3 y 1 alternados y suma de los dígitos de cada producto.
  ///
  /// POR QUÉ ESTA Y NO LA ANTERIOR: la anterior (pesos 1-2 desde la derecha, complemento a 10 **dividido 2**)
  /// terminaba dividiendo por 2, así que **sólo podía dar dígitos del 0 al 5**. Un verificador que nunca
  /// puede ser 6, 7, 8 ni 9 no es un verificador: lo confirman las boletas reales, que tienen dígitos 8.
  /// Esta reproduce el dígito de la boleta real de 58 posiciones que tenemos **y puede dar los diez**.
  ///
  /// OJO, ESTO NO ESTÁ CONFIRMADO: con UNA sola boleta de 58 posiciones, **14 fórmulas distintas (todas
  /// sanas) reproducen su dígito**, así que no se puede saber cuál usa el banco. Queda declarado en la ficha
  /// técnica que se le entrega al banco, pidiendo que la confirme. Si el banco confirma otra, se cambia
  /// **sólo este método** y se reemiten las boletas.
  /// </summary>
  public static int DigitoVerificador(string cuerpo57)
  {
    var suma = 0;
    var peso = 3;

    // Se recorre de derecha a izquierda: el último dígito del cuerpo lleva peso 3.
    for (var i = cuerpo57.Length - 1; i >= 0; i--)
    {
      if (!char.IsDigit(cuerpo57[i])) continue;
      suma += SumaDeDigitos((cuerpo57[i] - '0') * peso);
      peso = peso == 3 ? 1 : 3;
    }

    return (10 - (suma % 10)) % 10;
  }

  /// <summary>Suma los dígitos de un número (27 -> 9). Es parte del algoritmo del dígito verificador.</summary>
  private static int SumaDeDigitos(int numero) => (numero / 10) + (numero % 10);

  /// <summary>
  /// Arma el código de 58 dígitos. Los importes se reciben ya calculados.
  ///
  /// El formato tiene DOS juegos de fecha+importe (así viene la boleta real del banco). La boleta de
  /// SIRGA usa una sola fecha de vencimiento, así que los dos juegos van con el mismo valor; esta
  /// sobrecarga con dos juegos distintos existe para poder reproducir una boleta real en las pruebas.
  /// </summary>
  public static string Generar(
      string codigoEnte,
      string periodo,
      string cuit,
      DateTime primerVencimiento,
      decimal importePrimerVencimiento,
      DateTime segundoVencimiento,
      decimal importeSegundoVencimiento)
  {
    var cuerpo =
        codigoEnte.PadLeft(4, '0') +
        FormatoDiseno.ToString() +
        DatosGenerales(periodo, cuit) +
        MonedaPesos.ToString() +
        FechaJuliana(primerVencimiento) +
        Importe(importePrimerVencimiento) +
        FechaJuliana(segundoVencimiento) +
        Importe(importeSegundoVencimiento) +
        "0";

    return cuerpo + DigitoVerificador(cuerpo);
  }

  /// <summary>
  /// Atajo para Santiago del Estero: el segundo vencimiento se calcula a 10 días del primero.
  /// </summary>
  public static string GenerarSantiagoDelEstero(
      string periodo,
      string cuit,
      DateTime primerVencimiento,
      decimal importePrimerVencimiento,
      decimal importeSegundoVencimiento)
      => Generar(
          EnteSantiagoDelEstero,
          periodo,
          cuit,
          primerVencimiento,
          importePrimerVencimiento,
          primerVencimiento.AddDays(DiasEntreVencimientos),
          importeSegundoVencimiento);

  /// <summary>
  /// Talones de la boleta. El banco pide TRIPLICADO: uno para el cliente, uno para el banco y uno
  /// para el sindicato. Los tres llevan los mismos datos y el mismo código de barras.
  /// </summary>
  public static readonly string[] Talones =
  [
    "Talón para el CLIENTE",
    "Talón para el BANCO",
    "Talón para el SINDICATO"
  ];

  /// <summary>
  /// Primer vencimiento: el día configurado del mes siguiente al período.
  /// Ejemplo acordado con el sindicato: período 09/2026 con día 15 vence el 15/10/2026.
  /// Se usa AddDays sobre el día 1 del mes en lugar de new DateTime(anio, mes, dia) para que un día
  /// fuera de rango no rompa: la validación del día (1 a 28) está en la configuración.
  /// </summary>
  public static DateTime PrimerVencimiento(string periodo, int diaDelMes)
  {
    var partes = (periodo ?? string.Empty).Split('-');
    if (partes.Length != 2 || !int.TryParse(partes[0], out var anio) || !int.TryParse(partes[1], out var mes))
      throw new ArgumentException($"Período inválido: \"{periodo}\". Se espera el formato aaaa-MM.", nameof(periodo));

    if (mes < 1 || mes > 12)
      throw new ArgumentException($"Mes inválido en el período \"{periodo}\".", nameof(periodo));

    return new DateTime(anio, mes, 1).AddMonths(1).AddDays(Math.Max(diaDelMes, 1) - 1);
  }

  /// <summary>
  /// Genera el código a partir de la configuración bancaria del sindicato, para **UNA sola fecha de
  /// vencimiento**: la boleta de SIRGA ya no tiene 2° vencimiento.
  ///
  /// El FORMATO del banco tiene dos juegos de fecha+importe (así viene la boleta real), así que se repite
  /// el mismo dato en los dos: el código sigue teniendo los 58 dígitos que el banco lee.
  /// </summary>
  public static string GenerarConConfiguracion(
      UnionBankConfig configuracion,
      string periodo,
      string cuit,
      DateTime vencimiento,
      decimal importe)
  {
    ArgumentNullException.ThrowIfNull(configuracion);

    return Generar(
        configuracion.EntityCode,
        periodo,
        cuit,
        vencimiento,
        importe,
        vencimiento,
        importe);
  }
}
