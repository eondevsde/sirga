using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Actividad del empleado dentro del convenio (lo que el sistema anterior llama "Actividad" y guarda en
/// <c>maesoc.MAESOC_CODACT</c>, con su catálogo en <c>actividad</c>). NO es la categoría: la categoría fija
/// el mínimo de escala y la actividad describe la tarea (vendedor, maestranza, administrativo…).
/// </summary>
public class Activity
{
  public int Id { get; set; }

  /// <summary>Código del sistema anterior (<c>MAEACT_CODACT</c>), tal cual.</summary>
  [Required]
  [MaxLength(20)]
  public string Code { get; set; } = null!;

  [Required]
  [MaxLength(200)]
  public string Name { get; set; } = null!;

  public bool IsActive { get; set; } = true;
}
