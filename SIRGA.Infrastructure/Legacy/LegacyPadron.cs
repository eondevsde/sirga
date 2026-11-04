using System.Globalization;
using Microsoft.Data.SqlClient;
using SIRGA.Application.Interfaces;

namespace SIRGA.Infrastructure.Legacy;

/// <summary>
/// El SQL del PADRÓN del sistema anterior (`fotos_.maesoc` y `fotos_.socemp`), en un solo lugar.
///
/// Lo usan dos caminos:
///   * <see cref="LegacyDeclarationWriter"/> — al presentar la DDJJ (crea el socio que falta);
///   * <see cref="LegacyPadronWriter"/> — al guardar un empleado (actualiza el socio que ya existe).
///
/// Tener el SQL duplicado en los dos lados sería garantizar que se desincronicen los campos.
/// **Todas las columnas de `maesoc` son NOT NULL**, así que se llenan todas: las fechas desconocidas van
/// con el centinela `1000-01-01` y los textos vacíos con cadena vacía, igual que una fila real del legado.
/// </summary>
internal static class LegacyPadron
{
  internal static async Task<bool> ExisteAsync(
      SqlConnection conexion, SqlTransaction? transaccion, decimal cuil, CancellationToken ct)
  {
    await using var comando = new SqlCommand(
        "SELECT COUNT(*) FROM fotos_.maesoc WHERE MAESOC_CUIL = @cuil", conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    return Convert.ToInt32(await comando.ExecuteScalarAsync(ct)) > 0;
  }

  internal static async Task<bool> ExisteRelacionAsync(
      SqlConnection conexion, SqlTransaction? transaccion, decimal cuil, decimal cuite, CancellationToken ct)
  {
    await using var comando = new SqlCommand(
        "SELECT COUNT(*) FROM fotos_.socemp WHERE SOCEMP_CUIL = @cuil AND SOCEMP_CUITE = @cuite",
        conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    comando.Parameters.AddWithValue("@cuite", (double)cuite);
    return Convert.ToInt32(await comando.ExecuteScalarAsync(ct)) > 0;
  }

  internal static async Task InsertarAsync(
      SqlConnection conexion, SqlTransaction? transaccion, SocioLegado socio, decimal cuil, CancellationToken ct)
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
    AgregarParametrosDelSocio(comando, socio, cuil);
    await comando.ExecuteNonQueryAsync(ct);
  }

  /// <summary>
  /// Espeja los datos que SIRGA maneja. Un valor **vacío no pisa** el del padrón; los campos que SIRGA no
  /// tiene no se tocan; jubilado y carga familiar **sólo se ponen en 1** (nunca se borra un "sí" del
  /// sindicato). El `WHERE` compara campo por campo, así que no escribe cuando no hay nada que cambiar.
  /// </summary>
  internal static async Task<int> ActualizarAsync(
      SqlConnection conexion, SqlTransaction? transaccion, SocioLegado socio, decimal cuil, CancellationToken ct)
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
    AgregarParametrosDelSocio(comando, socio, cuil);
    return await comando.ExecuteNonQueryAsync(ct);
  }

  internal static async Task InsertarRelacionAsync(
      SqlConnection conexion, SqlTransaction? transaccion, SocioLegado socio, decimal cuil, decimal cuite,
      DateTime fechaPorDefecto, CancellationToken ct)
  {
    // CODSUC: se copia el que ya usan los demás empleados de esa empresa (el legado lo numera por
    // sucursal); si la empresa todavía no tiene a nadie, 0.
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
    comando.Parameters.AddWithValue("@fechaing", (object?)(socio.FechaIngreso?.Date) ?? fechaPorDefecto);
    comando.Parameters.AddWithValue("@nroleg", Recortar(socio.Legajo, 10));
    comando.Parameters.AddWithValue("@cuiteStr", cuite.ToString(CultureInfo.InvariantCulture));
    comando.Parameters.AddWithValue("@cuilStr", socio.Cuil);
    await comando.ExecuteNonQueryAsync(ct);
  }

