using System.Security.Claims;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Services;

/// <summary>
/// Alcance de datos de un usuario en el área sindicato.
/// Regla acordada: el rol Auditor ve SÓLO el sindicato al que el propio sindicato lo asignó, y eso
/// vale en todas las pantallas. Si todavía no se lo asignaron, no ve datos de NINGÚN sindicato (no
/// cae en "ve todo", que es lo que pasaba antes). El Sindicato ya está acotado por su propia cuenta,
/// el Admin no tiene límite y el estudio ve lo suyo.
/// </summary>
public class AlcanceAuditor(UnionService unionService)
{
  /// <summary>
  /// Devuelve si hay que filtrar por sindicato (o sea, si el usuario es Auditor) junto con el
  /// sindicato asignado, que puede ser null si todavía no se lo asignaron.
  /// </summary>
  public async Task<(bool EsAuditor, int? UnionId)> ResolverAsync(ClaimsPrincipal? user)
  {
    if (user?.Identity?.IsAuthenticated != true) return (false, null);
    if (!user.IsInRole("Auditor")) return (false, null);

    var username = user.Identity.Name ?? string.Empty;
    return (true, await unionService.GetUnionIdForAuditorAsync(username));
  }
}
