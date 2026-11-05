using System.Globalization;
using Microsoft.Data.SqlClient;
using SIRGA.Application.Interfaces;

namespace SIRGA.Infrastructure.Legacy;

/// <summary>
/// Vuelca una DDJJ de SIRGA a las tablas del sistema anterior (`ddjjt` = cabecera, `ddjj` = detalle), que
/// es de donde **SIGES WEB** lee para saber si una empresa debe.
///
/// CUÁNDO: **en la presentación**. En SIRGA se confecciona la DDJJ, se presenta, y recién ahí se escribe
/// acá. Antes de presentarla no existe para SIGES WEB, y por eso **tampoco se puede emitir su boleta**.
///
/// LA FÓRMULA (verificada al centavo contra el legado real, ver `PLAN_CONCILIACION_PAGOS.md`):
///   `ddjjt.titem1` = 2% de TODOS los empleados   (NO sólo de los no socios: eso era un error)
///   `ddjjt.titem2` = 2% de los socios
///   `ddjj.impo + ddjj.impoaux` = la remuneración, y SIGES recalcula el aporte desde ahí ⇒ **tienen que
///   coincidir**: `titem1` es exactamente el 2% de la suma de `ddjj`.
///
/// QUÉ NO TOCA: `fpago` e `impban1` (son del cobro, no de la presentación), y `estado`/`medio*`/`trans*`
/// (SIGES WEB no los usa; su significado todavía no se conoce).
///
/// ES IDEMPOTENTE: si la fila ya existe para (CUIT, período, `rect`), la actualiza en vez de duplicarla.
/// </summary>
public class LegacyDeclarationWriter : ILegacyDeclarationWriter
{
  private readonly string _cadena;

  public LegacyDeclarationWriter(string cadena)
  {
    if (string.IsNullOrWhiteSpace(cadena))
    {
      throw new InvalidOperationException(
          "Falta la cadena de conexion 'Legacy' (base Sindicato). Sin ella la DDJJ no se vuelca y SIGES " +
          "WEB seguiria viendo a la empresa como deudora.");
    }

    _cadena = cadena;
  }

  public async Task<bool> EstaEnElLegadoAsync(string cuit, string periodo, CancellationToken ct = default)
  {
    var fecha = PrimerDiaDelPeriodo(periodo);

    await using var conexion = new SqlConnection(_cadena);
    await conexion.OpenAsync(ct);
    await using var comando = new SqlCommand(
        "SELECT COUNT(*) FROM ddjjt WHERE CUIT_STR = @cuit AND periodo = @periodo", conexion, null);
    comando.Parameters.AddWithValue("@cuit", cuit);
    comando.Parameters.AddWithValue("@periodo", fecha);

    return Convert.ToInt32(await comando.ExecuteScalarAsync(ct)) > 0;
  }

  public async Task<string> EscribirAsync(DeclaracionLegado declaracion, CancellationToken ct = default)
  {
    var fecha = PrimerDiaDelPeriodo(declaracion.Periodo);

    await using var conexion = new SqlConnection(_cadena);
    await conexion.OpenAsync(ct);
    await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(ct);

    try
    {
      var cabecera = await EscribirCabeceraAsync(conexion, transaccion, declaracion, fecha, ct);
      var detalles = await EscribirDetalleAsync(conexion, transaccion, declaracion, fecha, ct);

      // El empleado tiene que existir en el PADRÓN del sindicato (maesoc) y estar relacionado con la
      // empresa (socemp): `ddjj` no lleva el nombre y SIGES lo busca ahí. Se crea sólo lo que falta.
      var maestro = await AsegurarMaestroAsync(conexion, transaccion, declaracion, fecha, ct);

      await transaccion.CommitAsync(ct);

      var control = VerificarConsistencia(declaracion);
      return $"DDJJ {declaracion.Periodo} (rect {declaracion.Rect}) volcada al legado: {cabecera}, " +
             $"{detalles} empleado(s), {maestro}. titem1 = {declaracion.Titem1:N2}, titem2 = {declaracion.Titem2:N2}{control}";
    }
    catch
    {
      // Si algo falla no queda media DDJJ escrita: se revierte todo y el error sube, porque una DDJJ a
      // medias en el legado es peor que no haberla escrito.
      await transaccion.RollbackAsync(ct);
      throw;
    }
  }

