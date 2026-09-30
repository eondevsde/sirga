using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Representa la suscripción de un sindicato a la plataforma SIRGA.
/// </summary>
public class UnionSubscription
{
  public int Id { get; set; }

  [Required]
  public int UnionId { get; set; }
  public virtual Union Union { get; set; } = null!;

  [Required]
  public decimal MonthlyFee { get; set; } // Abono Mensual

  [Required]
  public DateTime NextBillingDate { get; set; } // Fecha de próximo cobro / corte

  public bool IsServiceActive { get; set; } = true;

  public virtual ICollection<UnionSubscriptionPayment> Payments { get; set; } = new List<UnionSubscriptionPayment>();
}

/// <summary>
/// Registro de pagos de abono realizados por los sindicatos a SIRGA.
/// </summary>
public class UnionSubscriptionPayment
{
  public int Id { get; set; }

  public int UnionSubscriptionId { get; set; }
  public virtual UnionSubscription Subscription { get; set; } = null!;

  public DateTime PaymentDate { get; set; }
  public decimal Amount { get; set; }
  public string PaymentMethod { get; set; } = "Transferencia";
  public string? ReceiptNumber { get; set; }
  public string? Notes { get; set; }
}
