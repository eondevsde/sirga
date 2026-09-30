using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SIRGA.Application.Services;

namespace SIRGA.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="ILegacyCompanyLookup"/> contra la base legada (Sindicato).
/// Solo lectura; si la base no está disponible no bloquea el alta.
/// </summary>
public class LegacyCompanyLookup : ILegacyCompanyLookup
{
  private readonly string _connectionString;

  public LegacyCompanyLookup(IConfiguration configuration)
  {
    _connectionString = configuration.GetConnectionString("LegacyConnection")
        ?? "Server=localhost;Database=Sindicato;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
  }

  public async Task<LegacyCompanyInfo?> FindAsync(string normalizedCuit)
  {
    try
    {
      await using var conn = new SqlConnection(_connectionString);
      await conn.OpenAsync();
      await using var cmd = new SqlCommand(
          @"SELECT (SELECT TOP 1 m.MAEEMP_RAZSOC FROM fotos_.maeemp m WHERE m.MEEMP_CUIT_STR = @cuit) AS Name,
                   CASE WHEN EXISTS (SELECT 1 FROM dbo.ddjj d WHERE d.CUIT_STR = @cuit AND d.periodo >= '2026-01-01')
                         OR EXISTS (SELECT 1 FROM fotos_.socemp s WHERE s.SOCEMP_CUITE_STR = @cuit AND s.SOCEMP_ESTADO = 1)
                         THEN 1 ELSE 0 END AS HasRecords", conn);
      cmd.Parameters.AddWithValue("@cuit", normalizedCuit);
      using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
      if (await reader.ReadAsync())
      {
        var name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
        var hasRecords = reader.GetInt32(1) == 1;
        // Existe en el legado si aparece en maeemp o tiene registros de ddjj/socemp
        if (!string.IsNullOrEmpty(name) || hasRecords)
          return new LegacyCompanyInfo(name, hasRecords);
      }
      return null;
    }
    catch
    {
      // Sin acceso al legado: no bloqueamos el alta.
      return null;
    }
  }
}