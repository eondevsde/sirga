using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIRGA.Application.DTOs;
using NetBarcode;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace SIRGA.Application.Services;

public class PdfReportService
{
  public byte[] GenerateInspectionActPdf(InspectionActResponseDto act)
  {
    QuestPDF.Settings.License = LicenseType.Community;

    return Document.Create(container =>
    {
      container.Page(page =>
      {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

        // Cabecera según ACTA 9940
        page.Header().Column(col =>
        {
          col.Item().AlignCenter().Text(act.UnionName.ToUpper()).FontSize(14).SemiBold();
          col.Item().AlignCenter().Text(act.UnionAddress.ToUpper()).FontSize(9);
          col.Item().AlignCenter().Text($"WATSAPP: {act.UnionPhone}").FontSize(9);

          col.Item().PaddingTop(10).LineHorizontal(1);

          col.Item().PaddingTop(10).Row(row =>
          {
            row.RelativeItem().Column(c =>
            {
              c.Item().Text("Acta de Inspección - Ley Nº 24642").SemiBold();
              c.Item().Text("C.C.T. 130/75 - Empleados de Comercio");
            });

            row.RelativeItem().AlignRight().Column(c =>
            {
              c.Item().Text($"ACTA Nº: {act.ActNumber}").FontSize(12).SemiBold();
              c.Item().Text($"Fecha: {act.IssueDate:dd/MM/yyyy}");
            });
          });

          col.Item().PaddingTop(10).LineHorizontal(1);
        });

        page.Content().PaddingVertical(10).Column(col =>
        {
          // Datos de la Empresa
          col.Item().Table(table =>
          {
            table.ColumnsDefinition(columns =>
            {
              columns.ConstantColumn(100);
              columns.RelativeColumn();
              columns.ConstantColumn(80);
              columns.RelativeColumn();
            });

            table.Cell().Text("Razón Social:").SemiBold();
            table.Cell().Text(act.CompanyName);
            table.Cell().Text("CUIT:").SemiBold();
            table.Cell().Text(act.CompanyCuit);

            table.Cell().Text("Domicilio:").SemiBold();
            table.Cell().ColumnSpan(3).Text(act.CompanyAddress);

            table.Cell().Text("Periodos:").SemiBold();
            table.Cell().ColumnSpan(3).Text(act.PeriodRange);
          });

          col.Item().PaddingTop(20).Text("DETALLE DE DEUDA ESTIMADA").SemiBold().Underline();

          // Tabla de Detalle según Anexo 009940
          col.Item().PaddingTop(10).Table(table =>
          {
            table.ColumnsDefinition(columns =>
            {
              columns.RelativeColumn(); // Periodo
              columns.RelativeColumn(); // Capital
              columns.RelativeColumn(); // Intereses
              columns.RelativeColumn(); // Total
            });

            table.Header(header =>
            {
              header.Cell().Element(CellStyle).AlignCenter().Text("Periodo");
              header.Cell().Element(CellStyle).AlignRight().Text("Capital");
              header.Cell().Element(CellStyle).AlignRight().Text("Intereses");
              header.Cell().Element(CellStyle).AlignRight().Text("Total");
            });

            foreach (var p in act.Periods)
            {
              table.Cell().Element(CellContentStyle).AlignCenter().Text(p.Period);
              table.Cell().Element(CellContentStyle).AlignRight().Text(p.BaseAmount.ToString("N2"));
              table.Cell().Element(CellContentStyle).AlignRight().Text(p.CalculatedInterest.ToString("N2"));
              table.Cell().Element(CellContentStyle).AlignRight().Text(p.Total.ToString("N2"));
            }

            // Totales
            table.Cell().Element(TotalStyle).Text("TOTALES");
            table.Cell().Element(TotalStyle).AlignRight().Text(act.EstimatedDebt.ToString("N2"));
            table.Cell().Element(TotalStyle).AlignRight().Text(act.InterestAmount.ToString("N2"));
            table.Cell().Element(TotalStyle).AlignRight().Text(act.TotalAmount.ToString("N2"));
          });

          // Texto Legal
          col.Item().PaddingTop(20).Text(x =>
          {
            x.ParagraphSpacing(5);
            x.Span("Se intima al empleador a regularizar la situación de pago de los periodos detallados en un plazo de 48 hs. hábiles. ").FontSize(9);
            x.Span("El incumplimiento de la presente dará lugar a las acciones legales correspondientes según Ley 24642.").FontSize(9).Italic();
          });

          if (!string.IsNullOrEmpty(act.Observations))
          {
            col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(5).Text($"Obs: {act.Observations}").FontSize(8);
          }

          // Firmas
          col.Item().PaddingTop(50).Row(row =>
          {
            row.RelativeItem().Column(c =>
            {
              c.Item().PaddingTop(10).LineHorizontal(1);
              c.Item().AlignCenter().Text("Inspector Actuante").FontSize(8);
              c.Item().AlignCenter().Text("Firma y Sello").FontSize(8);
            });
            row.ConstantItem(50);
            row.RelativeItem().Column(c =>
            {
              c.Item().PaddingTop(10).LineHorizontal(1);
              c.Item().AlignCenter().Text("Representante Empresa").FontSize(8);
              c.Item().AlignCenter().Text("Firma y Aclaración").FontSize(8);
            });
          });
        });

        page.Footer().AlignCenter().Text(x =>
        {
          x.Span("Página ");
          x.CurrentPageNumber();
          x.Span(" - SIRGA v1.0");
        });
      });
    }).GeneratePdf();
  }

