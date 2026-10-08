using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SIRGA.Infrastructure.Persistence;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.WebUI.Middleware;
using MudBlazor.Services;
using SIRGA.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices(); // Agregar servicios de MudBlazor

// TEXTOS DEL FRONT EN ESPAÑOL (regla del proyecto).
// 1) MudBlazor trae sus textos internos EN INGLÉS y no los toma de la cultura: sin esto se veían
//    "PREVIOUS"/"NEXT" en los pasos de la DDJJ, "Rows per page:" en el paginador y "Ok"/"Cancel" en los
//    diálogos. El localizador propio los traduce (la clave es el texto en inglés).
builder.Services.AddTransient<MudBlazor.MudLocalizer, SIRGA.WebUI.Services.MudLocalizerEspanol>();

// 2) La cultura se fija EXPLÍCITAMENTE en es-AR en lugar de depender de la del servidor: así los nombres de
//    meses, los separadores de miles y las fechas salen en formato argentino también en producción.
var culturaAr = new System.Globalization.CultureInfo("es-AR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culturaAr;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culturaAr;

// Compresión de respuestas dinámicas (HTML de las páginas y JSON de la API).
// Los assets estáticos ya viajan precomprimidos por MapStaticAssets.
// EnableForHttps queda en false (su valor por defecto) para no exponer el patrón BREACH sobre TLS.
builder.Services.AddResponseCompression();

// ASP.NET Core Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 5;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<SIRG_DbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

// Configurar HttpClient para llamadas a la API interna
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7025/")
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddRazorPages();

builder.Services.AddDbContextFactory<SIRG_DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b =>
        {
            b.MigrationsAssembly(typeof(SIRG_DbContext).Assembly.FullName);
            // Reintenta ante fallos transitorios de SQL Server (cortes breves, deadlocks).
            b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        }));

// Se registra como Transient a propósito: el UnitOfWork (scoped) resuelve el contexto una
// sola vez y lo comparte entre sus 24 repositorios, pero los stores de Identity resuelven el
// suyo aparte. Con AddScoped había UNA sola instancia por circuito, y se reprodujo que
// Identity + repositorio en paralelo lanzan "A second operation was started on this context
// instance": el SemaphoreSlim solo serializa el acceso del repositorio, no el de Identity.
builder.Services.AddTransient<SIRG_DbContext>(p =>
    p.GetRequiredService<IDbContextFactory<SIRG_DbContext>>().CreateDbContext());

// Blazor Server: los componentes corren OnInitializedAsync en paralelo sobre el
// DbContext scoped del circuito. Un semáforo compartido serializa el acceso al contexto.
builder.Services.AddScoped(_ => new SemaphoreSlim(1, 1));

// Acceso a la base del sistema anterior (`Sindicato`), de la que se alimenta SIGES WEB: SIRGA maneja las
// DDJJ y registra los pagos, y SIGES WEB LEE `ddjj`/`ddjjt` para saber si una empresa debe.
// Se resuelve la cadena ACA y se pasa al repositorio: no hay ninguna cadena por defecto escondida en el
// codigo. Si falta la configuracion, el repositorio falla con un mensaje claro cuando se lo usa (y no en
// el arranque, para que la app siga funcionando en un entorno que no tenga el legado).
builder.Services.AddScoped(_ => new SIRGA.Infrastructure.Legacy.LegacySindicatoRepository(
    builder.Configuration.GetConnectionString("Legacy") ?? string.Empty));

// Orquesta la conciliación completa: registra el pago en SIRGA y después lo vuelca al legado. Va en
// Infrastructure porque es la única capa que ve las dos bases; la lógica de cada lado vive en su servicio.
builder.Services.AddScoped<SIRGA.Infrastructure.Legacy.BankRenditionProcessor>();

// Revisa cada tanto la carpeta donde el cliente de correo deja los archivos de rendición del banco.
// Si no hay carpeta configurada (RendicionBanco:CarpetaEntrada) no hace nada y lo avisa una vez. Y por
// defecto procesa en SIMULACRO (RendicionBanco:ModoSimulacro: false para que escriba de verdad).
builder.Services.AddHostedService<SIRGA.WebUI.Services.BankRenditionMonitor>();

