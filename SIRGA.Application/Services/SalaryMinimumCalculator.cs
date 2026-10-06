using SIRGA.Application.DTOs;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Application.Services;

/// <summary>
/// Cálculo del MÍNIMO DE ESCALA: el sueldo que el empleado debería cobrar según la escala vigente,
/// su antigüedad y su jornada. Es un valor de REFERENCIA para mostrarle al estudio si el recibo de
/// sueldo está debajo, igual o por encima de la escala. No se persiste ni reemplaza la remuneración
/// que carga el estudio.
///
/// ESTA ES LA ÚNICA COPIA DE LA FÓRMULA, a propósito. Antes estaba escrita dos veces —una en la
/// pantalla y otra en DeclarationService— con criterios distintos: la pantalla usaba sólo el básico
/// y el servicio contaba la antigüedad desde Employees.HireDate. Eso hacía que la pantalla mostrara
/// un número y la base guardara otro.
///
/// Definido con el usuario:
///   antigüedad = básico × (AntiguedadPercentage / 100) × años cumplidos desde el ingreso a la empresa
///   mínimo     = (básico + antigüedad + NR1 + NR2) × factor de jornada
/// con NR1/NR2 (acuerdos no remunerativos) INCLUIDOS en el mínimo, y jornada Parcial = 50% del
/// mínimo completo (también los NR se prorratean).
/// </summary>
public static class SalaryMinimumCalculator
{
  /// <summary>Factor de prorrateo de una jornada parcial (definido con el usuario: la mitad).</summary>
  public const decimal FactorJornadaParcial = 0.5m;

  /// <summary>
  /// Tolerancia para considerar que la remuneración "iguala" la escala. Sin esto, la comparación
  /// exigiría coincidir hasta el centavo y en la práctica nunca daría "igual".
  /// </summary>
  public const decimal ToleranciaIgualdad = 1m;

  public static decimal FactorJornada(JornadaType jornada) =>
      jornada == JornadaType.Parcial ? FactorJornadaParcial : 1m;

  /// <summary>
  /// Años de antigüedad cumplidos a la fecha de corte (el 1º del período declarado), contados desde
  /// la fecha de ingreso del empleado A ESA EMPRESA (<c>Assignments.CreatedAt</c>, que viene de
  /// <c>SOCEMP_FECHAING</c>). Se cuentan años cumplidos: si todavía no llegó el aniversario en el mes
  /// del período, ese año no cuenta.
  /// </summary>
  public static int AniosAntiguedad(DateTime fechaIngreso, DateTime fechaCorte)
  {
    if (fechaIngreso == default) return 0;

    int anios = fechaCorte.Year - fechaIngreso.Year;
    if (fechaCorte.Month < fechaIngreso.Month ||
        (fechaCorte.Month == fechaIngreso.Month && fechaCorte.Day < fechaIngreso.Day))
    {
      anios--;
    }
    return anios < 0 ? 0 : anios;
  }

  /// <summary>Importe de antigüedad sobre el básico de la escala.</summary>
  public static decimal Antiguedad(decimal basico, decimal antiguedadPorcentaje, int anios) =>
      Redondear(basico * (antiguedadPorcentaje / 100m) * anios);

  /// <summary>Mínimo de escala sin prorratear por jornada (básico + antigüedad + NR1 + NR2).</summary>
  public static decimal MinimoSinProrratear(decimal basico, decimal antiguedadPorcentaje, decimal nr1, decimal nr2, int anios) =>
      Redondear(basico + Antiguedad(basico, antiguedadPorcentaje, anios) + nr1 + nr2);

  /// <summary>Mínimo de escala final: incluye NR y se prorratea por jornada.</summary>
  public static decimal Minimo(
      decimal basico, decimal antiguedadPorcentaje, decimal nr1, decimal nr2,
      DateTime fechaIngreso, DateTime fechaCorte, JornadaType jornada)
  {
    int anios = AniosAntiguedad(fechaIngreso, fechaCorte);
    return Redondear(MinimoSinProrratear(basico, antiguedadPorcentaje, nr1, nr2, anios) * FactorJornada(jornada));
  }

  public static decimal Minimo(
      SalaryScale scale, DateTime fechaIngreso, DateTime fechaCorte, JornadaType jornada) =>
      Minimo(scale.BasicAmount, scale.AntiguedadPercentage, scale.AgreementNR1, scale.AgreementNR2,
             fechaIngreso, fechaCorte, jornada);

  public static decimal Minimo(
      SalaryScaleDto scale, DateTime fechaIngreso, DateTime fechaCorte, JornadaType jornada) =>
      Minimo(scale.BasicAmount, scale.AntiguedadPercentage, scale.AgreementNR1, scale.AgreementNR2,
             fechaIngreso, fechaCorte, jornada);

  /// <summary>
  /// Compara la remuneración del recibo contra el mínimo de escala. Devuelve <see cref="EscalaComparacion.SinDatos"/>
  /// cuando no hay mínimo (categoría sin escala): un cero NO significa que el sueldo esté bien.
  /// </summary>
  public static EscalaComparacion Comparar(decimal remuneracion, decimal minimo)
  {
    if (minimo <= 0) return EscalaComparacion.SinDatos;
    if (remuneracion < minimo - ToleranciaIgualdad) return EscalaComparacion.Debajo;
    if (remuneracion > minimo + ToleranciaIgualdad) return EscalaComparacion.Superior;
    return EscalaComparacion.Igual;
  }

  /// <summary>El dinero se redondea a 2 decimales "hacia afuera" (como lo hace la práctica contable), no con el redondeo bancario por defecto de .NET.</summary>
  private static decimal Redondear(decimal valor) =>
      Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
