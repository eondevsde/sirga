namespace SIRGA.Application.DTOs;

public class AdminDashboardDto
{
  public int ActiveUnionsCount { get; set; }
  public int DelinquentUnionsCount { get; set; } // Morosos
  public int TotalUnionsCount { get; set; }
  public int PendingRequestsCount { get; set; }
  public int TotalAccountingStudiosCount { get; set; }
  public int TotalEmployeesCount { get; set; }

  // Informe mensual de solicitudes
  public int RequestsCurrentMonth { get; set; }
  public int ApprovedCurrentMonth { get; set; }
  public int RejectedCurrentMonth { get; set; }

  public decimal MonthlyRevenue { get; set; }
}
