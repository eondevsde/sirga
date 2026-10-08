using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SIRGA.Infrastructure.Legacy;

/// <summary>
/// Acceso a la base del sistema anterior (`Sindicato`), de la que **se alimenta SIGES WEB**.
///
/// POR QUE EXISTE: SIRGA maneja las DDJJ y registra los pagos, pero **SIGES WEB lee `ddjj`/`ddjjt`** para
/// saber si una empresa debe o no, y para emitir actas. Si SIRGA no escribe ahí, una DDJJ creada en SIRGA
/// es **invisible** para SIGES WEB: no genera acta ni aparece en la verificación de deuda.
///
/// REGLAS QUE RESPETA ESTA CLASE:
/// - **Nunca escribe sin simulacro**: todo lo que modifica datos pasa primero por un metodo `Simular...`
///   que devuelve exactamente que filas y que valores se cambiarian, sin tocar nada.
/// - **Idempotente**: las actualizaciones llevan `AND fpago IS NULL`, asi reejecutar no pisa un pago ya
///   registrado ni cambia el importe de uno existente.
/// - **No toca lo que no entiende**: `estado`, `medio1/2/3` y `trans1/2/3` no se escriben porque SIGES WEB
///   no los usa (usa `fpago` y `impban1`). Ver `PLAN_CONCILIACION_PAGOS.md`.
/// </summary>
public class LegacySindicatoRepository
{
  private readonly string _cadena;

  /// <summary>
  /// Recibe la cadena de conexion ya resuelta (la registra la capa de arriba leyendo
  /// `ConnectionStrings:Legacy`). Se pasa la cadena y no la configuracion para que la clase se pueda
  /// probar sola y para que **no haya una cadena por defecto escondida en el codigo**.
  /// </summary>
  public LegacySindicatoRepository(string cadena)
  {
    // Sin cadena no se sigue: si se calla, el pago se registra en SIRGA y SIGES WEB nunca lo ve.
    if (string.IsNullOrWhiteSpace(cadena))
    {
      throw new InvalidOperationException(
          "Falta la cadena de conexion 'Legacy' (base Sindicato). Sin ella el legado no se actualiza y " +
          "SIGES WEB seguiria viendo deuda ya pagada.");
    }

    _cadena = cadena;
  }

  /// <summary>Una fila de la cabecera `ddjjt`, con los campos que importan para el pago.</summary>
  public sealed record FilaDdjjt(
      int Id,
      decimal Titem1,
      decimal Titem2,
      decimal? Impban1,
      DateTime? Fpago,
      int Rect,
      int Repite);

  /// <summary>
  /// Devuelve **la fila buena** de `ddjjt` para (CUIT, periodo), con la MISMA regla que usa SIGES WEB:
  /// primero la que esta pagada y tiene `titem1 > 0`; si no hay ninguna, la de mayor `rect`.
  ///
  /// Esa regla esta tomada del codigo de SIGES WEB (`EstudiosService` y `ReportesService`), no inventada:
  /// si se elige otra fila, se marca pagada la version equivocada de la DDJJ.
  /// </summary>
  public async Task<FilaDdjjt?> LeerFilaBuenaAsync(string cuit, DateTime periodo, CancellationToken ct = default)
  {
    const string sql = """
        SELECT TOP 1 Id, ISNULL(titem1, 0) AS titem1, ISNULL(titem2, 0) AS titem2, impban1, fpago, rect, repite
        FROM ddjjt
        WHERE CUIT_STR = @cuit AND periodo = @periodo
        ORDER BY
            CASE WHEN fpago IS NOT NULL AND ISNULL(titem1, 0) > 0 THEN 0 ELSE 1 END,
            rect DESC,
            Id DESC
        """;

    await using var conexion = new SqlConnection(_cadena);
    await conexion.OpenAsync(ct);
    await using var comando = new SqlCommand(sql, conexion);
    comando.Parameters.AddWithValue("@cuit", cuit);
    comando.Parameters.AddWithValue("@periodo", periodo);

    await using var lector = await comando.ExecuteReaderAsync(ct);
    if (!await lector.ReadAsync(ct)) return null;

    return new FilaDdjjt(
        Convert.ToInt32(lector["Id"]),
        Convert.ToDecimal(lector["titem1"]),
        Convert.ToDecimal(lector["titem2"]),
        lector["impban1"] is DBNull ? null : Convert.ToDecimal(lector["impban1"]),
        lector["fpago"] is DBNull ? null : Convert.ToDateTime(lector["fpago"]),
        Convert.ToInt32(lector["rect"]),
        Convert.ToInt32(lector["repite"]));
  }

