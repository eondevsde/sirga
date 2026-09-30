using Moq;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

public class MembershipResignationTests
{
  private readonly Mock<IUnitOfWork> _uow = new();
  private readonly Mock<IRepository<SindicataryAssignment>> _assignments = new();
  private readonly Mock<IRepository<Employee>> _employees = new();
  private readonly Mock<IRepository<Company>> _companies = new();
  private readonly EmployeeService _service;

  public MembershipResignationTests()
  {
    _uow.Setup(u => u.Assignments).Returns(_assignments.Object);
    _uow.Setup(u => u.Employees).Returns(_employees.Object);
    _uow.Setup(u => u.Companies).Returns(_companies.Object);
    _uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
    _service = new EmployeeService(_uow.Object);
  }

  private static SindicataryAssignment Assignment(Action<SindicataryAssignment> configure)
  {
    var a = new SindicataryAssignment { Id = 1, EmployeeId = 10, CompanyId = 20, UnionId = 30, IsVoluntaryMember = true, IsActive = true };
    configure(a);
    return a;
  }

  [Fact]
  public async Task Request_WhenNotVoluntaryMember_ReturnsError()
  {
    var a = Assignment(x => x.IsVoluntaryMember = false);
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, error) = await _service.RequestMembershipResignationAsync(1, "motivo");

    Assert.False(ok);
    Assert.Contains("no es socio voluntario", error!, StringComparison.OrdinalIgnoreCase);
    _assignments.Verify(r => r.UpdateAsync(It.IsAny<SindicataryAssignment>()), Times.Never);
  }

  [Fact]
  public async Task Request_WhenAlreadyRequested_ReturnsError()
  {
    var a = Assignment(x => x.MembershipResignationRequested = true);
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, error) = await _service.RequestMembershipResignationAsync(1, "motivo");

    Assert.False(ok);
    Assert.Contains("pendiente", error!, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task Request_WhenInactive_ReturnsError()
  {
    var a = Assignment(x => x.IsActive = false);
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, error) = await _service.RequestMembershipResignationAsync(1, "motivo");

    Assert.False(ok);
    Assert.Contains("vinculación activa", error!, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task Request_WhenValid_SetsFlagsAndReturnsSuccess()
  {
    var a = Assignment(_ => { });
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, error) = await _service.RequestMembershipResignationAsync(1, "Se muda de provincia");

    Assert.True(ok);
    Assert.Null(error);
    Assert.True(a.MembershipResignationRequested);
    Assert.Equal("Se muda de provincia", a.MembershipResignationReason);
    Assert.NotNull(a.MembershipResignationRequestedAt);
    _assignments.Verify(r => r.UpdateAsync(a), Times.Once);
  }

  [Fact]
  public async Task Request_WithoutReason_UsesDefault()
  {
    var a = Assignment(_ => { });
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, _) = await _service.RequestMembershipResignationAsync(1, "   ");

    Assert.True(ok);
    Assert.Equal("Sin motivo informado", a.MembershipResignationReason);
  }

  [Fact]
  public async Task Resolve_Approve_SetsNonMemberAndEndDate()
  {
    var a = Assignment(x => x.MembershipResignationRequested = true);
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, _) = await _service.ResolveMembershipResignationAsync(1, true, "ok");

    Assert.True(ok);
    Assert.False(a.IsVoluntaryMember);
    Assert.False(a.MembershipResignationRequested);
    Assert.NotNull(a.MembershipEndDate);
  }

  [Fact]
  public async Task Resolve_Reject_KeepsMember()
  {
    var a = Assignment(x => x.MembershipResignationRequested = true);
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, _) = await _service.ResolveMembershipResignationAsync(1, false, "no");

    Assert.True(ok);
    Assert.True(a.IsVoluntaryMember);
    Assert.False(a.MembershipResignationRequested);
    Assert.Null(a.MembershipEndDate);
  }

  [Fact]
  public async Task Resolve_WithoutPendingRequest_ReturnsError()
  {
    var a = Assignment(_ => { });
    _assignments.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(a);

    var (ok, error) = await _service.ResolveMembershipResignationAsync(1, true, "ok");

    Assert.False(ok);
    Assert.Contains("no hay una solicitud", error!, StringComparison.OrdinalIgnoreCase);
  }
}