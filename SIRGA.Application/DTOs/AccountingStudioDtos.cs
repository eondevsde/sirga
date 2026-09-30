namespace SIRGA.Application.DTOs;

public class AccountingStudioDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string ResponsibleUsername { get; set; } = string.Empty;
}

public class CompanyDashboardDto
{
  public int Id { get; set; }
  public string BusinessName { get; set; } = string.Empty;
  public string Cuit { get; set; } = string.Empty;
  public string LastPeriodDeclared { get; set; } = "Sin datos";
  public decimal TotalLastDeclaration { get; set; }
  public string Status { get; set; } = "Pendiente";
}