  /// <summary>Actualiza la fecha de ingreso y el legajo en la relación socio-empresa (sólo si vienen con dato).</summary>
  internal static async Task<int> ActualizarRelacionAsync(
      SqlConnection conexion, SqlTransaction? transaccion, SocioLegado socio, decimal cuil, decimal cuite,
      CancellationToken ct)
  {
    const string sql = """
        UPDATE fotos_.socemp SET
            SOCEMP_FECHAING = ISNULL(@fechaing, SOCEMP_FECHAING),
            SOCEMP_NROLEG   = ISNULL(NULLIF(@nroleg, ''), SOCEMP_NROLEG)
        WHERE SOCEMP_CUIL = @cuil AND SOCEMP_CUITE = @cuite
          AND (
                (@fechaing IS NOT NULL AND CONVERT(date, SOCEMP_FECHAING) <> @fechaing)
             OR (NULLIF(@nroleg, '') IS NOT NULL AND ISNULL(RTRIM(SOCEMP_NROLEG), '') <> @nroleg)
          );
        """;

    await using var comando = new SqlCommand(sql, conexion, transaccion);
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    comando.Parameters.AddWithValue("@cuite", (double)cuite);
    comando.Parameters.AddWithValue("@fechaing", (object?)(socio.FechaIngreso?.Date) ?? DBNull.Value);
    comando.Parameters.AddWithValue("@nroleg", Recortar(socio.Legajo, 10));
    return await comando.ExecuteNonQueryAsync(ct);
  }

  private static void AgregarParametrosDelSocio(SqlCommand comando, SocioLegado socio, decimal cuil)
  {
    comando.Parameters.AddWithValue("@cuil", (double)cuil);
    comando.Parameters.AddWithValue("@cuilStr", socio.Cuil);
    comando.Parameters.AddWithValue("@apellido", Recortar(socio.Apellido.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@nombre", Recortar(socio.Nombre.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@apenom",
        Recortar($"{socio.Apellido} {socio.Nombre}".Trim().ToUpperInvariant(), 100));
    comando.Parameters.AddWithValue("@tel", Recortar(socio.Telefono, 20));
    comando.Parameters.AddWithValue("@telcel", Recortar(socio.Celular, 30));
    comando.Parameters.AddWithValue("@fechanac", (object?)(socio.FechaNacimiento?.Date) ?? DBNull.Value);
    comando.Parameters.AddWithValue("@cargaf", (short)(socio.CargaFamiliar ? 1 : 0));
    comando.Parameters.AddWithValue("@jubil", socio.Jubilado ? 1 : 0);
    comando.Parameters.AddWithValue("@tipodoc", int.TryParse(socio.TipoDocumento, out var tipo) ? tipo : 0);
    comando.Parameters.AddWithValue("@nrodoc", Recortar(socio.Documento, 11));
    comando.Parameters.AddWithValue("@sexo", Recortar(socio.Sexo?.ToUpperInvariant(), 1));
    comando.Parameters.AddWithValue("@categoria", short.TryParse(socio.Categoria, out var cat) ? cat : (short)0);
    comando.Parameters.AddWithValue("@actividad", short.TryParse(socio.Actividad, out var act) ? act : (short)0);
    comando.Parameters.AddWithValue("@obrasocial", int.TryParse(socio.ObraSocial, out var os) ? os : 0);
    comando.Parameters.AddWithValue("@codpos", Recortar(socio.CodigoPostal, 10));
    comando.Parameters.AddWithValue("@calle", Recortar(socio.Calle?.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@nrocalle", Recortar(socio.Numero, 10));
    comando.Parameters.AddWithValue("@piso", Recortar(socio.Piso, 10));
    comando.Parameters.AddWithValue("@dto", Recortar(socio.Depto, 10));
    comando.Parameters.AddWithValue("@barrio", Recortar(socio.Barrio?.ToUpperInvariant(), 30));
    comando.Parameters.AddWithValue("@email", Recortar(socio.Email, 50));
    comando.Parameters.AddWithValue("@emaillab", Recortar(socio.EmailLaboral, 50));
  }

  /// <summary>Recorta al largo de la columna del legado (char/nvarchar): si no, el INSERT revienta.</summary>
  internal static string Recortar(string? valor, int largo)
  {
    var texto = (valor ?? string.Empty).Trim();
    return texto.Length <= largo ? texto : texto[..largo];
  }
}
