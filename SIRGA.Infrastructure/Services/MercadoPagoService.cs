using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.Extensions.Configuration;
using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Enums;

namespace SIRGA.Infrastructure.Services;

public class MercadoPagoService : IPaymentService
{
  private readonly IConfiguration _configuration;
  private readonly IUnitOfWork _unitOfWork;

  public MercadoPagoService(IConfiguration configuration, IUnitOfWork unitOfWork)
  {
    _configuration = configuration;
    _unitOfWork = unitOfWork;

    // Inicializar SDK con AccessToken de configuración
    var accessToken = _configuration["MercadoPago:AccessToken"];
    if (!string.IsNullOrEmpty(accessToken))
    {
      MercadoPagoConfig.AccessToken = accessToken;
    }
  }

  public async Task<PaymentPreferenceResponseDto> CreatePaymentPreferenceAsync(int declarationId)
  {
    var declaration = await _unitOfWork.Declarations.GetByIdAsync(declarationId);
    if (declaration == null) throw new Exception("Declaración no encontrada");

    var company = await _unitOfWork.Companies.GetByIdAsync(declaration.CompanyId);
    if (company == null) throw new Exception("Empresa no encontrada");

    var client = new PreferenceClient();

    var request = new PreferenceRequest
    {
      Items = new List<PreferenceItemRequest>
            {
                new PreferenceItemRequest
                {
                    Title = $"DDJJ - Periodo {declaration.Period} - {company.BusinessName}",
                    Quantity = 1,
                    CurrencyId = "ARS",
                    UnitPrice = declaration.BalanceAmount > 0 ? declaration.BalanceAmount : 0,
                }
            },
      Payer = new PreferencePayerRequest
      {
        Name = company.BusinessName,
        Identification = new MercadoPago.Client.Common.IdentificationRequest
        {
          Type = "CUIT",
          Number = company.Cuit
        }
      },
      BackUrls = new PreferenceBackUrlsRequest
      {
        Success = _configuration["MercadoPago:BackUrls:Success"],
        Failure = _configuration["MercadoPago:BackUrls:Failure"],
        Pending = _configuration["MercadoPago:BackUrls:Pending"],
      },
      AutoReturn = "approved",
      // El ExternalReference a nivel de PREFERENCIA identifica la transacción única (DDJJ + Empresa)
      ExternalReference = $"DDJJ_{declaration.Id}_CLI_{company.Id}",
      NotificationUrl = _configuration["MercadoPago:WebhookUrl"],
    };

    Preference preference = await client.CreateAsync(request);

    // Guardar la referencia externa en nuestra DB para auditoría
    declaration.ExternalReference = request.ExternalReference;
    await _unitOfWork.CompleteAsync();

    return new PaymentPreferenceResponseDto
    {
      PreferenceId = preference.Id,
      InitPoint = preference.InitPoint,
      SandboxInitPoint = preference.SandboxInitPoint
    };
  }

  public async Task<bool> ProcessWebhookAsync(string topic, string id)
  {
    // En Mercado Pago v2, el topic suele ser 'payment' o 'merchant_order'
    if (topic != "payment") return false;

    // Un id no numérico no es un fallo transitorio: se descarta sin llamar a la API. Antes
    // long.Parse lanzaba FormatException y el endpoint (anónimo) respondía 500 con cualquier
    // cosa que le mandaran. Un fallo real de MercadoPago sigue propagándose como 5xx a
    // propósito, para que MP reintente la notificación.
    if (!long.TryParse(id, out var paymentId)) return false;

    var client = new MercadoPago.Client.Payment.PaymentClient();
    var payment = await client.GetAsync(paymentId);

    if (payment.Status != "approved") return false;

    // Extraer el ID de la DDJJ desde el ExternalReference (Formato: DDJJ_{id}_CLI_{comp})
    if (string.IsNullOrEmpty(payment.ExternalReference)) return false;

    var parts = payment.ExternalReference.Split('_');
    if (parts.Length < 2 || !int.TryParse(parts[1], out int declarationId)) return false;

    var declaration = await _unitOfWork.Declarations.GetByIdAsync(declarationId);
    if (declaration == null) return false;

    declaration.Status = DeclarationStatus.Paid;
    declaration.MercadoPagoPaymentId = payment.Id.ToString();
    declaration.PaymentDate = payment.DateApproved;
    await _unitOfWork.CompleteAsync();
    return true;
  }
}
