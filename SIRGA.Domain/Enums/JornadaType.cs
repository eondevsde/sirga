namespace SIRGA.Domain.Enums;

/// <summary>
/// Jornada del empleado en la empresa.
///
/// El sistema anterior NO tenía este dato (no hay ninguna columna de jornada en las tablas del
/// gremio), así que todos los registros migrados quedan en <see cref="Completa"/>.
///
/// Definido con el usuario: una jornada <see cref="Parcial"/> liquida el 50% del mínimo de escala.
/// El prorrateo se aplica al mínimo COMPLETO (básico + antigüedad + acuerdos no remunerativos).
/// </summary>
public enum JornadaType
{
  Completa = 1,
  Parcial = 2
}
