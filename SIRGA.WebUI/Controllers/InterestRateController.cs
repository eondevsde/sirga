using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterestRateController(IUnitOfWork unitOfWork) : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<IEnumerable<InterestRate>>> GetAll()
  {
    return Ok(await unitOfWork.InterestRates.GetAllAsync());
  }

  [HttpPost]
  public async Task<ActionResult<InterestRate>> Create(InterestRate rate)
  {
    await unitOfWork.InterestRates.AddAsync(rate);
    await unitOfWork.CompleteAsync();
    return CreatedAtAction(nameof(GetAll), new { id = rate.Id }, rate);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(int id)
  {
    var rate = await unitOfWork.InterestRates.GetByIdAsync(id);
    if (rate == null) return NotFound();

    await unitOfWork.InterestRates.RemoveAsync(rate);
    await unitOfWork.CompleteAsync();
    return NoContent();
  }
}
