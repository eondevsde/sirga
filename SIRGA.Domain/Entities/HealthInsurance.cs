using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Obra social del empleado. El sistema anterior la guarda como código (<c>maesoc.MAESOC_CODOSM</c>) contra
/// su catálogo <c>obrasocial</c> (297 filas). El código no alcanza para mostrarlo: hace falta el nombre.
/// </summary>
public class HealthInsurance
{
  public int Id { get; set; }

  /// <summary>Código del sistema anterior (<c>MAEOSM_CODOSM</c>), tal cual.</summary>
  [Required]
  [MaxLength(20)]
  public string Code { get; set; } = null!;

  [Required]
  [MaxLength(250)]
  public string Name { get; set; } = null!;

  /// <summary>Sigla, cuando el catálogo la trae (por ejemplo OSPA).</summary>
  [MaxLength(30)]
  public string? Acronym { get; set; }

  public bool IsActive { get; set; } = true;
}
