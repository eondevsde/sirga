using SIRGA.Application.DTOs;
using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.Application.Services;

public class AssociationService(IUnitOfWork unitOfWork)
{
  public async Task<IEnumerable<CompanyAssociationRequestDto>> GetRequestsForUnionAsync(int unionId)
  {
    var requests = await unitOfWork.AssociationRequests.FindAsync(r => r.TargetUnionId == unionId);
    var result = new List<CompanyAssociationRequestDto>();

    // Procesamos de forma secuencial para evitar conflictos de DbContext en Blazor Server
    foreach (var req in requests)
    {
      var studio = await unitOfWork.AccountingStudios.GetByIdAsync(req.AccountingStudioId);
      var company = await unitOfWork.Companies.GetByIdAsync(req.CompanyId);
      var union = await unitOfWork.Unions.GetByIdAsync(req.TargetUnionId);

      result.Add(new CompanyAssociationRequestDto
      {
        Id = req.Id,
        AccountingStudioId = req.AccountingStudioId,
        AccountingStudioName = studio?.Name ?? "N/A",
        CompanyId = req.CompanyId,
        CompanyBusinessName = company?.BusinessName ?? "N/A",
        CompanyCuit = company?.Cuit ?? "N/A",
        TargetUnionId = req.TargetUnionId,
        TargetUnionName = union?.Name ?? "N/A",
        CreatedAt = req.CreatedAt,
        Status = req.Status,
        AdminComment = req.AdminComment,
        ProcessedAt = req.ProcessedAt
      });
    }

    return result.OrderByDescending(x => x.CreatedAt);
  }

  public async Task<(bool Success, string? Message)> ApproveRequestAsync(int requestId, string comment)
  {
    var request = await unitOfWork.AssociationRequests.GetByIdAsync(requestId);
    if (request == null) return (false, "La solicitud no existe.");
    if (request.Status != RequestStatus.Pending) return (false, "La solicitud ya fue procesada.");

    var studio = await unitOfWork.AccountingStudios.GetByIdAsync(request.AccountingStudioId);
    if (studio == null || !studio.IsActive || !studio.IsApproved)
    {
      request.Status = RequestStatus.Rejected;
      request.AdminComment = "Rechazada: el estudio no está aprobado o fue desactivado.";
      request.ProcessedAt = DateTime.UtcNow;
      await unitOfWork.AssociationRequests.UpdateAsync(request);
      await unitOfWork.CompleteAsync();
      return (false, "El estudio no está aprobado o fue desactivado. La solicitud fue rechazada.");
    }

    var company = await unitOfWork.Companies.GetByIdAsync(request.CompanyId);
    if (company == null) return (false, "La empresa de la solicitud ya no existe.");

    // Re-chequeo: la empresa no debe pertenecer a otro estudio activo
    if (company.AccountingStudioId is { } ownerId && ownerId != request.AccountingStudioId)
    {
      var currentOwner = await unitOfWork.AccountingStudios.GetByIdAsync(ownerId);
      if (currentOwner is { IsActive: true, IsApproved: true })
      {
        request.Status = RequestStatus.Rejected;
        request.AdminComment = comment + " | Rechazada: la empresa ya pertenece a otro estudio activo.";
        request.ProcessedAt = DateTime.UtcNow;
        await unitOfWork.AssociationRequests.UpdateAsync(request);
        await unitOfWork.CompleteAsync();
        return (false, "La empresa ya pertenece a otro estudio activo. La solicitud fue rechazada.");
      }
    }

    request.Status = RequestStatus.Approved;
    request.AdminComment = comment;
    request.ProcessedAt = DateTime.UtcNow;

    company.AccountingStudioId = request.AccountingStudioId;
    company.Status = CompanyStatus.Active;
    await unitOfWork.Companies.UpdateAsync(company);

    await unitOfWork.AssociationRequests.UpdateAsync(request);
    await unitOfWork.CompleteAsync();
    return (true, "Solicitud aprobada.");
  }

  public async Task RejectRequestAsync(int requestId, string comment)
  {
    var request = await unitOfWork.AssociationRequests.GetByIdAsync(requestId);
    if (request == null) return;

    request.Status = RequestStatus.Rejected;
    request.AdminComment = comment;
    request.ProcessedAt = DateTime.UtcNow;

    var company = await unitOfWork.Companies.GetByIdAsync(request.CompanyId);
    if (company != null && company.Status == CompanyStatus.PendingAuthorization)
    {
      company.Status = CompanyStatus.Rejected;
      await unitOfWork.Companies.UpdateAsync(company);
    }

    await unitOfWork.AssociationRequests.UpdateAsync(request);
    await unitOfWork.CompleteAsync();
  }

  public async Task<(bool Success, string? Error)> CreateRequestAsync(int studioId, int companyId, int unionId)
  {
    var pending = await unitOfWork.AssociationRequests.FindAsync(
        r => r.CompanyId == companyId && r.Status == RequestStatus.Pending);
    if (pending.Any())
      return (false, "Ya existe una solicitud pendiente para esta empresa.");

    var request = new CompanyAssociationRequest
    {
      AccountingStudioId = studioId,
      CompanyId = companyId,
      TargetUnionId = unionId,
      CreatedAt = DateTime.UtcNow,
      Status = RequestStatus.Pending
    };

    await unitOfWork.AssociationRequests.AddAsync(request);
    var ok = await unitOfWork.CompleteAsync() > 0;
    return (ok, ok ? null : "No se pudo registrar la solicitud.");
  }
}
