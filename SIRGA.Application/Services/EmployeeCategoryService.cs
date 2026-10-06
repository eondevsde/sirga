using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Application.DTOs;

namespace SIRGA.Application.Services;

public class EmployeeCategoryService(IUnitOfWork unitOfWork)
{
  /// <summary>
  /// Categorías de una RAMA de actividad. Antes se pedían por sindicato: las categorías son las mismas
  /// para todos los sindicatos de la misma rama (Comercio 130/75), igual que las escalas.
  /// </summary>
  public async Task<List<EmployeeCategory>> GetByRamaAsync(int ramaId)
  {
    var cats = await unitOfWork.EmployeeCategories.FindAsync(c => c.RamaId == ramaId);
    return cats.ToList();
  }

  public async Task CreateAsync(EmployeeCategory category)
  {
    await unitOfWork.EmployeeCategories.AddAsync(category);
    await unitOfWork.CompleteAsync();
  }

  public async Task UpdateAsync(EmployeeCategory category)
  {
    var existing = await unitOfWork.EmployeeCategories.GetByIdAsync(category.Id);
    if (existing != null)
    {
      existing.Name = category.Name;
      existing.Code = category.Code;
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task DeleteAsync(int id)
  {
    var existing = await unitOfWork.EmployeeCategories.GetByIdAsync(id);
    if (existing != null)
    {
      // Validar que no haya escalas usando esta categoría
      var scales = await unitOfWork.SalaryScales.FindAsync(s => s.EmployeeCategoryId == id);
      if (scales.Any()) throw new Exception("No se puede eliminar la categoría porque tiene escalas salariales asociadas.");

      await unitOfWork.EmployeeCategories.RemoveAsync(existing);
      await unitOfWork.CompleteAsync();
    }
  }
}