  /// <summary>Resultado del simulacro: que se cambiaria, sin haber cambiado nada.</summary>
  public sealed record SimulacroPago(
      bool HayFila,
      int? Id,
      string QueSeCambiaria,
      bool YaEstabaPaga,
      decimal Titem1,
      decimal Titem2,
      decimal? Impban1Actual,
      DateTime? FpagoActual);

  /// <summary>
  /// SIMULACRO: dice exactamente que se escribiria al registrar un pago, **sin tocar la base**.
  ///
  /// Se corre antes de cualquier escritura real, porque son datos del usuario: si el simulacro no muestra
  /// lo esperado, no se ejecuta la actualizacion.
  /// </summary>
  public async Task<SimulacroPago> SimularPagoAsync(
      string cuit, DateTime periodo, decimal importePagado, DateTime fechaPago, CancellationToken ct = default)
  {
    var fila = await LeerFilaBuenaAsync(cuit, periodo, ct);

    if (fila is null)
    {
      return new SimulacroPago(false, null,
          "NO EXISTE la fila en ddjjt: el pago quedaria sin registrar en el legado y SIGES WEB seguiria " +
          "viendo la deuda. Hay que decidir si se crea la DDJJ en el legado.",
          false, 0, 0, null, null);
    }

    if (fila.Fpago is not null)
    {
      return new SimulacroPago(true, fila.Id,
          $"LA DDJJ YA FIGURA PAGADA el {fila.Fpago:dd/MM/yyyy}. No se toca (idempotencia).",
          true, fila.Titem1, fila.Titem2, fila.Impban1, fila.Fpago);
    }

    return new SimulacroPago(true, fila.Id,
        $"Se pondria fpago = {fechaPago:dd/MM/yyyy} e impban1 = {importePagado:N2} en la fila Id {fila.Id} " +
        $"(titem1 = {fila.Titem1:N2}, titem2 = {fila.Titem2:N2}).",
        false, fila.Titem1, fila.Titem2, fila.Impban1, fila.Fpago);
  }

  /// <summary>Resultado de registrar un pago en el legado.</summary>
  public sealed record ResultadoPago(bool Actualizado, string Detalle, int? Id);

