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

      var result = JsonSerializer.Serialize(new { message = error?.Message });
      await response.WriteAsync(result);
    }
  }
}
