using Microsoft.EntityFrameworkCore;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

/// <summary>
/// Validaciones para el alta/edición de empresas por estudios contables:
/// normalización y dígito verificador de CUIT, deduplicación y pertenencia a estudios activos.
/// </summary>
public class CompanyValidationService(IUnitOfWork unitOfWork, ILegacyCompanyLookup legacyLookup)
{
  private static readonly int[] CuitWeights = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

  /// <summary>Normaliza un CUIT a 11 dígitos (completa con ceros a la izquierda, quita guiones/espacios).</summary>
  public static string NormalizeCuit(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    var digits = new string(value.Trim().Where(char.IsDigit).ToArray());
    if (digits.Length == 0) return string.Empty;
    return digits.Length >= 11 ? digits : digits.PadLeft(11, '0');
  }

  /// <summary>Formatea un CUIT normalizado como XX-XXXXXXXX-X.</summary>
  public static string FormatCuit(string? value) => NormalizeCuit(value) switch
  {
    { Length: 11 } d => $"{d[..2]}-{d.Substring(2, 8)}-{d[^1]}",
    var d => d
  };

  /// <summary>Valida formato, prefijo (persona física/jurídica) y dígito verificador del CUIT.</summary>
  public static (bool Ok, string? Error) ValidateCuit(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return (false, "El CUIT debe tener 11 dígitos.");
    var digits = new string(value.Trim().Where(char.IsDigit).ToArray());
    if (digits.Length != 11) return (false, "El CUIT debe tener 11 dígitos.");

    var prefix = digits[..2];
    if (prefix is not ("20" or "21" or "23" or "24" or "25" or "26" or "27"
                        or "30" or "31" or "32" or "33" or "34"))
      return (false, "El prefijo del CUIT no corresponde a un tipo válido (persona física o jurídica).");

    int sum = 0;
    for (int i = 0; i < 10; i++)
      sum += (digits[i] - '0') * CuitWeights[i];

    int dv = 11 - (sum % 11);
    if (dv == 11) dv = 0;
    else if (dv == 10) dv = 9;

    if (dv != digits[10] - '0')
      return (false, "El dígito verificador del CUIT es incorrecto.");

    return (true, null);
  }

  /// <summary>Normaliza texto para comparaciones: recorta, colapsa espacios y pasa a mayúsculas.</summary>
  public static string NormalizeText(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    return string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
  }

  /// <summary>Normaliza un teléfono conservando solo dígitos.</summary>
  public static string NormalizePhone(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    return new string(value.Trim().Where(char.IsDigit).ToArray());
  }

  /// <summary>Busca la casa central registrada con el mismo CUIT normalizado.</summary>
  public async Task<Company?> FindParentByCuitAsync(string? cuit)
  {
    var norm = NormalizeCuit(cuit);
    if (norm.Length != 11) return null;
    var matches = await unitOfWork.Companies.FindAsync(c => c.NormalizedCuit == norm && c.ParentCompanyId == null);
    return matches.FirstOrDefault();
  }

  /// <summary>Devuelve el estudio contable activo (aprobado) dueño de la empresa, o null.</summary>
  public async Task<AccountingStudio?> FindActiveOwnerAsync(Company company)
  {
    if (company.AccountingStudioId is not { } studioId) return null;
    var studio = await unitOfWork.AccountingStudios.GetByIdAsync(studioId);
    return studio is { IsActive: true, IsApproved: true } ? studio : null;
  }

  /// <summary>
  /// Detecta coincidencias exactas (CUIT, razón social, dirección+localidad, teléfono)
  /// entre casas centrales, ignorando la empresa indicada.
  /// </summary>
  public async Task<CompanyDuplicates> GetDuplicatesAsync(Company company, int? ignoreId = null)
  {
    var result = new CompanyDuplicates();
    var normCuit = NormalizeCuit(company.Cuit);
    var normName = NormalizeText(company.BusinessName);
    var normAddress = NormalizeText(company.Address);
    var normPhone = NormalizePhone(company.Phone);

    var all = await unitOfWork.Companies.RunQueryAsync(q =>
        q.Where(c => c.ParentCompanyId == null && c.Id != (ignoreId ?? -1))
         .AsNoTracking()
         .ToListAsync());

    foreach (var c in all)
    {
      if (normCuit.Length == 11 && NormalizeCuit(c.Cuit) == normCuit)
        result.CuitDuplicate = c;
      if (normName.Length > 0 && NormalizeText(c.BusinessName) == normName)
        result.BusinessNameDuplicate = c;
      if (!string.IsNullOrWhiteSpace(company.Address) && company.CityId.HasValue
          && c.CityId == company.CityId && normAddress.Length > 0 && NormalizeText(c.Address) == normAddress)
        result.AddressDuplicate = c;
      if (!string.IsNullOrWhiteSpace(company.Phone) && normPhone.Length > 0 && NormalizePhone(c.Phone) == normPhone)
        result.PhoneDuplicate = c;
    }

    return result;
  }

  /// <summary>Consulta los registros históricos del sindicato (maeemp/ddjj/socemp) para el CUIT.</summary>
  public async Task<LegacyCompanyInfo?> FindInLegacyAsync(string? cuit)
  {
    var norm = NormalizeCuit(cuit);
    if (norm.Length != 11) return null;
    return await legacyLookup.FindAsync(norm);
  }
}

/// <summary>Resultado de la detección de duplicados por campo.</summary>
public class CompanyDuplicates
{
  public Company? CuitDuplicate { get; set; }
  public Company? BusinessNameDuplicate { get; set; }
  public Company? AddressDuplicate { get; set; }
  public Company? PhoneDuplicate { get; set; }
}