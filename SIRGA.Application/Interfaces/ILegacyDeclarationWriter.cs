namespace SIRGA.Application.Interfaces;

/// <summary>Un empleado tal como va a la tabla `ddjj` del legado.</summary>
/// <param name="Cuil">CUIL del empleado (11 dígitos, sin guiones).</param>
/// <param name="Remuneracion">Va a `ddjj.impo`: SIGES calcula el aporte como (impo + impoaux) × 2%.</param>
/// <param name="EsSocio">Va a `ddjj.item2`: 1 si el empleado es socio (tiene cuota sindical).</param>
public sealed record DetalleLegado(string Cuil, decimal Remuneracion, bool EsSocio);

/// <summary>
/// Una DDJJ completa, lista para volcar al legado (`ddjjt` + `ddjj`), que es lo que lee SIGES WEB para
/// saber si una empresa debe. **No incluye el pago**: `fpago` e `impban1` se llenan cuando se cobra.
/// </summary>
/// <param name="Periodo">`YYYY-MM`, como lo usa SIRGA.</param>
/// <param name="Rect">0 en la primera versión; +1 en cada rectificación (el legado versiona por acá).</param>
/// <param name="Titem1">2% de TODOS los empleados (no sólo los no socios).</param>
/// <param name="Titem2">2% de los socios.</param>
public sealed record DeclaracionLegado(
    string Cuit,
    string Periodo,
    int Rect,
    decimal Titem1,
    decimal Titem2,
    IReadOnlyList<DetalleLegado> Detalles,
    string Usuario);

/// <summary>
/// Escribe una DDJJ en la base del sistema anterior. Existe como interfaz para que la capa de aplicación
/// pueda pedir el volcado **sin conocer la base del legado**: la implementación vive en Infrastructure,
/// que es la única que puede ver las dos bases.
/// </summary>
public interface ILegacyDeclarationWriter
{
  /// <summary>Escribe (o actualiza) la DDJJ en el legado y devuelve un detalle de lo que hizo.</summary>
  Task<string> EscribirAsync(DeclaracionLegado declaracion, CancellationToken ct = default);

  /// <summary>Dice si la DDJJ ya está reflejada en el legado. Es el control que pide el usuario antes de emitir una boleta.</summary>
  Task<bool> EstaEnElLegadoAsync(string cuit, string periodo, CancellationToken ct = default);
}
