namespace SIRGA.Domain.Entities;

/// <summary>
/// Detalle de liquidación por empleado dentro de una DDJJ.
/// </summary>
public class DeclarationDetail
{
  public int Id { get; set; }

  public int RegistryId { get; set; }
  public virtual DeclarationRegistry Registry { get; set; } = null!;

  public int AssignmentId { get; set; }
  public virtual SindicataryAssignment Assignment { get; set; } = null!;

  public decimal BaseSalary { get; set; } // Remuneracion Base

  public decimal LawContribution { get; set; } // Aporte Ley (Calculado)

  public decimal MemberContribution { get; set; } // Aporte Socio (Calculado)

  public decimal TotalWithheld { get; set; } // Total Retenido

  /// <summary>
  /// De dónde salió <see cref="BaseSalary"/>: <c>true</c> si se calculó con la escala (básico + antigüedad,
  /// prorrateado por jornada) porque el estudio NO cargó una remuneración, y <c>false</c> si el importe se
  /// cargó a mano desde el recibo de sueldo.
  ///
  /// Se guarda para poder <b>pre-cargar una rectificativa</b> sin congelar los mínimos viejos: sólo se
  /// copian los importes que se habían cargado a mano, y los que salieron de la escala se recalculan solos
  /// con la escala vigente. Sin este dato no se pueden distinguir (BaseSalary tiene el resultado, no el
  /// origen).
  ///
  /// En las filas que vinieron del sistema anterior queda en <c>true</c> (la escala): es el valor seguro,
  /// porque así no se copia como si fuera un importe cargado a mano.
  /// </summary>
  public bool UsaEscala { get; set; } = true;
}
