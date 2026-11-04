namespace SIRGA.Application.Interfaces;

/// <summary>
/// Los datos de un socio tal como viven en el PADRÓN del sistema anterior (`fotos_.maesoc`), más lo que
/// necesita su relación con la empresa (`fotos_.socemp`).
///
/// **Por qué existe**: la tabla `ddjj` no lleva el nombre. SIGES WEB arma el nombre del empleado buscándolo
/// en `maesoc` por CUIL y, si no está, muestra el CUIL con el nombre vacío. Como SIRGA es ahora el que da de
/// alta a los empleados, también tiene que mantener el padrón.
///
/// Los datos que SIRGA no tiene (estado civil, nacionalidad, grupo sanguíneo, CBU, impuestos, nro. de
/// afiliado, carnet, fechas de alta/baja) **no se incluyen**: no se inventan ni se pisan.
/// </summary>
public sealed record SocioLegado
{
  /// <summary>CUIL (11 dígitos, sin guiones). Es la clave del padrón.</summary>
  public string Cuil { get; init; } = string.Empty;

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

  /// <summary>Fecha de ingreso a la empresa (va a `socemp.SOCEMP_FECHAING`).</summary>
  public DateTime? FechaIngreso { get; init; }

  /// <summary>Número de legajo en la empresa (va a `socemp.SOCEMP_NROLEG`).</summary>
  public string? Legajo { get; init; }
}

/// <summary>
/// Mantiene el padrón del sistema anterior al día **cuando el estudio guarda un empleado** (sincronización
/// instantánea, decisión del usuario del 2026-11-04), sin esperar a que se presente la DDJJ.
///
/// **Sólo actualiza socios que YA existen**: el alta del socio en el padrón sigue ocurriendo en la
/// presentación de la DDJJ, que es cuando el empleado pasa a existir para el sindicato.
/// </summary>
public interface ILegacyPadronWriter
{
  /// <summary>
  /// Espeja los datos del socio en el padrón si ya está cargado. Devuelve un detalle de lo que hizo, o
  /// <c>null</c> si no había nada que hacer (socio inexistente o datos iguales).
  /// </summary>
  /// <param name="socio">Datos del socio.</param>
  /// <param name="cuitEmpresa">CUIT de la empresa, para la relación socio-empresa (`socemp`).</param>
  Task<string?> ActualizarSiExisteAsync(SocioLegado socio, string cuitEmpresa, CancellationToken ct = default);

  /// <summary>
  /// Lee del padrón el **teléfono y el domicilio** del socio. Con la política del 2026-11-04 esos campos son
  /// de SIGES una vez que el socio existe, así que SIRGA los **trae** en la presentación de la DDJJ en lugar
  /// de pisarlos.
  /// </summary>
  Task<ContactoSocioLegado?> LeerContactoAsync(string cuil, CancellationToken ct = default);
}

/// <summary>Teléfono y domicilio tal como están en el padrón del sindicato.</summary>
public sealed record ContactoSocioLegado
{
  public string? Telefono { get; init; }
  public string? Celular { get; init; }
  public string? Calle { get; init; }
  public string? Numero { get; init; }
  public string? Piso { get; init; }
  public string? Depto { get; init; }
  public string? Barrio { get; init; }
  public string? CodigoPostal { get; init; }
}
