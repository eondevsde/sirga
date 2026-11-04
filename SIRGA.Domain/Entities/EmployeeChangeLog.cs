using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Bitácora de cambios de la ficha de un empleado: **un renglón por campo modificado**, con el valor
/// anterior, el nuevo, quién lo hizo, desde dónde y cuándo. Es la base de la auditoría y el seguimiento que
/// pidió el usuario (2026-11-04): "que sea lo más completo posible".
///
/// **Por qué una tabla propia y no `AuditLogs`**: `AuditLogs` registra ACCIONES ("aprobó la DDJJ 123"), no
/// cambios campo por campo con el valor anterior. Para auditar una ficha hace falta el detalle.
///
/// **Se escribe desde el servicio**, no desde la pantalla: si mañana otro camino modifica un empleado, el
/// registro sale igual.
/// </summary>
public class EmployeeChangeLog
{
  public int Id { get; set; }

  public int EmployeeId { get; set; }
  public virtual Employee? Employee { get; set; }

  /// <summary>Asignación (empleado-empresa) modificada, cuando el campo es de la empresa (categoría, jornada,
  /// legajo, fecha de ingreso). Null para los campos de la persona.</summary>
  public int? AssignmentId { get; set; }

  /// <summary>Nombre del campo tal como se llama en el modelo (ej. <c>Mobile</c>, <c>CategoryCode</c>).</summary>
  [Required]
  [MaxLength(50)]
  public string Campo { get; set; } = null!;

  [MaxLength(500)]
  public string? ValorAnterior { get; set; }

  [MaxLength(500)]
  public string? ValorNuevo { get; set; }

  /// <summary>De dónde vino el cambio: <c>SIRGA</c> (una persona en la pantalla), <c>SIGES</c> (padrón del
  /// sindicato), <c>SISTEMA</c> (alta automática, migración, sincronización).</summary>
  [Required]
  [MaxLength(30)]
  public string Origen { get; set; } = "SIRGA";

  /// <summary>Usuario que hizo el cambio, cuando se conoce.</summary>
  [MaxLength(200)]
  public string? Usuario { get; set; }

  public DateTime Fecha { get; set; } = DateTime.Now;

  /// <summary>Contexto extra (por ejemplo el período de la DDJJ o el motivo de la sincronización).</summary>
  [MaxLength(300)]
  public string? Detalle { get; set; }
}
