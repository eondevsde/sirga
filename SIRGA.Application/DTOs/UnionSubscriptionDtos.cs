using SIRGA.Application.DTOs;

namespace SIRGA.Application.DTOs;

public class UnionSubscriptionDto
{
  public int Id { get; set; }
  public int UnionId { get; set; }
  public string UnionName { get; set; } = null!;
  public decimal MonthlyFee { get; set; }
  public DateTime NextBillingDate { get; set; }
  public bool IsServiceActive { get; set; }
}

public class UnionSubscriptionPaymentDto
{
  public int Id { get; set; }
  public int UnionSubscriptionId { get; set; }
  public DateTime PaymentDate { get; set; }
  public decimal Amount { get; set; }
  public string PaymentMethod { get; set; } = null!;
  public string? ReceiptNumber { get; set; }
  public string? Notes { get; set; }
}
