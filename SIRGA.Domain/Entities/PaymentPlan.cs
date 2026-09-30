using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Representa un Convenio o Plan de Pago para regularizar deudas de Actas de Inspección.
/// </summary>
public class PaymentPlan
{
  public int Id { get; set; }

  [Required]
  public int InspectionActId { get; set; }
  public virtual InspectionAct? InspectionAct { get; set; }

  [Required]
  public string PlanNumber { get; set; } = null!; // Ej: PP-163

  public DateTime SignatureDate { get; set; } = DateTime.Now;

  public decimal TotalDebt { get; set; } // Capital + Intereses del Acta
  public decimal DownPayment { get; set; } // Anticipo (Cuota 0)
  public int InstallmentsCount { get; set; } // Cantidad de cuotas
  public decimal InstallmentAmount { get; set; } // Monto de cada cuota

  public decimal FinancingInterestRate { get; set; } // Tasa aplicada al convenio

  public PaymentPlanStatus Status { get; set; } = PaymentPlanStatus.Active;

  public virtual ICollection<PaymentPlanInstallment> Installments { get; set; } = new List<PaymentPlanInstallment>();
}

public class PaymentPlanInstallment
{
  public int Id { get; set; }
  public int PaymentPlanId { get; set; }
  public int InstallmentNumber { get; set; } // 0 para anticipo, 1...N para cuotas
  public DateTime DueDate { get; set; }
  public decimal Amount { get; set; }
  public bool IsPaid { get; set; }
  public DateTime? PaymentDate { get; set; }
  public string? PaymentReference { get; set; } // Ref de MercadoPago o Transferencia
}

public enum PaymentPlanStatus
{
  Active,
  Completed,
  Defaulted, // Caído por falta de pago
  Cancelled
}
