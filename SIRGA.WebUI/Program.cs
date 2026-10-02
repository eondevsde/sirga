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

// Registro de UnitOfWork y Servicios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<DeclarationService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<UnionService>();
builder.Services.AddScoped<SalaryScaleService>();
builder.Services.AddScoped<InterestRateService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<InspectionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<AccountingStudioService>();
builder.Services.AddScoped<IPaymentService, MercadoPagoService>();
builder.Services.AddScoped<PdfReportService>();
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
