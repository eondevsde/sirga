namespace SIRGA.Domain.Entities;

public class PaymentTransaction
{
  public int Id { get; set; }
  public int DeclarationId { get; set; }
  public DeclarationRegistry? Declaration { get; set; }

  public decimal Amount { get; set; }
  public DateTime PaymentDate { get; set; }

  // Identificador externo (ej. ID de MercadoPago o Nro de Comprobante manual)
  public string? ExternalReference { get; set; }

  public string? PaymentMethod { get; set; } // "MercadoPago", "Transferencia", "Efectivo"

  public string? Notes { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
