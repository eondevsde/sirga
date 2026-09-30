using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;
using System.Text.Json;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(IUnitOfWork unitOfWork, ILogger<PaymentController> logger, BankReconciliationService reconciliationService, DeclarationService declarationService, PaymentService paymentService) : ControllerBase
{
  [HttpPost("import-excel")]
  public async Task<IActionResult> ImportExcel(IFormFile file)
  {
    if (file == null || file.Length == 0) return BadRequest("Archivo no proporcionado");

    using var stream = file.OpenReadStream();
    var result = await reconciliationService.ProcessBankExcelAsync(stream);

    return Ok(result);
  }

  [HttpPost("salaries-excel")]
  public async Task<IActionResult> SalariesExcel(IFormFile file)
  {
    if (file == null || file.Length == 0) return BadRequest("Archivo no proporcionado");

    using var stream = file.OpenReadStream();
    var salaries = await declarationService.ProcessExcelDeclarationAsync(stream);

    return Ok(salaries);
  }

  [HttpPost("bulk-confirm")]
  public async Task<IActionResult> BulkConfirm([FromBody] List<PaymentTransaction> payments)
  {
    foreach (var payment in payments)
    {
      var declaration = await unitOfWork.Declarations.GetByIdAsync(payment.DeclarationId);
      if (declaration != null)
      {
        await unitOfWork.PaymentTransactions.AddAsync(payment);
        declaration.Status = DeclarationStatus.Paid;
        await unitOfWork.Declarations.UpdateAsync(declaration);
      }
    }
    await unitOfWork.CompleteAsync();
    return Ok(new { count = payments.Count });
  }

  [HttpGet]
  public async Task<IActionResult> GetPayments()
  {
    var payments = await unitOfWork.PaymentTransactions.GetAllAsync();
    return Ok(payments.OrderByDescending(p => p.PaymentDate));
  }

  [HttpPost("manual")]
  public async Task<IActionResult> RegisterManualPayment([FromBody] PaymentTransaction payment)
  {
    try
    {
      var declaration = await unitOfWork.Declarations.GetByIdAsync(payment.DeclarationId);
      if (declaration == null) return NotFound("Declaración no encontrada");

      // Registrar el pago
      payment.CreatedAt = DateTime.UtcNow;
      payment.PaymentMethod = payment.PaymentMethod ?? "Manual/Transferencia";
      await unitOfWork.PaymentTransactions.AddAsync(payment);

      // Actualizar estado de la declaración
      declaration.Status = DeclarationStatus.Paid;
      await unitOfWork.Declarations.UpdateAsync(declaration);

      await unitOfWork.CompleteAsync();
      return Ok(payment);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error registrando pago manual");
      return BadRequest(ex.Message);
    }
  }

  [HttpPost("webhook/mercadopago")]
  public async Task<IActionResult> MercadoPagoWebhook([FromBody] JsonElement payload)
  {
    logger.LogInformation("Webhook recibido de MercadoPago: {Payload}", payload.ToString());

    var success = await paymentService.ProcessExternalNotificationAsync("mercadopago", payload);

    if (success) return Ok();
    return Accepted(); // Retornamos 202 si no lo procesamos pero recibimos la notificación
  }
}
