using SIRGA.Domain.Entities;

namespace SIRGA.Application.DTOs;

/// <summary>
/// Resultado de un alta o edición de empresa. Existe para poder mover la lógica que estaba en
/// CompaniesController a CompanyService conservando exactamente el mismo resultado HTTP:
/// <see cref="BadRequest"/> y <see cref="NotFound"/> sin mensaje se responden con ProblemDetails
/// (como hacía el controlador con BadRequest() / NotFound() sin argumentos) y con mensaje se
/// responden como texto plano (BadRequest("...") / NotFound("...")).
/// </summary>
public enum CompanySaveStatus
{
  /// <summary>Se guardó; la respuesta es 204.</summary>
  Ok,

  /// <summary>Se creó; la respuesta es 201 con la empresa.</summary>
  Created,

  /// <summary>Validación; 400. Sin mensaje se responde ProblemDetails.</summary>
  BadRequest,

  /// <summary>Duplicado o problema de pertenencia; 409.</summary>
  Conflict,

  /// <summary>No existe; 404. Sin mensaje se responde ProblemDetails.</summary>
  NotFound,

  /// <summary>El estudio no es dueño de la empresa; 403.</summary>
  Forbidden,

  /// <summary>Error inesperado; 500.</summary>
  InternalError
}

public class CompanySaveResult
{
  public CompanySaveStatus Status { get; init; }
  public string? Message { get; init; }
  public Company? Company { get; init; }

  public static CompanySaveResult Created(Company company) => new() { Status = CompanySaveStatus.Created, Company = company };
  public static CompanySaveResult Ok() => new() { Status = CompanySaveStatus.Ok };
  public static CompanySaveResult BadRequest(string? message = null) => new() { Status = CompanySaveStatus.BadRequest, Message = message };
  public static CompanySaveResult Conflict(string? message) => new() { Status = CompanySaveStatus.Conflict, Message = message };
  public static CompanySaveResult NotFound(string? message = null) => new() { Status = CompanySaveStatus.NotFound, Message = message };
  public static CompanySaveResult Forbidden() => new() { Status = CompanySaveStatus.Forbidden };
  public static CompanySaveResult InternalError(string message) => new() { Status = CompanySaveStatus.InternalError, Message = message };
}
