namespace SIRGA.Domain.Enums;

/// <summary>
/// Serie de interés de la tabla de tasas.
///
/// El sistema anterior guardaba DOS series por período en la misma tabla (<c>dbo.Intereses</c>,
/// columna <c>TipoDeInteres</c>), numeradas 1 y 2, sin ninguna tabla que dijera cuál era cuál. La 1 es
/// siempre la más baja y la 2 la más alta, que es el criterio con el que ARCA publica el interés
/// resarcitorio y el punitorio.
///
/// Definido con el usuario: **el recargo del 2° vencimiento de la boleta usa la serie
/// <see cref="Resarcitorio"/>** (la más baja).
/// </summary>
public enum InterestType
{
  /// <summary>Interés resarcitorio: la serie MÁS BAJA del sistema anterior (tipo 1).</summary>
  Resarcitorio = 1,

  /// <summary>Interés punitorio: la serie MÁS ALTA del sistema anterior (tipo 2).</summary>
  Punitorio = 2
}
