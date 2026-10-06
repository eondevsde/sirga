namespace SIRGA.Application.DTOs;

public class SalaryScaleDto
{
  public int Id { get; set; }

  /// <summary>Rama de actividad del sindicato: las escalas son nacionales por rama, no por sindicato.</summary>
  public int RamaId { get; set; }

  public DateTime Period { get; set; }
  public int EmployeeCategoryId { get; set; }
  public string CategoryCode { get; set; } = "";
  public string CategoryName { get; set; } = "";
  public decimal BasicAmount { get; set; }
  public decimal AgreementNR1 { get; set; }
  public decimal AgreementNR2 { get; set; }
  public decimal AntiguedadPercentage { get; set; }
  public bool IsActive { get; set; }
}

public class SalaryScaleImportDto
{
  public DateTime Period { get; set; }
  public string CategoryCode { get; set; } = "";
  public decimal BasicAmount { get; set; }
  public decimal AgreementNR1 { get; set; }
  public decimal AgreementNR2 { get; set; }
}
