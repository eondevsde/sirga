using SIRGA.Domain.Entities;

namespace SIRGA.Application.DTOs;

public class SubscriptionRequestDto
{
  public int Id { get; set; }
  public string UnionName { get; set; } = null!;
  public string Cuit { get; set; } = null!;
  public string ContactEmail { get; set; } = null!;
  public string? ContactPhone { get; set; }
  public string? WhatsApp { get; set; }
  public string Street { get; set; } = null!;
  public string StreetNumber { get; set; } = null!;
  public string? Floor { get; set; }
  public string Locality { get; set; } = null!;
  public string Province { get; set; } = null!;
  public string? CoverageArea { get; set; }
  public string ReferencePersonFullName { get; set; } = null!;
  public DateTime RequestDate { get; set; }
  public SubscriptionRequestStatus Status { get; set; }
  public string? AdminNotes { get; set; }
}
