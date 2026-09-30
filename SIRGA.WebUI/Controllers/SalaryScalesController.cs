using Microsoft.AspNetCore.Mvc;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;

namespace SIRGA.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalaryScalesController(SalaryScaleService salaryScaleService) : ControllerBase
{
  [HttpGet("{year}/{month}")]
  public async Task<ActionResult<IEnumerable<SalaryScaleDto>>> GetScales(int year, int month)
  {
    var period = new DateTime(year, month, 1);
    var scales = await salaryScaleService.GetScalesByPeriodAsync(period);
    return Ok(scales);
  }

  [HttpGet]
  public async Task<ActionResult<IEnumerable<SalaryScaleDto>>> GetAll()
  {
    var scales = await salaryScaleService.GetAllAsync();
    return Ok(scales);
  }

  [HttpPost]
  public async Task<IActionResult> Create(SalaryScaleDto dto)
  {
    await salaryScaleService.CreateScaleAsync(dto);
    return Ok();
  }

  [HttpPut("{id}")]
  public async Task<IActionResult> Update(int id, SalaryScaleDto dto)
  {
    dto.Id = id;
    await salaryScaleService.UpdateScaleAsync(dto);
    return Ok();
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(int id)
  {
    await salaryScaleService.DeleteScaleAsync(id);
    return Ok();
  }
}
