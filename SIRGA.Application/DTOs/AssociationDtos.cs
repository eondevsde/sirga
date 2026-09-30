using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Application.DTOs;

public class CompanyAssociationRequestDto
{
  public int Id { get; set; }

  // Estudio que solicita
  public int AccountingStudioId { get; set; }
  public string AccountingStudioName { get; set; } = string.Empty;

  // Empresa solicitada
  public int CompanyId { get; set; }
  public string CompanyBusinessName { get; set; } = string.Empty;
  public string CompanyCuit { get; set; } = string.Empty;

  // Sindicato que debe autorizar
  public int TargetUnionId { get; set; }
  public string TargetUnionName { get; set; } = string.Empty;

  public DateTime CreatedAt { get; set; }
  public RequestStatus Status { get; set; }
  public string? AdminComment { get; set; }
  public DateTime? ProcessedAt { get; set; }
}
