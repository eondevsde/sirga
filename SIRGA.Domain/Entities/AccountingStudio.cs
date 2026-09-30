using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Firmas que gestionan múltiples empresas.
/// </summary>
public class AccountingStudio
{
  public int Id { get; set; }

  [Required]
  [MaxLength(200)]
  public string Name { get; set; } = null!;

  [Required]
  [MaxLength(100)]
  public string ResponsibleUsername { get; set; } = null!;

  [MaxLength(200)]
  public string? Email { get; set; }

  [MaxLength(50)]
  public string? ContactPhone { get; set; }

  [MaxLength(50)]
  public string? ContactCellPhone { get; set; }

  [MaxLength(100)]
  public string? ReferencePerson { get; set; }

  [MaxLength(50)]
  public string? ReferencePhone { get; set; }

  [MaxLength(250)]
  public string? Address { get; set; }

  public int? CityId { get; set; }
  public City? City { get; set; }

  public bool IsApproved { get; set; } = false;
  public bool IsActive { get; set; } = true;

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? ApprovedAt { get; set; }

  [MaxLength(500)]
  public string? RejectionReason { get; set; }

  public ICollection<Company> Companies { get; set; } = new List<Company>();
}
