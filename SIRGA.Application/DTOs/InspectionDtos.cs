using SIRGA.Domain.Entities;

namespace SIRGA.Application.DTOs;

public class InspectionActResponseDto
{
  public int Id { get; set; }
  public string ActNumber { get; set; } = null!;
  public string CompanyName { get; set; } = null!;
  public string CompanyCuit { get; set; } = null!;
  public string CompanyAddress { get; set; } = null!;
  public string UnionName { get; set; } = null!;
  public string UnionAddress { get; set; } = "ENTRE RIOS 235 - SANTIAGO DEL ESTERO";
  public string UnionPhone { get; set; } = "3856178125";
  public DateTime IssueDate { get; set; }
  public string PeriodRange { get; set; } = null!;
  public decimal EstimatedDebt { get; set; }
  public decimal InterestAmount { get; set; }
  public decimal TotalAmount { get; set; }
  public string? Observations { get; set; }
  public List<InspectionActPeriodDto> Periods { get; set; } = new();
}

public class InspectionActPeriodDto
{
  public string Period { get; set; } = null!;
  public decimal BaseAmount { get; set; }
  public decimal CalculatedInterest { get; set; }
  public decimal Total => BaseAmount + CalculatedInterest;
}
