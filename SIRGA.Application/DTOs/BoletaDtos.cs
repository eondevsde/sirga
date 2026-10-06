namespace SIRGA.Application.DTOs;

/// <summary>
/// Datos que se imprimen en la boleta de pago bancaria, que va por TRIPLICADO (talón cliente, banco y
/// sindicato).
///
/// Se arma en <see cref="Services.BoletaService"/> a partir de la DDJJ y de la configuración bancaria
/// del sindicato. El armado NO se hace en la pantalla a propósito: así queda en la capa de negocio, se
/// puede probar, y la pantalla no puede mostrar una boleta distinta de la que se emite.
/// </summary>
public class BoletaDatosDto
{
  public string Sindicato { get; set; } = string.Empty;
  public string Banco { get; set; } = string.Empty;
  public string Sucursal { get; set; } = string.Empty;
  public string NumeroCliente { get; set; } = string.Empty;
  public string RazonSocial { get; set; } = string.Empty;
  public string Cuit { get; set; } = string.Empty;
  public string Periodo { get; set; } = string.Empty;

  public decimal AporteLey { get; set; }
  public decimal CuotaSindical { get; set; }

  /// <summary>Recargo del segundo vencimiento, en pesos.</summary>
  public decimal Recargo { get; set; }

  /// <summary>Tasa aplicada al recargo, expresada en PORCENTAJE sobre el total (1,00 = 1%).</summary>
  public decimal RecargoPorcentaje { get; set; }

  /// <summary>Días entre el primer y el segundo vencimiento que se usaron para el recargo.</summary>
  public int DiasRecargo { get; set; }

  /// <summary>
  /// Norma que fija la tasa usada para el recargo (por ejemplo "R (MEC) 823/2025"). Queda impresa en el
  /// circuito para poder auditar de dónde salió el importe del segundo vencimiento.
  /// </summary>
  public string? RecargoNorma { get; set; }

  /// <summary>Serie de la tasa usada para el recargo (hoy siempre resarcitoria).</summary>
  public string RecargoSerie { get; set; } = string.Empty;

  public DateTime PrimerVencimiento { get; set; }
  public decimal ImportePrimerVencimiento { get; set; }
  public DateTime SegundoVencimiento { get; set; }
  public decimal ImporteSegundoVencimiento { get; set; }

  /// <summary>Código de barras de 58 dígitos del banco.</summary>
  public string CodigoBarras { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de armar la boleta: los datos, o el motivo por el que NO se puede emitir.
/// Se devuelve el motivo en lugar de una boleta a medias para que la pantalla lo muestre: una boleta
/// sin configuración bancaria no se puede inventar.
/// </summary>
public record BoletaArmadoResult(BoletaDatosDto? Datos, string? Error, string? Advertencia);
