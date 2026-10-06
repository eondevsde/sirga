using System;
using System.ComponentModel.DataAnnotations;
using SIRGA.Domain.Enums;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Tasa de interés vigente para un tramo de fechas.
///
/// OJO CON LAS UNIDADES: las tasas se guardan como **FRACCIÓN**, no como porcentaje: 0,0275 es 2,75%.
/// El sistema anterior (<c>Sindicato.dbo.Intereses</c>) las guarda al revés, en porcentaje, así que la
/// migración divide por 100. Confundir las unidades deja las tasas 100 veces más grandes.
///
/// El sistema anterior tenía DOS series por período (tipo 1 = la más baja, tipo 2 = la más alta) y por
/// eso la tabla tiene <see cref="TypeOfInterest"/>: sin ese campo, buscar "la tasa vigente" devolvería
/// dos filas para la misma fecha y elegiría una al azar.
/// </summary>
public class InterestRate
{
  public int Id { get; set; }

  /// <summary>Desde cuándo rige esta tasa.</summary>
  public DateTime EffectiveDate { get; set; }

  /// <summary>Tasa mensual, EN FRACCIÓN (0,0275 = 2,75%).</summary>
  public decimal MonthlyRate { get; set; }

  /// <summary>
  /// Tasa diaria, EN FRACCIÓN (0,00091667 = 0,091667%). En el sistema anterior era <c>Mensual / 30</c>
  /// exacto en las 70 filas. Se guarda el dato tal como venía, pero **el recargo del 2° vencimiento NO
  /// se calcula con esta columna**: prorrateando por 30 no reproduce la boleta real del banco (da
  /// $666,45 contra $644,96 reales). Ver <c>BoletaService.CalcularRecargo</c>.
  /// </summary>
  public decimal? DailyRate { get; set; }

  /// <summary>
  /// Hasta cuándo rige (inclusive). <c>null</c> = sigue vigente. En el sistema anterior el "sin fin" se
  /// guardaba como 2999-12-31 y la migración lo convierte a null.
  /// </summary>
  public DateTime? ValidTo { get; set; }

  /// <summary>Serie de la tasa: resarcitoria (la baja) o punitoria (la alta).</summary>
  public InterestType TypeOfInterest { get; set; } = InterestType.Resarcitorio;

  /// <summary>Norma que fija la tasa (por ejemplo "R (MEC) 823/2025").</summary>
  [MaxLength(100)]
  public string? Norm { get; set; }

  /// <summary>Descripción libre, para las tasas que se cargan a mano.</summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Si la tasa está vigente para el cálculo. Una tasa que no corresponde se **desactiva**, no se borra:
  /// es parte de la escala histórica y borrarla dejaría un hueco sin rastro. Los que calculan (la boleta
  /// y la mora de la DDJJ) sólo miran las activas.
  /// </summary>
  public bool IsActive { get; set; } = true;
}
