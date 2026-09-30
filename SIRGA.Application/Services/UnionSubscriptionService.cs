using SIRGA.Application.DTOs;
using SIRGA.Domain.Entities;
using SIRGA.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

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

      // 1. Crear el usuario de Identity si no existe
      var existingUser = await _userManager.FindByEmailAsync(request.ContactEmail);
      if (existingUser == null)
      {
        var user = new IdentityUser { UserName = request.ContactEmail, Email = request.ContactEmail, EmailConfirmed = true };
        var createResult = await _userManager.CreateAsync(user, "12345");
        if (createResult.Succeeded)
        {
          // 2. Asignar el rol de Sindicato
          if (!await _roleManager.RoleExistsAsync("Sindicato"))
          {
            await _roleManager.CreateAsync(new IdentityRole("Sindicato"));
          }
          await _userManager.AddToRoleAsync(user, "Sindicato");
        }
      }

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
    var subscriptions = await _unitOfWork.UnionSubscriptions.GetAllAsync();
    var requests = await _unitOfWork.UnionSubscriptionRequests.GetAllAsync();
    var studios = await _unitOfWork.AccountingStudios.GetAllAsync();
    var employees = await _unitOfWork.Employees.GetAllAsync();
    var payments = await _unitOfWork.UnionSubscriptionPayments.GetAllAsync();

    var now = DateTime.Now;
    var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

    return new AdminDashboardDto
    {
      ActiveUnionsCount = subscriptions.Count(s => s.IsServiceActive && s.NextBillingDate >= now),
      DelinquentUnionsCount = subscriptions.Count(s => !s.IsServiceActive || s.NextBillingDate < now),
      TotalUnionsCount = subscriptions.Count(),
      PendingRequestsCount = requests.Count(r => r.Status == SubscriptionRequestStatus.Pending),
      TotalAccountingStudiosCount = studios.Count(),
      TotalEmployeesCount = employees.Count(),

      RequestsCurrentMonth = requests.Count(r => r.RequestDate >= firstDayOfMonth),
      ApprovedCurrentMonth = requests.Count(r => r.Status == SubscriptionRequestStatus.Approved && r.RequestDate >= firstDayOfMonth),
      RejectedCurrentMonth = requests.Count(r => r.Status == SubscriptionRequestStatus.Rejected && r.RequestDate >= firstDayOfMonth),

      MonthlyRevenue = payments.Where(p => p.PaymentDate >= firstDayOfMonth).Sum(p => p.Amount)
    };
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
