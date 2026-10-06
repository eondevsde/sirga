using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Banco donde el sindicato cobra las boletas de las DDJJ.
///
/// Se modela como entidad propia (y no como un texto suelto en el sindicato) porque un sindicato puede
/// operar con más de un banco: cada uno tiene su estructura de código de barras, su número de cliente
/// y su archivo de rendición. Hoy se usa el Banco Santiago del Estero (BSE), pero el modelo no lo
/// asume: cualquier banco se carga como un registro más.
/// </summary>
public class Bank
{
  public int Id { get; set; }

  /// <summary>Nombre del banco, tal como debe figurar en la boleta (por ejemplo "Banco Santiago del Estero").</summary>
  [Required]
  [MaxLength(120)]
  public string Name { get; set; } = string.Empty;

  /// <summary>Sucursal con la que opera el sindicato (en la boleta del BSE: "Sucursal Central").</summary>
  [MaxLength(120)]
  public string? Branch { get; set; }

  [MaxLength(300)]
  public string? Notes { get; set; }

  public bool IsActive { get; set; } = true;
}
