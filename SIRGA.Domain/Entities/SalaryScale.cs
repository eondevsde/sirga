using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Catálogo de escalas salariales vigentes.
/// </summary>
public class SalaryScale
{
  public int Id { get; set; }

  [Required]
  public DateTime Period { get; set; }

  public int EmployeeCategoryId { get; set; }
  public virtual EmployeeCategory EmployeeCategory { get; set; } = null!;

  public decimal BasicAmount { get; set; } // El Importe base

  public decimal AntiguedadPercentage { get; set; } // % por año de antigüedad (ej: 1% para Comercio)

  public decimal AgreementNR1 { get; set; } // Acuerdo no remunerativo 1

  public decimal AgreementNR2 { get; set; } // Acuerdo no remunerativo 2

  public bool IsActive { get; set; }

  public int UnionId { get; set; }
  public virtual Union Union { get; set; } = null!;
}
