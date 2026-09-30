using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Application.Services;

public class SalaryScaleService(IUnitOfWork unitOfWork)
{
  public async Task<IEnumerable<SalaryScaleDto>> GetScalesByPeriodAsync(DateTime period)
  {
    var scales = await unitOfWork.SalaryScales.FindAsync(s => s.Period.Year == period.Year && s.Period.Month == period.Month);
    var categoryIds = scales.Select(s => s.EmployeeCategoryId).Distinct();
    var categories = (await unitOfWork.EmployeeCategories.FindAsync(c => categoryIds.Contains(c.Id))).ToDictionary(c => c.Id);

    return scales.Select(s => new SalaryScaleDto
    {
      Id = s.Id,
      UnionId = s.UnionId,
      Period = s.Period,
      EmployeeCategoryId = s.EmployeeCategoryId,
      CategoryCode = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Code : "N/A",
      CategoryName = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Name : "N/A",
      BasicAmount = s.BasicAmount,
      AgreementNR1 = s.AgreementNR1,
      AgreementNR2 = s.AgreementNR2,
      AntiguedadPercentage = s.AntiguedadPercentage,
      IsActive = s.IsActive
    });
  }

  public async Task<IEnumerable<SalaryScaleDto>> GetAllAsync()
  {
    var scales = await unitOfWork.SalaryScales.GetAllAsync();
    var categories = (await unitOfWork.EmployeeCategories.GetAllAsync()).ToDictionary(c => c.Id);

    return scales.Select(s => new SalaryScaleDto
    {
      Id = s.Id,
      UnionId = s.UnionId,
      Period = s.Period,
      EmployeeCategoryId = s.EmployeeCategoryId,
      CategoryCode = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Code : "N/A",
      CategoryName = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Name : "N/A",
      BasicAmount = s.BasicAmount,
      AgreementNR1 = s.AgreementNR1,
      AgreementNR2 = s.AgreementNR2,
      AntiguedadPercentage = s.AntiguedadPercentage,
      IsActive = s.IsActive
    }).OrderByDescending(s => s.Period).ThenBy(s => s.CategoryCode);
  }

  public async Task<List<SalaryScaleDto>> GetByUnionAsync(int unionId)
  {
    var scales = await unitOfWork.SalaryScales.FindAsync(s => s.UnionId == unionId);
    var categories = (await unitOfWork.EmployeeCategories.FindAsync(c => c.UnionId == unionId)).ToDictionary(c => c.Id);

    return scales.Select(s => new SalaryScaleDto
    {
      Id = s.Id,
      UnionId = s.UnionId,
      Period = s.Period,
      EmployeeCategoryId = s.EmployeeCategoryId,
      CategoryCode = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Code : "N/A",
      CategoryName = categories.ContainsKey(s.EmployeeCategoryId) ? categories[s.EmployeeCategoryId].Name : "N/A",
      BasicAmount = s.BasicAmount,
      AgreementNR1 = s.AgreementNR1,
      AgreementNR2 = s.AgreementNR2,
      AntiguedadPercentage = s.AntiguedadPercentage,
      IsActive = s.IsActive
    }).OrderByDescending(s => s.Period).ToList();
  }

  public async Task CreateAsync(SalaryScaleDto dto)
  {
    var entity = new SalaryScale
    {
      Period = dto.Period,
      EmployeeCategoryId = dto.EmployeeCategoryId,
      UnionId = dto.UnionId,
      BasicAmount = dto.BasicAmount,
      AntiguedadPercentage = dto.AntiguedadPercentage,
      AgreementNR1 = dto.AgreementNR1,
      AgreementNR2 = dto.AgreementNR2,
      IsActive = dto.IsActive
    };

    await unitOfWork.SalaryScales.AddAsync(entity);
    await unitOfWork.CompleteAsync();
  }

  public async Task UpdateAsync(SalaryScaleDto dto)
  {
    var entity = await unitOfWork.SalaryScales.GetByIdAsync(dto.Id);
    if (entity != null)
    {
      entity.BasicAmount = dto.BasicAmount;
      entity.AgreementNR1 = dto.AgreementNR1;
      entity.AgreementNR2 = dto.AgreementNR2;
      entity.AntiguedadPercentage = dto.AntiguedadPercentage;
      entity.IsActive = dto.IsActive;
      entity.Period = dto.Period;
      entity.EmployeeCategoryId = dto.EmployeeCategoryId;
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task DeleteAsync(int id)
  {
    var entity = await unitOfWork.SalaryScales.GetByIdAsync(id);
    if (entity != null)
    {
      await unitOfWork.SalaryScales.RemoveAsync(entity);
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task CreateScaleAsync(SalaryScaleDto dto)
  {
    var entity = new SalaryScale
    {
      Period = dto.Period,
      EmployeeCategoryId = dto.EmployeeCategoryId,
      UnionId = dto.UnionId,
      BasicAmount = dto.BasicAmount,
      AntiguedadPercentage = dto.AntiguedadPercentage,
      AgreementNR1 = dto.AgreementNR1,
      AgreementNR2 = dto.AgreementNR2,
      IsActive = true
    };

    await unitOfWork.SalaryScales.AddAsync(entity);
    await unitOfWork.CompleteAsync();
  }

  public async Task UpdateScaleAsync(SalaryScaleDto dto)
  {
    var entity = await unitOfWork.SalaryScales.GetByIdAsync(dto.Id);
    if (entity != null)
    {
      entity.BasicAmount = dto.BasicAmount;
      entity.AgreementNR1 = dto.AgreementNR1;
      entity.AgreementNR2 = dto.AgreementNR2;
      entity.IsActive = dto.IsActive;
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task DeleteScaleAsync(int id)
  {
    var entity = await unitOfWork.SalaryScales.GetByIdAsync(id);
    if (entity != null)
    {
      await unitOfWork.SalaryScales.RemoveAsync(entity);
      await unitOfWork.CompleteAsync();
    }
  }

  public async Task<bool> SeedScalesFromSqlInfoAsync()
  {
    // Este método se podría usar para una carga inicial controlada
    // Por ahora, el usuario puede ejecutar el .sql directamente en la base
    return true;
  }
}
