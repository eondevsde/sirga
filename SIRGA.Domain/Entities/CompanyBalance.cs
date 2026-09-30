using System;

namespace SIRGA.Domain.Entities;

public class CompanyBalance
{
  public int Id { get; set; }
  public int CompanyId { get; set; }
  public Company? Company { get; set; }

  public decimal CreditAmount { get; set; } // Saldo a favor acumulado
  public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}

public class BalanceHistory
{
  public int Id { get; set; }
  public int CompanyId { get; set; }
  public int? DeclarationId { get; set; } // DDJJ que originó el movimiento
  public decimal Amount { get; set; } // Positivo (crédito ganado), Negativo (crédito usado)
  public string Concept { get; set; } = string.Empty; // Ej: "Rectificativa Periodo 2024-05"
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
