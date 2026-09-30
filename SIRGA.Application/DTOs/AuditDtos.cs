namespace SIRGA.Application.DTOs;

public class AuditDashboardDto
{
  public decimal TotalCollected { get; set; }
  public int TotalDeclarations { get; set; }
  public int CompaniesCount { get; set; }
  public int ActiveEmployees { get; set; }
  public List<MonthlyCollectionDto> MonthlyCollections { get; set; } = new();
  public List<CompanyDebtDto> Debtors { get; set; } = new();
  public List<AuditLogDto> RecentLogs { get; set; } = new();
}

public class AuditLogDto
{
  public int Id { get; set; }
  public string EntityName { get; set; } = null!;
  public int EntityId { get; set; }
  public string Action { get; set; } = null!;
  public string? PreviousValues { get; set; }
  public string? NewValues { get; set; }
  public string UserId { get; set; } = null!;
  public string? UserEmail { get; set; }
  public DateTime Timestamp { get; set; }
  public string? IPAddress { get; set; }
}

public class MonthlyCollectionDto
{
  public string Period { get; set; } = null!;
  public decimal Amount { get; set; }
}

public class CompanyDebtDto
{
  public int CompanyId { get; set; }
  public string BusinessName { get; set; } = null!;
  // BranchName es opcional en la entidad Company: una empresa puede no tener sucursal.
  public string? BranchName { get; set; }
  public string LastPeriodPresented { get; set; } = null!;
  public int EmployeeCount { get; set; }
  public decimal EstimatedDebt { get; set; }
  public decimal Interests { get; set; }
}