  public byte[] GeneratePaymentPlanPdf(PaymentPlanResponseDto plan)
  {
    QuestPDF.Settings.License = LicenseType.Community;

    return Document.Create(container =>
    {
      container.Page(page =>
        {
          page.Size(PageSizes.A4);
          page.Margin(1.5f, Unit.Centimetre);
          page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

          page.Header().Column(col =>
            {
              col.Item().AlignCenter().Text("SINDICATO DE TRABAJADORES DE COMERCIO").FontSize(14).SemiBold();
              col.Item().AlignCenter().Text("CONVENIO DE REFINANCIACIÓN DE DEUDA").FontSize(12).SemiBold();
              col.Item().PaddingTop(10).LineHorizontal(1);
            });

          page.Content().PaddingVertical(10).Column(col =>
            {
              col.Item().Row(row =>
                {
                  row.RelativeItem().Column(c =>
                    {
                      c.Item().Text($"Empresa: {plan.CompanyName}").SemiBold();
                      c.Item().Text($"CUIT: {plan.CompanyCuit}");
                      c.Item().Text($"Acta de Referencia: {plan.ActNumber}");
                    });
                  row.RelativeItem().AlignRight().Column(c =>
                    {
                      c.Item().Text($"Fecha Plan: {plan.StartDate:dd/MM/yyyy}");
                      c.Item().Text($"Cuotas: {plan.InstallmentsCount}");
                      c.Item().Text($"Tasa Mensual: {plan.FinancingInterestRate:N2}%");
                    });
                });

              col.Item().PaddingTop(20).Text("CRONOGRAMA DE PAGOS").SemiBold().Underline();

              col.Item().PaddingTop(10).Table(table =>
                {
                  table.ColumnsDefinition(columns =>
                    {
                      columns.ConstantColumn(80);
                      columns.RelativeColumn();
                      columns.RelativeColumn();
                      columns.RelativeColumn();
                    });

                  table.Header(header =>
                    {
                      header.Cell().Element(CellStyle).Text("Cuota");
                      header.Cell().Element(CellStyle).Text("Vencimiento");
                      header.Cell().Element(CellStyle).AlignRight().Text("Importe");
                      header.Cell().Element(CellStyle).AlignCenter().Text("Estado");
                    });

                  foreach (var inst in plan.Installments)
                  {
                    table.Cell().Element(CellContentStyle).Text(inst.InstallmentNumber == 0 ? "ANTICIPO" : inst.InstallmentNumber.ToString());
                    table.Cell().Element(CellContentStyle).Text($"{inst.DueDate:dd/MM/yyyy}");
                    table.Cell().Element(CellContentStyle).AlignRight().Text(inst.Amount.ToString("C2"));
                    table.Cell().Element(CellContentStyle).AlignCenter().Text(inst.Status);
                  }

                  table.Cell().ColumnSpan(2).Element(TotalStyle).Text("TOTAL DEL CONVENIO");
                  table.Cell().ColumnSpan(2).Element(TotalStyle).AlignRight().Text(plan.Installments.Sum(i => i.Amount).ToString("C2"));
                });

              col.Item().PaddingTop(30).Text(x =>
              {
                x.Span("El presente convenio implica el reconocimiento de la deuda reclamada en el Acta de Oficio mencionada. ");
                x.Span("La falta de pago de dos cuotas consecutivas o tres alternadas producirá la caducidad de pleno derecho del plan, haciéndose exigible el total de la deuda remanente.").Italic().FontSize(9);
              });

              col.Item().PaddingTop(50).Row(row =>
                {
                  row.RelativeItem().Column(c =>
                  {
                    c.Item().LineHorizontal(1);
                    c.Item().AlignCenter().Text("Firma del Deudor");
                  });
                  row.ConstantItem(100);
                  row.RelativeItem().Column(c =>
                  {
                    c.Item().LineHorizontal(1);
                    c.Item().AlignCenter().Text("Sindicato Receptor");
                  });
                });
            });

          page.Footer().AlignCenter().Text(x =>
            {
              x.Span("Página ");
              x.CurrentPageNumber();
              x.Span(" - Ref: " + plan.ActNumber);
            });
        });
    }).GeneratePdf();
  }