  /// <summary>Control de la regla de consistencia: titem1 tiene que ser el 2% de la suma de la remuneración.</summary>
  private static string VerificarConsistencia(DeclaracionLegado declaracion)
  {
    var dosPorCiento = Math.Round(declaracion.Detalles.Sum(d => d.Remuneracion) * 0.02m, 2);
    return Math.Abs(dosPorCiento - declaracion.Titem1) <= 0.05m
        ? ""
        : $". ATENCION: titem1 no coincide con el 2% de la suma de las remuneraciones ({dosPorCiento:N2}): " +
          "SIGES calcula el aporte desde ddjj y leeria otro numero.";
  }

  /// <summary>
  /// Indica si la columna Id de esa tabla es IDENTITY. En PRODUCCIÓN `ddjj` y `ddjjt` la tienen como
  /// IDENTITY (las numera SQL solo), pero la copia de desarrollo no. Si es IDENTITY no se puede insertar
  /// el Id a mano: SQL responde error 544. Por eso la sentencia se arma según el caso.
  /// </summary>
  private static async Task<bool> IdEsIdentidadAsync(
      SqlConnection conexion, SqlTransaction? transaccion, string tabla, CancellationToken ct)
  {
    var sql = $"SELECT COLUMNPROPERTY(OBJECT_ID(N'dbo.{tabla}'), N'Id', 'IsIdentity');";
    await using var comando = new SqlCommand(sql, conexion, transaccion);
    var valor = await comando.ExecuteScalarAsync(ct);
    return valor is not null && valor != DBNull.Value && Convert.ToInt32(valor) == 1;
  }

  private static async Task<string> EscribirCabeceraAsync(
      SqlConnection conexion, SqlTransaction transaccion, DeclaracionLegado declaracion, DateTime fecha, CancellationToken ct)
  {
    // Idempotente: si ya existe la fila de ese (CUIT, periodo, rect) se actualiza, no se duplica.
    var sql = """
        IF EXISTS (SELECT 1 FROM ddjjt WHERE CUIT_STR = @cuit AND periodo = @periodo AND rect = @rect)
        BEGIN
            UPDATE ddjjt
            SET titem1 = @titem1, titem2 = @titem2, cuit = @cuitFloat,
                fmod = GETDATE(), usuario = @usuario
            WHERE CUIT_STR = @cuit AND periodo = @periodo AND rect = @rect;
            SELECT 'actualizada';
        END
        ELSE
        BEGIN
            INSERT INTO ddjjt (Id, periodo, cuit, CUIT_STR, rect, repite, fpres, titem1, titem2, fmod, usuario)
            VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM ddjjt WITH (UPDLOCK, HOLDLOCK)), @periodo, @cuitFloat, @cuit, @rect, 0, GETDATE(), @titem1, @titem2, GETDATE(), @usuario);
            SELECT 'creada';
        END
        """;

    // En producción Id es IDENTITY: no se puede insertar a mano (error 544).
    if (await IdEsIdentidadAsync(conexion, transaccion, "ddjjt", ct))
    {
      sql = sql
          .Replace("INSERT INTO ddjjt (Id, periodo", "INSERT INTO ddjjt (periodo")
          .Replace("VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM ddjjt WITH (UPDLOCK, HOLDLOCK)), @periodo", "VALUES (@periodo");
    }

    await using var comando = new SqlCommand(sql, conexion, transaccion);
    comando.Parameters.AddWithValue("@cuit", declaracion.Cuit);
    comando.Parameters.AddWithValue("@cuitFloat", (double)decimal.Parse(declaracion.Cuit, CultureInfo.InvariantCulture));
    comando.Parameters.AddWithValue("@periodo", fecha);
    comando.Parameters.AddWithValue("@rect", declaracion.Rect);
    comando.Parameters.AddWithValue("@titem1", (double)declaracion.Titem1);
    comando.Parameters.AddWithValue("@titem2", (double)declaracion.Titem2);
    comando.Parameters.AddWithValue("@usuario", declaracion.Usuario);

    return (string)(await comando.ExecuteScalarAsync(ct) ?? "?");
  }

