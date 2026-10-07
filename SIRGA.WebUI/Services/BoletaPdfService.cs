using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIRGA.Application.DTOs;
using SIRGA.Application.Services;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;

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
  /// <summary>Margen de la hoja, en puntos.</summary>
  private const float MargenPagina = 24f;

  /// <summary>Separación entre talones, en puntos.</summary>
  private const float SeparacionTalones = 8f;
  /// <summary>
  /// Logo del sindicato para el encabezado de cada talón.
  ///
  /// Va **embebido en el ensamblado** (ver `SIRGA.WebUI.csproj`), no se lee del disco: así se encuentra
  /// siempre —en desarrollo, publicado o desde un verificador— sin depender de rutas. Queda además la copia
  /// en `wwwroot/images` por si el front lo necesita.
  ///
  /// **Si por algún motivo no estuviera, la boleta se emite igual SIN el logo**: no se puede dejar de emitir
  /// una boleta por una imagen.
  /// </summary>
  private static readonly Lazy<byte[]?> Logo = new(CargarLogo, isThreadSafe: true);

  private const string NombreArchivoLogo = "logo-santiago-del-estero.jpg";

  private static byte[]? CargarLogo()
  {
    var ensamblado = typeof(BoletaPdfService).Assembly;

    var recurso = ensamblado.GetManifestResourceNames()
        .FirstOrDefault(n => n.EndsWith(NombreArchivoLogo, StringComparison.OrdinalIgnoreCase));

    if (recurso is null) return null;

    using var stream = ensamblado.GetManifestResourceStream(recurso);
    if (stream is null) return null;

    using var memoria = new MemoryStream();
    stream.CopyTo(memoria);
    return memoria.ToArray();
  }

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
        pagina.Margin(MargenPagina);
        pagina.DefaultTextStyle(estilo => estilo.FontSize(9));

        // Los tres talones se reparten TODO el alto de la hoja: antes quedaban apilados arriba y sobraba un
        // tercio de papel abajo. El alto se calcula del tamaño de hoja y los márgenes —no se pone un número
        // fijo— así sigue estando bien si cambia el papel o el margen.
        // OJO: acá NO sirve `Extend()`: con varios elementos, cada uno se queda con la hoja entera y la
        // boleta se va a 6 páginas (probado).
        float altoTalon = (PageSizes.A4.Height - (MargenPagina * 2) - (SeparacionTalones * 2)) / BoletaBse.Talones.Length;

        pagina.Content().Column(columna =>
        {
          columna.Spacing(SeparacionTalones);

          // Los talones salen de BoletaBse.Talones (el banco pide triplicado): no se escriben a mano
          // acá para que no se desincronicen si el banco cambia el pedido.
          foreach (var talon in BoletaBse.Talones)
            columna.Item().Height(altoTalon).Element(contenedor => Talon(contenedor, boleta, talon));
        });
      });
    }).GeneratePdf();
  }

  private static void Talon(IContainer contenedor, BoletaDatosDto boleta, string etiquetaTalon)
  {
    contenedor.Border(1).BorderColor(Colors.Grey.Medium).Padding(8).Layers(capas =>
    {
      // El talón se arma en DOS CAPAS: arriba el encabezado y los datos, y PEGADO AL PIE el código de
      // barras con su número. Se hace con capas y no con un espaciador porque así el pie queda al fondo de
      // la caja sin depender de cuánto mida el contenido: si no, cualquier línea de más (por ejemplo el
      // detalle del interés, que sólo aparece cuando la DDJJ está vencida) lo correría.
      capas.PrimaryLayer().Column(columna =>
      {
      columna.Spacing(3);

      // Encabezado: logo del sindicato, conceptos que se cobran y datos bancarios.
      columna.Item().Row(fila =>
      {
        // El logo va primero, a la izquierda del nombre del sindicato. Se dibuja sólo si el archivo está.
        if (Logo.Value is { } logo)
        {
          fila.ConstantItem(44).Image(logo).FitArea();
        }

        fila.RelativeItem().PaddingLeft(Logo.Value is null ? 0 : 6).Column(datos =>
        {
          // El nombre del sindicato y su jurisdicción van en DOS líneas: el nombre ya no incluye la
          // provincia (si no, se duplicaba).
          datos.Item().Text(boleta.Sindicato).Bold().FontSize(11);
          if (!string.IsNullOrWhiteSpace(boleta.Jurisdiccion))
            datos.Item().Text(boleta.Jurisdiccion).Bold().FontSize(10);
          datos.Item().Text("Aportes Art. 100°  Convenio 130/75 | Aportes Cuota Sindical |").Italic().FontSize(8);
          datos.Item().Text($"{boleta.Banco} - {boleta.Sucursal}").FontSize(8);
          datos.Item().Text($"Nota de Crédito para la Cta. Cte. N° {boleta.NumeroCliente}").FontSize(8);
        });

        // A la derecha van la etiqueta del talón, la fecha de emisión y el QR. El QR va ACÁ y no al lado del
        // código de barras a propósito: un lector láser que barre la línea del código de barras no se lo
        // encuentra en el camino. Es la misma línea del encabezado, así que no agrega alto al talón (y los
        // tres talones siguen entrando en una hoja).
        fila.ConstantItem(150).Row(derecha =>
        {
          derecha.RelativeItem().Column(texto =>
          {
            texto.Item().AlignRight().Text(etiquetaTalon).Bold().FontSize(8);
            texto.Item().AlignRight().Text($"Emitida: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7);
          });

          // El QR va SIN reescalar: si QuestPDF lo redimensiona, interpola los píxeles y los bordes de los
          // módulos quedan borrosos, que es justo lo que perjudica que el lector lo tome.
          derecha.ConstantItem(46).PaddingLeft(4).Image(CodigoQr(boleta.CodigoBarras)).UseOriginalImage().FitArea();
        });
      });

      columna.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

      // RAZÓN SOCIAL SOLA, a todo el ancho del talón: es el dato más largo y, metido en una columna, se
      // cortaba o se partía en varias líneas.
      columna.Item().Text($"Razón Social: {boleta.RazonSocial}").Bold();

      // Debajo, los datos en tres columnas: el contribuyente, los aportes y el vencimiento con el importe.
      columna.Item().Row(fila =>
      {
        fila.RelativeItem().Column(datos =>
        {
          datos.Item().Text($"C.U.I.T.: {boleta.Cuit}");
          datos.Item().Text($"Período: {boleta.Periodo}");
        });

        fila.RelativeItem().Column(datos =>
        {
          // El porcentaje sale del sindicato, no está escrito a mano: cambia por convenio.
          datos.Item().Text($"Ap. {boleta.AporteLeyPorcentaje:0.##}% $ {boleta.AporteLey:N2}");
          datos.Item().Text($"Ap. CS {boleta.CuotaSindicalPorcentaje:0.##}% $ {boleta.CuotaSindical:N2}");
          datos.Item().Text($"Total DDJJ $ {boleta.Capital:N2}");
        });

        // UNA sola fecha de vencimiento con el importe que el banco cobra, en DOS líneas: la fecha arriba y
        // el importe abajo (como pidió el usuario, en lugar de todo en un renglón largo).
        fila.ConstantItem(260).Column(datos =>
        {
          datos.Item().Text($"Vencimiento: {boleta.Vencimiento:dd/MM/yyyy}").Bold();
          datos.Item().Text($"Importe Total a Depositar: $ {boleta.ImporteTotal:N2}").Bold();

          if (boleta.Interes > 0)
          {
            datos.Item()
                .Text($"Incluye interés resarcitorio por {boleta.DiasInteres} día(s): $ {boleta.Interes:N2} " +
                      $"({boleta.InteresPorcentaje:0.####}%){(string.IsNullOrWhiteSpace(boleta.InteresNorma) ? "" : $" - {boleta.InteresNorma}")}")
                .FontSize(7);
          }
        });
      });

      });

      // Capa del PIE: el código de barras, su número en texto (para cargarlo a mano si el lector falla) y la
      // nota, anclados al fondo del talón.
      capas.Layer().AlignBottom().Column(pie =>
      {
        pie.Spacing(3);
        pie.Item().Element(cont => CodigoDeBarras(cont, boleta.CodigoBarras));
        pie.Item().AlignCenter().Text(boleta.CodigoBarras).FontSize(8).LetterSpacing(0.05f);
        pie.Item().AlignCenter().Text("Pagos posteriores al vencimiento, sujetos a actualización.").FontSize(7).Italic();
      });
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

    contenedor.Height(54).Row(fila =>
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

  /// <summary>
  /// Genera el QR con EL MISMO contenido del código de barras y devuelve una imagen lista para imprimir.
  ///
  /// La corrección de errores es M (15%), que es la habitual para un documento impreso: tolera manchas o un
  /// doblez sin dejar de leerse. El margen de 2 módulos es la zona blanca que el lector necesita alrededor.
  /// </summary>
  private static byte[] CodigoQr(string contenido)
  {
    var pistas = new Dictionary<EncodeHintType, object>
    {
      { EncodeHintType.MARGIN, 2 },
      { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M }
    };

    var matriz = new MultiFormatWriter().encode(contenido, BarcodeFormat.QR_CODE, 0, 0, pistas);

    var modulos = new bool[matriz.Width, matriz.Height];
    for (var x = 0; x < matriz.Width; x++)
    {
      for (var y = 0; y < matriz.Height; y++)
      {
        modulos[x, y] = matriz[x, y];
      }
    }

    return MatrizABmp(modulos, EscalaQr);
  }

  /// <summary>
  /// Cuántos píxeles se dibuja cada módulo del QR. Con 1 el QR queda de 29x29 px y al imprimirlo a 44 pt
  /// sale borroso (Skia lo suaviza), lo que complica que el lector lo tome: a 8 px por módulo son ~230 px y
  /// los bordes quedan nítidos.
  /// </summary>
  private const int EscalaQr = 8;

  /// <summary>
  /// Convierte la matriz de módulos del QR (true = oscuro) en un BMP de 24 bits.
  ///
  /// Se arma a mano porque QuestPDF recibe las imágenes como bytes y el proyecto no tiene un codificador:
  /// el BMP **sin compresión** es el más simple de escribir y Skia —el motor que usa QuestPDF— lo lee sin
  /// problema. Se escribe en blanco y negro puro (sin grises) para que los módulos queden nítidos.
  /// </summary>
  private static byte[] MatrizABmp(bool[,] modulos, int escala)
  {
    int modulosX = modulos.GetLength(0);
    int modulosY = modulos.GetLength(1);
    int ancho = modulosX * escala;
    int alto = modulosY * escala;
    int bytesPorFila = (ancho * 3 + 3) / 4 * 4;   // cada fila del BMP se alinea a 4 bytes
    int tamanoPixeles = bytesPorFila * alto;
    const int tamanoCabecera = 14 + 40;           // BITMAPFILEHEADER + BITMAPINFOHEADER

    var datos = new byte[tamanoCabecera + tamanoPixeles];

    datos[0] = (byte)'B';
    datos[1] = (byte)'M';
    BitConverter.GetBytes(tamanoCabecera + tamanoPixeles).CopyTo(datos, 2);   // tamaño total
    BitConverter.GetBytes(tamanoCabecera).CopyTo(datos, 10);                  // desde dónde empiezan los píxeles

    BitConverter.GetBytes(40).CopyTo(datos, 14);                              // tamaño de este encabezado
    BitConverter.GetBytes(ancho).CopyTo(datos, 18);
    BitConverter.GetBytes(alto).CopyTo(datos, 22);
    BitConverter.GetBytes((short)1).CopyTo(datos, 26);                        // planos de color
    BitConverter.GetBytes((short)24).CopyTo(datos, 28);                       // bits por píxel
    BitConverter.GetBytes(tamanoPixeles).CopyTo(datos, 34);

    // Los píxeles van de ABAJO hacia arriba y en orden BGR. Cada módulo se expande a un bloque de
    // `escala` x `escala` píxeles.
    for (var y = 0; y < alto; y++)
    {
      var inicioFila = tamanoCabecera + ((alto - 1 - y) * bytesPorFila);
      var moduloY = y / escala;

      for (var x = 0; x < ancho; x++)
      {
        var valor = modulos[x / escala, moduloY] ? (byte)0 : (byte)255;
        var pixel = inicioFila + (x * 3);
        datos[pixel] = valor;
        datos[pixel + 1] = valor;
        datos[pixel + 2] = valor;
      }
    }

    return datos;
  }
}
