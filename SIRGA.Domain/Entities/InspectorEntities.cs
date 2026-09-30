using System;
using System.Collections.Generic;

namespace SIRGA.Domain.Entities;

public class Inspector
{
  public int Id { get; set; }
  public string Name { get; set; } = null!;
  public string Dni { get; set; } = null!;
  public string? Phone { get; set; }
  public bool IsActive { get; set; } = true;

  // Relación con visitas
  public virtual ICollection<InspectionVisit> Visits { get; set; } = new List<InspectionVisit>();
}

public class InspectionVisit
{
  public int Id { get; set; }
  public int InspectionActId { get; set; }
  public virtual InspectionAct InspectionAct { get; set; } = null!;

  public int InspectorId { get; set; }
  public virtual Inspector Inspector { get; set; } = null!;

  public int VisitNumber { get; set; } // 1, 2 o 3
  public DateTime VisitDate { get; set; }
  public string Status { get; set; } = null!; // "No quiso atender", "Atendió - No puede pagar", etc.
  public string? Observations { get; set; }
  public string DocumentNumber { get; set; } = null!; // Número de constancia emitida

  public bool DebtorSigned { get; set; }
}

public class PrejudicialNotice
{
  public int Id { get; set; }
  public int InspectionActId { get; set; }
  public virtual InspectionAct InspectionAct { get; set; } = null!;

  public string NoticeNumber { get; set; } = null!; // PRE-00001
  public DateTime IssueDate { get; set; }
  public DateTime ExpirationDate { get; set; } // Fin de tiempo de gracia
  public bool WasNotified { get; set; }
  public string? Observations { get; set; }

  public bool IsJudicial { get; set; } // Si pasó a instancia judicial

  // Archivos adjuntos (Demanda, Oficio, etc)
  public virtual ICollection<NoticeAttachment> Attachments { get; set; } = new List<NoticeAttachment>();
}

public class NoticeAttachment
{
  public int Id { get; set; }
  public int PrejudicialNoticeId { get; set; }
  public string FileName { get; set; } = null!;
  public string FileType { get; set; } = null!; // "Demanda", "Oficio", "Prueba"
  public string FileUrl { get; set; } = null!; // Path storage
  public DateTime UploadDate { get; set; }
}