  private static async Task<int> EscribirDetalleAsync(
      SqlConnection conexion, SqlTransaction transaccion, DeclaracionLegado declaracion, DateTime fecha, CancellationToken ct)
  {
    // `ddjj` exige las siete banderas de bit (no tienen default). `item1` = 1 (aporte de ley aplica) y
    // `item2` = 1 si es socio, que es lo que SIGES usa para separar empleados de socios.
    var sql = """
        IF EXISTS (SELECT 1 FROM ddjj WHERE CUIT_STR = @cuit AND CUIL_STR = @cuil AND periodo = @periodo AND rect = @rect)
        BEGIN
            UPDATE ddjj SET impo = @impo, impoaux = 0, item1 = 1, item2 = @esSocio
            WHERE CUIT_STR = @cuit AND CUIL_STR = @cuil AND periodo = @periodo AND rect = @rect;
        END
        ELSE
        BEGIN
            INSERT INTO ddjj (Id, periodo, cuite, cuil, CUIT_STR, CUIL_STR, rect, impo, impoaux,
                              item1, item2, item3, item4, cargaf, jub, jorp)
            VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM ddjj WITH (UPDLOCK, HOLDLOCK)), @periodo, @cuitFloat, @cuilFloat, @cuit, @cuil, @rect, @impo, 0,
                    1, @esSocio, 0, 0, 0, 0, 0);
        END
        """;

    // En producción Id es IDENTITY: no se puede insertar a mano (error 544).
    if (await IdEsIdentidadAsync(conexion, transaccion, "ddjj", ct))
    {
      sql = sql
          .Replace("INSERT INTO ddjj (Id, periodo", "INSERT INTO ddjj (periodo")
          .Replace("VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM ddjj WITH (UPDLOCK, HOLDLOCK)), @periodo", "VALUES (@periodo");
    }

    var escritos = 0;
    foreach (var detalle in declaracion.Detalles)
    {
      await using var comando = new SqlCommand(sql, conexion, transaccion);
      comando.Parameters.AddWithValue("@cuit", declaracion.Cuit);
      comando.Parameters.AddWithValue("@cuil", detalle.Cuil);
      comando.Parameters.AddWithValue("@cuitFloat", (double)decimal.Parse(declaracion.Cuit, CultureInfo.InvariantCulture));
      comando.Parameters.AddWithValue("@cuilFloat", (double)decimal.Parse(detalle.Cuil, CultureInfo.InvariantCulture));
      comando.Parameters.AddWithValue("@periodo", fecha);
      comando.Parameters.AddWithValue("@rect", declaracion.Rect);
      comando.Parameters.AddWithValue("@impo", (double)detalle.Remuneracion);
      comando.Parameters.AddWithValue("@esSocio", detalle.EsSocio);
      await comando.ExecuteNonQueryAsync(ct);
      escritos++;
    }

    return escritos;
  }

