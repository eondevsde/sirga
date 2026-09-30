using System.ComponentModel.DataAnnotations;
using SIRGA.Domain.Enums;

namespace SIRGA.Domain.Entities;

public class CompanyAssociationRequest
{
  public int Id { get; set; }

  [Required]
  public int AccountingStudioId { get; set; }
  public virtual AccountingStudio AccountingStudio { get; set; } = null!;

  [Required]
  public int CompanyId { get; set; }
  public virtual Company Company { get; set; } = null!;

  [Required]
  public int TargetUnionId { get; set; }
  public virtual Union TargetUnion { get; set; } = null!;

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public string? AdminComment { get; set; }

  public RequestStatus Status { get; set; } = RequestStatus.Pending;

  public DateTime? ProcessedAt { get; set; }
}

public enum RequestStatus
{
  Pending = 0,
  Approved = 1,
  Rejected = 2
}