  public byte[] GeneratePaymentBoleta(DeclarationResponseDto declaration)
  {
    // Configurar QuestPDF (License)
    QuestPDF.Settings.License = LicenseType.Community;

    var document = Document.Create(container =>
    {
      container.Page(page =>
          {
            page.Size(PageSizes.A4);
            page.Margin(1, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

            page.Header().Row(row =>
                {
                  row.RelativeItem().Column(col =>
                    {
                      col.Item().Text("SIRGA - Sistema de Recaudación").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                      col.Item().Text("Boleta de Pago Presencial").FontSize(14);
                    });

                  row.RelativeItem().AlignRight().Column(col =>
                    {
                      col.Item().Text($"Vencimiento: {declaration.ExpirationDate:dd/MM/yyyy}").SemiBold();
                      col.Item().Text($"Periodo: {declaration.Period}").FontSize(12);
                    });
                });

            page.Content().PaddingVertical(10).Column(x =>
                {
                  // Datos de la Empresa y Sindicato
                  x.Item().Table(table =>
                    {
                      table.ColumnsDefinition(columns =>
                        {
                          columns.RelativeColumn();
                          columns.RelativeColumn();
                        });

                      table.Cell().Column(c =>
                    {
                      c.Item().Text("DATOS DEL EMPLEADOR").SemiBold();
                      c.Item().Text($"Empresa: {declaration.CompanyName}");
                      c.Item().Text($"CUIT: {declaration.CompanyCuit}");
                    });

                      table.Cell().Column(c =>
                    {
                      c.Item().Text("SINDICATO RECEPTOR").SemiBold();
                      c.Item().Text(declaration.UnionName);
                    });
                    });

                  x.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                  // Detalle de Liquidación
                  x.Item().Text("DETALLE DE CONCEPTOS").SemiBold();
                  x.Item().Table(table =>
                    {
                      table.ColumnsDefinition(columns =>
                        {
                          columns.RelativeColumn(3);
                          columns.RelativeColumn();
                        });

                      table.Header(header =>
                        {
                          header.Cell().Element(CellStyle).Text("Concepto");
                          header.Cell().Element(CellStyle).AlignRight().Text("Importe");
                        });

                      // Concepto 1: Aporte Ley
                      table.Cell().Element(CellContentStyle).Text("Aporte Ley 2%");
                      table.Cell().Element(CellContentStyle).AlignRight().Text($"$ {declaration.AporteLey:N2}");

                      // Concepto 2: Seguro Sepelio
                      table.Cell().Element(CellContentStyle).Text("Seguro de Sepelio 0.5%");
                      table.Cell().Element(CellContentStyle).AlignRight().Text($"$ {declaration.SeguroSepelio:N2}");

                      // Concepto 3: Cuota Socio (si es mayor a 0)
                      if (declaration.CuotaSocio > 0)
                      {
                        table.Cell().Element(CellContentStyle).Text("Cuota Socio 2%");
                        table.Cell().Element(CellContentStyle).AlignRight().Text($"$ {declaration.CuotaSocio:N2}");
                      }

                      if (declaration.IsRectification)
                      {
                        table.Cell().Element(CellContentStyle).Text("SUBTOTAL DECLARADO").SemiBold();
                        table.Cell().Element(CellContentStyle).AlignRight().Text($"$ {declaration.TotalAmount:N2}").SemiBold();

                        table.Cell().Element(CellContentStyle).Text("(-) PAGADO ANTERIORMENTE").FontColor(Colors.Red.Medium);
                        table.Cell().Element(CellContentStyle).AlignRight().Text($"$ {declaration.PreviousAmount:N2}").FontColor(Colors.Red.Medium);
                      }

                      // Calcular intereses para mostrar el desglose si corresponde
                      var interest = declaration.BalanceAmount - (declaration.IsRectification ? (declaration.TotalAmount - declaration.PreviousAmount) : declaration.TotalAmount);
                      if (interest > 0.01m)
                      {
                        table.Cell().Element(CellContentStyle).Text("INTERESES POR MORA").FontColor(Colors.Orange.Medium);
                        table.Cell().Element(CellContentStyle).AlignRight().Text($"$ {interest:N2}").FontColor(Colors.Orange.Medium);
                      }

                      // Total
                      table.Cell().Element(TotalStyle).Text(declaration.BalanceAmount < 0 ? "SALDO A FAVOR" : "TOTAL A PAGAR");
                      table.Cell().Element(TotalStyle).AlignRight().Text($"$ {(declaration.BalanceAmount > 0 ? declaration.BalanceAmount : (declaration.BalanceAmount < 0 ? Math.Abs(declaration.BalanceAmount) : 0)):N2}");
                    });

                  if (DateTime.Now > declaration.ExpirationDate && declaration.BalanceAmount > 0)
                  {
                    x.Item().PaddingTop(10).Background(Colors.Red.Lighten5).Padding(5).Column(c =>
                    {
                      c.Item().Text("AVISO DE MORA").SemiBold().FontColor(Colors.Red.Medium);
                      c.Item().Text("Esta boleta incluye recargos por pago fuera de término calculados a la fecha de emisión.").FontSize(8);
                    });
                  }

                  var codigoPresencial = declaration.PresencialBarCode;
                  if (!string.IsNullOrWhiteSpace(codigoPresencial))
                  {
                    x.Item().PaddingTop(30).Row(row =>
                      {
                        // QR Code
                        row.ConstantItem(100).Column(col =>
                      {
                        col.Item().Image(GenerateQrCode(codigoPresencial));
                        col.Item().AlignCenter().Text("PAGO WEB").FontSize(8);
                      });

                        row.RelativeItem().PaddingLeft(20).Column(col =>
                      {
                        col.Item().Text("CÓDIGO PARA PAGO EN VENTANILLA").SemiBold();
                        col.Item().PaddingTop(5).Image(GenerateBarcode(codigoPresencial));
                        col.Item().AlignCenter().Text(codigoPresencial).FontSize(12).ExtraBold();
                      });
                      });
                  }

                  x.Item().PaddingTop(20).Text("Esta boleta es válida únicamente para pago en bancos habilitados y entidades de cobro (Rapipago/Pago Fácil).").FontSize(8).Italic();
                });

            page.Footer().AlignCenter().Text(x =>
                {
                  x.Span("Página ");
                  x.CurrentPageNumber();
                });
          });
    })
    .GeneratePdf();

    return document;
  }

  public byte[] GenerateVisitNoticePdf(VisitDto visit, InspectionActResponseDto act)
  {
    QuestPDF.Settings.License = LicenseType.Community;

    return Document.Create(container =>
    {
      container.Page(page =>
      {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

        page.Header().Column(col =>
        {
          col.Item().AlignCenter().Text(act.UnionName.ToUpper()).FontSize(14).SemiBold();
          col.Item().AlignCenter().Text("CONSTANCIA DE VISITA DE INSPECCIÓN").FontSize(12).SemiBold();
          col.Item().PaddingTop(10).LineHorizontal(1);
        });

        page.Content().PaddingVertical(20).Column(col =>
        {
          col.Item().Row(row =>
          {
            row.RelativeItem().Text(t =>
            {
              t.Span("CONSTANCIA Nro: ").SemiBold();
              t.Span(visit.DocumentNumber);
            });
            row.RelativeItem().AlignRight().Text(t =>
            {
              t.Span("Fecha: ").SemiBold();
              t.Span(visit.VisitDate.ToString("dd/MM/yyyy HH:mm"));
            });
          });

          col.Item().PaddingTop(20).Text(t =>
          {
            t.Span("En la fecha, se constituye el inspector Sr/a. ");
            t.Span(visit.InspectorName).SemiBold();
            t.Span(" en el domicilio sito en ");
            t.Span(act.CompanyAddress).SemiBold();
            t.Span(", perteneciente a la firma ");
            t.Span(act.CompanyName).SemiBold();
            t.Span(" (CUIT: ");
            t.Span(act.CompanyCuit);
            t.Span(").");
          });

          col.Item().PaddingTop(15).Text(t =>
          {
            t.Span("Motivo de la visita: ").SemiBold();
            t.Span($"Verificación de deuda Acta Nro. {act.ActNumber}.");
          });

          col.Item().PaddingTop(10).Text(t =>
          {
            t.Span("Visita Número: ").SemiBold();
            t.Span(visit.VisitNumber.ToString());
            t.Span(" de 3 reglamentarias.");
          });

          col.Item().PaddingTop(20).Border(1).Padding(10).Column(inner =>
          {
            inner.Item().Text("ESTADO DE LA VISITA / OBSERVACIONES:").SemiBold().Underline();
            inner.Item().PaddingTop(5).Text(visit.Status).FontSize(12);
            if (!string.IsNullOrEmpty(visit.Observations))
            {
              inner.Item().PaddingTop(10).Text("Detalle: " + visit.Observations).Italic();
            }
          });

          col.Item().PaddingTop(30).Text(t =>
          {
            t.Span("IMPORTANTE: ").SemiBold();
            t.Span("Se recuerda que el incumplimiento en la regularización de los aportes sindicales (Ley 24642) faculta a la organización a iniciar las acciones legales correspondientes. ");
            if (visit.VisitNumber >= 3)
            {
              t.Span("ESTA REPRESENTA LA ÚLTIMA VISITA INFORMATIVA. Ante la falta de respuesta, se derivarán las actuaciones al departamento jurídico.").FontColor(Colors.Red.Medium).Bold();
            }
          });

          col.Item().PaddingTop(60).Row(row =>
          {
            row.RelativeItem().Column(c =>
            {
              c.Item().LineHorizontal(1);
              c.Item().AlignCenter().Text("Firma del Inspector").FontSize(9);
              c.Item().AlignCenter().Text(visit.InspectorName).FontSize(8);
            });
            row.ConstantItem(80);
            row.RelativeItem().Column(c =>
            {
              c.Item().LineHorizontal(1);
              c.Item().AlignCenter().Text("Firma y Sello de la Empresa").FontSize(9);
              c.Item().AlignCenter().Text("(O constancia de negativa a firmar)").FontSize(8);
            });
          });
        });

        page.Footer().AlignCenter().Text(x =>
        {
          x.Span("SIRGA - Sistema de Recaudación - Documento Oficial de Notificación");
        });
      });
    }).GeneratePdf();
  }

  private byte[] GenerateBarcode(string content)
  {
    var barcode = new Barcode(content, NetBarcode.Type.Code128, true);
    return barcode.GetByteArray();
  }

  private byte[] GenerateQrCode(string content)
  {
    using var qrGenerator = new QRCodeGenerator();
    using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
    using var qrCode = new PngByteQRCode(qrCodeData);
    return qrCode.GetGraphic(20);
  }

  // Estilos de Celda
  static IContainer CellStyle(IContainer container)
  {
    return container.DefaultTextStyle(x => x.SemiBold())
                    .PaddingVertical(5)
                    .BorderBottom(1)
                    .BorderColor(Colors.Black);
  }

  static IContainer CellContentStyle(IContainer container)
  {
    return container.PaddingVertical(5);
  }

  static IContainer TotalStyle(IContainer container)
  {
    return container.PaddingVertical(10)
                    .BorderTop(2)
                    .BorderColor(Colors.Black)
                    .DefaultTextStyle(x => x.FontSize(12).ExtraBold());
  }
}
