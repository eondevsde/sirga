using System.Net;
using System.Text.Json;

namespace SIRGA.WebUI.Middleware;

public class ErrorHandlerMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<ErrorHandlerMiddleware> _logger;

  public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task Invoke(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception error)
    {
      var response = context.Response;
      if (response.HasStarted)
      {
        _logger.LogWarning("La respuesta ya comenzó, no se puede escribir el error JSON.");
        _logger.LogError(error, "Error capturado después de que la respuesta comenzó: {Message}", error.Message);
        return;
      }

      response.ContentType = "application/json";

      switch (error)
      {
        case KeyNotFoundException e:
          response.StatusCode = (int)HttpStatusCode.NotFound;
          break;
        default:
          _logger.LogError(error, error.Message);
          response.StatusCode = (int)HttpStatusCode.InternalServerError;
          break;
      }

      // El mensaje interno NO se devuelve al cliente: un 500 provocado desde afuera (por ejemplo
      // con un id inválido en un webhook) alcanzaba para leer detalles de la excepción —nombres de
      // tablas y columnas, restricciones, mensajes de EF—. El detalle queda en el log del servidor.
      var result = JsonSerializer.Serialize(new { message = "Ocurrió un error interno. Intente nuevamente y, si persiste, avise al administrador." });
      await response.WriteAsync(result);
    }
  }
}
