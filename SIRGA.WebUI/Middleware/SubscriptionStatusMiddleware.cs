using System.Security.Claims;
using SIRGA.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace SIRGA.WebUI.Middleware;

public class SubscriptionStatusMiddleware
{
  private readonly RequestDelegate _next;

  public SubscriptionStatusMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
  {
    // 1. Si no está autenticado, seguir
    if (context.User.Identity?.IsAuthenticated != true)
    {
      await _next(context);
      return;
    }

    // 2. Si es Admin global, siempre tiene acceso
    if (context.User.IsInRole("Admin"))
    {
      await _next(context);
      return;
    }

    // 3. Obtener el UnionId del usuario (Sindicato, Estudio o Empresa)
    var unionIdClaim = context.User.FindFirst("UnionId")?.Value;
    if (string.IsNullOrEmpty(unionIdClaim) || !int.TryParse(unionIdClaim, out int unionId))
    {
      await _next(context);
      return;
    }

    // 4. Verificar estado de suscripción con una consulta puntual (antes traía las
    //    suscripciones y los sindicatos completos en cada request autenticado).
    using (var scope = serviceProvider.CreateScope())
    {
      var subscriptionService = scope.ServiceProvider.GetRequiredService<UnionSubscriptionService>();
      var bloqueado = await subscriptionService.IsServiceBlockedAsync(unionId);

      if (bloqueado)
      {
        // Evitar bucle infinito si ya está en la página de bloqueo o logout
        var path = context.Request.Path.Value?.ToLower();
        if (path != "/subscription-expired" && path != "/logout" && !path!.Contains("/api/"))
        {
          context.Response.Redirect("/subscription-expired");
          return;
        }
      }
    }

    await _next(context);
  }
}