  /// <summary>
  /// Deja al empleado en el PADRÓN del sindicato: `fotos_.maesoc` (el socio) y `fotos_.socemp` (su
  /// relación con la empresa). **Sólo inserta lo que falta**: el padrón es la tabla que el sindicato
  /// mantiene a mano desde hace años y no se puede pisar.
  ///
  /// POR QUÉ ACÁ Y NO EN EL ALTA: el empleado "existe" para el sindicato recién cuando se presenta la DDJJ
  /// (decisión del usuario, 2026-11-04). Y va en la MISMA transacción del volcado: si algo falla no queda
  /// un socio creado por una DDJJ que no se presentó.
  /// </summary>
  private static async Task<string> AsegurarMaestroAsync(
      SqlConnection conexion, SqlTransaction transaccion, DeclaracionLegado declaracion, DateTime fecha, CancellationToken ct)
  {
    var sociosNuevos = 0;
    var sociosActualizados = 0;
    var relacionesNuevas = 0;

    if (!decimal.TryParse(declaracion.Cuit, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cuite))
      return "sin alta en el padrón (CUIT inválido)";

    foreach (var detalle in declaracion.Detalles)
    {
      if (!decimal.TryParse(detalle.Cuil, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cuil))
        continue;

      var socio = ASocio(detalle);

      if (!await LegacyPadron.ExisteAsync(conexion, transaccion, cuil, ct))
      {
        await LegacyPadron.InsertarAsync(conexion, transaccion, socio, cuil, ct);
        sociosNuevos++;
      }
      else if (await LegacyPadron.ActualizarAsync(conexion, transaccion, socio, cuil, incluirContacto: false, ct) > 0)
      {
        // El socio ya estaba: se espejan los datos que SIRGA maneja (decisión del usuario). Los campos
        // que SIRGA no tiene no se tocan y los vacíos tampoco pisan lo que haya en el padrón.
        sociosActualizados++;
      }

      if (!await LegacyPadron.ExisteRelacionAsync(conexion, transaccion, cuil, cuite, ct))
      {
        await LegacyPadron.InsertarRelacionAsync(conexion, transaccion, socio, cuil, cuite, fecha, ct);
        relacionesNuevas++;
      }
    }

    if (sociosNuevos == 0 && sociosActualizados == 0 && relacionesNuevas == 0)
      return "padrón sin cambios (ya estaba todo igual)";

    var partes = new List<string>();
    if (sociosNuevos > 0) partes.Add($"{sociosNuevos} socio(s) nuevo(s)");
    if (sociosActualizados > 0) partes.Add($"{sociosActualizados} socio(s) con datos actualizados");
    if (relacionesNuevas > 0) partes.Add($"{relacionesNuevas} relación(es) socio-empresa");

    return string.Join(" y ", partes);
  }

  /// <summary>Traduce el detalle de la DDJJ a los datos del padrón (el SQL vive en <see cref="LegacyPadron"/>).</summary>
  private static SocioLegado ASocio(DetalleLegado detalle) => new()
  {
    Cuil = detalle.Cuil,
    Apellido = detalle.Apellido,
    Nombre = detalle.Nombre,
    Documento = detalle.Documento,
    TipoDocumento = detalle.TipoDocumento,
    FechaNacimiento = detalle.FechaNacimiento,
    Sexo = detalle.Sexo,
    Telefono = detalle.Telefono,
    Celular = detalle.Celular,
    Email = detalle.Email,
    EmailLaboral = detalle.EmailLaboral,
    Calle = detalle.Calle,
    Numero = detalle.Numero,
    Piso = detalle.Piso,
    Depto = detalle.Depto,
    Barrio = detalle.Barrio,
    CodigoPostal = detalle.CodigoPostal,
    Categoria = detalle.Categoria,
    Actividad = detalle.Actividad,
    ObraSocial = detalle.ObraSocial,
    Jubilado = detalle.Jubilado,
    CargaFamiliar = detalle.CargaFamiliar,
    FechaIngreso = detalle.FechaIngreso,
    Legajo = detalle.Legajo
  };
  /// <summary>`YYYY-MM` al primer día del mes, que es como el legado guarda el período (`datetime`).</summary>
  private static DateTime PrimerDiaDelPeriodo(string periodo)
  {
    if (DateTime.TryParseExact(periodo + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var fecha))
    {
      return fecha;
    }

    throw new ArgumentException($"El periodo '{periodo}' no tiene el formato YYYY-MM.", nameof(periodo));
  }
}
