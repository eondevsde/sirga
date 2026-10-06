using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Categorías de empleados de una RAMA de actividad (el convenio).
///
/// Cuelgan de la rama y NO del sindicato: las categorías de Comercio (130/75) son las mismas para todos
/// los sindicatos de esa rama, igual que las escalas. Antes colgaban del sindicato y había que cargarlas
/// repetidas.
/// </summary>
public class EmployeeCategory
{
  public int Id { get; set; }

  [Required]
  [MaxLength(50)]
  public string Code { get; set; } = null!; // Ej: "13", "2"

  [Required]
  [MaxLength(200)]
  public string Name { get; set; } = null!; // Ej: "MAESTRANZA B"

  public int RamaId { get; set; }
  public virtual Rama Rama { get; set; } = null!;

  public bool IsActive { get; set; } = true;
}
