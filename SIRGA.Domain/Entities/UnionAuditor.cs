using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Asignación de un auditor a un sindicato. La hace el propio sindicato desde su pantalla, y es lo
/// que limita al rol Auditor a ver sólo los datos de ese sindicato en todas las pantallas.
/// Un auditor sin fila acá no ve datos de ningún sindicato (no cae en "ve todo").
/// </summary>
public class UnionAuditor
{
  public int Id { get; set; }

  [Required]
  public int UnionId { get; set; }
  public virtual Union Union { get; set; } = null!;

  /// <summary>Usuario con rol Auditor que queda habilitado para este sindicato.</summary>
  [Required]
  public string Username { get; set; } = string.Empty;

  public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

  /// <summary>Quién lo asignó: el usuario del sindicato que lo dio de alta.</summary>
  public string? AssignedByUsername { get; set; }
}
