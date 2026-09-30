using SIRGA.Application.DTOs;
using SIRGA.Domain.Entities;
using SIRGA.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SIRGA.Application.Services;

public class UnionSubscriptionService
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly UserManager<IdentityUser> _userManager;
  private readonly RoleManager<IdentityRole> _roleManager;

  public UnionSubscriptionService(
    IUnitOfWork unitOfWork,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager)
  {
    _unitOfWork = unitOfWork;
    _userManager = userManager;
    _roleManager = roleManager;
  }

  // Contraseña inicial de los usuarios de sindicato. Es una deuda conocida (ver la sección de
  // deuda técnica de DOCUMENTACION_SIRGA.md): cambiarla requiere un flujo de primer acceso.
  private const string ClaveInicialSindicato = "12345";
  private const string RolSindicato = "Sindicato";

  /// <summary>
  /// Garantiza el usuario de Identity del sindicato con rol Sindicato. Es idempotente: si el
  /// email ya está registrado lo informa en lugar de fallar. La lógica estaba duplicada entre el
  /// alta desde el panel de sindicatos y la aprobación de una solicitud de suscripción.
  /// </summary>
  public async Task<(bool Creado, bool YaExistia, string? Error)> EnsureUnionUserAsync(string? email)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      return (false, false, "El email del administrador es requerido.");
    }

    if (await _userManager.FindByEmailAsync(email) != null)
    {
      return (false, true, null);
    }

    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
    var resultado = await _userManager.CreateAsync(user, ClaveInicialSindicato);
    if (!resultado.Succeeded)
    {
      return (false, false, string.Join("; ", resultado.Errors.Select(e => e.Description)));
    }

    if (!await _roleManager.RoleExistsAsync(RolSindicato))
    {
      await _roleManager.CreateAsync(new IdentityRole(RolSindicato));
    }
    await _userManager.AddToRoleAsync(user, RolSindicato);

    return (true, false, null);
  }

  // --- Solicitudes ---
  public async Task CreateRequestAsync(SubscriptionRequestDto dto)
  {
    var request = new UnionSubscriptionRequest
    {
      UnionName = dto.UnionName,
      Cuit = dto.Cuit,
      ContactEmail = dto.ContactEmail,
      ContactPhone = dto.ContactPhone,
      WhatsApp = dto.WhatsApp,
      Street = dto.Street,
      StreetNumber = dto.StreetNumber,
      Floor = dto.Floor,
      Locality = dto.Locality,
      Province = dto.Province,
      CoverageArea = dto.CoverageArea,
      ReferencePersonFullName = dto.ReferencePersonFullName,
      RequestDate = DateTime.Now,
      Status = SubscriptionRequestStatus.Pending
    };

    await _unitOfWork.UnionSubscriptionRequests.AddAsync(request);
    await _unitOfWork.CompleteAsync();
  }

  public async Task<IEnumerable<SubscriptionRequestDto>> GetPendingRequestsAsync()
  {
    var requests = await _unitOfWork.UnionSubscriptionRequests.FindAsync(r => r.Status == SubscriptionRequestStatus.Pending);
    return requests.Select(r => new SubscriptionRequestDto
    {
      Id = r.Id,
      UnionName = r.UnionName,
      Cuit = r.Cuit,
      ContactEmail = r.ContactEmail,
      ContactPhone = r.ContactPhone,
      RequestDate = r.RequestDate,
      Status = r.Status
    });
  }

  public async Task UpdateRequestStatusAsync(int requestId, SubscriptionRequestStatus status, string? notes)
  {
    var request = await _unitOfWork.UnionSubscriptionRequests.GetByIdAsync(requestId);
    if (request == null) return;

    request.Status = status;
    request.AdminNotes = notes;

    if (status == SubscriptionRequestStatus.Approved)
    {
      // AL APROBAR: Creamos el Sindicato y su Suscripción automáticamente
      var union = new Union
      {
        Name = request.UnionName,
        CctCode = "PENDIENTE", // El Admin lo configurará después
        JurisdictionProvince = request.Province ?? "Consultar",
        JurisdictionCity = request.Locality,
        AdminEmail = request.ContactEmail, // Guardamos el email de contacto como admin
        IsActive = true
      };
      await _unitOfWork.Unions.AddAsync(union);
      await _unitOfWork.CompleteAsync(); // Para obtener el Id de la unión

      // 1 y 2. Crear el usuario de Identity con rol Sindicato si no existe. Es la misma lógica
      // que usa el alta desde el panel de sindicatos (antes estaba duplicada acá).
      await EnsureUnionUserAsync(request.ContactEmail);

      var subscription = new UnionSubscription
      {
        UnionId = union.Id,
        MonthlyFee = 50000, // Monto base ejemplo
        NextBillingDate = DateTime.Now.AddMonths(1),
        IsServiceActive = true
      };
      await _unitOfWork.UnionSubscriptions.AddAsync(subscription);
    }

    await _unitOfWork.CompleteAsync();
  }

  public async Task<AdminDashboardDto> GetAdminDashboardStatsAsync()
  {
    // Los 11 contadores se agregan en SQL. Antes se traían cinco tablas completas —incluidos
    // los 27.934 empleados con su columna varbinary(max) de biometría— solo para contarlas.
    var now = DateTime.Now;
    var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

    return new AdminDashboardDto
    {
      ActiveUnionsCount = await _unitOfWork.UnionSubscriptions.RunQueryAsync(q =>
        q.CountAsync(s => s.IsServiceActive && s.NextBillingDate >= now)),

      DelinquentUnionsCount = await _unitOfWork.UnionSubscriptions.RunQueryAsync(q =>
        q.CountAsync(s => !s.IsServiceActive || s.NextBillingDate < now)),

      TotalUnionsCount = await _unitOfWork.UnionSubscriptions.RunQueryAsync(q => q.CountAsync()),

      PendingRequestsCount = await _unitOfWork.UnionSubscriptionRequests.RunQueryAsync(q =>
        q.CountAsync(r => r.Status == SubscriptionRequestStatus.Pending)),

      TotalAccountingStudiosCount = await _unitOfWork.AccountingStudios.RunQueryAsync(q => q.CountAsync()),

      TotalEmployeesCount = await _unitOfWork.Employees.RunQueryAsync(q => q.CountAsync()),

      RequestsCurrentMonth = await _unitOfWork.UnionSubscriptionRequests.RunQueryAsync(q =>
        q.CountAsync(r => r.RequestDate >= firstDayOfMonth)),

      ApprovedCurrentMonth = await _unitOfWork.UnionSubscriptionRequests.RunQueryAsync(q =>
        q.CountAsync(r => r.Status == SubscriptionRequestStatus.Approved && r.RequestDate >= firstDayOfMonth)),

      RejectedCurrentMonth = await _unitOfWork.UnionSubscriptionRequests.RunQueryAsync(q =>
        q.CountAsync(r => r.Status == SubscriptionRequestStatus.Rejected && r.RequestDate >= firstDayOfMonth)),

      MonthlyRevenue = await _unitOfWork.UnionSubscriptionPayments.RunQueryAsync(q =>
        q.Where(p => p.PaymentDate >= firstDayOfMonth).SumAsync(p => (decimal?)p.Amount)) ?? 0m
    };
  }

  /// <summary>
  /// Indica si corresponde bloquear el acceso del sindicato por suscripción inactiva o vencida.
  /// Es una consulta puntual: el SubscriptionStatusMiddleware la ejecuta en cada request
  /// autenticado y antes traía las suscripciones y los sindicatos completos para mirar una fila.
  /// </summary>
  public async Task<bool> IsServiceBlockedAsync(int unionId)
  {
    var now = DateTime.Now;
    return await _unitOfWork.UnionSubscriptions.RunQueryAsync(q =>
        q.AnyAsync(s => s.UnionId == unionId && (!s.IsServiceActive || s.NextBillingDate < now)));
  }

  // --- Suscripciones ---
  public async Task<IEnumerable<UnionSubscriptionDto>> GetAllSubscriptionsAsync()
  {
    var subscriptions = await _unitOfWork.UnionSubscriptions.GetAllAsync();
    var unions = await _unitOfWork.Unions.GetAllAsync();

    return subscriptions.Select(s => new UnionSubscriptionDto
    {
      Id = s.Id,
      UnionId = s.UnionId,
      UnionName = unions.FirstOrDefault(u => u.Id == s.UnionId)?.Name ?? "Desconocido",
      MonthlyFee = s.MonthlyFee,
      NextBillingDate = s.NextBillingDate,
      IsServiceActive = s.IsServiceActive
    });
  }

  public async Task CreateSubscriptionAsync(UnionSubscriptionDto dto)
  {
    var subscription = new UnionSubscription
    {
      UnionId = dto.UnionId,
      MonthlyFee = dto.MonthlyFee,
      NextBillingDate = dto.NextBillingDate,
      IsServiceActive = true
    };

    await _unitOfWork.UnionSubscriptions.AddAsync(subscription);
    await _unitOfWork.CompleteAsync();
  }

  public async Task RecordPaymentAsync(UnionSubscriptionPaymentDto dto)
  {
    var payment = new UnionSubscriptionPayment
    {
      UnionSubscriptionId = dto.UnionSubscriptionId,
      PaymentDate = dto.PaymentDate,
      Amount = dto.Amount,
      PaymentMethod = dto.PaymentMethod,
      ReceiptNumber = dto.ReceiptNumber,
      Notes = dto.Notes
    };

    await _unitOfWork.UnionSubscriptionPayments.AddAsync(payment);

    // Actualizar la fecha de próximo cobro (sumar 1 mes)
    var sub = await _unitOfWork.UnionSubscriptions.GetByIdAsync(dto.UnionSubscriptionId);
    if (sub != null)
    {
      sub.NextBillingDate = sub.NextBillingDate.AddMonths(1);
    }

    await _unitOfWork.CompleteAsync();
  }
}
