
namespace SIRGA.Domain.Entities;

public partial class Union
{
  /// <summary>
  /// Email del usuario administrador de este sindicato.
  /// Se utiliza para filtrar los datos y que cada sindicato solo vea lo propio.
  /// </summary>
  public string? AdminEmail { get; set; }
}
