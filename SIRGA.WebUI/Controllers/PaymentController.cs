using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;
using System.Text.Json;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController(ILogger<PaymentController> logger, BankReconciliationService reconciliationService, DeclarationService declarationService, PaymentService paymentService) : ControllerBase
{
  // Cada endpoint exige el rol de la pantalla que lo usa. La lógica que estaba acá se movió a
  // PaymentService para que las pantallas puedan llamarla directamente, sin pasar por HTTP: el
  // HttpClient de los componentes Blazor no lleva la cookie de sesión, así que con el endpoint
  // protegido habrían empezado a recibir 401.
  [HttpPost("import-excel")]
  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  public async Task<IActionResult> ImportExcel(IFormFile file)
  {
    if (file == null || file.Length == 0) return BadRequest("Archivo no proporcionado");

    using var stream = file.OpenReadStream();
    var result = await reconciliationService.ProcessBankExcelAsync(stream);

    return Ok(result);
  }

  [HttpPost("salaries-excel")]
  [Authorize(Roles = "EstudioContable,Auditor,Admin")]
  public async Task<IActionResult> SalariesExcel(IFormFile file)
  {
    if (file == null || file.Length == 0) return BadRequest("Archivo no proporcionado");

    using var stream = file.OpenReadStream();
    var salaries = await declarationService.ProcessExcelDeclarationAsync(stream);

    return Ok(salaries);
  }

  [HttpPost("bulk-confirm")]
  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  public async Task<IActionResult> BulkConfirm([FromBody] List<PaymentTransaction> payments)
  {
    var confirmados = await paymentService.ConfirmBulkAsync(payments);
    return Ok(new { count = confirmados });
  }

  [HttpGet]
  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  public async Task<IActionResult> GetPayments()
  {
    return Ok(await paymentService.GetTransactionsAsync());
  }

  [HttpPost("manual")]
  [Authorize(Roles = "Sindicato,Auditor,Admin")]
  public async Task<IActionResult> RegisterManualPayment([FromBody] PaymentTransaction payment)
  {
    try
    {
      var registrado = await paymentService.RegisterManualPaymentAsync(payment);
      if (registrado == null) return NotFound("Declaración no encontrada");
      return Ok(registrado);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error registrando pago manual");
      return BadRequest(ex.Message);
    }
  }

  // El webhook lo llama MercadoPago desde afuera, así que queda anónimo a propósito: lo que
  // corresponde acá es verificar la firma de la notificación, no exigir cookie de sesión.
  [HttpPost("webhook/mercadopago")]
  public async Task<IActionResult> MercadoPagoWebhook([FromBody] JsonElement payload)
  {
    logger.LogInformation("Webhook recibido de MercadoPago: {Payload}", payload.ToString());

    var success = await paymentService.ProcessExternalNotificationAsync("mercadopago", payload);

    if (success) return Ok();
    return Accepted(); // Retornamos 202 si no lo procesamos pero recibimos la notificación
  }
}
