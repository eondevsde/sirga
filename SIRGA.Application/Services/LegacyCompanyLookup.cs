namespace SIRGA.Application.Services;

/// <summary>Información de la empresa en los registros históricos del sindicato.</summary>
public record LegacyCompanyInfo(string BusinessName, bool HasRecords);

/// <summary>
/// Consulta read-only a la base legada (Sindicato) para verificar empresas reales.
/// </summary>
public interface ILegacyCompanyLookup
{
  /// <summary>Busca el CUIT normalizado (11 dígitos) en el legado. Devuelve null si no existe o no hay acceso.</summary>
  Task<LegacyCompanyInfo?> FindAsync(string normalizedCuit);
}