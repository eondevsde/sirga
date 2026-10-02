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

  /// <summary>
  /// Traspasa una empresa de un estudio a otro (A -> B). Lo decide y ejecuta el admin del sindicato,
  /// no el estudio: por eso no nace de una solicitud, sino de esta acción directa.
  /// Cierra la fila vigente del historial del estudio anterior, abre la del nuevo y cambia el dueño.
  /// Todo va en el mismo SaveChanges, así que no puede quedar a medias: o se traspasa, o no pasa nada.
  /// </summary>
  public async Task<(bool Success, string? Error)> TransferCompanyAsync(
      int companyId, int toStudioId, int unionId, string? authorizedBy, string? reason)
  {
    var company = await unitOfWork.Companies.GetByIdAsync(companyId);
    if (company == null) return (false, "No se encontró la empresa.");

    if (company.AccountingStudioId == toStudioId)
      return (false, "La empresa ya pertenece a ese estudio.");

    var destino = await unitOfWork.AccountingStudios.GetByIdAsync(toStudioId);
    if (destino == null) return (false, "No se encontró el estudio de destino.");

    var ahora = DateTime.UtcNow;

    // Cerrar la fila vigente (la que tiene To vacío) del estudio que la tenía hasta ahora.
    var vigentes = await unitOfWork.CompanyStudioHistories.FindAsync(
        h => h.CompanyId == companyId && h.To == null);
    foreach (var fila in vigentes)
    {
      fila.To = ahora;
      await unitOfWork.CompanyStudioHistories.UpdateAsync(fila);
    }

    await unitOfWork.CompanyStudioHistories.AddAsync(new CompanyStudioHistory
    {
      CompanyId = companyId,
      AccountingStudioId = toStudioId,
      UnionId = unionId,
      From = ahora,
      AuthorizedByUsername = authorizedBy,
      Reason = reason
    });

    company.AccountingStudioId = toStudioId;
    await unitOfWork.Companies.UpdateAsync(company);

    var ok = await unitOfWork.CompleteAsync() > 0;
    return (ok, ok ? null : "No se pudo registrar el traspaso.");
  }

  /// <summary>
  /// Empresas que salieron de un estudio por un traspaso, para poder avisarle. Se lee del historial:
  /// cada fila cerrada (con To) es una empresa que ese estudio tuvo a cargo y ya no tiene.
  /// El volumen es chico (un traspaso es un hecho excepcional), así que se resuelven los nombres acá.
  /// </summary>
  public async Task<IEnumerable<CompanyLeftDto>> GetCompaniesLeftAsync(int studioId)
  {
    var filas = await unitOfWork.CompanyStudioHistories.FindAsync(
        h => h.AccountingStudioId == studioId && h.To != null);

    if (!filas.Any()) return Enumerable.Empty<CompanyLeftDto>();

    var resultado = new List<CompanyLeftDto>();
    foreach (var fila in filas.OrderByDescending(h => h.To))
    {
      var empresa = await unitOfWork.Companies.GetByIdAsync(fila.CompanyId);

      AccountingStudio? actual = null;
      if (empresa?.AccountingStudioId is { } actualId)
        actual = await unitOfWork.AccountingStudios.GetByIdAsync(actualId);

      resultado.Add(new CompanyLeftDto(
          fila.CompanyId,
          empresa?.BusinessName ?? "(empresa eliminada)",
          empresa?.Cuit,
          fila.From,
          fila.To,
          actual?.Name,
          fila.AuthorizedByUsername,
          fila.Reason));
    }

    return resultado;
  }
}

/// <summary>Una empresa que salió de un estudio por un traspaso: es el aviso al estudio que la perdió.</summary>
public record CompanyLeftDto(
    int CompanyId,
    string BusinessName,
    string? Cuit,
    DateTime From,
    DateTime? To,
    string? CurrentStudioName,
    string? AuthorizedByUsername,
    string? Reason);
