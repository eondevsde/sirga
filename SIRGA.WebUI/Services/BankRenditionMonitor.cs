using SIRGA.Application.Services;
using SIRGA.Infrastructure.Legacy;

namespace SIRGA.WebUI.Services;

/// <summary>
/// Revisa cada tanto la carpeta donde caen los archivos de rendición del banco y los procesa.
///
/// **POR QUÉ UNA CARPETA Y NO LEER EL CORREO**: leer la casilla desde SIRGA obligaría a guardar la
/// contraseña del correo dentro de la aplicación. En cambio, el cliente de correo que ya está instalado
/// (una regla de Outlook, o un flujo) puede **dejar el adjunto en esta carpeta**: la descarga queda
/// resuelta sin credenciales dentro de SIRGA. Después el resto lo hace SIRGA: leer, conciliar y volcar al
/// legado.
///
/// **POR DEFECTO NO ESCRIBE**: procesa en simulacro y deja el detalle en el log. Para que registre de
/// verdad hay que ponerlo explícitamente en la configuración (`RendicionBanco:ModoSimulacro: false`).
///
/// **NUNCA TIRA ABAJO LA APLICACIÓN**: si el correo no llegó, si la carpeta no existe o si algo falla, se
/// registra y se sigue. Que falte la rendición de un día no puede impedir que SIRGA funcione.
/// </summary>
public class BankRenditionMonitor : BackgroundService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<BankRenditionMonitor> _logger;
  private readonly string? _carpetaEntrada;
  private readonly bool _soloSimulacro;
  private readonly TimeSpan _intervalo;

  public BankRenditionMonitor(
      IServiceScopeFactory scopeFactory,
      IConfiguration configuracion,
      ILogger<BankRenditionMonitor> logger)
  {
    _scopeFactory = scopeFactory;
    _logger = logger;

    _carpetaEntrada = configuracion["RendicionBanco:CarpetaEntrada"];
    _soloSimulacro = !string.Equals(configuracion["RendicionBanco:ModoSimulacro"], "false",
        StringComparison.OrdinalIgnoreCase);

    var minutos = int.TryParse(configuracion["RendicionBanco:IntervaloMinutos"], out var valor) && valor > 0
        ? valor
        : 30;
    _intervalo = TimeSpan.FromMinutes(minutos);
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (string.IsNullOrWhiteSpace(_carpetaEntrada))
    {
      // Se avisa una vez y se sale: es una función opcional, no un error de arranque.
      _logger.LogInformation(
          "Rendición del banco: no hay carpeta configurada (RendicionBanco:CarpetaEntrada). No se revisa nada.");
      return;
    }

    _logger.LogInformation("Rendición del banco: se revisa '{Carpeta}' cada {Minutos} minuto(s), en modo {Modo}.",
        _carpetaEntrada, _intervalo.TotalMinutes, _soloSimulacro ? "SIMULACRO (no escribe)" : "ESCRITURA REAL");

    using var temporizador = new PeriodicTimer(_intervalo);

    do
    {
      try
      {
        await RevisarUnaVezAsync(stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        return;
      }
      catch (Exception ex)
      {
        // Un problema con la rendición no puede detener la aplicación ni el servicio.
        _logger.LogError(ex,
            "Rendición del banco: falló la revisión de la carpeta. Se reintenta en {Minutos} minuto(s).",
            _intervalo.TotalMinutes);
      }
    }
    while (await EsperarAsync(temporizador, stoppingToken));
  }

  private static async Task<bool> EsperarAsync(PeriodicTimer temporizador, CancellationToken stoppingToken)
  {
    try
    {
      return await temporizador.WaitForNextTickAsync(stoppingToken);
    }
    catch (OperationCanceledException)
    {
      return false;
    }
  }

  /// <summary>Un barrido completo. Público para poder dispararlo desde una pantalla o una prueba.</summary>
  public async Task<BankRenditionInbox.ResultadoBarrido> RevisarUnaVezAsync(CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(_carpetaEntrada)) return new BankRenditionInbox.ResultadoBarrido([]);

    using var alcance = _scopeFactory.CreateScope();
    var procesador = alcance.ServiceProvider.GetRequiredService<BankRenditionProcessor>();

    var resultado = await new BankRenditionInbox().BarrerAsync(_carpetaEntrada, async rendicion =>
    {
      var proceso = await procesador.ProcesarAsync(rendicion, _soloSimulacro, "rendicion-banco", ct);

      return $"{proceso.Conciliadas} conciliada(s), {proceso.VolcadasAlLegado} volcada(s) al legado, " +
             $"{proceso.SinDdjj} sin DDJJ, {proceso.Parciales} parcial(es)" +
             (proceso.Simulacro ? " [SIMULACRO: no se escribió nada]" : "");
    });

    foreach (var archivo in resultado.Archivos)
    {
      if (archivo.Ok)
      {
        _logger.LogInformation("Rendición del banco: {Archivo} procesado. {Detalle}",
            archivo.Archivo, archivo.Detalle);
      }
      else
      {
        _logger.LogWarning("Rendición del banco: {Archivo} NO se pudo procesar. {Detalle}",
            archivo.Archivo, archivo.Detalle);
      }
    }

    if (resultado.Archivos.Count == 0)
      _logger.LogDebug("Rendición del banco: no había archivos nuevos.");

    return resultado;
  }
}
