using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SIRGA.Domain.Enums;

namespace SIRGA.Domain.Entities;

public class SindicataryAssignment
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public virtual Employee Employee { get; set; } = null!;

    public int CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    public int UnionId { get; set; }
    public virtual Union Union { get; set; } = null!;

    public bool IsVoluntaryMember { get; set; }

    [MaxLength(100)]
    public string? CategoryCode { get; set; }

    /// <summary>
    /// Actividad en esta empresa (código del catálogo <see cref="Activity"/>, origen <c>MAESOC_CODACT</c>).
    /// Es POR EMPRESA, igual que la categoría, y no reemplaza a la categoría: la categoría fija el mínimo.
    /// </summary>
    [MaxLength(20)]
    public string? ActivityCode { get; set; }

    /// <summary>
    /// Número de legajo del empleado EN ESTA EMPRESA (origen <c>socemp.SOCEMP_NROLEG</c>: 35.959 de 36.019
    /// lo tienen). Va en la asignación y no en el empleado porque el mismo socio tiene otro legajo en cada
    /// empresa, que es como lo guarda el sistema anterior.
    /// </summary>
    [MaxLength(30)]
    public string? FileNumber { get; set; }

    /// <summary>
    /// Jornada del empleado en esta empresa. Prorratea el mínimo de escala (Parcial = 50%).
    /// El valor por defecto es <see cref="JornadaType.Completa"/> porque el sistema anterior no
    /// tenía el dato: darlo por parcial sería inventar una quita de haberes.
    /// </summary>
    public JornadaType Jornada { get; set; } = JornadaType.Completa;

    /// <summary>
    /// Fecha de ingreso del empleado A ESTA EMPRESA. Viene de la migración de
    /// <c>fotos_.socemp.SOCEMP_FECHAING</c> (ver SIRGA.Migration/Program.cs), así que a pesar del
    /// nombre NO es un timestamp técnico de alta del registro: es el dato de negocio.
    ///
    /// De acá se cuenta la ANTIGÜEDAD para el mínimo de escala. No usar Employees.HireDate: es la
    /// fecha de alta al sindicato y da una antigüedad distinta de la del ingreso a la empresa.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public DateTime? TerminationDate { get; set; }

    public bool? MembershipResignationRequested { get; set; }

    [MaxLength(500)]
    public string? MembershipResignationReason { get; set; }

    public DateTime? MembershipResignationRequestedAt { get; set; }

    public DateTime? MembershipEndDate { get; set; }
}