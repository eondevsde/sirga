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

  // Configuración de porcentajes (SIRGA Multiafiliado)
  public decimal LawContributionPercentage { get; set; } = 2.0m; // Aporte de Ley (habitualmente 2%)
  public decimal UnionMemberPercentage { get; set; } = 2.0m; // Cuota Sindical / Socio

  // Propiedades de Navegación
  public virtual ICollection<SindicataryAssignment> Assignments { get; set; } = new List<SindicataryAssignment>();
  public virtual ICollection<DeclarationRegistry> Declarations { get; set; } = new List<DeclarationRegistry>();
  public virtual ICollection<EmployeeCategory> Categories { get; set; } = new List<EmployeeCategory>();
}
