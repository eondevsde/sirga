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

  public decimal PreviousAmount { get; set; } // Monto pagado anteriormente (si es rectificativa)
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