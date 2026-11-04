namespace SIRGA.Application.Interfaces;

/// <summary>
/// Un empleado tal como va a la tabla `ddjj` del legado, más los datos con los que se lo crea en el
/// **padrón de socios** (`fotos_.maesoc` y `fotos_.socemp`) cuando todavía no existe.
///
/// POR QUÉ: la tabla `ddjj` **no lleva el nombre**. SIGES WEB arma el nombre del empleado buscándolo en
/// `maesoc` por CUIL, y si no está muestra el CUIL con el nombre vacío. Como SIRGA es ahora el que da de
/// alta a los empleados, también tiene que darlos de alta en el padrón del sindicato: si no, un empleado
/// nuevo existe para SIRGA pero no para SIGES.
///
/// Los datos que SIRGA no tiene (estado civil, nacionalidad, grupo sanguíneo, etc.) van **vacíos**:
/// no se inventan valores en el padrón del sindicato.
/// </summary>
/// <param name="Cuil">CUIL del empleado (11 dígitos, sin guiones).</param>
/// <param name="Remuneracion">Va a `ddjj.impo`: SIGES calcula el aporte como (impo + impoaux) × 2%.</param>
/// <param name="EsSocio">Va a `ddjj.item2`: 1 si el empleado es socio (tiene cuota sindical).</param>
public sealed record DetalleLegado(string Cuil, decimal Remuneracion, bool EsSocio)
{
  public string Apellido { get; init; } = string.Empty;
  public string Nombre { get; init; } = string.Empty;
  public string? Documento { get; init; }
  public string? TipoDocumento { get; init; }
  public DateTime? FechaNacimiento { get; init; }
  public string? Sexo { get; init; }
  public string? Telefono { get; init; }
  public string? Celular { get; init; }
  public string? Email { get; init; }
  public string? EmailLaboral { get; init; }
  public string? Calle { get; init; }
  public string? Numero { get; init; }
  public string? Piso { get; init; }
  public string? Depto { get; init; }
  public string? Barrio { get; init; }
  public string? CodigoPostal { get; init; }
  public string? Categoria { get; init; }
  public string? Actividad { get; init; }
  public string? ObraSocial { get; init; }
  public bool Jubilado { get; init; }
  public bool CargaFamiliar { get; init; }
  public DateTime? FechaIngreso { get; init; }
  public string? Legajo { get; init; }
}

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
