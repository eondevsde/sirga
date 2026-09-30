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

    // 4. Verificar estado de suscripción
    using (var scope = serviceProvider.CreateScope())
    {
      var subscriptionService = scope.ServiceProvider.GetRequiredService<UnionSubscriptionService>();
      var subscriptions = await subscriptionService.GetAllSubscriptionsAsync();
      var sub = subscriptions.FirstOrDefault(s => s.UnionId == unionId);

      if (sub != null)
      {
        // Si el servicio está explícitamente inactivo o la fecha de vencimiento pasó
        if (!sub.IsServiceActive || sub.NextBillingDate < DateTime.Now)
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
    }

    await _next(context);
  }
}
