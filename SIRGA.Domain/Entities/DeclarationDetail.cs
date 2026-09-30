namespace SIRGA.Domain.Entities;

/// <summary>
/// Detalle de liquidación por empleado dentro de una DDJJ.
/// </summary>
public class DeclarationDetail
{
  public int Id { get; set; }

  public int RegistryId { get; set; }
  public virtual DeclarationRegistry Registry { get; set; } = null!;

  public int AssignmentId { get; set; }
  public virtual SindicataryAssignment Assignment { get; set; } = null!;

  public decimal BaseSalary { get; set; } // Remuneracion Base

  public decimal LawContribution { get; set; } // Aporte Ley (Calculado)

  public decimal MemberContribution { get; set; } // Aporte Socio (Calculado)

  public decimal TotalWithheld { get; set; } // Total Retenido
}
