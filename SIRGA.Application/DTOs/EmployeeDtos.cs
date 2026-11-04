using SIRGA.Domain.Enums;

namespace SIRGA.Application.DTOs;

public class EmployeeDto
{
  public int Id { get; set; }

  /// <summary>CUIL del empleado (en SIRGA, <c>Employees.Dni</c> guarda el CUIL).</summary>
  public string Dni { get; set; } = string.Empty;

  /// <summary>Apellido y nombre juntos. Se mantiene por compatibilidad; la ficha usa los separados.</summary>
  public string FullName { get; set; } = string.Empty;

  // ---- Datos del sistema anterior (maesoc) ----------------------------------------------
  public string? LastName { get; set; }
  public string? FirstName { get; set; }
  public DateTime? BirthDate { get; set; }

  /// <summary>"M" o "F", tal como viene del sistema anterior.</summary>
  public string? Sex { get; set; }

  public string? DocumentTypeCode { get; set; }
  public string? DocumentNumber { get; set; }
  public string? Phone { get; set; }
  public string? Mobile { get; set; }
  public string? Email { get; set; }
  public string? WorkEmail { get; set; }
  public string? Address { get; set; }
  public string? AddressNumber { get; set; }
  public string? Floor { get; set; }
  public string? Apartment { get; set; }
  public string? Neighborhood { get; set; }
  public string? ZipCode { get; set; }
  public string? AffiliationNumber { get; set; }
  public string? CardNumber { get; set; }

  // ---- Actividad, obra social, legajo y condiciones (datos del sistema anterior) ---------
  /// <summary>Actividad en ESTA empresa (código del catálogo). No reemplaza a la categoría.</summary>
  public string? ActivityCode { get; set; }
  public string? ActivityName { get; set; }

  /// <summary>Número de legajo del empleado en esta empresa.</summary>
  public string? FileNumber { get; set; }

  public string? HealthInsuranceCode { get; set; }
  public string? HealthInsuranceName { get; set; }
  public bool IsRetired { get; set; }
  public bool HasFamilyCharge { get; set; }

  /// <summary>
  /// "APELLIDO, Nombre" si están separados; si todavía no se migraron, el nombre completo tal cual.
  /// Evita que la pantalla muestre dos columnas vacías mientras el respaldo no se aplicó.
  /// </summary>
  public string NombreParaMostrar =>
      !string.IsNullOrWhiteSpace(LastName) || !string.IsNullOrWhiteSpace(FirstName)
          ? $"{LastName?.Trim()}, {FirstName?.Trim()}".Trim(',', ' ')
          : FullName;

  public string DomicilioParaMostrar
  {
    get
    {
      if (string.IsNullOrWhiteSpace(Address)) return "—";
      var numero = string.IsNullOrWhiteSpace(AddressNumber) ? "" : $" {AddressNumber}";
      var piso = string.IsNullOrWhiteSpace(Floor) ? "" : $", piso {Floor}";
      var dpto = string.IsNullOrWhiteSpace(Apartment) ? "" : $" dto. {Apartment}";
      return $"{Address}{numero}{piso}{dpto}";
    }
  }

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

  /// <summary>Nombre de la categoría, resuelto para mostrar (si se pudo).</summary>
  public string? CategoryName { get; set; }

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
  /// <summary>CUIL (11 dígitos). Es la llave del empleado en SIRGA.</summary>
  public string Dni { get; set; } = string.Empty;

  public string LastName { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;

  /// <summary>Se completa solo con Apellido + Nombre; no lo pide la pantalla.</summary>
  public string FullName { get; set; } = string.Empty;

  public DateTime? BirthDate { get; set; }

  /// <summary>"M" o "F".</summary>
  public string? Sex { get; set; }

  public string? DocumentTypeCode { get; set; }
  public string? DocumentNumber { get; set; }
  public string? Phone { get; set; }
  public string? Mobile { get; set; }
  public string? Email { get; set; }
  public string? Address { get; set; }

  /// <summary>Categoría del convenio (código de la rama). Sin categoría la DDJJ liquida el mínimo en cero.</summary>
  public string? CategoryCode { get; set; }

  /// <summary>Jornada en esta empresa: Parcial = 50% del mínimo de escala.</summary>
  public JornadaType Jornada { get; set; } = JornadaType.Completa;

  /// <summary>Actividad en esta empresa (código del catálogo).</summary>
  public string? ActivityCode { get; set; }

  /// <summary>Número de legajo en esta empresa.</summary>
  public string? FileNumber { get; set; }

  public string? HealthInsuranceCode { get; set; }
  public bool IsRetired { get; set; }
  public bool HasFamilyCharge { get; set; }

  /// <summary>
  /// Fecha de ingreso del empleado A LA EMPRESA (Assignments.CreatedAt). Si no se indica, se usa la
  /// fecha del alta. Es de donde sale la ANTIGÜEDAD para el mínimo de escala, por eso se puede cargar
  /// a mano cuando la persona ingresó antes de que se la registre en el sistema.
  /// </summary>
  public DateTime? EntryDate { get; set; }

  public int CompanyId { get; set; }
  public int UnionId { get; set; }
  public bool IsVoluntaryMember { get; set; }
}
