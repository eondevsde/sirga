namespace SIRGA.Application.DTOs;

public class UnionDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string JurisdictionProvince { get; set; } = string.Empty;
  public string? JurisdictionCity { get; set; }
  public string CctCode { get; set; } = string.Empty;
  public string? AdminEmail { get; set; }
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Porcentajes configurados del sindicato. Están acá para que la pantalla de DDJJ no tenga que
  /// hardcodear el 2%: si la pantalla usa un porcentaje y el servicio que persiste usa otro, el
  /// estudio ve un total y se guarda otro, sin aviso.
  /// </summary>
  public decimal LawContributionPercentage { get; set; } = 2m;

  /// <summary>Porcentaje de la cuota de socio (sólo se aplica a los afiliados).</summary>
  public decimal UnionMemberPercentage { get; set; } = 2m;

  /// <summary>Rama de actividad (el convenio). Nullable: un sindicato puede no tenerla asignada.</summary>
  public int? RamaId { get; set; }

  /// <summary>Nombre de la rama, para mostrarlo sin tener que buscarlo.</summary>
  public string? RamaName { get; set; }
}
