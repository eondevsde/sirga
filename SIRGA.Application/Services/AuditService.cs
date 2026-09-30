using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Enums;
using SIRGA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
    // Todas las cifras se agregan en SQL. Antes se traían las cuatro tablas completas a
    // memoria (248.151 DDJJ, 27.934 empleados, 36.011 asignaciones y 4.507 empresas) y se
    // recorría empresa por empresa en C#: 103,7 segundos medidos con datos reales, con el
    // proceso pasando de 119 MB a 497 MB.
    var stats = new AuditDashboardDto
    {
      TotalCollected = await unitOfWork.Declarations.RunQueryAsync(q =>
        q.Where(d => !unionId.HasValue || d.UnionId == unionId.Value).SumAsync(d => (decimal?)d.TotalAmount)) ?? 0m,

      TotalDeclarations = await unitOfWork.Declarations.RunQueryAsync(q =>
        q.CountAsync(d => !unionId.HasValue || d.UnionId == unionId.Value)),

      CompaniesCount = await unitOfWork.Companies.RunQueryAsync(q =>
        q.CountAsync(c => c.ParentCompanyId == null)),

      ActiveEmployees = await unitOfWork.Assignments.RunQueryAsync(q => q.CountAsync())
    };

    // Recaudación mensual (últimos 6 períodos), agregada en SQL
    stats.MonthlyCollections = await unitOfWork.Declarations.RunQueryAsync(q =>
        q.Where(d => !unionId.HasValue || d.UnionId == unionId.Value)
         .GroupBy(d => d.Period)
         .Select(g => new MonthlyCollectionDto { Period = g.Key, Amount = g.Sum(d => d.TotalAmount) })
         .OrderByDescending(m => m.Period)
         .Take(6)
         .ToListAsync());

    // Mapa de deudores: empresas con empleados asignados que no presentaron DDJJ en el
    // período actual (el mes anterior, que es el que se liquida). Se resuelve con tres
    // agregaciones y diccionarios, en vez de recorrer las listas completas por empresa.
    var now = DateTime.Now;
    var currentPeriod = now.AddMonths(-1).ToString("yyyy-MM");

    var presentedCompanyIds = (await unitOfWork.Declarations.RunQueryAsync(q =>
        q.Where(d => d.Period == currentPeriod).Select(d => d.CompanyId).Distinct().ToListAsync())).ToHashSet();

    var employeesByCompany = (await unitOfWork.Assignments.RunQueryAsync(q =>
        q.GroupBy(a => a.CompanyId)
         .Select(g => new { CompanyId = g.Key, Total = g.Count() })
         .ToListAsync())).ToDictionary(x => x.CompanyId, x => x.Total);

    var lastPeriodByCompany = (await unitOfWork.Declarations.RunQueryAsync(q =>
        q.Where(d => !unionId.HasValue || d.UnionId == unionId.Value)
         .GroupBy(d => d.CompanyId)
         .Select(g => new { CompanyId = g.Key, LastPeriod = g.Max(d => d.Period) })
         .ToListAsync())).ToDictionary(x => x.CompanyId, x => x.LastPeriod);

    var interestRates = await unitOfWork.InterestRates.RunQueryAsync(q =>
        q.OrderByDescending(r => r.EffectiveDate).ToListAsync());
    var currentRate = interestRates.FirstOrDefault(r => r.EffectiveDate <= now)?.MonthlyRate ?? 0.03m;

    var companies = await unitOfWork.Companies.RunQueryAsync(q =>
        q.Select(c => new { c.Id, c.BusinessName, c.BranchName }).ToListAsync());

    foreach (var company in companies)
    {
      if (presentedCompanyIds.Contains(company.Id)) continue;
      if (!employeesByCompany.TryGetValue(company.Id, out var empCount) || empCount == 0) continue;

      lastPeriodByCompany.TryGetValue(company.Id, out var lastPresent);

      // Cálculo aproximado de deuda e intereses (base mock, igual que antes)
      decimal estimatedBase = empCount * 5000; // Mock de base imponible promedio
      decimal calculatedInterest = 0;

      if (lastPresent != null)
      {
        var lastPeriodDate = DateTime.Parse(lastPresent + "-01");
        var monthDiff = ((now.Year - lastPeriodDate.Year) * 12) + now.Month - lastPeriodDate.Month - 1;

        if (monthDiff > 0)
        {
          calculatedInterest = estimatedBase * currentRate * monthDiff;
        }
      }

      stats.Debtors.Add(new CompanyDebtDto
      {
        CompanyId = company.Id,
        BusinessName = company.BusinessName,
        BranchName = company.BranchName,
        EmployeeCount = empCount,
        LastPeriodPresented = lastPresent ?? "Nunca",
        EstimatedDebt = estimatedBase,
        Interests = calculatedInterest
      });
    }

    // Auditoría reciente: el Take(50) se aplica en SQL. Antes se traía la tabla completa,
    // que incluye las columnas nvarchar(max) con los JSON de valores anteriores y nuevos.
    stats.RecentLogs = await unitOfWork.AuditLogs.RunQueryAsync(q =>
        q.OrderByDescending(l => l.Timestamp)
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
         })
         .ToListAsync());

    return stats;
  }
}
