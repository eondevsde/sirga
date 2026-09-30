using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public DateTime? TerminationDate { get; set; }

    public bool? MembershipResignationRequested { get; set; }

    [MaxLength(500)]
    public string? MembershipResignationReason { get; set; }

    public DateTime? MembershipResignationRequestedAt { get; set; }

    public DateTime? MembershipEndDate { get; set; }
}