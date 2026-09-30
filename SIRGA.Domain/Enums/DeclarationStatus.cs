namespace SIRGA.Domain.Enums;

public enum DeclarationStatus
{
  Draft = 1,       // Borrador
  Submitted = 2,   // Presentado (Listo para auditar)
  Approved = 3,    // Aprobado
  Rejected = 4,    // Rechazado (Requiere corrección)
  Paid = 5         // Pagado
}
