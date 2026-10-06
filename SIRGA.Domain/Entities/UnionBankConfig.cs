using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Configuración bancaria de un sindicato: en qué banco cobra, con qué número de cliente y con qué
/// reglas de vencimiento. Es lo que permite emitir la boleta "según el sindicato" sin hardcodear nada.
///
/// Cada valor es configurable porque cambia de un sindicato a otro y de un banco a otro:
///   - El código de ente del código de barras: Santiago del Estero 0134, La Banda 0135, Frías 0136,
///     Añatuya 0137.
///   - El número de cliente / cuenta corriente (en la boleta del BSE figura como "Nota de Crédito
///     para la Cta. Cte. N° 18588639").
///   - Los vencimientos: por defecto el 15 del mes posterior al período y 10 días después el segundo,
///     pero quedan configurables porque hay boletas reales con 12 días de diferencia.
/// </summary>
public class UnionBankConfig
{
  public int Id { get; set; }

  public int UnionId { get; set; }
  public Union? Union { get; set; }

  public int BankId { get; set; }
  public Bank? Bank { get; set; }

  /// <summary>Número de cliente / cuenta corriente que el banco le asigna al sindicato.</summary>
  [Required]
  [MaxLength(30)]
  public string ClientNumber { get; set; } = string.Empty;

  /// <summary>Código del ente en el código de barras (4 posiciones, por ejemplo 0134).</summary>
  [Required]
  [MaxLength(4)]
  public string EntityCode { get; set; } = string.Empty;

  /// <summary>Valor del campo "formato diseño" de la estructura del banco (constante 4 en el BSE).</summary>
  public int DesignFormat { get; set; } = 4;

  /// <summary>Día del mes posterior al período en que vence el primer vencimiento (15 por defecto).</summary>
  public int FirstDueDay { get; set; } = 15;

  /// <summary>Días que se suman al primer vencimiento para obtener el segundo (10 por defecto).</summary>
  public int DaysToSecondDue { get; set; } = 10;

  public bool IsActive { get; set; } = true;
}
