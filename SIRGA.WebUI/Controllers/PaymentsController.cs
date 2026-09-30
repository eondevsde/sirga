using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Interfaces;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
  [HttpPost("preferences/{declarationId:int}")]
  public async Task<IActionResult> CreatePreference(int declarationId)
  {
    try
    {
      var result = await paymentService.CreatePaymentPreferenceAsync(declarationId);
      return Ok(result);
    }
    catch (Exception ex)
    {
      return BadRequest(new { message = ex.Message });
    }
  }

  [HttpPost("webhook")]
  public async Task<IActionResult> Webhook([FromQuery] string topic, [FromQuery] string id)
  {
    // MP envía notificaciones por POST usualmente con query params o body
    // v2 usa 'topic' e 'id' en el query string para notificaciones IPN básicas
    bool processed = await paymentService.ProcessWebhookAsync(topic, id);
    return processed ? Ok() : BadRequest();
  }
}
