using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Representa un Acta de Oficio emitida por el Sindicato cuando una empresa
/// no declara o no paga periodos específicos.
/// </summary>
public class InspectionAct
{
  public int Id { get; set; }

  [Required]
  public int CompanyId { get; set; }
  public Company? Company { get; set; }

  [Required]
  public int UnionId { get; set; }
  public Union? Union { get; set; }

  [Required]
  public string ActNumber { get; set; } = null!; // Ej: ACT-2026-0001

  public DateTime IssueDate { get; set; } = DateTime.Now;

  [Required]
  public string PeriodRange { get; set; } = null!; // Ej: 2025-01 a 2025-06

  public decimal EstimatedDebt { get; set; }
  public decimal InterestAmount { get; set; }
  public decimal TotalAmount => EstimatedDebt + InterestAmount;

  public string? Observations { get; set; }

  public InspectionActStatus Status { get; set; } = InspectionActStatus.Pending;

  // Lista de periodos detallados que componen este acta
  public List<InspectionActPeriod> Periods { get; set; } = new();
}

public class InspectionActPeriod
{
  public int Id { get; set; }
  public int InspectionActId { get; set; }
  public string Period { get; set; } = null!; // YYYY-MM
  public decimal BaseAmount { get; set; }
  public decimal CalculatedInterest { get; set; }
  public bool WasAlreadyDeclared { get; set; } // Si existía DDJJ pero no se pagó
}

public enum InspectionActStatus
{
  Pending,
  Notified,
  InCovenant, // Si pasó a un convenio de pago
  Paid,
  Cancelled,
  Legal       // Pasó a instancia prejudicial/judicial
}
