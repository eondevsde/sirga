namespace SIRGA.Application.DTOs;

public class EmployeeDto
{
  public int Id { get; set; }
  public string Dni { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public DateTime JoinedDate { get; set; }
  public DateTime? LeftDate { get; set; }
  public bool IsActive { get; set; }
  public bool IsVoluntaryMember { get; set; } // De la tabla de asignación
  public int AssignmentId { get; set; } // Referencia a la asignación específica
  public bool MembershipResignationRequested { get; set; } // Renuncia a socio pendiente de aprobación
}

public class CreateEmployeeDto
{
  public string Dni { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public int CompanyId { get; set; }
  public int UnionId { get; set; }
  public bool IsVoluntaryMember { get; set; }
}
