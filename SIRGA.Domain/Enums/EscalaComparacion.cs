namespace SIRGA.Domain.Enums;

/// <summary>
/// Resultado de comparar la remuneración que carga el estudio contra el mínimo de escala vigente.
/// Es lo que la pantalla de DDJJ le muestra al estudio: si está pagando debajo, igual o por encima
/// de la escala. <see cref="SinDatos"/> se usa cuando no hay escala para la categoría (sin eso, un
/// cero se leería como "está por encima", que es justo lo contrario de lo que pasa).
/// </summary>
public enum EscalaComparacion
{
  SinDatos = 0,
  Debajo = 1,
  Igual = 2,
  Superior = 3
}
