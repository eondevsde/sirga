using System;
using System.Collections.Generic;

namespace SIRGA.Application.DTOs;

public class PaymentPlanResponseDto
{
  public int Id { get; set; }
  public string ActNumber { get; set; } = null!;
  public string CompanyName { get; set; } = null!;
  public string CompanyCuit { get; set; } = null!;
  public DateTime StartDate { get; set; }
  public decimal TotalAmountToFinance { get; set; }
  public decimal DownPayment { get; set; }
  public int InstallmentsCount { get; set; }
  public decimal FinancingInterestRate { get; set; }
  public string Status { get; set; } = null!;
  public List<PaymentPlanInstallmentDto> Installments { get; set; } = new();
}

public class PaymentPlanInstallmentDto
{
  public int InstallmentNumber { get; set; }
  public DateTime DueDate { get; set; }
  public decimal Amount { get; set; }
  public string Status { get; set; } = null!;
}
