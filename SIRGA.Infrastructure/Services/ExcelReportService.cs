using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SIRGA.Domain.Entities;
using SIRGA.Application.DTOs;

namespace SIRGA.Application.Interfaces
{
  public interface IReportService
  {
    Task<byte[]> ExportEmployeesToExcelAsync(IEnumerable<Employee> employees, string title);
    Task<byte[]> ExportDebtorsToExcelAsync(IEnumerable<CompanyDebtDto> debtors, string title);
    Task<byte[]> ExportCollectionsToExcelAsync(IEnumerable<MonthlyCollectionDto> collections, string title);
  }
}

namespace SIRGA.Infrastructure.Services
{
  public class ExcelReportService : SIRGA.Application.Interfaces.IReportService
  {
    public Task<byte[]> ExportEmployeesToExcelAsync(IEnumerable<Employee> employees, string title)
    {
      using var workbook = new XLWorkbook();
      var worksheet = workbook.Worksheets.Add("Afiliados");

      worksheet.Cell(1, 1).Value = title;
      worksheet.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

      var headers = new[] { "DNI", "Nombre Completo", "Empresa", "Sucursal/Localidad", "Fecha Ingreso", "Estado" };
      for (int i = 0; i < headers.Length; i++)
      {
        var cell = worksheet.Cell(3, i + 1);
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
      }

      int row = 4;
      foreach (var emp in employees)
      {
        var mainAssignment = emp.Assignments?.FirstOrDefault();

        worksheet.Cell(row, 1).Value = emp.Dni;
        worksheet.Cell(row, 2).Value = emp.FullName;
        worksheet.Cell(row, 3).Value = mainAssignment?.Company?.BusinessName ?? "N/A";
        worksheet.Cell(row, 4).Value = mainAssignment?.Company?.Address ?? "N/A";
        worksheet.Cell(row, 5).Value = emp.HireDate.ToShortDateString();
        worksheet.Cell(row, 6).Value = emp.Status.ToString();
        row++;
      }

      worksheet.Columns().AdjustToContents();

      using var stream = new MemoryStream();
      workbook.SaveAs(stream);
      return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> ExportDebtorsToExcelAsync(IEnumerable<CompanyDebtDto> debtors, string title)
    {
      using var workbook = new XLWorkbook();
      var worksheet = workbook.Worksheets.Add("Deudores");

      worksheet.Cell(1, 1).Value = title;
      worksheet.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

      var headers = new[] { "Empresa", "Sucursal", "Empleados", "Ãšltimo Periodo", "Deuda Capital", "Intereses ARCA", "Total" };
      for (int i = 0; i < headers.Length; i++)
      {
        var cell = worksheet.Cell(3, i + 1);
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.Orange;
        cell.Style.Font.FontColor = XLColor.White;
      }

      int row = 4;
      foreach (var d in debtors)
      {
        worksheet.Cell(row, 1).Value = d.BusinessName;
        worksheet.Cell(row, 2).Value = d.BranchName;
        worksheet.Cell(row, 3).Value = d.EmployeeCount;
        worksheet.Cell(row, 4).Value = d.LastPeriodPresented;
        worksheet.Cell(row, 5).Value = (double)d.EstimatedDebt;
        worksheet.Cell(row, 6).Value = (double)d.Interests;
        worksheet.Cell(row, 7).FormulaA1 = "E" + row + "+F" + row;

        worksheet.Cell(row, 5).Style.NumberFormat.Format = "$ #,##0.00";
        worksheet.Cell(row, 6).Style.NumberFormat.Format = "$ #,##0.00";
        worksheet.Cell(row, 7).Style.NumberFormat.Format = "$ #,##0.00";
        row++;
      }

      worksheet.Columns().AdjustToContents();
      using var stream = new MemoryStream();
      workbook.SaveAs(stream);
      return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> ExportCollectionsToExcelAsync(IEnumerable<MonthlyCollectionDto> collections, string title)
    {
      using var workbook = new XLWorkbook();
      var worksheet = workbook.Worksheets.Add("RecaudaciÃ³n");

      worksheet.Cell(1, 1).Value = title;
      worksheet.Range(1, 1, 1, 2).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

      var headers = new[] { "Periodo", "Total Recaudado" };
      for (int i = 0; i < headers.Length; i++)
      {
        var cell = worksheet.Cell(3, i + 1);
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.Green;
        cell.Style.Font.FontColor = XLColor.White;
      }

      int row = 4;
      foreach (var c in collections)
      {
        worksheet.Cell(row, 1).Value = c.Period;
        worksheet.Cell(row, 2).Value = (double)c.Amount;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "$ #,##0.00";
        row++;
      }

      worksheet.Columns().AdjustToContents();
      using var stream = new MemoryStream();
      workbook.SaveAs(stream);
      return Task.FromResult(stream.ToArray());
    }
  }
}
