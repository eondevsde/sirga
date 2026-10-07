namespace SIRGA.Domain.Enums;

public enum DeclarationStatus
{
  Draft = 1,        // Borrador (sin presentar todavía)
  Submitted = 2,    // Presentada (Lista para auditar)
  Approved = 3,     // Aprobada
  Rejected = 4,     // Rechazada (Requiere corrección)
  Paid = 5,         // Pagada
  Rectificada = 6   // Reemplazada por una versión posterior. NO se borra: queda como antecedente
                    // y no se puede aprobar ni cobrar (si no, se cobraría dos veces la misma DDJJ).
}
