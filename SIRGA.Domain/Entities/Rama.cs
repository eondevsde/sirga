using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Rama de actividad: el convenio colectivo de trabajo.
///
/// Las escalas salariales son NACIONALES y dependen de la RAMA, no del sindicato ni de la ciudad:
/// Comercio (CCT 130/75), Panaderos, Farmacéuticos, etc. Varios sindicatos comparten la misma rama, así
/// que las categorías y las escalas van a colgar de acá y no de cada sindicato: se cargan una sola vez
/// y las usan todos los sindicatos de esa rama.
///
/// Modelar esto evita el problema que ya apareció: la misma escala nacional copiada por sindicato
/// (Santiago del Estero y La Banda comparten Comercio 130/75, y no tienen por qué tener cada uno su
/// propia copia de las 21 categorías).
/// </summary>
public class Rama
{
  public int Id { get; set; }

  /// <summary>Código del convenio (por ejemplo "130/75" para Comercio).</summary>
  [Required]
  [MaxLength(20)]
  public string Code { get; set; } = string.Empty;

  /// <summary>Nombre de la rama (por ejemplo "Comercio").</summary>
  [Required]
  [MaxLength(120)]
  public string Name { get; set; } = string.Empty;

  [MaxLength(300)]
  public string? Notes { get; set; }

  public bool IsActive { get; set; } = true;

  // Propiedades de Navegación
  /// <summary>Categorías de esta rama. Las escalas de esas categorías son las que se usan para liquidar.</summary>
  public virtual ICollection<EmployeeCategory> Categories { get; set; } = new List<EmployeeCategory>();
}
