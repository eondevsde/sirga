using SIRGA.Domain.Enums;

namespace SIRGA.Application.DTOs;

public class EmployeeDto
{
  public int Id { get; set; }
  public string Dni { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;

  /// <summary>Fecha de alta del empleado (Employees.HireDate). NO es la fecha de ingreso a la empresa.</summary>
  public DateTime JoinedDate { get; set; }

  public DateTime? LeftDate { get; set; }
  public bool IsActive { get; set; }
  public bool IsVoluntaryMember { get; set; } // De la tabla de asignación
  public int AssignmentId { get; set; } // Referencia a la asignación específica
  public bool MembershipResignationRequested { get; set; } // Renuncia a socio pendiente de aprobación

  /// <summary>
  /// Código de categoría de la asignación (Assignments.CategoryCode). Sin esto la pantalla de DDJJ no
  /// podía saber la categoría del empleado y usaba un valor fijo "MAESTRANZA-A" que no coincide con
  /// ningún código real, así que el mínimo de escala daba siempre cero.
  /// </summary>
  public string? CategoryCode { get; set; }

  /// <summary>
  /// Fecha de ingreso del empleado A ESTA EMPRESA (Assignments.CreatedAt, origen SOCEMP_FECHAING).
  /// Es la fecha desde la que se cuenta la antigüedad para el mínimo de escala.
  /// </summary>
  public DateTime AssignmentEntryDate { get; set; }

  /// <summary>Jornada en esta empresa: prorratea el mínimo de escala (Parcial = 50%).</summary>
  public JornadaType Jornada { get; set; } = JornadaType.Completa;
}

public class CreateEmployeeDto
{
  public string Dni { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public int CompanyId { get; set; }
  public int UnionId { get; set; }
  public bool IsVoluntaryMember { get; set; }
}
