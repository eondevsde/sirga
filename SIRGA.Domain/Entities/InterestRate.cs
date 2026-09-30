using System;

namespace SIRGA.Domain.Entities;

public class InterestRate
{
  public int Id { get; set; }
  public DateTime EffectiveDate { get; set; }
  public decimal MonthlyRate { get; set; }
  public string Description { get; set; } = string.Empty;
}
