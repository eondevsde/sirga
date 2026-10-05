using System.ComponentModel.DataAnnotations;
using SIRGA.Domain.Enums;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Empresa empleadora.
/// </summary>
public class Company
{
  public int Id { get; set; }

  [Required]
  [MaxLength(200)]
  public string BusinessName { get; set; } = null!; // Razon Social

  /// <summary>
  /// Nombre de fantasía: como el estudio reconoce a la empresa en el día a día ("Supermat") a
  /// diferencia de la razón social ("SUPERMERCADOS TUCUMAN S.A."). Es opcional y se usa para buscar.
  /// </summary>
  [MaxLength(200)]
  public string? TradeName { get; set; }

  [Required]
  [MaxLength(20)]
  public string Cuit { get; set; } = null!;

  // CUIT normalizado a 11 dígitos (columna calculada en la base, usada para deduplicar)
  public string NormalizedCuit { get; set; } = string.Empty;

  // Normalizando geografía
  public int? CityId { get; set; }
  public virtual City? City { get; set; }

  [Required]
  [MaxLength(200)]
  public string Address { get; set; } = null!;

  [MaxLength(100)]
  public string? Phone { get; set; }

  [MaxLength(100)]
  public string? ContactEmail { get; set; }

  // Persona de Referencia (igual que en Estudios)
  [MaxLength(200)]
  public string? ReferencePerson { get; set; }

  [MaxLength(100)]
  public string? ReferencePhone { get; set; }

  // Jerarquía de Sucursales
  public int? ParentCompanyId { get; set; }
  [System.Text.Json.Serialization.JsonIgnore]
  public virtual Company? ParentCompany { get; set; }
  [System.Text.Json.Serialization.JsonIgnore]
  public virtual ICollection<Company> Branches { get; set; } = new List<Company>();

  // Relación con Estudio Contable
  public int? AccountingStudioId { get; set; }
  [System.Text.Json.Serialization.JsonIgnore]
  public virtual AccountingStudio? AccountingStudio { get; set; }

  public CompanyStatus Status { get; set; } = CompanyStatus.PendingAuthorization;

  // Actividad derivada del legado (socemp.SOCEMP_ESTADO / ddjj recientes), independiente del flujo de aprobación
  public bool IsActive { get; set; } = true;

  // Propiedades de Navegación
  public virtual ICollection<SindicataryAssignment> Assignments { get; set; } = new List<SindicataryAssignment>();
  public virtual ICollection<DeclarationRegistry> Declarations { get; set; } = new List<DeclarationRegistry>();

  // Mantenemos BranchName por si es una sucursal específica, pero lo hacemos opcional
  [MaxLength(200)]
  public string? BranchName { get; set; }
}
