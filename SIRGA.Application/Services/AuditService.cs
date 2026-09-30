using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Enums;
using SIRGA.Domain.Entities;
using System.Text.Json;

namespace SIRGA.Application.Services;

public class AuditService(IUnitOfWork unitOfWork)
{
  public async Task LogActionAsync(string entityName, int entityId, string action, object? oldValues, object? newValues, string userId, string? userEmail = null, string? ipAddress = null)
  {
    var log = new AuditLog
    {
      EntityName = entityName,
      EntityId = entityId,
      Action = action,
      PreviousValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
      NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
      UserId = userId,
      UserEmail = userEmail,
      Timestamp = DateTime.UtcNow,
      IPAddress = ipAddress
    };

    await unitOfWork.AuditLogs.AddAsync(log);
    await unitOfWork.CompleteAsync();
  }

  public async Task<AuditDashboardDto> GetStatsAsync(int? unionId = null)
  {
    var declarations = await unitOfWork.Declarations.FindAsync(d => !unionId.HasValue || d.UnionId == unionId.Value);
    var companies = await unitOfWork.Companies.GetAllAsync();
    var assignments = await unitOfWork.Assignments.GetAllAsync();
    var interestRates = (await unitOfWork.InterestRates.GetAllAsync()).OrderByDescending(r => r.EffectiveDate).ToList();

    var stats = new AuditDashboardDto
    {
      TotalCollected = declarations.Sum(d => d.TotalAmount),
      TotalDeclarations = declarations.Count(),
      CompaniesCount = companies.Count(c => c.ParentCompanyId == null),
      ActiveEmployees = assignments.Count()
    };

    // Recaudación mensual (Últimos 6 meses)
    stats.MonthlyCollections = declarations
        .GroupBy(d => d.Period)
        .OrderByDescending(g => g.Key)
        .Take(6)
        .Select(g => new MonthlyCollectionDto
        {
          Period = g.Key,
          Amount = g.Sum(d => d.TotalAmount)
        }).ToList();

    // Mapa de Deudores (Empresas con empleados pero sin DDJJ en el periodo actual)
    // Simulamos periodo actual como el mes anterior (que es el que se liquida)
    var now = DateTime.Now;
    var currentPeriod = now.AddMonths(-1).ToString("yyyy-MM");

    foreach (var company in companies)
    {
      var hasPresented = declarations.Any(d => d.CompanyId == company.Id && d.Period == currentPeriod);
      var empCount = assignments.Count(a => a.CompanyId == company.Id);

      if (!hasPresented && empCount > 0)
      {
        var lastPresent = declarations
            .Where(d => d.CompanyId == company.Id)
            .OrderByDescending(d => d.Period)
            .FirstOrDefault();

        // Cálculo aproximado de deuda e intereses
        decimal estimatedBase = empCount * 5000; // Mock de base imponible promedio
        decimal calculatedInterest = 0;

        if (lastPresent != null)
        {
          // Si tiene presentaciones previas, calculamos meses de mora
          var lastPeriodDate = DateTime.Parse(lastPresent.Period + "-01");
          var monthDiff = ((now.Year - lastPeriodDate.Year) * 12) + now.Month - lastPeriodDate.Month - 1;

          if (monthDiff > 0)
          {
            var currentRate = interestRates.FirstOrDefault(r => r.EffectiveDate <= now)?.MonthlyRate ?? 0.03m;
            calculatedInterest = estimatedBase * currentRate * monthDiff;
          }
        }

        stats.Debtors.Add(new CompanyDebtDto
        {
          CompanyId = company.Id,
          BusinessName = company.BusinessName,
          BranchName = company.BranchName,
          EmployeeCount = empCount,
          LastPeriodPresented = lastPresent?.Period ?? "Nunca",
          EstimatedDebt = estimatedBase,
          Interests = calculatedInterest
        });
      }
    }

    // Auditoría Reciente
    var logs = await unitOfWork.AuditLogs.GetAllAsync();
    stats.RecentLogs = logs.OrderByDescending(l => l.Timestamp)
        .Take(50)
        .Select(l => new AuditLogDto
        {
          Id = l.Id,
          EntityName = l.EntityName,
          EntityId = l.EntityId,
          Action = l.Action,
          PreviousValues = l.PreviousValues,
          NewValues = l.NewValues,
          UserId = l.UserId,
          UserEmail = l.UserEmail,
          IPAddress = l.IPAddress,
          Timestamp = l.Timestamp
        }).ToList();

    return stats;
  }
}
