using System.ComponentModel.DataAnnotations;

namespace SIRGA.Domain.Entities;

/// <summary>
/// Historial de qué estudio contable tuvo a cargo una empresa.
/// Cuando el admin del sindicato aprueba un traspaso A -> B se cierra la fila del estudio anterior
/// (se le pone To) y se abre la del nuevo. Una fila con To vacío es la que la tiene hoy, así que el
/// dueño actual se puede leer del historial y no hace falta duplicar el dato.
/// </summary>
public class CompanyStudioHistory
{
  public int Id { get; set; }

  [Required]
  public int CompanyId { get; set; }
  public virtual Company Company { get; set; } = null!;

  [Required]
  public int AccountingStudioId { get; set; }
  public virtual AccountingStudio AccountingStudio { get; set; } = null!;

  /// <summary>Sindicato por el que se autorizó el traspaso.</summary>
  public int? UnionId { get; set; }
  public virtual Union? Union { get; set; }

  public DateTime From { get; set; } = DateTime.UtcNow;

  /// <summary>Vacío mientras la empresa siga a cargo de ese estudio.</summary>
  public DateTime? To { get; set; }

  /// <summary>Usuario que autorizó el cambio: el admin del sindicato.</summary>
  public string? AuthorizedByUsername { get; set; }

  public string? Reason { get; set; }
}
