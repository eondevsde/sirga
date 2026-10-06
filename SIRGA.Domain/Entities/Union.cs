using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Catálogo de Sindicatos.
/// </summary>
public partial class Union // Sindicato
{
  public int Id { get; set; }

  [Required]
  [MaxLength(200)]
  public string Name { get; set; } = null!;

  [Required]
  [MaxLength(100)]
  public string JurisdictionProvince { get; set; } = null!;
  [MaxLength(100)]
  public string? JurisdictionCity { get; set; } // Localidad/Ciudad (ej: "La Banda", "Capital")
  [Required]
  [MaxLength(50)]
  public string CctCode { get; set; } = null!;

  public bool IsActive { get; set; }

  /// <summary>
  /// Rama de actividad (el convenio) a la que pertenece el sindicato. Es lo que va a permitir que las
  /// categorías y las escalas cuelguen de la RAMA y no de cada sindicato: varios sindicatos comparten la
  /// misma rama (Santiago del Estero y La Banda comparten Comercio 130/75) y hoy hay que copiarles la
  /// escala a cada uno.
  ///
  /// Queda nullable a propósito: un sindicato puede no tener la rama asignada todavía, y eso tiene que
  /// poder verse en vez de romper.
  /// </summary>
  public int? RamaId { get; set; }
  public virtual Rama? Rama { get; set; }

  // Configuración de porcentajes (SIRGA Multiafiliado)
  public decimal LawContributionPercentage { get; set; } = 2.0m; // Aporte de Ley (habitualmente 2%)
  public decimal UnionMemberPercentage { get; set; } = 2.0m; // Cuota Sindical / Socio

  // Propiedades de Navegación
  public virtual ICollection<SindicataryAssignment> Assignments { get; set; } = new List<SindicataryAssignment>();
  public virtual ICollection<DeclarationRegistry> Declarations { get; set; } = new List<DeclarationRegistry>();
}
