using System.ComponentModel.DataAnnotations;
using SIRGA.Domain.Enums;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Maestro de Empleados.
/// </summary>
/// <remarks>
/// El sistema anterior (<c>Sindicato.dbo.fotos_.maesoc</c>) guarda **49 columnas** por socio; SIRGA
/// sólo tenía 7 y por eso la ficha del empleado mostraba nada más que el nombre completo. Estos
/// campos son los que se migran de `maesoc` (ver `script_sirga_db_empleados_desde_maesoc.sql`):
/// identidad separada (apellido y nombre), documento, nacimiento, contacto y domicilio.
/// > **OJO**: `Dni` en SIRGA es el **CUIL** (`MAESOC_CUIL`), no el número de documento. El documento
/// > es otro dato y puede no ser el mismo número.
/// </remarks>
public class Employee
{
  public int Id { get; set; }

  /// <summary>CUIL del empleado (en el sistema anterior: <c>MAESOC_CUIL</c>). Es la llave del cruce.</summary>
  [Required]
  [MaxLength(20)]
  public string Dni { get; set; } = null!;

  /// <summary>
  /// Apellido y nombre como una sola cadena. Se mantiene porque media aplicación lo usa, pero la
  /// ficha muestra <see cref="LastName"/> y <see cref="FirstName"/> por separado cuando están.
  /// </summary>
  [Required]
  [MaxLength(200)]
  public string FullName { get; set; } = null!;

  /// <summary>Apellido (en el sistema anterior vienen SEPARADOS: <c>MAESOC_APELLIDO</c>).</summary>
  [MaxLength(100)]
  public string? LastName { get; set; }

  /// <summary>Nombre (origen <c>MAESOC_NOMBRE</c>).</summary>
  [MaxLength(100)]
  public string? FirstName { get; set; }

  /// <summary>Fecha de nacimiento (<c>MAESOC_FECHANAC</c>).</summary>
  public DateTime? BirthDate { get; set; }

  /// <summary>Sexo según el sistema anterior: <c>"M"</c> o <c>"F"</c>.</summary>
  [MaxLength(1)]
  public string? Sex { get; set; }

  /// <summary>Tipo de documento (<c>MAESOC_TIPODOC</c>), tal como viene del sistema anterior.</summary>
  [MaxLength(3)]
  public string? DocumentTypeCode { get; set; }

  /// <summary>Número de documento (<c>MAESOC_NRODOC</c>). NO es el CUIL: ver la nota de la clase.</summary>
  [MaxLength(20)]
  public string? DocumentNumber { get; set; }

  [MaxLength(50)]
  public string? Phone { get; set; }

  [MaxLength(50)]
  public string? Mobile { get; set; }

  [MaxLength(120)]
  public string? Email { get; set; }

  [MaxLength(120)]
  public string? WorkEmail { get; set; }

  [MaxLength(150)]
  public string? Address { get; set; }

  [MaxLength(20)]
  public string? AddressNumber { get; set; }

  [MaxLength(10)]
  public string? Floor { get; set; }

  [MaxLength(10)]
  public string? Apartment { get; set; }

  [MaxLength(100)]
  public string? Neighborhood { get; set; }

  [MaxLength(20)]
  public string? ZipCode { get; set; }

  /// <summary>Número de afiliado al sindicato (<c>MAESOC_NROAFIL</c>).</summary>
  [MaxLength(30)]
  public string? AffiliationNumber { get; set; }

  /// <summary>Número de carnet (<c>MAESOC_NROCARNET</c>).</summary>
  [MaxLength(30)]
  public string? CardNumber { get; set; }

  /// <summary>
  /// Obra social del empleado: código del catálogo (<see cref="HealthInsurance"/>, origen
  /// <c>MAESOC_CODOSM</c>). El catálogo tiene 297 obras sociales y el 100% de los socios tiene código.
  /// </summary>
  [MaxLength(20)]
  public string? HealthInsuranceCode { get; set; }

  /// <summary>
  /// Jubilado (<c>MAESOC_JUBIL</c>). En el sistema anterior 0 = no, 1 = sí (261 socios) y 2 = otro
  /// estado previsional (20): los dos últimos se toman como jubilado.
  /// </summary>
  public bool IsRetired { get; set; }

  /// <summary>Carga familiar (<c>MAESOC_CARGAFLIA</c>): en el origen es 0 o 1, o sea sí/no.</summary>
  public bool HasFamilyCharge { get; set; }

  public DateTime HireDate { get; set; } // Fecha de Alta

  public DateTime? TerminationDate { get; set; } // Fecha de Baja

  public EmployeeStatus Status { get; set; }

  public byte[]? BiometricData { get; set; }

  // Propiedades de Navegación
  public virtual ICollection<SindicataryAssignment> Assignments { get; set; } = new List<SindicataryAssignment>();
}
