using SIRGA.Application.DTOs;

namespace SIRGA.Application.Interfaces;

public interface IPaymentService
{
  Task<PaymentPreferenceResponseDto> CreatePaymentPreferenceAsync(int declarationId);
  Task<bool> ProcessWebhookAsync(string topic, string id);
}
