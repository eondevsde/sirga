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
}
