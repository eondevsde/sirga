using ClosedXML.Excel;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Application.Services;

public class BankReconciliationService
{
  private readonly IUnitOfWork _unitOfWork;

  public BankReconciliationService(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async Task<ReconciliationResultDto> ProcessBankExcelAsync(Stream fileStream)
  {
    var result = new ReconciliationResultDto();
    using var workbook = new XLWorkbook(fileStream);
    var worksheet = workbook.Worksheet(1);
    var rows = worksheet.RowsUsed().Skip(1); // Saltar encabezado

    foreach (var row in rows)
    {
      try
      {
        // Mapeo tentativo: Col A = Fecha, Col B = Referencia/Concepto, Col C = Importe, Col D = CUIT/ID
        var date = row.Cell(1).GetDateTime();
        var reference = row.Cell(2).GetString();
        var amount = (decimal)row.Cell(3).GetDouble();
        var rawId = row.Cell(4).GetString();

        // Intentar matchear por ID de Declaración directamente si está en la referencia
        int matchedId = 0;
        if (int.TryParse(rawId, out matchedId))
        {
          var declaration = await _unitOfWork.Declarations.GetByIdAsync(matchedId);
          if (declaration != null && declaration.Status != DeclarationStatus.Paid)
          {
            if (Math.Abs(declaration.TotalAmount - amount) < 1) // Tolerancia de 1 peso
            {
              result.MatchedPayments.Add(new PaymentTransaction
              {
                DeclarationId = declaration.Id,
                Amount = amount,
                PaymentDate = date,
                ExternalReference = reference,
                PaymentMethod = "Importación Bancaria",
                Notes = $"Conciliado automáticamente desde Excel"
              });
              continue;
            }
          }
        }

        result.UnmatchedRows.Add(new UnmatchedRowDto
        {
          RowNumber = row.RowNumber(),
          Reference = reference,
          Amount = amount,
          Reason = "No se encontró coincidencia exacta por ID o Monto"
        });
      }
      catch
      {
        result.Errors.Add($"Error en fila {row.RowNumber()}: Formato Inválido");
      }
    }
    return result;
  }
}

public class ReconciliationResultDto
{
  public List<PaymentTransaction> MatchedPayments { get; set; } = new();
  public List<UnmatchedRowDto> UnmatchedRows { get; set; } = new();
  public List<string> Errors { get; set; } = new();
}

public class UnmatchedRowDto
{
  public int RowNumber { get; set; }
  public string Reference { get; set; } = "";
  public decimal Amount { get; set; }
  public string Reason { get; set; } = "";
}
