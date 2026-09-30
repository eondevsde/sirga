using System;
using System.Collections.Generic;

namespace SIRGA.Application.DTOs;

public class CompanyBalanceDto
{
  public int CompanyId { get; set; }
  public string BusinessName { get; set; } = string.Empty;
  public decimal CurrentCredit { get; set; }
  public DateTime LastUpdate { get; set; }
  public List<BalanceMovementDto> Movements { get; set; } = new();
}

public class BalanceMovementDto
{
  public DateTime Date { get; set; }
  public decimal Amount { get; set; }
  public string Concept { get; set; } = string.Empty;
  public int? DeclarationId { get; set; }
}