// Registro de UnitOfWork y Servicios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<DeclarationService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<UnionService>();
builder.Services.AddScoped<SalaryScaleService>();
builder.Services.AddScoped<InterestRateService>();
builder.Services.AddScoped<AuditService>();
// Bancos y configuración bancaria por sindicato: PENDIENTE de registrar.
//
// BankService todavía NO se registra a propósito. El proyecto no registra los repositorios en el
// contenedor: los construye la UnitOfWork por dentro, con su propio contexto. Al inyectar
// IRepository<Bank> directo se creaba una SEGUNDA instancia con OTRO contexto, y el CompleteAsync()
// de la UnitOfWork guardaba en el suyo: las escrituras se perdían en silencio.
//
// Para terminarlo hay que agregar, como en el resto del proyecto:
//   1) IUnitOfWork: IRepository<Bank> Banks { get; } y IRepository<UnionBankConfig> UnionBankConfigs { get; }
//   2) UnitOfWork: las dos propiedades, construidas con el mismo contexto que las demás
//   3) BankService: recibir IUnitOfWork y usarlo en lugar de los repositorios sueltos
//   4) Recién entonces: builder.Services.AddScoped<BankService>();
builder.Services.AddScoped<BankService>();
builder.Services.AddScoped<InspectionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<AccountingStudioService>();
builder.Services.AddScoped<IPaymentService, MercadoPagoService>();
builder.Services.AddScoped<PdfReportService>();
// Arma los datos de la boleta bancaria (vencimientos, recargo y código de barras) a partir de la DDJJ.
builder.Services.AddScoped<BoletaService>();
// Boleta de pago del banco, en PDF y por triplicado (talón cliente, banco y sindicato).
builder.Services.AddScoped<SIRGA.WebUI.Services.BoletaPdfService>();
builder.Services.AddScoped<IReportService, ExcelReportService>();
builder.Services.AddScoped<BankReconciliationService>();
builder.Services.AddScoped<DebtNotificationService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<BalanceService>();
builder.Services.AddScoped<AssociationService>();

// Alcance del usuario en el área sindicato: resuelve qué sindicato puede ver un auditor (y "ninguno"
// si todavía no se lo asignaron). Lo usan las pantallas para filtrar.
builder.Services.AddScoped<SIRGA.WebUI.Services.AlcanceAuditor>();
builder.Services.AddScoped<CompanyValidationService>();
builder.Services.AddScoped<ILegacyCompanyLookup, LegacyCompanyLookup>();
builder.Services.AddScoped<EmployeeCategoryService>();
builder.Services.AddScoped<UnionSubscriptionService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// QuestPDF (boletas y reportes) escribe archivos temporales mientras arma el PDF. Si %TEMP% no es
// escribible, la generación falla con "Unable to find a suitable temporary storage location" y la
// boleta NO sale. Pasa, por ejemplo, cuando el proceso corre con un token restringido. Se prueban
// carpetas escribiendo de verdad y se usa la primera que sirva; si ninguna sirve se deja el
// comportamiento por defecto de QuestPDF (no se aborta el arranque por esto).
foreach (var carpetaTemporal in new[]
         {
             Path.Combine(Path.GetTempPath(), "sirga-questpdf"),
             Path.Combine(AppContext.BaseDirectory, "questpdf-tmp")
         })
{
    try
    {
        Directory.CreateDirectory(carpetaTemporal);
        var prueba = Path.Combine(carpetaTemporal, $"prueba-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(prueba, "x");
        File.Delete(prueba);

        QuestPDF.Settings.TemporaryStoragePath = carpetaTemporal;
        break;
    }
    catch
    {
        // No era escribible: se prueba la siguiente.
    }
}

// Seed de Datos Iniciales
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SIRG_DbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await SIRGA.Infrastructure.DbInitializer.SeedIdentityAsync(userManager, roleManager);
    await SIRGA.Infrastructure.DbInitializer.SeedLocationsAsync(context);
}

// Configure the HTTP request pipeline.
app.UseResponseCompression();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Middleware de Suscripción SaaS
app.UseMiddleware<SubscriptionStatusMiddleware>();

app.UseAntiforgery();

app.UseMiddleware<ErrorHandlerMiddleware>(); // Middleware Global de Errores

// Sirve los assets estáticos con fingerprint en la URL, compresión previa y caché
// inmutable (reemplaza a UseStaticFiles, que los entregaba sin ninguna de las tres).
app.MapStaticAssets();

app.MapControllers(); // Mapear rutas de controladores
app.MapRazorComponents<SIRGA.WebUI.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