  /// <summary>
  /// Registra el pago en `ddjjt` escribiendo **solo `fpago` e `impban1`**.
  ///
  /// ORDEN OBLIGATORIO (regla del proyecto: son datos del usuario):
  ///   1. se verifica que la fila exista y que no este ya pagada;
  ///   2. se RESPALDA la fila en `_BackupDdjjtPago` (la tabla se crea si no existe y el respaldo NO se
  ///      pisa si ya estaba);
  ///   3. recien ahi se actualiza, con `AND fpago IS NULL` para que sea **idempotente**.
  ///
  /// Todo va en **una transaccion**: si algo falla, no queda ni el respaldo a medias ni el pago escrito.
  /// **No toca** `estado`, `medio1/2/3` ni `trans1/2/3` (SIGES WEB no los usa; ver el plan).
  /// </summary>
  public async Task<ResultadoPago> MarcarPagadaAsync(
      string cuit, DateTime periodo, decimal importePagado, DateTime fechaPago, string usuario,
      CancellationToken ct = default)
  {
    var fila = await LeerFilaBuenaAsync(cuit, periodo, ct);

    if (fila is null)
    {
      return new ResultadoPago(false,
          "La DDJJ no existe en el legado: NO se toca nada. Falta decidir si se crea (pendiente).", null);
    }

    if (fila.Fpago is not null)
    {
      return new ResultadoPago(false,
          $"La DDJJ ya figura pagada el {fila.Fpago:dd/MM/yyyy}: no se pisa el pago existente.", fila.Id);
    }

    await using var conexion = new SqlConnection(_cadena);
    await conexion.OpenAsync(ct);
    await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(ct);

    // 1) RESPALDO de la fila, antes de tocar nada.
    const string sqlRespaldo = """
        IF OBJECT_ID('dbo._BackupDdjjtPago') IS NULL
        BEGIN
            SELECT Id, CUIT_STR, periodo, titem1, titem2, impban1, fpago, rect, repite,
                   CAST(GETDATE() AS datetime) AS FechaRespaldo,
                   CAST(@usuario AS nvarchar(100)) AS UsuarioRespaldo
            INTO dbo._BackupDdjjtPago
            FROM dbo.ddjjt WHERE 1 = 0;
        END

        IF NOT EXISTS (SELECT 1 FROM dbo._BackupDdjjtPago WHERE Id = @id)
        BEGIN
            INSERT INTO dbo._BackupDdjjtPago
                (Id, CUIT_STR, periodo, titem1, titem2, impban1, fpago, rect, repite, FechaRespaldo, UsuarioRespaldo)
            SELECT Id, CUIT_STR, periodo, titem1, titem2, impban1, fpago, rect, repite, GETDATE(), @usuario
            FROM dbo.ddjjt WHERE Id = @id;
        END
        """;

    await using (var respaldo = new SqlCommand(sqlRespaldo, conexion, transaccion))
    {
      respaldo.Parameters.AddWithValue("@id", fila.Id);
      respaldo.Parameters.AddWithValue("@usuario", (object?)usuario ?? DBNull.Value);
      await respaldo.ExecuteNonQueryAsync(ct);
    }

    // 2) ACTUALIZACION idempotente: solo si sigue sin pagar.
    const string sqlUpdate = """
        UPDATE ddjjt
        SET fpago = @fechaPago, impban1 = @importe, fmod = GETDATE(), usuario = @usuario
        WHERE Id = @id AND fpago IS NULL
        """;

    int filasAfectadas;
    await using (var update = new SqlCommand(sqlUpdate, conexion, transaccion))
    {
      update.Parameters.AddWithValue("@id", fila.Id);
      update.Parameters.AddWithValue("@fechaPago", fechaPago);
      update.Parameters.AddWithValue("@importe", importePagado);
      update.Parameters.AddWithValue("@usuario", (object?)usuario ?? DBNull.Value);
      filasAfectadas = await update.ExecuteNonQueryAsync(ct);
    }

    // 3) VERIFICACION releyendo: no alcanza con que no de error.
    var quedoEscrito = false;
    await using (var verificar = new SqlCommand(
        "SELECT fpago FROM ddjjt WHERE Id = @id", conexion, transaccion))
    {
      verificar.Parameters.AddWithValue("@id", fila.Id);
      await using var lector = await verificar.ExecuteReaderAsync(ct);
      quedoEscrito = await lector.ReadAsync(ct) && lector["fpago"] is not DBNull;
    }

    if (!quedoEscrito || filasAfectadas != 1)
    {
      await transaccion.RollbackAsync(ct);
      return new ResultadoPago(false,
          $"La verificacion fallo (filas afectadas: {filasAfectadas}): se REVIRTIO todo, incluso el respaldo.",
          fila.Id);
    }

    await transaccion.CommitAsync(ct);
    return new ResultadoPago(true,
        $"Pago registrado: fpago = {fechaPago:dd/MM/yyyy}, impban1 = {importePagado:N2} (fila Id {fila.Id}). " +
        "La fila anterior quedo respaldada en _BackupDdjjtPago.", fila.Id);
  }
}
