namespace SIRGA.Application.DTOs;

public class UnionDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string JurisdictionProvince { get; set; } = string.Empty;
  public string? JurisdictionCity { get; set; }
  public string CctCode { get; set; } = string.Empty;
  public string? AdminEmail { get; set; }
  public bool IsActive { get; set; } = true;
}
