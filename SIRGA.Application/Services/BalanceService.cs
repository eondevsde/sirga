using Microsoft.Extensions.Logging;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

public class BalanceService(IUnitOfWork unitOfWork, ILogger<BalanceService> logger)
{
  public async Task<decimal> GetAvailableCreditAsync(int companyId)
  {
    var balance = (await unitOfWork.CompanyBalances.FindAsync(b => b.CompanyId == companyId)).FirstOrDefault();
    return balance?.CreditAmount ?? 0;
  }

  public async Task ProcessDeclarationBalanceAsync(int declarationId)
  {
    var declaration = await unitOfWork.Declarations.GetByIdAsync(declarationId);
    if (declaration == null) return;

    // Si es una rectificativa con saldo negativo (pagó de más)
    if (declaration.IsRectification && declaration.BalanceAmount < 0)
    {
      decimal creditToGenerate = Math.Abs(declaration.BalanceAmount);
      await AddCreditAsync(declaration.CompanyId, creditToGenerate, declarationId, $"Crédito por Rectificativa Periodo {declaration.Period}");
    }
  }

  public async Task AddCreditAsync(int companyId, decimal amount, int? declarationId, string concept)
  {
    var balance = (await unitOfWork.CompanyBalances.FindAsync(b => b.CompanyId == companyId)).FirstOrDefault();
    if (balance == null)
    {
      balance = new CompanyBalance { CompanyId = companyId, CreditAmount = 0 };
      await unitOfWork.CompanyBalances.AddAsync(balance);
    }

    balance.CreditAmount += amount;
    balance.LastUpdate = DateTime.UtcNow;

    await unitOfWork.BalanceHistories.AddAsync(new BalanceHistory
    {
      CompanyId = companyId,
      DeclarationId = declarationId,
      Amount = amount,
      Concept = concept
    });

    await unitOfWork.CompleteAsync();
    logger.LogInformation("Se agregaron {Amount} de crédito a Empresa {CompanyId}. Nuevo saldo: {Total}", amount, companyId, balance.CreditAmount);
  }

  public async Task<decimal> UseCreditAsync(int companyId, decimal amountNeeded, int declarationId)
  {
    var balance = (await unitOfWork.CompanyBalances.FindAsync(b => b.CompanyId == companyId)).FirstOrDefault();
    if (balance == null || balance.CreditAmount <= 0) return 0;

    decimal amountToUse = Math.Min(balance.CreditAmount, amountNeeded);

    balance.CreditAmount -= amountToUse;
    balance.LastUpdate = DateTime.UtcNow;

    await unitOfWork.BalanceHistories.AddAsync(new BalanceHistory
    {
      CompanyId = companyId,
      DeclarationId = declarationId,
      Amount = -amountToUse,
      Concept = $"Uso de saldo en DDJJ ID {declarationId}"
    });

    await unitOfWork.CompleteAsync();
    logger.LogInformation("Se usaron {Amount} de crédito para Empresa {CompanyId}. Saldo restante: {Total}", amountToUse, companyId, balance.CreditAmount);

    return amountToUse;
  }

  public async Task<CompanyBalanceDto?> GetFullStatementAsync(int companyId)
  {
    var company = await unitOfWork.Companies.GetByIdAsync(companyId);
    if (company == null) return null;

    var balance = (await unitOfWork.CompanyBalances.FindAsync(b => b.CompanyId == companyId)).FirstOrDefault();
    var history = (await unitOfWork.BalanceHistories.FindAsync(h => h.CompanyId == companyId))
                  .OrderByDescending(h => h.CreatedAt);

    var dto = new CompanyBalanceDto
    {
      CompanyId = companyId,
      BusinessName = company.BusinessName,
      CurrentCredit = balance?.CreditAmount ?? 0,
      LastUpdate = balance?.LastUpdate ?? DateTime.MinValue,
      Movements = history.Select(h => new BalanceMovementDto
      {
        Date = h.CreatedAt,
        Amount = h.Amount,
        Concept = h.Concept,
        DeclarationId = h.DeclarationId
      }).ToList()
    };

    return dto;
  }
}

