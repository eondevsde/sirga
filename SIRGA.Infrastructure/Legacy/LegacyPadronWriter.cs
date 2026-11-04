using System.Globalization;
using Microsoft.Data.SqlClient;
using SIRGA.Application.Interfaces;

namespace SIRGA.Infrastructure.Legacy;

/// <summary>
/// Mantiene al día el padrón del sistema anterior (`fotos_.maesoc` + `fotos_.socemp`) **cuando el estudio
/// guarda un empleado**. Es la sincronización instantánea que pidió el usuario (2026-11-04): antes había que
/// esperar a presentar la DDJJ para que SIGES viera el celular corregido.
///
/// **Sólo toca socios que ya existen.** El alta del socio en el padrón sigue siendo en la presentación de la
/// DDJJ (decisión del usuario: recién ahí el empleado existe para el sindicato). Si el socio no está, este
/// servicio no hace nada y lo informa.
/// </summary>
public class LegacyPadronWriter : ILegacyPadronWriter
{
  private readonly string _cadena;

  public LegacyPadronWriter(string cadena) => _cadena = cadena;

  public async Task<string?> ActualizarSiExisteAsync(
      SocioLegado socio, string cuitEmpresa, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(_cadena))
      return "No se actualizó el padrón: falta la cadena de conexión 'Legacy'.";

    if (!decimal.TryParse(socio.Cuil, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cuil))
      return null;

    await using var conexion = new SqlConnection(_cadena);
    await conexion.OpenAsync(ct);
    await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(ct);

    try
    {
      // El socio tiene que existir: si no está en el padrón, el alta la hace la presentación de la DDJJ.
      if (!await LegacyPadron.ExisteAsync(conexion, transaccion, cuil, ct))
      {
        await transaccion.RollbackAsync(ct);
        return null;
      }

      var cambios = new List<string>();

      if (await LegacyPadron.ActualizarAsync(conexion, transaccion, socio, cuil, ct) > 0)
        cambios.Add("datos del socio");

      // La relación con la empresa también se mantiene al día: si no existe, se crea (el empleado ya está
      // en el padrón, así que SIGES tiene que verlo en esta empresa); si existe, se actualizan la fecha de
      // ingreso y el legajo.
      if (decimal.TryParse(cuitEmpresa, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cuite))
      {
        if (await LegacyPadron.ExisteRelacionAsync(conexion, transaccion, cuil, cuite, ct))
        {
          if (await LegacyPadron.ActualizarRelacionAsync(conexion, transaccion, socio, cuil, cuite, ct) > 0)
            cambios.Add("relación socio-empresa");
        }
        else
        {
          await LegacyPadron.InsertarRelacionAsync(conexion, transaccion, socio, cuil, cuite, DateTime.Today, ct);
          cambios.Add("relación socio-empresa nueva");
        }
      }

      await transaccion.CommitAsync(ct);

      return cambios.Count == 0 ? null : "Padrón actualizado: " + string.Join(", ", cambios) + ".";
    }
    catch
    {
      await transaccion.RollbackAsync(ct);
      throw;
    }
  }
}
