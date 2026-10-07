namespace SIRGA.Application.DTOs;

/// <summary>
/// Datos que se imprimen en la boleta de pago bancaria, que va por TRIPLICADO (talón cliente, banco y
/// sindicato).
///
/// La boleta tiene **UNA SOLA FECHA DE VENCIMIENTO** (no hay 2° vencimiento). Si la DDJJ todavía no
/// venció, esa fecha es la del banco para el período y el importe es el total. Si YA venció, la fecha es
/// **la que el cliente declara que va a pagar** y el importe incluye el **interés resarcitorio** calculado
/// desde el día siguiente al vencimiento original hasta esa fecha.
///
/// Se arma en <see cref="Services.BoletaService"/> a partir de la DDJJ y de la configuración bancaria
/// del sindicato. El armado NO se hace en la pantalla a propósito: así queda en la capa de negocio, se
/// puede probar, y la pantalla no puede mostrar una boleta distinta de la que se emite.
/// </summary>
public class BoletaDatosDto
{
  public string Sindicato { get; set; } = string.Empty;

  /// <summary>
  /// Jurisdicción del sindicato (la provincia), que se imprime en su propia línea debajo del nombre. El
  /// nombre del sindicato ya NO la incluye: tenerla en los dos lados la duplicaba en la boleta.
  /// </summary>
  public string Jurisdiccion { get; set; } = string.Empty;

  public string Banco { get; set; } = string.Empty;
  public string Sucursal { get; set; } = string.Empty;
  public string NumeroCliente { get; set; } = string.Empty;
  public string RazonSocial { get; set; } = string.Empty;
  public string Cuit { get; set; } = string.Empty;
  public string Periodo { get; set; } = string.Empty;

  public decimal AporteLey { get; set; }
  public decimal CuotaSindical { get; set; }

  /// <summary>
  /// Fecha de vencimiento que lleva impresa la boleta: la del banco si está en término, o la fecha de
  /// pago declarada por el cliente si la DDJJ ya estaba vencida.
  /// </summary>
  public DateTime Vencimiento { get; set; }

  /// <summary>El total de la DDJJ, sin interés. Es la base sobre la que se calcula el interés.</summary>
  public decimal Capital { get; set; }

  /// <summary>Interés resarcitorio hasta el vencimiento. Cero si se paga en término.</summary>
  public decimal Interes { get; set; }

  /// <summary>Días de mora que se cobraron (0 si está en término).</summary>
  public int DiasInteres { get; set; }

  /// <summary>Tasa total aplicada sobre el capital, en PORCENTAJE (1,00 = 1%). Para mostrar y auditar.</summary>
  public decimal InteresPorcentaje { get; set; }

  /// <summary>
  /// Norma (o normas, si el período cruzó un cambio de tasa) que fija la tasa usada. Queda impresa en el
  /// circuito para poder auditar de dónde salió el interés.
  /// </summary>
  public string? InteresNorma { get; set; }

  /// <summary>Total a depositar = capital + interés.</summary>
  public decimal ImporteTotal { get; set; }

  /// <summary>Código de barras de 58 dígitos del banco.</summary>
  public string CodigoBarras { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de armar la boleta: los datos, o el motivo por el que NO se puede emitir.
/// Se devuelve el motivo en lugar de una boleta a medias para que la pantalla lo muestre: una boleta
/// sin configuración bancaria no se puede inventar.
/// </summary>
public record BoletaArmadoResult(BoletaDatosDto? Datos, string? Error, string? Advertencia);

/// <summary>
/// Vencimiento de una DDJJ y si ya pasó, para que la pantalla sepa si tiene que preguntarle al cliente
/// cuándo va a pagar (y con eso calcular el interés).
/// </summary>
public record VencimientoBoletaDto(DateTime Vencimiento, bool Vencida, decimal Capital);
