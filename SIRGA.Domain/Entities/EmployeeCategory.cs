using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Categorías de empleados configurables por Sindicato
/// </summary>
public class EmployeeCategory
{
  public int Id { get; set; }

  [Required]
  [MaxLength(50)]
  public string Code { get; set; } = null!; // Ej: "CAT1", "ADMIN_A"

  [Required]
  [MaxLength(200)]
  public string Name { get; set; } = null!; // Ej: "Administrativo A"

  public int UnionId { get; set; }
  public virtual Union Union { get; set; } = null!;

  public bool IsActive { get; set; } = true;
}
