namespace SIRGA.Application.DTOs;

public class MembershipResignationRequestDto
{
  public int AssignmentId { get; set; }
  public string Reason { get; set; } = "";
}

public class MembershipResignationResolveDto
{
  public int AssignmentId { get; set; }
  public bool Approve { get; set; }
  public string Comment { get; set; } = "";
}

public class MembershipResignationDto
{
  public int AssignmentId { get; set; }
  public int EmployeeId { get; set; }
  public string EmployeeName { get; set; } = "";
  public string Dni { get; set; } = "";
  public int CompanyId { get; set; }
  public string CompanyBusinessName { get; set; } = "";
  public string CompanyCuit { get; set; } = "";
  public int UnionId { get; set; }
  public string Reason { get; set; } = "";
  public DateTime RequestedAt { get; set; }
}