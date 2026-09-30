using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

public enum SubscriptionRequestStatus
{
  Pending,
  Approved,
  Rejected
}

public class UnionSubscriptionRequest
{
  public int Id { get; set; }

  [Required]
  [MaxLength(200)]
  public string UnionName { get; set; } = null!;

  [Required]
  [MaxLength(20)]
  public string Cuit { get; set; } = null!;

  [Required]
  [EmailAddress]
  public string ContactEmail { get; set; } = null!;

  [MaxLength(50)]
  public string? ContactPhone { get; set; }

  [MaxLength(50)]
  public string? WhatsApp { get; set; }

  [Required]
  [MaxLength(200)]
  public string Street { get; set; } = null!;

  [Required]
  [MaxLength(20)]
  public string StreetNumber { get; set; } = null!;

  [MaxLength(20)]
  public string? Floor { get; set; }

  [Required]
  [MaxLength(100)]
  public string Locality { get; set; } = null!;

  [Required]
  [MaxLength(100)]
  public string Province { get; set; } = null!;

  [MaxLength(500)]
  public string? CoverageArea { get; set; }

  [Required]
  [MaxLength(200)]
  public string ReferencePersonFullName { get; set; } = null!;

  public DateTime RequestDate { get; set; } = DateTime.Now;

  public SubscriptionRequestStatus Status { get; set; } = SubscriptionRequestStatus.Pending;

  public string? AdminNotes { get; set; }
}
