namespace SIRGA.Application.DTOs;

/// <summary>Resultado de la consulta previa al alta (CUIT existente, dueño y registros históricos).</summary>
public class CompanyLookupDto
{
  public string NormalizedCuit { get; set; } = string.Empty;
  public bool CuitExists { get; set; }
  public int? ExistingCompanyId { get; set; }
  public string? ExistingBusinessName { get; set; }
  public string? OwnerStudioName { get; set; }
  public bool OwnerStudioActive { get; set; }
  public bool AlreadyOwnedByCurrentStudio { get; set; }
  public bool LegacyFound { get; set; }
  public string? LegacyBusinessName { get; set; }
  public bool LegacyHasRecords { get; set; }
}