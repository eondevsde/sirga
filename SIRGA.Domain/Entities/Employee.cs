using System.ComponentModel.DataAnnotations;
using SIRGA.Domain.Enums;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Maestro de Empleados.
/// </summary>
public class Employee
{
  public int Id { get; set; }

  [Required]
  [MaxLength(20)]
  public string Dni { get; set; } = null!;

  [Required]
  [MaxLength(200)]
  public string FullName { get; set; } = null!;

  public DateTime HireDate { get; set; } // Fecha de Alta

  public DateTime? TerminationDate { get; set; } // Fecha de Baja

  public EmployeeStatus Status { get; set; }

  public byte[]? BiometricData { get; set; }

  // Propiedades de Navegación
  public virtual ICollection<SindicataryAssignment> Assignments { get; set; } = new List<SindicataryAssignment>();
}
