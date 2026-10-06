using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;
using ZXing;
using ZXing.Common;

namespace SIRGA.WebUI.Services;

/// <summary>
/// Boleta de pago para el cobro en el banco, en PDF y por TRIPLICADO: talón para el CLIENTE, talón para
/// el BANCO y talón para el SINDICATO. Los tres llevan los mismos datos y el mismo código de barras.
///
/// El código de barras se dibuja a partir del patrón que genera ZXing: se arma una fila con las barras
/// como rectángulos, así no hace falta ninguna imagen ni una librería de dibujo extra.
///
/// Este servicio sólo DIBUJA: los datos los arma <see cref="BoletaService"/> (capa de negocio). Antes
/// tenía su propio record de datos y nadie lo llamaba, porque faltaba justamente quien lo armara.
/// </summary>
public class BoletaPdfService
{
  public byte[] Generar(BoletaDatosDto boleta)
  {
    ArgumentNullException.ThrowIfNull(boleta);

    if (string.IsNullOrWhiteSpace(boleta.CodigoBarras))
      throw new ArgumentException("La boleta necesita su código de barras.", nameof(boleta));

    QuestPDF.Settings.License = LicenseType.Community;

    return Document.Create(documento =>
    {
      documento.Page(pagina =>
      {
        pagina.Size(PageSizes.A4);
        pagina.Margin(24);
        pagina.DefaultTextStyle(estilo => estilo.FontSize(9));

        pagina.Content().Column(columna =>
        {
          columna.Spacing(8);

          // Los talones salen de BoletaBse.Talones (el banco pide triplicado): no se escriben a mano
          // acá para que no se desincronicen si el banco cambia el pedido.
          foreach (var talon in BoletaBse.Talones)
            columna.Item().Element(contenedor => Talon(contenedor, boleta, talon));
        });
      });
    }).GeneratePdf();
  }

  private static void Talon(IContainer contenedor, BoletaDatosDto boleta, string etiquetaTalon)
  {
    contenedor.Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(columna =>
    {
      columna.Spacing(4);

      // Encabezado: sindicato, conceptos que se cobran y datos bancarios.
      columna.Item().Row(fila =>
      {
        fila.RelativeItem().Column(datos =>
        {
          datos.Item().Text(boleta.Sindicato).Bold().FontSize(11);
          datos.Item().Text("Aportes Art. 100°  Convenio 130/75 | Aportes Cuota Sindical |").Italic().FontSize(8);
          datos.Item().Text($"{boleta.Banco} - {boleta.Sucursal}").FontSize(8);
          datos.Item().Text($"Nota de Crédito para la Cta. Cte. N° {boleta.NumeroCliente}").FontSize(8);
        });

        fila.ConstantItem(150).AlignRight().Column(derecha =>
        {
          derecha.Item().Text(etiquetaTalon).Bold().FontSize(8);
          derecha.Item().Text($"Emitida: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7);
        });
      });

      columna.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

      // Datos del contribuyente y del período.
      columna.Item().Row(fila =>
      {
        fila.RelativeItem().Column(datos =>
        {
          datos.Item().Text($"Razón Social: {boleta.RazonSocial}").Bold();
          datos.Item().Text($"C.U.I.T.: {boleta.Cuit}");
          datos.Item().Text($"Período: {boleta.Periodo}");
        });

        fila.RelativeItem().Column(datos =>
        {
          datos.Item().Text($"Ap. 2% $ {boleta.AporteLey:N2}");
          datos.Item().Text($"Ap. C.S. $ {boleta.CuotaSindical:N2}");
          datos.Item().Text($"Imp. Rec.: $ {boleta.Recargo:N2}");
        });

        // Vencimientos con sus importes: es lo que el banco cobra.
        fila.ConstantItem(260).Column(datos =>
        {
          datos.Item().Text($"1er Venc. {boleta.PrimerVencimiento:dd/MM/yyyy} - Importe Total a Depositar: $ {boleta.ImportePrimerVencimiento:N2}").Bold();
          datos.Item().Text($"2do Venc. {boleta.SegundoVencimiento:dd/MM/yyyy} - Importe Total a Depositar: $ {boleta.ImporteSegundoVencimiento:N2}").Bold();
        });
      });

      // Código de barras y su número en texto, para poder cargarlo a mano si el lector falla.
      columna.Item().PaddingTop(6).Element(cont => CodigoDeBarras(cont, boleta.CodigoBarras));
      columna.Item().AlignCenter().Text(boleta.CodigoBarras).FontSize(8).LetterSpacing(0.05f);
      columna.Item().AlignCenter().Text("Pagos posteriores al primer vencimiento, sujetos a actualización.").FontSize(7).Italic();
    });
  }

  /// <summary>
  /// Dibuja el código de barras. ZXing devuelve la matriz de módulos (barras claras y oscuras) y acá se
  /// agrupan las columnas consecutivas del mismo color: cada grupo es un rectángulo.
  /// </summary>
  private static void CodigoDeBarras(IContainer contenedor, string contenido)
  {
    var matriz = new MultiFormatWriter().encode(contenido, BarcodeFormat.CODE_128, AnchoModulos(contenido), 1);

    // Se recorren las columnas de la fila 0 agrupando las de igual color.
    var grupos = new List<(bool Oscuro, int Ancho)>();
    for (var x = 0; x < matriz.Width; x++)
    {
      var oscuro = matriz[x, 0];
      if (grupos.Count > 0 && grupos[^1].Oscuro == oscuro)
        grupos[^1] = (oscuro, grupos[^1].Ancho + 1);
      else
        grupos.Add((oscuro, 1));
    }

    var total = grupos.Sum(g => g.Ancho);
    if (total == 0) return;

    contenedor.Height(52).Row(fila =>
    {
      fila.Spacing(0);
      foreach (var grupo in grupos)
      {
        var ancho = grupo.Ancho;
        fila.RelativeItem(ancho).Background(grupo.Oscuro ? Colors.Black : Colors.White);
      }
    });
  }

  /// <summary>
  /// Cantidad de columnas que se le piden a ZXing. Es el ancho del patrón sin escalar: después cada
  /// grupo se estira proporcionalmente al ancho disponible, así la boleta se ve igual en cualquier
  /// impresora.
  /// </summary>
  private static int AnchoModulos(string contenido) => Math.Max(contenido.Length * 11, 200);
}
