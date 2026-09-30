namespace SIRGA.Application.DTOs;

public class InspectorStatsDto
{
  public int InspectorId { get; set; }
  public string Name { get; set; } = null!;
  public int TotalVisits { get; set; }
  public int SuccessfulVisits { get; set; } // Atendido
  public int RefusedVisits { get; set; }     // No quiso atender
  public decimal CoverageRate => TotalVisits == 0 ? 0 : (decimal)SuccessfulVisits / TotalVisits * 100;
}

public class PrejudicialNoticeDto
{
  public int Id { get; set; }
  public string NoticeNumber { get; set; } = null!;
  public string CompanyName { get; set; } = null!;
  public string CompanyCuit { get; set; } = null!;
  public DateTime IssueDate { get; set; }
  public DateTime ExpirationDate { get; set; }
  public bool IsJudicial { get; set; }
  public List<AttachmentDto> Attachments { get; set; } = new();
}

public class AttachmentDto
{
  public int Id { get; set; }
  public string FileName { get; set; } = null!;
  public string FileType { get; set; } = null!;
  public string FileUrl { get; set; } = null!;
  public DateTime UploadDate { get; set; }
}

public class InspectorDto
{
  public int Id { get; set; }
  public string Name { get; set; } = null!;
  public string Dni { get; set; } = null!;
  public string? Phone { get; set; }
  public bool IsActive { get; set; }
}

public class VisitRegistrationDto
{
  public int InspectionActId { get; set; }
  public int InspectorId { get; set; }
  public string Status { get; set; } = null!;
  public string? Observations { get; set; }
  public string DocumentNumber { get; set; } = null!;
}

public class VisitDto
{
  public int Id { get; set; }
  public string InspectorName { get; set; } = null!;
  public int VisitNumber { get; set; }
  public DateTime VisitDate { get; set; }
  public string Status { get; set; } = null!;
  public string DocumentNumber { get; set; } = null!;
  public string? Observations { get; set; }
}
