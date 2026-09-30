using Microsoft.Extensions.Logging;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;
using System.Text.Json;

namespace SIRGA.Application.Services;

public class PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger)
{
  public async Task<bool> ProcessExternalNotificationAsync(string provider, JsonElement payload)
  {
    try
    {
      if (provider.ToLower() == "mercadopago")
      {
        // En un escenario real, aquí se consultaría la API de MP usando el ID recibido
        // Para este MVP, simulamos que el payload trae la información necesaria

        string action = payload.GetProperty("action").GetString() ?? "";
        if (action == "payment.created" || action == "payment.updated")
        {
          var data = payload.GetProperty("data");
          string externalId = data.GetProperty("id").GetString() ?? "";

          // Supongamos que guardamos el external_reference que es nuestro DeclarationId
          // En MP se suele enviar en la creación del preference
          if (payload.TryGetProperty("external_reference", out var extRefProp))
          {
            if (int.TryParse(extRefProp.GetString(), out int declarationId))
            {
              return await ConfirmPaymentAsync(declarationId, externalId, "MercadoPago");
            }
          }
        }
      }
      return false;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error procesando notificación de pago de {Provider}", provider);
      return false;
    }
  }

  public async Task<bool> ConfirmPaymentAsync(int declarationId, string reference, string method, string? notes = null)
  {
    var declaration = await unitOfWork.Declarations.GetByIdAsync(declarationId);
    if (declaration == null)
    {
      logger.LogWarning("Se recibió pago para DDJJ {Id} pero no existe en la base", declarationId);
      return false;
    }

    if (declaration.Status == DeclarationStatus.Paid)
    {
      logger.LogInformation("La DDJJ {Id} ya estaba marcada como pagada", declarationId);
      return true;
    }

    var transaction = new PaymentTransaction
    {
      DeclarationId = declarationId,
      Amount = declaration.TotalAmount,
      PaymentDate = DateTime.UtcNow,
      ExternalReference = reference,
      PaymentMethod = method,
      Notes = notes ?? $"Pago confirmado vía {method}",
      CreatedAt = DateTime.UtcNow
    };

    await unitOfWork.PaymentTransactions.AddAsync(transaction);
    declaration.Status = DeclarationStatus.Paid;
    await unitOfWork.Declarations.UpdateAsync(declaration);

    await unitOfWork.CompleteAsync();
    logger.LogInformation("Pago confirmado exitosamente para DDJJ {Id}", declarationId);
    return true;
  }
}
