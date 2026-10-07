namespace SIRGA.Application.DTOs;

public class DeclarationRequestDto
{
  public int CompanyId { get; set; }
  public int UnionId { get; set; }
  public string Period { get; set; } = null!; // YYYY-MM
  public List<EmployeeSalaryDto> Salaries { get; set; } = new();
}

public class EmployeeSalaryDto
{
  public int EmployeeId { get; set; }
  public decimal BaseSalary { get; set; }
}

/// <summary>
/// La DDJJ VIGENTE de una empresa y período: la que quedaría reemplazada si se presenta otra. La pantalla
/// la usa para AVISAR que presentar va a ser una rectificativa (y contra cuánto se calcula el saldo).
/// </summary>
public record DeclarationAnteriorDto(
    int Id,
    int VersionNumber,
    decimal TotalAmount,
    decimal Pagado,
    string Status,
    DateTime SubmissionDate);

/// <summary>
/// Comparación EMPLEADO POR EMPLEADO entre la versión vigente de una DDJJ y lo que daría presentarla de
/// nuevo. Se arma con el MISMO cálculo que guarda la DDJJ, así no puede diferir de lo que se va a guardar.
/// </summary>
public record ComparacionRectificativaDto(
    int VersionAnterior,
    decimal TotalAnterior,
    decimal PagadoAnterior,
    decimal TotalNuevo,
    decimal DiferenciaTotal,
    IReadOnlyList<ComparacionEmpleadoDto> Empleados);

/// <summary>Un empleado en la comparación. <c>Estado</c>: Igual, Sube, Baja, Nuevo o Sale.</summary>
public record ComparacionEmpleadoDto(
    string Empleado,
    string? Dni,
    string Categoria,
    decimal BaseAnterior,
    decimal BaseNueva,
    decimal ImporteAnterior,
    decimal ImporteNuevo,
    string Estado,
    bool UsoEscala);

public class DeclarationResponseDto
{
  public int Id { get; set; }
  public string Period { get; set; } = null!;
  public decimal TotalAmount { get; set; }
  public string Status { get; set; } = null!;
  public string CompanyName { get; set; } = null!;
  public string UnionName { get; set; } = null!;
  public DateTime SubmissionDate { get; set; }
  public bool IsRectification { get; set; }

  /// <summary>Número de versión: 1 es la original, 2 la primera rectificativa, y así.</summary>
  public int VersionNumber { get; set; }

  /// <summary>En una rectificativa, lo que se había PAGADO de la versión anterior.</summary>
  public decimal PreviousAmount { get; set; }
  public decimal BalanceAmount { get; set; }
  public string? MercadoPagoPaymentId { get; set; }
  public DateTime? PaymentDate { get; set; }

  // Campos para Boleta Presencial
  public string? PresencialBarCode { get; set; }
  public string? PresencialReference { get; set; }

  // Campos detallados para el reporte PDF
  public string CompanyCuit { get; set; } = null!;
  public DateTime ExpirationDate { get; set; }
  public decimal AporteLey { get; set; }
  public decimal SeguroSepelio { get; set; }
  public decimal CuotaSocio { get; set; }
}

public class PaymentPreferenceResponseDto
{
  public string Id { get; set; } = null!;
  public string InitPoint { get; set; } = null!;
  public string ExternalReference { get; set; } = null!;

  // Propiedades adicionales esperadas por MercadoPagoService
  public string? PreferenceId { get; set; }
  public string? SandboxInitPoint { get; set; }
}

public class DeclarationDetailDto
{
  public int EmployeeId { get; set; }
  public string EmployeeName { get; set; } = null!;
  public string Dni { get; set; } = null!;
  public decimal BaseSalary { get; set; }
  public decimal LawContribution { get; set; }
  public decimal MemberContribution { get; set; }
  public decimal TotalWithheld { get; set; }
  public bool IsSocio { get; set; }
}

public class PagedDeclarationsDto
{
  public int TotalCount { get; set; }
  public decimal TotalAmount { get; set; }
  public List<DeclarationResponseDto> Items { get; set; } = new();
}

public class AuditActionRequestDto
{
  public int DeclarationId { get; set; }
  public string Observation { get; set; } = "";
}