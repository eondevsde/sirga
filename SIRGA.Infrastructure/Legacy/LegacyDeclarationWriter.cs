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

  private static async Task<string> EscribirCabeceraAsync(
      SqlConnection conexion, SqlTransaction transaccion, DeclaracionLegado declaracion, DateTime fecha, CancellationToken ct)
  {
    // Idempotente: si ya existe la fila de ese (CUIT, periodo, rect) se actualiza, no se duplica.
    const string sql = """
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
    const string sql = """
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

      if (!await ExisteSocioAsync(conexion, transaccion, cuil, ct))
      {
        await InsertarSocioAsync(conexion, transaccion, detalle, cuil, ct);
        sociosNuevos++;
      }
      else if (await ActualizarSocioAsync(conexion, transaccion, detalle, cuil, ct) > 0)
      {
        // El socio ya estaba: se espejan los datos que SIRGA maneja (decisión del usuario). Los campos
        // que SIRGA no tiene no se tocan y los vacíos tampoco pisan lo que haya en el padrón.
        sociosActualizados++;
      }

      if (!await ExisteRelacionAsync(conexion, transaccion, cuil, cuite, ct))
      {
        await InsertarRelacionAsync(conexion, transaccion, detalle, cuil, cuite, fecha, ct);
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

  private static async Task<bool> ExisteSocioAsync(
      SqlConnection conexion, SqlTransaction transaccion, decimal cuil, CancellationToken ct)
  {
    await using var comando = new SqlCommand(
        "SELECT COUNT(*) FROM fotos_.maesoc WHERE MAESOC_CUIL = @cuil", conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    return Convert.ToInt32(await comando.ExecuteScalarAsync(ct)) > 0;
  }

  private static async Task<bool> ExisteRelacionAsync(
      SqlConnection conexion, SqlTransaction transaccion, decimal cuil, decimal cuite, CancellationToken ct)
  {
    await using var comando = new SqlCommand(
        "SELECT COUNT(*) FROM fotos_.socemp WHERE SOCEMP_CUIL = @cuil AND SOCEMP_CUITE = @cuite", conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    comando.Parameters.AddWithValue("@cuite", (double)cuite);
    return Convert.ToInt32(await comando.ExecuteScalarAsync(ct)) > 0;
  }

  /// <summary>
  /// Inserta el socio en el padrón copiando la FORMA de una fila real del legado: las fechas que no se
  /// conocen van con el centinela `1000-01-01` y los textos que no tenemos van vacíos. Nada inventado.
  /// </summary>
  private static async Task InsertarSocioAsync(
      SqlConnection conexion, SqlTransaction transaccion, DetalleLegado detalle, decimal cuil, CancellationToken ct)
  {
    const string sql = """
        INSERT INTO fotos_.maesoc (
            MAESOC_CUIL, MAESOC_CUIL_STR, MAESOC_APELLIDO, MAESOC_NOMBRE, APENOM,
            MAESOC_TEL, MAESOC_TELCEL, MAESOC_FECHANAC, MAESOC_FECHADOC, MAESOC_CARGAFLIA, MAESOC_JUBIL,
            MAESOC_TIPODOC, MAESOC_NRODOC, MAESOC_SEXO, MAESOC_ESTCIV, MAESOC_GRUPOSANG,
            MAESOC_CODCAT, MAESOC_CODACT, MAESOC_CODOSM,
            MAESOC_CODPROV, MAESOC_CODLOC, MAESOC_CODPOS, MAESOC_NACIONALIDAD,
            MAESOC_CALLE, MAESOC_NROCALLE, MAESOC_PISO, MAESOC_DTO, MAESOC_BARRIO,
            MAESOC_NROAFIL, MAESOC_NROCARNET, MAESOC_EMAIL, MAESOC_EMAILLAB,
            MAESOC_IMPEST1, MAESOC_IMPEST2, MAESOC_IMPEST3, MAESOC_IMPEST4, MAESOC_IMPEST5, MAESOC_IMPEST6,
            MAESOC_DEBAUT_CBU, MAESOC_DEBAUT_MONTO,
            MAESOC_FECHAALTA, MAESOC_FECHAMOD, MAESOC_FECHADESEMPLEO, MAESOC_USUMOD, MAESOC_ESTADO)
        VALUES (
            @cuil, @cuilStr, @apellido, @nombre, @apenom,
            @tel, @telcel, @fechanac, '10000101', @cargaf, @jubil,
            @tipodoc, @nrodoc, @sexo, ' ', 0,
            @categoria, @actividad, @obrasocial,
            ' ', 0, @codpos, '',
            @calle, @nrocalle, @piso, @dto, @barrio,
            '', '', @email, @emaillab,
            '', '', '', '', '', '',
            '', 0,
            '10000101', GETDATE(), '10000101', 'SIRGA', 1);
        """;

    await using var comando = new SqlCommand(sql, conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    comando.Parameters.AddWithValue("@cuilStr", detalle.Cuil);
    comando.Parameters.AddWithValue("@apellido", Recortar(detalle.Apellido.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@nombre", Recortar(detalle.Nombre.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@apenom",
        Recortar($"{detalle.Apellido} {detalle.Nombre}".Trim().ToUpperInvariant(), 100));
    comando.Parameters.AddWithValue("@tel", Recortar(detalle.Telefono, 20));
    comando.Parameters.AddWithValue("@telcel", Recortar(detalle.Celular, 30));
    comando.Parameters.AddWithValue("@fechanac", (object?)(detalle.FechaNacimiento?.Date) ?? DBNull.Value);
    comando.Parameters.AddWithValue("@cargaf", (short)(detalle.CargaFamiliar ? 1 : 0));
    comando.Parameters.AddWithValue("@jubil", detalle.Jubilado ? 1 : 0);
    comando.Parameters.AddWithValue("@tipodoc", int.TryParse(detalle.TipoDocumento, out var tipo) ? tipo : 0);
    comando.Parameters.AddWithValue("@nrodoc", Recortar(detalle.Documento, 11));
    comando.Parameters.AddWithValue("@sexo", string.IsNullOrWhiteSpace(detalle.Sexo) ? " " : Recortar(detalle.Sexo.ToUpperInvariant(), 1));
    comando.Parameters.AddWithValue("@categoria", short.TryParse(detalle.Categoria, out var cat) ? cat : (short)0);
    comando.Parameters.AddWithValue("@actividad", short.TryParse(detalle.Actividad, out var act) ? act : (short)0);
    comando.Parameters.AddWithValue("@obrasocial", int.TryParse(detalle.ObraSocial, out var os) ? os : 0);
    comando.Parameters.AddWithValue("@codpos", Recortar(detalle.CodigoPostal, 10));
    comando.Parameters.AddWithValue("@calle", Recortar(detalle.Calle?.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@nrocalle", Recortar(detalle.Numero, 10));
    comando.Parameters.AddWithValue("@piso", Recortar(detalle.Piso, 10));
    comando.Parameters.AddWithValue("@dto", Recortar(detalle.Depto, 10));
    comando.Parameters.AddWithValue("@barrio", Recortar(detalle.Barrio?.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@email", Recortar(detalle.Email, 50));
    comando.Parameters.AddWithValue("@emaillab", Recortar(detalle.EmailLaboral, 50));

    await comando.ExecuteNonQueryAsync(ct);
  }

  /// <summary>
  /// Espeja en el padrón los datos que SIRGA maneja, para un socio que YA existe. Reglas (decididas por el
  /// usuario el 2026-11-04):
  ///   * un valor VACÍO en SIRGA **no pisa** lo que haya en el padrón;
  ///   * los campos que SIRGA no tiene (estado civil, nacionalidad, grupo sanguíneo, CBU, impuestos,
  ///     nro. de afiliado, carnet, fechas de alta/baja, estado) **no se tocan**;
  ///   * jubilado y carga familiar sólo se ponen en 1: nunca se borra un "sí" que haya cargado el sindicato.
  /// Devuelve 1 si la fila cambió y 0 si ya estaba igual (el WHERE evita escrituras y conteos falsos).
  /// </summary>
  private static async Task<int> ActualizarSocioAsync(
      SqlConnection conexion, SqlTransaction transaccion, DetalleLegado detalle, decimal cuil, CancellationToken ct)
  {
    const string sql = """
        UPDATE fotos_.maesoc SET
            MAESOC_APELLIDO = ISNULL(NULLIF(@apellido, ''), MAESOC_APELLIDO),
            MAESOC_NOMBRE   = ISNULL(NULLIF(@nombre, ''),   MAESOC_NOMBRE),
            APENOM          = ISNULL(NULLIF(@apenom, ''),   APENOM),
            MAESOC_NRODOC   = ISNULL(NULLIF(@nrodoc, ''),   MAESOC_NRODOC),
            MAESOC_TIPODOC  = CASE WHEN @tipodoc > 0 THEN @tipodoc ELSE MAESOC_TIPODOC END,
            MAESOC_FECHANAC = ISNULL(@fechanac, MAESOC_FECHANAC),
            MAESOC_SEXO     = ISNULL(NULLIF(@sexo, ''),     MAESOC_SEXO),
            MAESOC_TEL      = ISNULL(NULLIF(@tel, ''),      MAESOC_TEL),
            MAESOC_TELCEL   = ISNULL(NULLIF(@telcel, ''),   MAESOC_TELCEL),
            MAESOC_EMAIL    = ISNULL(NULLIF(@email, ''),    MAESOC_EMAIL),
            MAESOC_EMAILLAB = ISNULL(NULLIF(@emaillab, ''), MAESOC_EMAILLAB),
            MAESOC_CALLE    = ISNULL(NULLIF(@calle, ''),    MAESOC_CALLE),
            MAESOC_NROCALLE = ISNULL(NULLIF(@nrocalle, ''), MAESOC_NROCALLE),
            MAESOC_PISO     = ISNULL(NULLIF(@piso, ''),     MAESOC_PISO),
            MAESOC_DTO      = ISNULL(NULLIF(@dto, ''),      MAESOC_DTO),
            MAESOC_BARRIO   = ISNULL(NULLIF(@barrio, ''),   MAESOC_BARRIO),
            MAESOC_CODPOS   = ISNULL(NULLIF(@codpos, ''),   MAESOC_CODPOS),
            MAESOC_CODCAT   = CASE WHEN @categoria > 0  THEN @categoria  ELSE MAESOC_CODCAT  END,
            MAESOC_CODACT   = CASE WHEN @actividad > 0  THEN @actividad  ELSE MAESOC_CODACT  END,
            MAESOC_CODOSM   = CASE WHEN @obrasocial > 0 THEN @obrasocial ELSE MAESOC_CODOSM  END,
            MAESOC_JUBIL    = CASE WHEN @jubil = 1  THEN 1 ELSE MAESOC_JUBIL END,
            MAESOC_CARGAFLIA= CASE WHEN @cargaf = 1 THEN 1 ELSE MAESOC_CARGAFLIA END,
            MAESOC_FECHAMOD = GETDATE(),
            MAESOC_USUMOD   = 'SIRGA'
        WHERE MAESOC_CUIL = @cuil
          AND (
                (NULLIF(@apellido, '') IS NOT NULL AND ISNULL(RTRIM(MAESOC_APELLIDO), '') <> @apellido)
             OR (NULLIF(@nombre, '')   IS NOT NULL AND ISNULL(RTRIM(MAESOC_NOMBRE), '')   <> @nombre)
             OR (NULLIF(@apenom, '')   IS NOT NULL AND ISNULL(RTRIM(APENOM), '')          <> @apenom)
             OR (NULLIF(@nrodoc, '')   IS NOT NULL AND ISNULL(RTRIM(MAESOC_NRODOC), '')   <> @nrodoc)
             OR (@tipodoc > 0  AND ISNULL(MAESOC_TIPODOC, 0) <> @tipodoc)
             OR (@fechanac IS NOT NULL AND ISNULL(MAESOC_FECHANAC, '10000101') <> @fechanac)
             OR (NULLIF(@sexo, '')     IS NOT NULL AND ISNULL(RTRIM(MAESOC_SEXO), '')     <> @sexo)
             OR (NULLIF(@tel, '')      IS NOT NULL AND ISNULL(RTRIM(MAESOC_TEL), '')      <> @tel)
             OR (NULLIF(@telcel, '')   IS NOT NULL AND ISNULL(RTRIM(MAESOC_TELCEL), '')   <> @telcel)
             OR (NULLIF(@email, '')    IS NOT NULL AND ISNULL(RTRIM(MAESOC_EMAIL), '')    <> @email)
             OR (NULLIF(@emaillab, '') IS NOT NULL AND ISNULL(RTRIM(MAESOC_EMAILLAB), '') <> @emaillab)
             OR (NULLIF(@calle, '')    IS NOT NULL AND ISNULL(RTRIM(MAESOC_CALLE), '')    <> @calle)
             OR (NULLIF(@nrocalle, '') IS NOT NULL AND ISNULL(RTRIM(MAESOC_NROCALLE), '') <> @nrocalle)
             OR (NULLIF(@piso, '')     IS NOT NULL AND ISNULL(RTRIM(MAESOC_PISO), '')     <> @piso)
             OR (NULLIF(@dto, '')      IS NOT NULL AND ISNULL(RTRIM(MAESOC_DTO), '')      <> @dto)
             OR (NULLIF(@barrio, '')   IS NOT NULL AND ISNULL(RTRIM(MAESOC_BARRIO), '')   <> @barrio)
             OR (NULLIF(@codpos, '')   IS NOT NULL AND ISNULL(RTRIM(MAESOC_CODPOS), '')   <> @codpos)
             OR (@categoria > 0  AND ISNULL(MAESOC_CODCAT, 0) <> @categoria)
             OR (@actividad > 0  AND ISNULL(MAESOC_CODACT, 0) <> @actividad)
             OR (@obrasocial > 0 AND ISNULL(MAESOC_CODOSM, 0) <> @obrasocial)
             OR (@jubil = 1  AND ISNULL(MAESOC_JUBIL, 0) <> 1)
             OR (@cargaf = 1 AND ISNULL(MAESOC_CARGAFLIA, 0) <> 1)
          );
        """;

    await using var comando = new SqlCommand(sql, conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    comando.Parameters.AddWithValue("@apellido", Recortar(detalle.Apellido.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@nombre", Recortar(detalle.Nombre.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@apenom",
        Recortar($"{detalle.Apellido} {detalle.Nombre}".Trim().ToUpperInvariant(), 100));
    comando.Parameters.AddWithValue("@nrodoc", Recortar(detalle.Documento, 11));
    comando.Parameters.AddWithValue("@tipodoc", int.TryParse(detalle.TipoDocumento, out var tipo) ? tipo : 0);
    comando.Parameters.AddWithValue("@fechanac", (object?)(detalle.FechaNacimiento?.Date) ?? DBNull.Value);
    comando.Parameters.AddWithValue("@sexo", Recortar(detalle.Sexo?.ToUpperInvariant(), 1));
    comando.Parameters.AddWithValue("@tel", Recortar(detalle.Telefono, 20));
    comando.Parameters.AddWithValue("@telcel", Recortar(detalle.Celular, 30));
    comando.Parameters.AddWithValue("@email", Recortar(detalle.Email, 50));
    comando.Parameters.AddWithValue("@emaillab", Recortar(detalle.EmailLaboral, 50));
    comando.Parameters.AddWithValue("@calle", Recortar(detalle.Calle?.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@nrocalle", Recortar(detalle.Numero, 10));
    comando.Parameters.AddWithValue("@piso", Recortar(detalle.Piso, 10));
    comando.Parameters.AddWithValue("@dto", Recortar(detalle.Depto, 10));
    comando.Parameters.AddWithValue("@barrio", Recortar(detalle.Barrio?.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@codpos", Recortar(detalle.CodigoPostal, 10));
    comando.Parameters.AddWithValue("@categoria", short.TryParse(detalle.Categoria, out var cat) ? cat : (short)0);
    comando.Parameters.AddWithValue("@actividad", short.TryParse(detalle.Actividad, out var act) ? act : (short)0);
    comando.Parameters.AddWithValue("@obrasocial", int.TryParse(detalle.ObraSocial, out var os) ? os : 0);
    comando.Parameters.AddWithValue("@jubil", detalle.Jubilado ? 1 : 0);
    comando.Parameters.AddWithValue("@cargaf", detalle.CargaFamiliar ? 1 : 0);

    return await comando.ExecuteNonQueryAsync(ct);
  }
  private static async Task InsertarRelacionAsync(
      SqlConnection conexion, SqlTransaction transaccion, DetalleLegado detalle, decimal cuil, decimal cuite,
      DateTime fechaPeriodo, CancellationToken ct)
  {
    // CODSUC: se copia el que ya usan los demás empleados de esa empresa (el sistema anterior lo numera
    // por sucursal); si la empresa todavía no tiene a nadie, 0.
    const string sql = """
        INSERT INTO fotos_.socemp (
            SOCEMP_CUIL, SOCEMP_CUITE, SOCEMP_CODSUC, SOCEMP_ESTADO, SOCEMP_FECHAING,
            SOCEMP_FECHABAJA, SOCEMP_CAUSABAJA, SOCEMP_NROLEG, SOCEMP_SOCIOCENTRO,
            SOCEMP_ULT_EMPRESA, SOCEMP_CUITE_STR, SOCEMP_CUIL_STR)
        VALUES (
            @cuil, @cuite,
            ISNULL((SELECT TOP 1 SOCEMP_CODSUC FROM fotos_.socemp WHERE SOCEMP_CUITE = @cuite
                    GROUP BY SOCEMP_CODSUC ORDER BY COUNT(*) DESC), 0),
            1, @fechaing,
            '10000101', ' ', @nroleg, 0,
            'S', @cuiteStr, @cuilStr);
        """;

    await using var comando = new SqlCommand(sql, conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    comando.Parameters.AddWithValue("@cuite", (double)cuite);
    comando.Parameters.AddWithValue("@fechaing", (object?)(detalle.FechaIngreso?.Date) ?? fechaPeriodo);
    comando.Parameters.AddWithValue("@nroleg", Recortar(detalle.Legajo, 10));
    comando.Parameters.AddWithValue("@cuiteStr", cuite.ToString(CultureInfo.InvariantCulture));
    comando.Parameters.AddWithValue("@cuilStr", detalle.Cuil);

    await comando.ExecuteNonQueryAsync(ct);
  }

  /// <summary>Recorta al largo de la columna del legado (char/nvarchar): si no, el INSERT revienta.</summary>
  private static string Recortar(string? valor, int largo)
  {
    var texto = (valor ?? string.Empty).Trim();
    return texto.Length <= largo ? texto : texto[..largo];
  }
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
