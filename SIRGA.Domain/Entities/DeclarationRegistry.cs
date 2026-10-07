using System.ComponentModel.DataAnnotations;
using SIRGA.Domain.Enums;

namespace SIRGA.Domain.Entities;

public class DeclarationRegistry
{
  public int Id { get; set; }

  [Required]
  [MaxLength(7)]
  public string Period { get; set; } = null!;

  public int CompanyId { get; set; }
  public virtual Company Company { get; set; } = null!;

  public int UnionId { get; set; }
  public virtual Union Union { get; set; } = null!;

  public DeclarationStatus Status { get; set; }

  public decimal TotalAmount { get; set; }

  public bool IsRectification { get; set; }

  /// <summary>
  /// Número de versión de la DDJJ para esa empresa y período: 1 es la original, 2 la primera
  /// rectificativa, 3 la segunda, y así. Es el mismo criterio del sistema anterior (su columna `rect`
  /// era el número de versión, no un booleano) y permite mostrar la cadena de versiones.
  /// </summary>
  public int VersionNumber { get; set; } = 1;

  /// <summary>
  /// En una rectificativa, lo que se había PAGADO de la versión anterior (no su total declarado): si la
  /// anterior no se pagó, se debe el total nuevo completo. Ver <c>DeclarationService</c>.
  /// </summary>
  public decimal PreviousAmount { get; set; }
  public decimal BalanceAmount { get; set; }   // Saldo Deudor/Acreedor (Diferencia)

  public DateTime SubmissionDate { get; set; }

  [MaxLength(100)]
  public string? ExternalReference { get; set; }

  [MaxLength(100)]
  public string? MercadoPagoPaymentId { get; set; }

  public DateTime? PaymentDate { get; set; }

  // Nuevos campos para Pago Presencial
  [MaxLength(200)]
  public string? PresencialBarCode { get; set; }

  [MaxLength(50)]
  public string? PresencialReference { get; set; }

  public virtual ICollection<DeclarationDetail> Details { get; set; } = new List<DeclarationDetail>();
}