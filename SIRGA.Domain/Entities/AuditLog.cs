using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

public class AuditLog
{
  public int Id { get; set; }

  [Required]
  public string EntityName { get; set; } = null!; // E.g., "DeclarationRegistry"

  [Required]
  public int EntityId { get; set; }

  [Required]
  public string Action { get; set; } = null!; // E.g., "INSERT", "UPDATE", "DELETE", "RECTIFY"

  public string? PreviousValues { get; set; } // JSON format representation of OLD data

  public string? NewValues { get; set; }      // JSON format representation of NEW data

  [Required]
  public string UserId { get; set; } = null!;

  public string? UserEmail { get; set; }

  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  public string? IPAddress { get; set; }
}
